/// <summary>
/// 전역 UI 패널 잠금.
/// 하나의 패널이 열려 있으면 다른 패널을 열 수 없고 플레이어 이동도 차단됩니다.
/// </summary>
public static class UIManager
{
    public static bool IsAnyPanelOpen { get; private set; }

    /// <summary>패널을 열려고 시도합니다. 이미 다른 패널이 열려 있으면 false.</summary>
    public static bool TryOpen()
    {
        if (IsAnyPanelOpen) return false;
        IsAnyPanelOpen = true;
        return true;
    }

    /// <summary>패널이 닫힐 때 반드시 호출하세요.</summary>
    public static void Close() => IsAnyPanelOpen = false;
}
