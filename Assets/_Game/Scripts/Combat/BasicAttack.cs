/// <summary>기본기(Q) 1회의 유효 수치.</summary>
public struct BasicAttackSpec
{
    public float range;
    public float halfAngle;
    public float damageMultiplier;
    public float cooldown;
    public string[] strengthTags;   // 슬롯 0 스킬의 강점 태그(없으면 null)
}

/// <summary>
/// 기본기(Q = 슬롯 0) 수치 해석 순수 로직 (설계도 22 §4-1).
/// 활성 캐릭터가 슬롯 0 스킬을 가지면 그 수치를, 없으면 호출자가 준 기본값을 쓴다.
/// P1은 부채꼴(range/halfAngle)만 지원한다. 기본기는 MP를 소모하지 않는다.
/// </summary>
public static class BasicAttack
{
    public static BasicAttackSpec Resolve(CombatCharacter c, float defRange,
                                          float defHalfAngle, float defCooldown)
    {
        var spec = new BasicAttackSpec
        {
            range            = defRange,
            halfAngle        = defHalfAngle,
            damageMultiplier = 1f,
            cooldown         = defCooldown,
        };
        if (c == null) return spec;

        int idx = c.IndexForSlot(SkillSlots.Basic);
        var s   = idx >= 0 ? c.SkillAt(idx) : null;
        if (s == null) return spec;

        spec.range            = s.range;
        spec.halfAngle        = s.halfAngle;
        spec.damageMultiplier = s.damageMultiplier;
        spec.cooldown         = SkillScaling.EffectiveCooldown(s, c.SkillLevel(idx));
        if (s.milestoneLevel > 0 && c.SkillLevel(idx) >= s.milestoneLevel)
        {
            spec.range            += s.milestoneRangeBonus;
            spec.halfAngle        += s.milestoneHalfAngleBonus;
            spec.damageMultiplier += s.milestoneDamageBonus;
        }
        spec.strengthTags     = s.strengthTags != null && s.strengthTags.Length > 0 ? s.strengthTags : null;
        return spec;
    }
}
