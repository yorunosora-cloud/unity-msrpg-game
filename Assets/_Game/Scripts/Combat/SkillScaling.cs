using UnityEngine;

/// <summary>
/// 스킬 레벨에 따른 유효 수치를 계산하는 순수 static 헬퍼.
/// SkillExecutor(전투)와 RnEPanel(UI) 양쪽에서 호출해 공식 중복·드리프트를 방지한다.
/// </summary>
public static class SkillScaling
{
    // ── 상수 (튜닝 가능) ──────────────────────────────────────────────────────

    /// <summary>스킬 최대 레벨.</summary>
    public const int   MaxLevel     = 5;
    /// <summary>레벨당 위력 상승률 (base × (1 + (lv-1) × PowerPerLevel)).</summary>
    public const float PowerPerLevel = 0.12f;
    /// <summary>상 난이도 정답 시 숙련도 보너스 비율 (임계값 기준).</summary>
    public const float HighBonusPct  = 0.03f;
    /// <summary>중 난이도 정답 시 숙련도 보너스 비율.</summary>
    public const float MidBonusPct   = 0.01f;

    // 인덱스=레벨. _thresholds[1]=6 (Lv.1→2 임계값), [4]=24 (Lv.4→5).
    static readonly int[] _thresholds = { 0, 6, 10, 16, 24 };

    // ── 임계값 ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 주어진 레벨에서 다음 레벨로 올라가기 위한 숙련도 임계값.
    /// Lv.1→2:6 / 2→3:10 / 3→4:16 / 4→5:24.
    /// 만렙(Lv.5) 이상이면 int.MaxValue 반환.
    /// </summary>
    public static int Threshold(int level)
    {
        if (level >= MaxLevel) return int.MaxValue;
        int clamped = Mathf.Clamp(level, 1, MaxLevel - 1);
        return _thresholds[clamped];
    }

    // ── 유효수치 계산 ─────────────────────────────────────────────────────────

    /// <summary>
    /// SkillDef와 현재 스킬 레벨로부터 유효 전투 수치를 계산한다.
    /// 위력 곡선: base × (1 + (level-1) × 0.12). Lv.1 = base, Lv.5 ≈ 1.48×.
    /// level ≥ milestoneLevel 이면 마일스톤 보너스를 추가로 적용한다.
    /// </summary>
    public static EffectiveSkill Compute(SkillDef def, int level)
    {
        float mult = 1f + (level - 1) * PowerPerLevel;

        var result = new EffectiveSkill
        {
            damageMultiplier  = def.damageMultiplier * mult,
            healPercent       = def.healPercent * mult,
            // 버프는 (배율-1) 부분에만 곱해 기저값 1f 보존
            buffAtkMultiplier = 1f + (def.buffAtkMultiplier - 1f) * mult,
            buffDuration      = def.buffDuration,
            range             = def.range,
            halfAngle         = def.halfAngle,
            extraTargets      = 0,
        };

        // 마일스톤 적용
        if (def.milestoneLevel > 0 && level >= def.milestoneLevel)
        {
            result.range        += def.milestoneRangeBonus;
            result.halfAngle    += def.milestoneHalfAngleBonus;
            result.healPercent  += def.milestoneHealPercentBonus;
            result.buffDuration += def.milestoneBuffDurationBonus;
            result.extraTargets += def.milestoneExtraTargets;
        }

        return result;
    }

    // ── 숙련도 보너스 ─────────────────────────────────────────────────────────

    /// <summary>
    /// 문제 난이도에 따른 숙련도 추가 보너스 계산.
    /// threshold = 정답한 문제가 속한 레벨의 임계값.
    /// 상 +3% / 중 +1% / 하 0.
    /// </summary>
    public static int ProficiencyBonus(ProblemDifficulty difficulty, int threshold)
    {
        switch (difficulty)
        {
            case ProblemDifficulty.High: return Mathf.RoundToInt(threshold * HighBonusPct);
            case ProblemDifficulty.Mid:  return Mathf.RoundToInt(threshold * MidBonusPct);
            default:                     return 0;
        }
    }
}

/// <summary>
/// 레벨별 스킬 유효 수치. SkillScaling.Compute() 가 반환하는 값 묶음.
/// </summary>
public struct EffectiveSkill
{
    public float damageMultiplier;   // Strike / Aoe 데미지 배율
    public float healPercent;        // HealBuff 회복%
    public float buffAtkMultiplier;  // HealBuff 공격력 배율
    public float buffDuration;       // HealBuff 버프 지속 시간 (초)
    public float range;              // 사거리
    public float halfAngle;          // 전방 ±각도
    public int   extraTargets;       // Strike 추가 타겟 수 (마일스톤)
}
