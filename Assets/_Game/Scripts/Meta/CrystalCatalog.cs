using UnityEngine;

/// <summary>
/// 결정 메타데이터 헬퍼.
/// 대륙(Continent) + country 필드로 CrystalKind를 결정하며,
/// 지구과학(EarthSci)은 country 값으로 4종 중 하나를 선택합니다.
/// </summary>
public static class CrystalCatalog
{
    // ── 지구과학 country 상수 ─────────────────────────────────────────────
    public const string CountryAstronomy  = "astronomy";
    public const string CountryGeology    = "geology";
    public const string CountryOcean      = "ocean";
    public const string CountryAtmosphere = "atmosphere";

    // ── 표시 이름 ─────────────────────────────────────────────────────────

    /// <summary>결정 영문 표시명 (예: "Prime Force").</summary>
    public static string DisplayName(CrystalKind kind) => kind switch
    {
        CrystalKind.PrimeForce      => "Prime Force",
        CrystalKind.ElementaCrystal => "Elementa Crystal",
        CrystalKind.LifeCode        => "Life Code",
        CrystalKind.MemoryOfStar    => "Memory of Star",
        CrystalKind.BoneOfTheEarth  => "Bone of the Earth",
        CrystalKind.OceanPrime      => "Ocean Prime",
        CrystalKind.TornadoCore     => "Tornado Core",
        CrystalKind.Axioma          => "Axioma",
        CrystalKind.PrimeData       => "Prime Data",
        _                           => kind.ToString(),
    };

    /// <summary>도감 표기용 한글 과목 라벨 (예: "[물리]").</summary>
    public static string ContinentLabel(CrystalKind kind) => kind switch
    {
        CrystalKind.PrimeForce      => "[물리]",
        CrystalKind.ElementaCrystal => "[화학]",
        CrystalKind.LifeCode        => "[생명]",
        CrystalKind.MemoryOfStar    => "[지구·천문]",
        CrystalKind.BoneOfTheEarth  => "[지구·지질]",
        CrystalKind.OceanPrime      => "[지구·해양]",
        CrystalKind.TornadoCore     => "[지구·대기]",
        CrystalKind.Axioma          => "[수학]",
        CrystalKind.PrimeData       => "[정보]",
        _                           => "[?]",
    };

    // ── 매핑 ──────────────────────────────────────────────────────────────

    /// <summary>
    /// CharacterDef로부터 결정 종류를 결정합니다.
    /// Mesoria(융합) 또는 캐릭터가 null이면 null을 반환합니다.
    /// 지구과학에서 알 수 없는 country는 MemoryOfStar로 폴백합니다.
    /// </summary>
    public static CrystalKind? KindFor(CharacterDef def)
    {
        if (def == null) return null;

        switch (def.continent)
        {
            case Continent.Physics:   return CrystalKind.PrimeForce;
            case Continent.Chemistry: return CrystalKind.ElementaCrystal;
            case Continent.Biology:   return CrystalKind.LifeCode;
            case Continent.Math:      return CrystalKind.Axioma;
            case Continent.Info:      return CrystalKind.PrimeData;
            case Continent.Mesoria:   return null; // 융합 대륙 — 결정 없음

            case Continent.EarthSci:
                return def.country switch
                {
                    CountryAstronomy  => CrystalKind.MemoryOfStar,
                    CountryGeology    => CrystalKind.BoneOfTheEarth,
                    CountryOcean      => CrystalKind.OceanPrime,
                    CountryAtmosphere => CrystalKind.TornadoCore,
                    _ =>
                        Fallback(def),
                };

            default: return null;
        }
    }

    static CrystalKind Fallback(CharacterDef def)
    {
        Debug.LogWarning(
            $"[CrystalCatalog] EarthSci 캐릭터 '{def.id}'의 country('{def.country}')를 " +
            "인식할 수 없습니다. MemoryOfStar로 대체합니다. " +
            $"유효 값: {CountryAstronomy}, {CountryGeology}, {CountryOcean}, {CountryAtmosphere}");
        return CrystalKind.MemoryOfStar;
    }
}
