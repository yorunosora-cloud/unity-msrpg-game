/// <summary>
/// MSRPG 어셈블리 전역 런타임 홀더.
/// GameBootstrap(Assembly-CSharp)이 Stats를 설정하고,
/// PartyController(MSRPG 어셈블리)가 Party / Active를 설정한다.
/// </summary>
public static class PlayerRuntime
{
    /// <summary>현재 세션의 PlayerStats (레거시, 저장 흐름 유지용). GameBootstrap이 설정.</summary>
    public static PlayerStats Stats { get; set; }

    /// <summary>현재 조작 중인 파티. PartyController가 Start()에서 설정.</summary>
    public static Party Party { get; set; }

    /// <summary>현재 활성 캐릭터. PartyController가 교체 시마다 갱신.</summary>
    public static CombatCharacter Active { get; set; }
}
