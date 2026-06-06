/// <summary>
/// 가챠 밸런스 상수 (설계 §7-1).
/// 확률·천장·비용을 이곳에서 한 번에 조정하세요.
/// </summary>
public static class GachaConfig
{
    // ── 등급별 기본 가중치 (합계 100) ─────────────────────────────────────
    public const float WeightN   = 40f;
    public const float WeightR   = 30f;
    public const float WeightSR  = 18f;
    public const float WeightSSR = 10f;
    public const float WeightUR  =  2f;

    // ── 천장 (하드 피티) ──────────────────────────────────────────────────
    /// <summary>이 횟수를 채우면 SSR 이상 확정.</summary>
    public const int PityCap = 50;

    // ── 10연 보장 ─────────────────────────────────────────────────────────
    /// <summary>10연 중 SR+ 없으면 마지막 1뽑에 SR 보장.</summary>
    public const bool TenPullSrGuarantee = true;

    // ── 뽑기 비용 (논문/책) ───────────────────────────────────────────────
    public const int CostSingle = 10;
    public const int CostTen    = 100;

    // ── 중복 보상 결정 수량 ───────────────────────────────────────────────
    /// <summary>중복 캐릭터 획득 시 등급별 지급 결정 수량.</summary>
    public static int CrystalForRarity(Rarity r) => r switch
    {
        Rarity.N   =>  1,
        Rarity.R   =>  2,
        Rarity.SR  =>  5,
        Rarity.SSR => 10,
        Rarity.UR  => 20,
        _          =>  0,
    };
}
