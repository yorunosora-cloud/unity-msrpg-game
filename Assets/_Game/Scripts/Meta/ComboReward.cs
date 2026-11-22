/// <summary>
/// 시너지 연계 성공 보상(설계도 18 §3-11). 03 시너지의 SynergyResolver가 연계 성공 시 호출한다(P6에서 연결).
/// 정액 연구 자원 보너스 — 값은 튜닝.
/// </summary>
public static class ComboReward
{
    /// <summary>연계 1회당 지급하는 연구 자원(튜닝값).</summary>
    public const int ComboBonusAmount = 3;

    /// <summary>시전자 속성의 연구 자원을 지급하고 지급량을 반환한다. 지갑이 없으면 0.</summary>
    public static int Award(StudyMaterialWallet wallet, Continent element)
    {
        if (wallet == null) return 0;
        wallet.Add(element, ComboBonusAmount);
        return ComboBonusAmount;
    }
}
