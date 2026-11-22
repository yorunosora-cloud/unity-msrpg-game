/// <summary>
/// 정밀 처치 등급 (설계도 18 §3-10). 막타가 적의 남은 HP에 가까울수록(오버킬이 적을수록) 보상 배율이 높다.
/// 기준 피해 = 방어·받는피해배율까지 적용한 최종 피해(화면에 보이는 숫자).
/// </summary>
public static class PrecisionKillGrader
{
    /// <summary>hpBefore = 이 타격 직전 적 HP, dmg = 이 타격의 최종 피해. 반환 = 보상 배율(1.0~2.0).</summary>
    public static float Grade(int hpBefore, int dmg)
    {
        if (hpBefore <= 0) return 1.0f;
        float overkill = (float)(dmg - hpBefore) / hpBefore;
        if (overkill <= 0.0f)  return 2.0f;   // 완벽(±0)
        if (overkill <= 0.10f) return 1.5f;   // 우수
        if (overkill <= 0.30f) return 1.2f;   // 양호
        return 1.0f;                          // 보통
    }
}
