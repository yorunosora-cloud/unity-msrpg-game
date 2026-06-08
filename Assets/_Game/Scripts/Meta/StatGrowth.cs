using UnityEngine;

/// <summary>
/// 레벨에 따른 스탯 성장 계산 (설계 §4).
/// 선형 성장: base × (1 + (level - 1) × Growth)
/// </summary>
public static class StatGrowth
{
    /// <summary>레벨당 스탯 증가율 (8%).</summary>
    public const float Growth = 0.08f;

    /// <summary>주어진 레벨에서의 최종 스탯을 계산합니다.</summary>
    public static CharacterStats ComputeStats(CharacterDef def, int level)
    {
        float m = 1f + (level - 1) * Growth;
        var b   = def.baseStats;
        return new CharacterStats
        {
            hp  = Mathf.RoundToInt(b.hp  * m),
            atk = Mathf.RoundToInt(b.atk * m),
            def = Mathf.RoundToInt(b.def * m),
            spd = Mathf.RoundToInt(b.spd * m),
            mp  = Mathf.RoundToInt(b.mp  * m),
        };
    }

    /// <summary>등급별 최대 레벨 상한.</summary>
    public static int LevelCap(Rarity rarity) => rarity.LevelCap();

    /// <summary>주어진 레벨에서 다음 레벨까지 필요한 EXP. CombatCharacter, RnEPanel 공용.</summary>
    public static int NextExp(int level)
        => (int)System.Math.Floor(100.0 * System.Math.Pow(1.45, level - 1));
}
