using UnityEngine;

public enum Rarity { N, R, SR, SSR, UR }

public static class RarityExtensions
{
    /// <summary>등급별 레벨 상한 (설계 §4).</summary>
    public static int LevelCap(this Rarity r) => r switch
    {
        Rarity.N   => 40,
        Rarity.R   => 50,
        Rarity.SR  => 60,
        Rarity.SSR => 70,
        Rarity.UR  => 80,
        _          => 40,
    };

    /// <summary>가챠 기본 가중치 (합계 100). GachaConfig와 일치해야 함.</summary>
    public static float GachaWeight(this Rarity r) => r switch
    {
        Rarity.N   => 40f,
        Rarity.R   => 30f,
        Rarity.SR  => 18f,
        Rarity.SSR => 10f,
        Rarity.UR  =>  2f,
        _          =>  0f,
    };

    /// <summary>도감·결과창 표시 색상 (디자인 시스템 §B-4). UR은 IsRainbow()로 별도 처리.</summary>
    public static Color DisplayColor(this Rarity r) => r switch
    {
        Rarity.N   => new Color(0.298f, 0.686f, 0.314f), // #4CAF50 녹색
        Rarity.R   => new Color(0.612f, 0.153f, 0.690f), // #9C27B0 보라
        Rarity.SR  => new Color(1.000f, 0.839f, 0.000f), // #FFD600 노랑
        Rarity.SSR => new Color(1.000f, 0.702f, 0.000f), // #FFB300 황금
        Rarity.UR  => new Color(1.000f, 0.000f, 0.502f), // #FF0080 무지개 폴백색
        _          => Color.white,
    };

    /// <summary>UR 등급은 hue-shift 무지개 애니메이션이 필요함을 알린다.</summary>
    public static bool IsRainbow(this Rarity r) => r == Rarity.UR;
}
