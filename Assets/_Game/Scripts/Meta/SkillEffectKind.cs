/// <summary>
/// 액티브 스킬 효과 범주 (설계 §5-1).
/// Strike·Aoe는 데미지 계산, HealBuff는 자가 회복+버프, Mark는 표식 부여.
/// </summary>
public enum SkillEffectKind
{
    Strike,    // 단일 고위력 — 부채꼴 내 가장 가까운 적 1체
    Aoe,       // 광역 폭발  — 부채꼴 내 모든 적
    HealBuff,  // 자가 회복 + 일시 공격력 버프
    Mark,      // 적에게 속성 표식 부여 (시너지 연계용 — 소모는 후속 단계)
}
