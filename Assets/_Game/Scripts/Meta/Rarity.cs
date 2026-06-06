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

    /// <summary>도감·결과창 표시 색상.</summary>
    public static Color DisplayColor(this Rarity r) => r switch
    {
        Rarity.N   => new Color(0.75f, 0.75f, 0.75f), // 회색
        Rarity.R   => new Color(0.20f, 0.60f, 1.00f), // 파랑
        Rarity.SR  => new Color(0.60f, 0.20f, 1.00f), // 보라
        Rarity.SSR => new Color(1.00f, 0.75f, 0.00f), // 금
        Rarity.UR  => new Color(1.00f, 0.30f, 0.10f), // 빨강
        _          => Color.white,
    };
}
