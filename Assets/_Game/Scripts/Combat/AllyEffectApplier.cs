using System;

/// <summary>
/// 아군(자신·대기 파티원) 대상 효과 적용 — 순수 C# (설계도 22 §4-3, §4-5).
/// 즉발 효과(회복·MP·정화)는 즉시, 시간 효과(버프·보호막·피해감소)는 StatusHolder로 부여한다.
/// 대기 캐릭터의 이로운 효과는 StatusHolder가 예약 상태로 보관한다.
/// </summary>
public static class AllyEffectApplier
{
    /// <summary>
    /// 효과 1건을 target에 적용. 실제 회복된 HP를 반환(그 외 0).
    /// caster는 현재 활성 캐릭터(target==caster이면 즉시 시작, 아니면 예약).
    /// </summary>
    public static int Apply(SkillEffectEntry e, float value, string sourceId,
                            CombatCharacter caster, CombatCharacter target)
    {
        if (target == null) return 0;
        bool targetIsActive = target == caster;

        switch (e.type)
        {
            case SkillEffectType.Heal:
            {
                if (target.IsDowned) return 0;
                int before = target.Hp;
                target.Heal(Math.Max(1, (int)Math.Round(target.MaxHp * value)));
                return target.Hp - before;
            }
            case SkillEffectType.Shield:
                if (target.IsDowned) return 0;
                target.ApplyStatus(new StatusEffect
                {
                    Id = Key(sourceId, e), Kind = StatusKind.Shield,
                    Value = target.MaxHp * value, Duration = e.duration, IsBeneficial = true,
                }, targetIsActive);
                return 0;

            case SkillEffectType.StatBuff:
                if (target.IsDowned) return 0;
                target.ApplyStatus(new StatusEffect
                {
                    Id = Key(sourceId, e), Kind = StatusKind.StatMod, Stat = e.stat,
                    Value = value, Duration = e.duration, IsBeneficial = true,
                    MaxStacks = Math.Max(1, e.maxStacks), ConsumeOnHit = e.consumeOnHit,
                }, targetIsActive);
                return 0;

            case SkillEffectType.DamageTakenMult:
                if (target.IsDowned) return 0;
                target.ApplyStatus(new StatusEffect
                {
                    Id = Key(sourceId, e), Kind = StatusKind.StatMod, Stat = StatKind.DamageTaken,
                    Value = value, Duration = e.duration, IsBeneficial = value <= 1f,
                    MaxStacks = Math.Max(1, e.maxStacks), ElementMask = e.elementMask, ElementInvert = e.elementInvert,
                }, targetIsActive);
                return 0;

            case SkillEffectType.StoreDamage:
                if (!target.IsDowned) target.BeginDamageStore(value, e.duration, e.areaRadius > 0f ? e.areaRadius : 3f);
                return 0;

            case SkillEffectType.RestoreMp:
                if (!target.IsDowned)
                    target.RecoverMp(Math.Max(1, (int)Math.Round(target.MaxMp * value)));
                return 0;

            case SkillEffectType.Cleanse:
                if (!target.IsDowned) target.Cleanse();
                return 0;

            case SkillEffectType.SelfDamage:
                if (target == caster)
                {
                    int dmg = Math.Min((int)Math.Round(target.MaxHp * value), target.Hp - 1);
                    if (dmg > 0) target.Damage(dmg);
                }
                return 0;
        }
        return 0;   // 적 전용 효과(피해·도트 등)는 무시
    }

    static string Key(string sourceId, SkillEffectEntry e)
        => e.elementMask == AttackElement.None ? $"{sourceId}:{e.type}:{e.stat}" : $"{sourceId}:{e.type}:{e.stat}:{(int)e.elementMask}{(e.elementInvert ? "!" : "")}";
}
