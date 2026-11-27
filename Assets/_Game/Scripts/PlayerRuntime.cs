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

    /// <summary>플레이어 이동·점프 상태 스냅샷. PlayerController가 매 프레임 갱신 (폼 자동 전환 등이 읽는다).</summary>
    public static CombatMotionState Motion { get; set; }

    /// <summary>최대 점프 높이(m) — 높이 비례 위력의 기준. 컨트롤러가 jumpSpeed²/2|g|로 갱신한다.</summary>
    public static float MaxJumpHeight { get; set; } = 1.5f;
}
