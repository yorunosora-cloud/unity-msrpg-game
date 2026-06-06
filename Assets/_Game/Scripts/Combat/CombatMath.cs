using UnityEngine;

/// <summary>
/// 전투 데미지 계산 유틸리티 (순수 정적, 테스트 가능).
/// 설계 §전투 — 약점 크리티컬 배수: 1.8×.
/// </summary>
public static class CombatMath
{
    /// <summary>약점 적중 시 데미지 배수.</summary>
    public const float WeaknessCritMultiplier = 1.8f;

    /// <summary>
    /// 기본 데미지를 계산합니다.
    /// base = max(1, atk - def). 약점이면 ×WeaknessCritMultiplier 반올림. 최소 1 보장.
    /// </summary>
    /// <param name="atk">공격력</param>
    /// <param name="def">방어력</param>
    /// <param name="isWeakness">약점 속성 여부</param>
    /// <returns>최종 데미지 (≥ 1)</returns>
    public static int ComputeDamage(int atk, int def, bool isWeakness)
    {
        int baseDmg = Mathf.Max(1, atk - def);
        if (!isWeakness) return baseDmg;
        return Mathf.Max(1, Mathf.RoundToInt(baseDmg * WeaknessCritMultiplier));
    }
}
