using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스킬 효과 적용 정적 유틸리티.
/// PlayerSkills가 CombatCharacter.CastSkill()로 발동 판정 후 이 클래스로 실제 효과를 적용한다.
/// CombatCharacter(순수 클래스)와 Enemy(MonoBehaviour) 사이의 의존성을 여기서만 처리한다.
/// </summary>
public static class SkillExecutor
{
    /// <summary>스킬 효과를 effectKind에 따라 적용한다.</summary>
    /// <param name="skill">발동할 스킬 정의</param>
    /// <param name="skillIndex">발동자가 보유한 스킬 슬롯 인덱스 (레벨 조회용)</param>
    /// <param name="caster">발동자 CombatCharacter (버프 후 Atk는 이미 반영됨)</param>
    /// <param name="origin">발동자 Transform (위치·전방 방향 기준)</param>
    public static void Execute(SkillDef skill, int skillIndex, CombatCharacter caster, Transform origin)
    {
        // 효과 목록이 있으면 새 경로, 비어 있으면 기존 effectKind 경로(호환)
        if (skill.effects != null && skill.effects.Length > 0)
        {
            ExecuteEffects(skill, skillIndex, caster, origin);
            return;
        }

        var eff = SkillScaling.Compute(skill, caster.SkillLevel(skillIndex));
        switch (skill.effectKind)
        {
            case SkillEffectKind.Strike:   ExecStrike(eff, caster, origin);   break;
            case SkillEffectKind.Aoe:      ExecAoe(eff, caster, origin);      break;
            case SkillEffectKind.HealBuff: ExecHealBuff(eff, caster, origin); break;
            case SkillEffectKind.Mark:     ExecMark(eff, caster, origin);     break;
        }
    }

    // ── 효과 목록 경로 (설계도 22 §4-3) ───────────────────────────────────

    static void ExecuteEffects(SkillDef skill, int skillIndex, CombatCharacter caster, Transform origin)
    {
        int level = caster.SkillLevel(skillIndex);
        var eff   = SkillScaling.Compute(skill, level);
        var party = PlayerRuntime.Party;

        // 적 대상은 형태 판정으로 한 번만 수집(거리순). 마일스톤 범위 보너스 반영.
        List<(ISkillTarget t, float d)> area = null;

        foreach (var entry in skill.effects)
        {
            float v = SkillScaling.ScaleEffectValue(entry, level);

            if (entry.target == TargetScope.EnemyArea)
            {
                area ??= CollectInShape(skill, eff, origin);
                ApplyToEnemies(entry, v, skill, caster, origin, area);
            }
            else
            {
                foreach (var ally in TargetResolver.Resolve(party, caster, entry.target, entry.targetCap))
                {
                    int healed = AllyEffectApplier.Apply(entry, v, skill.id, caster, ally);
                    if (healed > 0 && ally == caster)
                        DamageNumber.SpawnHeal(origin.position + Vector3.up * 2f, healed);
                }
            }
        }
    }

    static List<(ISkillTarget t, float d)> CollectInShape(SkillDef skill, EffectiveSkill eff, Transform origin)
    {
        var list   = new List<(ISkillTarget, float)>();
        float radius = skill.radius + (eff.range - skill.range);   // 마일스톤 범위 보너스를 원형에도 반영

        void Consider(ISkillTarget t)
        {
            if (!IsAlive(t)) return;
            if (SkillShapes.Contains(skill.shape, origin.position, origin.forward, t.Transform.position,
                                     eff.range, eff.halfAngle, skill.lineWidth, radius, out float d))
                list.Add((t, d));
        }

        foreach (var e in Enemy.All.ToArray()) Consider(e);
        if (Boss.Active != null) Consider(Boss.Active);

        list.Sort((a, b) => a.Item2.CompareTo(b.Item2));
        return list;
    }

    static bool IsAlive(ISkillTarget t)
    {
        if (t is UnityEngine.Object o && o == null) return false;
        return !t.IsDead;
    }

    static void ApplyToEnemies(SkillEffectEntry e, float v, SkillDef skill, CombatCharacter caster,
                               Transform origin, List<(ISkillTarget t, float d)> area)
    {
        int n = e.targetCap > 0 ? Mathf.Min(e.targetCap, area.Count) : area.Count;
        for (int i = 0; i < n; i++)
        {
            var t = area[i].t;
            if (!IsAlive(t)) continue;

            switch (e.type)
            {
                case SkillEffectType.Damage:
                    t.ReceiveHit(caster.Element, Mathf.RoundToInt(caster.Atk * v));
                    break;
                case SkillEffectType.Dot:
                    t.ApplyStatus(Harmful(skill, e, StatusKind.Dot, StatKind.Atk, caster.Atk * v));
                    break;
                case SkillEffectType.Slow:
                    t.ApplyStatus(Harmful(skill, e, StatusKind.StatMod, StatKind.MoveSpeed, v));
                    break;
                case SkillEffectType.Stun:
                    t.ApplyStatus(Harmful(skill, e, StatusKind.Stun, StatKind.Atk, 0f));
                    break;
                case SkillEffectType.StatDebuff:
                    t.ApplyStatus(Harmful(skill, e, StatusKind.StatMod, e.stat, v));
                    break;
                case SkillEffectType.DamageTakenMult:
                    t.ApplyStatus(Harmful(skill, e, StatusKind.StatMod, StatKind.DamageTaken, v));
                    break;
                case SkillEffectType.Mark:
                    t.SetMark(caster.Element);
                    break;
                case SkillEffectType.Knockback:
                    t.Displace(origin.position, v, pull: false);
                    break;
                case SkillEffectType.Pull:
                    t.Displace(origin.position, v, pull: true);
                    break;
            }
        }
    }

    static StatusEffect Harmful(SkillDef skill, SkillEffectEntry e, StatusKind kind, StatKind stat, float value)
        => new StatusEffect
        {
            Id = $"{skill.id}:{e.type}:{stat}", Kind = kind, Stat = stat, Value = value,
            Duration = e.duration, IsBeneficial = false, MaxStacks = Mathf.Max(1, e.maxStacks),
        };

    // ── Strike — 부채꼴 내 가장 가까운 적 1체(+추가 타겟) 고위력 공격 ────────

    static void ExecStrike(EffectiveSkill eff, CombatCharacter caster, Transform origin)
    {
        // 범위 내 적을 거리 순으로 정렬
        var targets = new List<(Enemy enemy, float dist)>();
        foreach (var enemy in Enemy.All.ToArray())
        {
            if (!IsValidTarget(enemy, origin, eff.range, eff.halfAngle, out float d))
                continue;
            targets.Add((enemy, d));
        }
        targets.Sort((a, b) => a.dist.CompareTo(b.dist));

        int hitCount = 1 + eff.extraTargets;  // 기본 1 + 마일스톤 추가
        int atk = Mathf.RoundToInt(caster.Atk * eff.damageMultiplier);

        for (int i = 0; i < hitCount && i < targets.Count; i++)
            targets[i].enemy.ReceiveHit(caster.Element, atk);
    }

    // ── Aoe — 부채꼴 내 모든 적 범위 공격 ─────────────────────────────────

    static void ExecAoe(EffectiveSkill eff, CombatCharacter caster, Transform origin)
    {
        int atk = Mathf.RoundToInt(caster.Atk * eff.damageMultiplier);

        foreach (var enemy in Enemy.All.ToArray())
        {
            if (!IsValidTarget(enemy, origin, eff.range, eff.halfAngle, out _))
                continue;
            enemy.ReceiveHit(caster.Element, atk);
        }
    }

    // ── HealBuff — 자가 회복 + 공격력 버프 ────────────────────────────────

    static void ExecHealBuff(EffectiveSkill eff, CombatCharacter caster, Transform origin)
    {
        // 회복
        int healAmt = Mathf.Max(1, Mathf.RoundToInt(caster.MaxHp * eff.healPercent));
        caster.Heal(healAmt);
        DamageNumber.SpawnHeal(origin.position + Vector3.up * 2f, healAmt);

        // 공격력 버프 (중복 적용 시 ApplyAtkBuff 내부에서 더 긴 쪽으로 갱신)
        caster.ApplyAtkBuff(eff.buffAtkMultiplier, eff.buffDuration);
    }

    // ── Mark — 부채꼴 내 적에게 속성 표식 부여 ────────────────────────────

    static void ExecMark(EffectiveSkill eff, CombatCharacter caster, Transform origin)
    {
        foreach (var enemy in Enemy.All.ToArray())
        {
            if (!IsValidTarget(enemy, origin, eff.range, eff.halfAngle, out _))
                continue;
            enemy.SetMark(caster.Element);
        }
    }

    // ── 공통 유틸 ─────────────────────────────────────────────────────────

    /// <summary>
    /// 대상 Enemy가 부채꼴 범위(range, halfAngle) 안에 있는지 확인한다.
    /// 수직 차이는 무시 (PlayerCombat.DoAttack과 동일 방식).
    /// </summary>
    static bool IsValidTarget(Enemy enemy, Transform origin,
                               float range, float halfAngle, out float dist)
    {
        dist = 0f;
        if (enemy == null || !enemy.gameObject.activeInHierarchy) return false;

        Vector3 toEnemy = enemy.transform.position - origin.position;
        toEnemy.y = 0f;
        dist = toEnemy.magnitude;

        if (dist > range)                                     return false;
        if (Vector3.Angle(origin.forward, toEnemy) > halfAngle) return false;
        return true;
    }
}
