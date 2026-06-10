using UnityEngine;

/// <summary>
/// 런타임 대륙 테마 색 계산.
/// ContinentPanel이 OnEnable에서 호출한다.
/// </summary>
public static class UIThemeRuntime
{
    /// <summary>대륙 시그니처 색을 패널 배경으로 어둡게 변환 (H 유지, S=0.42, V=0.14).</summary>
    public static Color PanelBg(Continent c)
    {
        Color.RGBToHSV(ContinentColors.Of(c), out float h, out _, out _);
        var col = Color.HSVToRGB(h, 0.42f, 0.14f);
        col.a = 0.96f;
        return col;
    }

    public static Continent ActiveContinent =>
        PlayerRuntime.Party?.Active?.Element ?? Continent.Mesoria;

    public static Color ActivePanelBg => PanelBg(ActiveContinent);
}
