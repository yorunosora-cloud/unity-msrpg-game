/// <summary>
/// MSRPG 어셈블리 전역에서 현재 플레이어의 PlayerStats를 참조하기 위한 정적 홀더.
/// GameBootstrap(Assembly-CSharp)이 Start()에서 Stats를 설정한다.
/// 전투 스크립트(MSRPG 어셈블리)는 PlayerRuntime.Stats로 접근한다.
/// </summary>
public static class PlayerRuntime
{
    /// <summary>현재 세션의 PlayerStats. GameBootstrap이 씬 초기화 시 설정.</summary>
    public static PlayerStats Stats { get; set; }
}
