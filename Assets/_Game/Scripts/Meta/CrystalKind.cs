/// <summary>
/// 결정(進化 재화) 종류 — 과목(대륙)별 9종.
/// 가챠에서 중복 캐릭터 획득 시 해당 과목 결정이 지급됩니다.
/// 지구과학(EarthSci)은 세부 분야(country)에 따라 4종으로 분기.
/// </summary>
public enum CrystalKind
{
    PrimeForce,        // 물리 (Physics)
    ElementaCrystal,   // 화학 (Chemistry)
    LifeCode,          // 생명과학 (Biology)
    MemoryOfStar,      // 지구과학·천문 (EarthSci + astronomy)
    BoneOfTheEarth,    // 지구과학·지질 (EarthSci + geology)
    OceanPrime,        // 지구과학·해양 (EarthSci + ocean)
    TornadoCore,       // 지구과학·대기 (EarthSci + atmosphere)
    Axioma,            // 수학 (Math)
    PrimeData,         // 정보·컴퓨터과학 (Info)
}
