using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>패시브가 월드(적·위치)에 일으켜야 할 효과 1건. PassiveHost가 실행한다.</summary>
public struct PassiveFire
{
    public PassiveTriggerType trigger;
    public SkillEffectEntry   effect;
    public float              value;
    public HitInfo            hit;
    public string             skillId;
}

/// <summary>
/// 캐릭터 1명의 패시브 실행기 (설계도 22 §4-7). 순수 C# — 월드 효과는 OnFire로 발행하고 PassiveHost가 실행한다.
/// 경로 A(이벤트: 스택·효과) · B(상시 수정자) · C(타격 공식)를 모두 담당한다.
/// 스택·타이머는 활성일 때만 흐르고 대기 시 유지(규칙 1), 기절·초기화 시 소멸한다.
/// </summary>
public class PassiveRunner
{
    sealed class Inst
    {
        public int             index;
        public SkillDef        skill;
        public bool            ms;          // 마일스톤 적용 여부
        public StackTracker    stack;
        public ModifierState[] mods;
        public float           icd;         // 내부 쿨타임 남은 시간
        public float           prevHp = 1f; // HpBelowPercent 에지 판정용 직전 HP 비율
    }

    readonly CombatCharacter _o;
    readonly List<Inst>      _list = new();
    bool _isActive;

    /// <summary>월드에서 실행해야 할 효과 발행.</summary>
    public event Action<PassiveFire> OnFire;

    /// <summary>파티 존재 수정자가 하나라도 있는지(Party.PresenceMult 최적화용).</summary>
    public bool HasPartyWide { get; private set; }

    public PassiveRunner(CombatCharacter owner, SkillDef[] skills)
    {
        _o = owner;
        if (skills == null) return;
        for (int i = 0; i < skills.Length; i++)
        {
            var s = skills[i];
            if (s == null || s.skillType != SkillKind.Passive) continue;
            var n = new Inst { index = i, skill = s };
            Build(n, false);
            _list.Add(n);
            if (n.mods != null)
                foreach (var m in n.mods) if (m.IsPartyWide) HasPartyWide = true;
        }
    }

    void Build(Inst n, bool ms)
    {
        n.ms = ms;
        var spec = n.skill.passive;
        bool hasStack = spec.stack.maxStacks > 0 || spec.stack.damageRatio > 0f;
        n.stack = hasStack ? new StackTracker(spec.stack.Resolve(ms)) : null;
        var mods = spec.modifiers;
        n.mods = new ModifierState[mods?.Length ?? 0];
        for (int k = 0; k < n.mods.Length; k++) n.mods[k] = new ModifierState(mods[k], ms);
    }

    bool Milestone(Inst n) => n.skill.milestoneLevel > 0 && _o.SkillLevel(n.index) >= n.skill.milestoneLevel;

    /// <summary>해금·폼 조건을 만족하는(살아 있는) 패시브만 순회. 마일스톤 변화는 여기서 동기화.</summary>
    IEnumerable<Inst> Live()
    {
        if (_o.IsDowned) yield break;
        foreach (var n in _list)
        {
            bool ms = Milestone(n);
            if (ms != n.ms) Build(n, ms);
            if (!_o.IsUnlocked(n.index)) continue;
            if (!_o.FormReqMatches(n.skill.formReq)) continue;
            yield return n;
        }
    }

    // ── 조회 ──────────────────────────────────────────────────────────────

    public int Stacks(string skillId)
    {
        foreach (var n in _list)
            if (n.skill.id == skillId) return n.stack?.Stacks ?? 0;
        return 0;
    }

    // ── 시간 ──────────────────────────────────────────────────────────────

    /// <summary>모든 캐릭터에 매 프레임. isActive일 때만 스택 타이머·내부 쿨타임이 흐른다.</summary>
    public void Tick(float dt, bool isActive)
    {
        _isActive = isActive;
        foreach (var n in _list)
        {
            n.stack?.Tick(dt, isActive);
            if (isActive && n.icd > 0f) n.icd = Mathf.Max(0f, n.icd - dt);
        }
    }

    /// <summary>활성 캐릭터 전용: 이동 상태 기반 수정자(등속운동 램프) 진행.</summary>
    public void TickMotion(float dt, CombatMotionState motion)
    {
        foreach (var n in Live())
            foreach (var m in n.mods) m.Tick(dt, motion.isMoving);
    }

    // ── 경로 A: 이벤트 ─────────────────────────────────────────────────────

    /// <summary>공격 행동의 적중 목록(같은 행동은 actionId 동일). 패시브 유발 피해(isProc)는 제외.</summary>
    public void NotifyHits(List<HitInfo> hits, bool isBasic)
    {
        if (hits == null || hits.Count == 0) return;
        var real = new List<HitInfo>();
        foreach (var h in hits) if (!h.isProc) { var c = h; c.isBasic = isBasic; real.Add(c); }
        if (real.Count == 0) return;

        foreach (var n in Live())
        {
            var spec = n.skill.passive;
            if (spec.trigger != PassiveTriggerType.OnHit) continue;

            if (spec.hitCount == HitCountMode.PerTarget)
            {
                foreach (var h in real) Fire(n, h, PassiveTriggerType.OnHit, null);
            }
            else
            {
                var seen = new HashSet<int>();
                foreach (var h in real)
                {
                    if (!seen.Add(h.actionId)) continue;
                    var merged = h; merged.damage = 0;
                    foreach (var x in real) if (x.actionId == h.actionId) merged.damage += x.damage;
                    Fire(n, merged, PassiveTriggerType.OnHit, null);
                }
            }
        }
    }

    public void NotifyHitTaken(HitInfo hit)
    {
        if (hit.isProc) return;
        foreach (var n in Live())
            if (n.skill.passive.trigger == PassiveTriggerType.OnHitTaken)
                Fire(n, hit, PassiveTriggerType.OnHitTaken, null);
    }

    public void NotifyFormChange(string from, string to)
    {
        foreach (var n in Live())
            if (n.skill.passive.trigger == PassiveTriggerType.OnFormChange)
                Fire(n, default, PassiveTriggerType.OnFormChange, to);
    }

    public void NotifySelfDebuffed()
    {
        foreach (var n in Live())
            if (n.skill.passive.trigger == PassiveTriggerType.SelfDebuffed)
                Fire(n, default, PassiveTriggerType.SelfDebuffed, null);
    }

    /// <summary>HP 비율 변화 통지. 임계를 위→아래로 넘는 순간(생존)만 발동(에지 트리거).</summary>
    public void NotifyHp(float frac)
    {
        foreach (var n in _list)
        {
            var spec = n.skill.passive;
            if (spec.trigger != PassiveTriggerType.HpBelowPercent) { continue; }

            bool crossed = n.prevHp > spec.value && frac <= spec.value && frac > 0f;
            n.prevHp = frac;
            if (!crossed) continue;
            if (_o.IsDowned || !_o.IsUnlocked(n.index) || !_o.FormReqMatches(n.skill.formReq)) continue;
            Fire(n, default, PassiveTriggerType.HpBelowPercent, null);
        }
    }

    void Fire(Inst n, HitInfo hit, PassiveTriggerType trig, string toForm)
    {
        var spec = n.skill.passive;
        if (spec.internalCooldown > 0f)
        {
            if (n.icd > 0f) return;
            n.icd = spec.internalCooldown;
        }

        if (n.stack != null)
        {
            if (spec.stack.damageRatio > 0f) n.stack.AddDamage(hit.damage, _o.BaseAtk);
            else                             n.stack.Add();
        }

        if (spec.effects == null) return;
        foreach (var e in spec.effects)
        {
            if (trig == PassiveTriggerType.OnFormChange && !string.IsNullOrEmpty(e.whenForm) && e.whenForm != toForm) continue;

            var e2 = e;
            if (n.ms)
            {
                if (e.milestoneValue    > 0f) e2.value    = e.milestoneValue;
                if (e.milestoneDuration > 0f) e2.duration = e.milestoneDuration;
            }

            if (IsDirect(e2))
            {
                foreach (var ally in TargetResolver.Resolve(_o.Party, _o, e2.target, e2.targetCap))
                    AllyEffectApplier.Apply(e2, e2.value, n.skill.id, _o, ally);
            }
            else
            {
                OnFire?.Invoke(new PassiveFire { trigger = trig, effect = e2, value = e2.value, hit = hit, skillId = n.skill.id });
            }
        }
    }

    /// <summary>아군(자신·대기) 대상의 즉발·시간 효과는 월드 없이 바로 적용한다.</summary>
    static bool IsDirect(SkillEffectEntry e)
    {
        bool allyScope = e.target == TargetScope.Self || e.target == TargetScope.Bench || e.target == TargetScope.SelfAndBench;
        if (!allyScope) return false;
        switch (e.type)
        {
            case SkillEffectType.Heal: case SkillEffectType.Shield: case SkillEffectType.StatBuff:
            case SkillEffectType.DamageTakenMult: case SkillEffectType.RestoreMp:
            case SkillEffectType.Cleanse: case SkillEffectType.SelfDamage:
                return true;
        }
        return false;
    }

    // ── 경로 B: 상시 수정자 ────────────────────────────────────────────────

    /// <summary>능력치 k에 곱할 패시브 배율(스택 효과 + 활성일 때의 상시 수정자). 파티 존재·적 오라 제외.</summary>
    public float StatMult(StatKind k, float lostHpPercent)
    {
        float m = 1f;
        foreach (var n in Live())
        {
            var spec = n.skill.passive;
            if (n.stack != null && spec.stack.stat == k && spec.stack.decay != StackDecay.ConsumeAtCap)
                m *= Mathf.Max(0f, 1f + n.stack.Amount);

            if (!_isActive) continue;
            foreach (var mod in n.mods)
                if (mod.Stat == k && !mod.IsPartyWide && mod.EnemyAuraRadius <= 0f)
                    m *= mod.Multiplier(lostHpPercent);
        }
        return m;
    }

    /// <summary>활성인 동안 둔화 면역 수정자가 하나라도 살아 있는지.</summary>
    public bool IsSlowImmune
    {
        get
        {
            if (!_isActive) return false;
            foreach (var n in Live())
                foreach (var m in n.mods)
                    if (m.ImmuneToSlow) return true;
            return false;
        }
    }

    /// <summary>파티 전원에 적용되는 수정자(활성·대기 무관). 같은 패시브 id 중복 제거는 호출 쪽(Party) 책임.</summary>
    public IEnumerable<(string id, ModifierState mod)> PartyWideModifiers()
    {
        foreach (var n in Live())
            foreach (var m in n.mods)
                if (m.IsPartyWide) yield return (n.skill.id, m);
    }

    /// <summary>활성인 동안 주변 적에게 적용할 오라(반경, 능력치, 배율).</summary>
    public IEnumerable<(float radius, StatKind stat, float value)> EnemyAuras()
    {
        if (!_isActive) yield break;
        foreach (var n in Live())
            foreach (var m in n.mods)
                if (m.EnemyAuraRadius > 0f) yield return (m.EnemyAuraRadius, m.Stat, m.Multiplier(0f));
    }

    // ── 경로 C: 타격 공식 · 소모 ───────────────────────────────────────────

    /// <summary>ConsumeAtCap 스택(양자화)이 가득 찼으면 소모하고 passive.value 배율을 반환(아니면 1).</summary>
    public float ConsumeCapStacks()
    {
        float m = 1f;
        foreach (var n in Live())
        {
            var spec = n.skill.passive;
            if (n.stack == null || spec.stack.decay != StackDecay.ConsumeAtCap) continue;
            if (n.stack.TryConsume()) m *= spec.value > 0f ? spec.value : 1f;
        }
        return m;
    }

    /// <summary>타격 공식 배율(로렌츠·도플러·일·회전축). 공격 행동의 대상마다 호출.</summary>
    public float FormulaMult(ISkillTarget target, float skillRange, Vector3 playerPos)
    {
        float m = 1f;
        foreach (var n in Live())
        {
            var spec = n.skill.passive;
            float c = spec.value > 0f ? spec.value : 2000f;
            switch (spec.formula)
            {
                case PassiveFormulaId.Lorentz:
                    if (target != null) m *= PassiveFormula.Lorentz(target.Spd, c);
                    break;
                case PassiveFormulaId.Doppler:
                    if (target != null) m *= PassiveFormula.Doppler(target.Spd * target.ApproachSign(playerPos), c);
                    break;
                case PassiveFormulaId.RangeWork:
                    m *= PassiveFormula.RangeWork(skillRange);
                    break;
                case PassiveFormulaId.AxisDistance:
                    if (target != null && target.HasMark(n.skill.id))
                    {
                        Vector3 d = target.Transform.position - playerPos; d.y = 0f;
                        float bonus = n.ms && spec.milestoneValue > 0f ? spec.milestoneValue : spec.value;
                        m *= PassiveFormula.AxisDistance(d.magnitude, 5f, bonus);
                    }
                    break;
            }
        }
        return m;
    }

    // ── 초기화 ────────────────────────────────────────────────────────────

    /// <summary>기절·전체 회복·전투 구역 이탈 시: 스택·내부 쿨타임·램프 초기화.</summary>
    public void Clear()
    {
        foreach (var n in _list)
        {
            Build(n, n.ms);
            n.icd = 0f;
            n.prevHp = 1f;
        }
    }
}
