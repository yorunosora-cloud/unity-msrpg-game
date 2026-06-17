using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

/// <summary>도감(Codex) 관련 EditMode 테스트 — 필터·정렬·보유여부 순수 로직 검증.</summary>
public class CodexTests
{
    // ════════════════════════════════════════════════════════════════════════
    // 격리: 테스트별 ScriptableObject 자동 정리
    // ════════════════════════════════════════════════════════════════════════

    readonly List<CharacterDef> _created = new List<CharacterDef>();

    [TearDown]
    public void Cleanup()
    {
        foreach (var d in _created)
            if (d != null) Object.DestroyImmediate(d);
        _created.Clear();
    }

    CharacterDef MakeDef(string id, string nameKo, Continent continent, Rarity rarity, int dexNumber = 0)
    {
        var def = ScriptableObject.CreateInstance<CharacterDef>();
        def.id        = id;
        def.nameKo    = nameKo;
        def.continent = continent;
        def.rarity    = rarity;
        def.dexNumber = dexNumber;
        _created.Add(def);
        return def;
    }

    // ════════════════════════════════════════════════════════════════════════
    // 1. OwnedCharacter.SkillLevel — 기본값 1, skillProgress 불변
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void OwnedCharacter_SkillLevel_DefaultOne_NoMutation()
    {
        var owned = new OwnedCharacter { id = "char_a" };
        int countBefore = owned.skillProgress.Count;

        int level = owned.SkillLevel("some_skill");

        Assert.AreEqual(1, level, "SkillLevel이 없는 skillId에 대해 1을 반환해야 합니다.");
        Assert.AreEqual(countBefore, owned.skillProgress.Count, "SkillLevel은 skillProgress를 변형하면 안 됩니다.");
    }

    // ════════════════════════════════════════════════════════════════════════
    // 2. 대륙 필터 — LINQ Where
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void ContinentFilter_ReturnsOnlyMatchingContinent()
    {
        var physics   = MakeDef("p1", "물리A", Continent.Physics,   Rarity.R,  dexNumber: 1);
        var chemistry = MakeDef("c1", "화학A", Continent.Chemistry, Rarity.SR, dexNumber: 2);
        var math      = MakeDef("m1", "수학A", Continent.Math,      Rarity.N,  dexNumber: 3);

        var filtered = new[] { physics, chemistry, math }
            .Where(d => d.continent == Continent.Physics).ToList();

        Assert.AreEqual(1, filtered.Count);
        Assert.AreEqual("p1", filtered[0].id);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 3. 희귀도 필터 — LINQ Where
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void RarityFilter_ReturnsOnlyMatchingRarity()
    {
        var n   = MakeDef("n1",   "N캐릭",   Continent.Info,     Rarity.N,   dexNumber: 1);
        var sr  = MakeDef("sr1",  "SR캐릭",  Continent.Biology,  Rarity.SR,  dexNumber: 2);
        var ssr = MakeDef("ssr1", "SSR캐릭", Continent.EarthSci, Rarity.SSR, dexNumber: 3);

        var filtered = new[] { n, sr, ssr }
            .Where(d => d.rarity == Rarity.SR).ToList();

        Assert.AreEqual(1, filtered.Count);
        Assert.AreEqual("sr1", filtered[0].id);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 4. 보유 필터 — Has(id) 기반 비보유 제외
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void OwnedFilter_ExcludesUnownedCharacters()
    {
        var d1 = MakeDef("owned_char",   "보유A",  Continent.Math,    Rarity.R,  dexNumber: 1);
        var d2 = MakeDef("unowned_char", "미보유B", Continent.Physics, Rarity.SR, dexNumber: 2);

        var roster = new Roster();
        roster.Add("owned_char");

        var filtered = new[] { d1, d2 }
            .Where(d => roster.Has(d.id)).ToList();

        Assert.AreEqual(1, filtered.Count);
        Assert.AreEqual("owned_char", filtered[0].id);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 5. 도감번호 정렬 — dexNumber==0 → 맨 끝
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void SortByDexNumber_ZeroGoesToEnd()
    {
        var d0 = MakeDef("no_dex", "번호없음", Continent.Math,      Rarity.R,  dexNumber: 0);
        var d1 = MakeDef("dex_1",  "첫번째",  Continent.Physics,   Rarity.SR, dexNumber: 1);
        var d3 = MakeDef("dex_3",  "세번째",  Continent.Chemistry, Rarity.N,  dexNumber: 3);

        var sorted = new[] { d0, d3, d1 }
            .OrderBy(d => d.dexNumber == 0 ? int.MaxValue : d.dexNumber)
            .ToList();

        Assert.AreEqual("dex_1",  sorted[0].id, "dexNumber=1이 첫 번째여야 합니다.");
        Assert.AreEqual("dex_3",  sorted[1].id, "dexNumber=3이 두 번째여야 합니다.");
        Assert.AreEqual("no_dex", sorted[2].id, "dexNumber=0은 맨 끝이어야 합니다.");
    }

    // ════════════════════════════════════════════════════════════════════════
    // 6. 희귀도 내림차순 정렬 — UR > SSR > SR > R > N
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void SortByRarityDescending_HigherRarityFirst()
    {
        var n   = MakeDef("n1",   "N",   Continent.Info,     Rarity.N,   dexNumber: 1);
        var r   = MakeDef("r1",   "R",   Continent.Biology,  Rarity.R,   dexNumber: 2);
        var sr  = MakeDef("sr1",  "SR",  Continent.Math,     Rarity.SR,  dexNumber: 3);
        var ssr = MakeDef("ssr1", "SSR", Continent.Physics,  Rarity.SSR, dexNumber: 4);
        var ur  = MakeDef("ur1",  "UR",  Continent.EarthSci, Rarity.UR,  dexNumber: 5);

        var sorted = new[] { n, ssr, r, ur, sr }
            .OrderByDescending(d => (int)d.rarity)
            .ToList();

        Assert.AreEqual(Rarity.UR,  sorted[0].rarity, "UR이 첫 번째여야 합니다.");
        Assert.AreEqual(Rarity.SSR, sorted[1].rarity, "SSR이 두 번째여야 합니다.");
        Assert.AreEqual(Rarity.SR,  sorted[2].rarity, "SR이 세 번째여야 합니다.");
        Assert.AreEqual(Rarity.R,   sorted[3].rarity, "R이 네 번째여야 합니다.");
        Assert.AreEqual(Rarity.N,   sorted[4].rarity, "N이 마지막이어야 합니다.");
    }

    // ════════════════════════════════════════════════════════════════════════
    // 7. 이름 정렬 — nameKo 가나다순
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void SortByNameKo_KoreanAlphabetical()
    {
        var da = MakeDef("c_da", "다나", Continent.Math,    Rarity.R,  dexNumber: 1);
        var ga = MakeDef("c_ga", "가나", Continent.Physics, Rarity.SR, dexNumber: 2);
        var na = MakeDef("c_na", "나나", Continent.Info,    Rarity.N,  dexNumber: 3);

        var sorted = new[] { da, ga, na }
            .OrderBy(d => d.nameKo)
            .ToList();

        // 유니코드 한글 음절 순서: 가 < 나 < 다
        Assert.AreEqual("c_ga", sorted[0].id, "가나가 첫 번째여야 합니다.");
        Assert.AreEqual("c_na", sorted[1].id, "나나가 두 번째여야 합니다.");
        Assert.AreEqual("c_da", sorted[2].id, "다나가 세 번째여야 합니다.");
    }

    // ════════════════════════════════════════════════════════════════════════
    // 8. Roster.Get — 미보유 캐릭터는 null 반환
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Roster_Get_ReturnsNullForUnownedCharacter()
    {
        var roster = new Roster();
        var result = roster.Get("not_in_roster");
        Assert.IsNull(result, "미보유 캐릭터 조회 시 null을 반환해야 합니다.");
    }

    // ════════════════════════════════════════════════════════════════════════
    // 9. CharacterDef 신규 필드 기본값 — dexNumber=0, gachaObtainable=true
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void CharacterDef_NewFieldDefaults_AreCorrect()
    {
        var def = MakeDef("x", "", Continent.Physics, Rarity.N); // MakeDef가 _created에 등록 → TearDown에서 정리

        Assert.AreEqual(0,    def.dexNumber,      "dexNumber 기본값은 0이어야 합니다.");
        Assert.IsTrue(def.gachaObtainable,         "gachaObtainable 기본값은 true여야 합니다.");
    }
}
