using UnityEngine;

/// <summary>대륙별 시그니처 색 (디자인 시스템 §B-1). Continent enum 순서 일치.</summary>
public static class ContinentColors
{
    private static readonly Color[] _colors =
    {
        new Color(0.169f, 0.498f, 1.000f), // Physics   #2B7FFF
        new Color(1.000f, 0.090f, 0.267f), // Chemistry #FF1744
        new Color(0.239f, 0.769f, 0.153f), // Biology   #3DC427
        new Color(0.769f, 0.361f, 0.125f), // EarthSci  #C45C20
        new Color(0.784f, 0.824f, 0.000f), // Math      #C8D200
        new Color(0.667f, 0.000f, 1.000f), // Info      #AA00FF
        new Color(0.784f, 0.659f, 0.251f), // Mesoria   #C8A840
    };

    public static Color Of(Continent c)
    {
        int i = (int)c;
        Debug.Assert((uint)i < (uint)_colors.Length, $"ContinentColors.Of: unknown continent {c}");
        return (uint)i < (uint)_colors.Length ? _colors[i] : Color.white;
    }
}
