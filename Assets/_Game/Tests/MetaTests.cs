using NUnit.Framework;
using UnityEngine;

/// <summary>메타 레이어 EditMode 테스트 (PlayFab 의존 없음).</summary>
public class MetaTests
{
    // ════════════════════════════════════════════════════════════════════════
    // Wallet
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Wallet_Add_IncreasesBalance()
    {
        var w = new Wallet();
        int prev = w.Paper;
        w.Add(CurrencyKind.Paper, 10);
        Assert.AreEqual(prev + 10, w.Paper);
    }

    [Test]
    public void Wallet_TrySpend_SucceedsWhenAffordable()
    {
        var w = new Wallet();
        w.Add(CurrencyKind.Paper, 100);
        Assert.IsTrue(w.TrySpend(CurrencyKind.Paper, 50));
    }

    [Test]
    public void Wallet_TrySpend_FailsWhenInsufficient_AndBalanceUnchanged()
    {
        var w = new Wallet(); // Paper = 30
        bool ok = w.TrySpend(CurrencyKind.Paper, 10_000);
        Assert.IsFalse(ok);
        Assert.AreEqual(30, w.Paper);
    }

    [Test]
    public void Wallet_OnChanged_FiresOnAdd()
    {
        var w = new Wallet();
        int fired = 0;
        w.OnChanged += () => fired++;
        w.Add(CurrencyKind.Gold, 100);
        Assert.AreEqual(1, fired);
    }

    [Test]
    public void WalletData_JsonRoundTrip()
    {
        var w = new Wallet();
        w.Add(CurrencyKind.Gold, 500);
        var json   = JsonUtility.ToJson(w.Export());
        var loaded = JsonUtility.FromJson<WalletData>(json);
        var w2 = new Wallet();
        w2.LoadState(loaded);
        Assert.AreEqual(w.Gold,     w2.Gold);
        Assert.AreEqual(w.Paper,    w2.Paper);
        Assert.AreEqual(w.Focus,    w2.Focus);
        Assert.AreEqual(w.Fragment, w2.Fragment);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Roster
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Roster_Add_NewCharacter_ReturnsTrue()
    {
        var r = new Roster();
        Assert.IsTrue(r.Add("char_a"));
        Assert.IsTrue(r.Has("char_a"));
    }

    [Test]
    public void Roster_Add_Duplicate_ReturnsFalse_AndIncrementsDupes()
    {
        var r = new Roster();
        r.Add("char_a");
        Assert.IsFalse(r.Add("char_a"));
        Assert.AreEqual(1, r.Get("char_a").dupes);
    }

    [Test]
    public void RosterData_JsonRoundTrip()
    {
        var r = new Roster();
        r.Add("a"); r.Add("b"); r.Add("a"); // a 중복
        var json   = JsonUtility.ToJson(r.Export());
        var loaded = JsonUtility.FromJson<RosterData>(json);
        var r2 = new Roster();
        r2.LoadState(loaded);
        Assert.AreEqual(2, r2.Owned.Count);
        Assert.AreEqual(1, r2.Get("a").dupes);
        Assert.AreEqual(0, r2.Get("b").dupes);
    }

    // ════════════════════════════════════════════════════════════════════════
    // StatGrowth
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void StatGrowth_Level1_EqualsBase()
    {
        var def = MakeDef(Rarity.SR, new CharacterStats { hp = 1000, atk = 100 });
        var s   = StatGrowth.ComputeStats(def, 1);
        Assert.AreEqual(1000, s.hp);
        Assert.AreEqual(100,  s.atk);
    }

    [Test]
    public void StatGrowth_HigherLevel_IsStronger()
    {
        var def = MakeDef(Rarity.R, new CharacterStats { hp = 1000 });
        Assert.Greater(StatGrowth.ComputeStats(def, 10).hp,
                       StatGrowth.ComputeStats(def,  1).hp);
    }

    [Test]
    public void StatGrowth_LevelCaps_MatchDesign()
    {
        Assert.AreEqual(40, StatGrowth.LevelCap(Rarity.N));
        Assert.AreEqual(50, StatGrowth.LevelCap(Rarity.R));
        Assert.AreEqual(60, StatGrowth.LevelCap(Rarity.SR));
        Assert.AreEqual(70, StatGrowth.LevelCap(Rarity.SSR));
        Assert.AreEqual(80, StatGrowth.LevelCap(Rarity.UR));
    }

    // ════════════════════════════════════════════════════════════════════════
    // GachaService
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Gacha_Single_SpendsPaper()
    {
        var (svc, wallet, _, _, _) = MakeGachaSetup(seed: 42, paperBonus: 100);
        int before = wallet.Paper;
        svc.RollOne();
        Assert.AreEqual(before - GachaConfig.CostSingle, wallet.Paper);
    }

    [Test]
    public void Gacha_Single_InsufficientPaper_ReturnsNull()
    {
        var (svc, wallet, _, _, _) = MakeGachaSetup(seed: 42);
        // Paper = 30 → 모두 소진
        while (wallet.CanAfford(CurrencyKind.Paper, GachaConfig.CostSingle))
            svc.RollOne();
        Assert.IsNull(svc.RollOne());
    }

    [Test]
    public void Gacha_Ten_SpendsTenCost()
    {
        var (svc, wallet, _, _, _) = MakeGachaSetup(seed: 42, paperBonus: 200);
        int before = wallet.Paper;
        svc.RollTen();
        Assert.AreEqual(before - GachaConfig.CostTen, wallet.Paper);
    }

    [Test]
    public void Gacha_Ten_GuaranteesSrPlus_Always()
    {
        int violations = 0;
        for (int seed = 0; seed < 500; seed++)
        {
            var (svc, _, _, _, _) = MakeGachaSetup(seed: seed, paperBonus: 200);
            var results = svc.RollTen();
            if (results == null) { violations++; continue; }
            bool hasSr = System.Array.Exists(results, r => r.rarity >= Rarity.SR);
            if (!hasSr) violations++;
        }
        Assert.AreEqual(0, violations, $"10연 SR 미보장 발생: {violations}회");
    }

    [Test]
    public void Gacha_PityCap_ForcesSSROnPull50()
    {
        var (svc, wallet, _, state, _) = MakeGachaSetup(seed: 999, paperBonus: 10_000);
        // 천장 직전(49)으로 피티 설정
        state.LoadState(new GachaStateData { pityCounter = GachaConfig.PityCap - 1 });
        Assert.AreEqual(GachaConfig.PityCap - 1, state.PityCounter);

        var r = svc.RollOne();
        Assert.IsTrue(r.HasValue);
        Assert.GreaterOrEqual((int)r.Value.rarity, (int)Rarity.SSR);
        // 피티 리셋 확인
        Assert.AreEqual(0, state.PityCounter);
    }

    [Test]
    public void Gacha_Distribution_Approximate()
    {
        var (svc, wallet, _, _, _) = MakeGachaSetup(seed: 12345, paperBonus: 200_000);
        int[] counts = new int[5];
        int   total  = 10_000;

        for (int i = 0; i < total; i++)
        {
            var r = svc.RollOne();
            if (r.HasValue) counts[(int)r.Value.rarity]++;
        }

        float nPct  = counts[0] / (float)total * 100f;
        float rPct  = counts[1] / (float)total * 100f;
        float srPct = counts[2] / (float)total * 100f;

        Assert.That(nPct,  Is.EqualTo(GachaConfig.WeightN).Within(5f),  $"N 분포: {nPct:F1}%");
        Assert.That(rPct,  Is.EqualTo(GachaConfig.WeightR).Within(5f),  $"R 분포: {rPct:F1}%");
        Assert.That(srPct, Is.EqualTo(GachaConfig.WeightSR).Within(5f), $"SR 분포: {srPct:F1}%");
    }

    [Test]
    public void GachaStateData_JsonRoundTrip()
    {
        var s = new GachaState();
        s.IncrementPity(); s.IncrementPity();
        var json   = JsonUtility.ToJson(s.Export());
        var loaded = JsonUtility.FromJson<GachaStateData>(json);
        var s2 = new GachaState();
        s2.LoadState(loaded);
        Assert.AreEqual(s.PityCounter, s2.PityCounter);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 헬퍼
    // ════════════════════════════════════════════════════════════════════════

    static CharacterDef MakeDef(Rarity rarity, CharacterStats stats)
    {
        var def = ScriptableObject.CreateInstance<CharacterDef>();
        def.rarity    = rarity;
        def.baseStats = stats;
        return def;
    }

    static CharacterDef MakeDefWithContinent(Continent continent, string country)
    {
        var def = ScriptableObject.CreateInstance<CharacterDef>();
        def.id        = $"test_{continent}_{country}";
        def.continent = continent;
        def.country   = country;
        def.rarity    = Rarity.R;
        return def;
    }

    static CharacterDatabase MakeTestDb()
    {
        var db    = ScriptableObject.CreateInstance<CharacterDatabase>();
        var chars = new CharacterDef[5];
        var rars  = new[] { Rarity.N, Rarity.R, Rarity.SR, Rarity.SSR, Rarity.UR };
        for (int i = 0; i < 5; i++)
        {
            var c = ScriptableObject.CreateInstance<CharacterDef>();
            c.id = $"test_{rars[i]}"; c.nameKo = c.id; c.rarity = rars[i];
            chars[i] = c;
        }
        db.SetCharacters(chars);
        return db;
    }

    static (GachaService svc, Wallet wallet, Roster roster, GachaState state, CrystalWallet crystals)
        MakeGachaSetup(int seed, int paperBonus = 0)
    {
        var db       = MakeTestDb();
        var wallet   = new Wallet();
        if (paperBonus > 0) wallet.Add(CurrencyKind.Paper, paperBonus);
        var roster   = new Roster();
        var state    = new GachaState();
        var crystals = new CrystalWallet();
        var svc      = new GachaService(db, wallet, roster, state, crystals, new System.Random(seed));
        return (svc, wallet, roster, state, crystals);
    }

    // ════════════════════════════════════════════════════════════════════════
    // CrystalWallet
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void CrystalWallet_Add_Get_ReturnsCorrectAmount()
    {
        var cw = new CrystalWallet();
        cw.Add(CrystalKind.PrimeForce, 5);
        Assert.AreEqual(5, cw.Get(CrystalKind.PrimeForce));
        // 다른 종류는 영향 없음
        Assert.AreEqual(0, cw.Get(CrystalKind.Axioma));
    }

    [Test]
    public void CrystalWallet_TrySpend_SucceedsAndFails()
    {
        var cw = new CrystalWallet();
        cw.Add(CrystalKind.LifeCode, 10);
        Assert.IsTrue(cw.TrySpend(CrystalKind.LifeCode, 3));
        Assert.AreEqual(7, cw.Get(CrystalKind.LifeCode));
        Assert.IsFalse(cw.TrySpend(CrystalKind.LifeCode, 100));
        Assert.AreEqual(7, cw.Get(CrystalKind.LifeCode));
    }

    [Test]
    public void CrystalWalletData_JsonRoundTrip()
    {
        var cw = new CrystalWallet();
        cw.Add(CrystalKind.PrimeForce,     3);
        cw.Add(CrystalKind.ElementaCrystal, 7);
        cw.Add(CrystalKind.Axioma,          2);

        var json   = UnityEngine.JsonUtility.ToJson(cw.Export());
        var loaded = UnityEngine.JsonUtility.FromJson<CrystalWalletData>(json);
        var cw2    = new CrystalWallet();
        cw2.LoadState(loaded);

        Assert.AreEqual(3, cw2.Get(CrystalKind.PrimeForce));
        Assert.AreEqual(7, cw2.Get(CrystalKind.ElementaCrystal));
        Assert.AreEqual(2, cw2.Get(CrystalKind.Axioma));
        Assert.AreEqual(0, cw2.Get(CrystalKind.LifeCode));
    }

    // ════════════════════════════════════════════════════════════════════════
    // CrystalCatalog
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void CrystalCatalog_KindFor_MapsContinent()
    {
        Assert.AreEqual(CrystalKind.PrimeForce,
            CrystalCatalog.KindFor(MakeDefWithContinent(Continent.Physics, "")));

        Assert.AreEqual(CrystalKind.OceanPrime,
            CrystalCatalog.KindFor(MakeDefWithContinent(Continent.EarthSci, CrystalCatalog.CountryOcean)));

        Assert.IsNull(
            CrystalCatalog.KindFor(MakeDefWithContinent(Continent.Mesoria, "")));

        Assert.IsNull(
            CrystalCatalog.KindFor(null));
    }

    [Test]
    public void CrystalCatalog_EarthSci_FallbackIsMemoryOfStar()
    {
        // 알 수 없는 country → MemoryOfStar 폴백 (경고 로그 발생)
        var result = CrystalCatalog.KindFor(
            MakeDefWithContinent(Continent.EarthSci, "unknown-country"));
        Assert.AreEqual(CrystalKind.MemoryOfStar, result);
    }

    // ════════════════════════════════════════════════════════════════════════
    // GachaConfig — CrystalForRarity
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void GachaConfig_CrystalForRarity_IncreasesWithRarity()
    {
        Assert.Greater(GachaConfig.CrystalForRarity(Rarity.R),   GachaConfig.CrystalForRarity(Rarity.N));
        Assert.Greater(GachaConfig.CrystalForRarity(Rarity.SR),  GachaConfig.CrystalForRarity(Rarity.R));
        Assert.Greater(GachaConfig.CrystalForRarity(Rarity.SSR), GachaConfig.CrystalForRarity(Rarity.SR));
        Assert.Greater(GachaConfig.CrystalForRarity(Rarity.UR),  GachaConfig.CrystalForRarity(Rarity.SSR));
    }

    // ════════════════════════════════════════════════════════════════════════
    // GachaService — 중복 결정 지급
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Gacha_Duplicate_AwardsCrystal()
    {
        var (svc, wallet, _, _, crystals) = MakeGachaSetup(seed: 1, paperBonus: 10_000);

        // 같은 캐릭터를 두 번 추첨 → 두 번째는 반드시 중복
        // db는 등급별 1개씩이므로 같은 등급이 두 번 나오면 반드시 중복
        // R 캐릭터(inertia)가 중복날 때까지 뽑는다 (시드 고정 + 많은 논문)
        GachaResult? dupResult = null;
        for (int i = 0; i < 200; i++)
        {
            var r = svc.RollOne();
            if (r.HasValue && !r.Value.isNew && r.Value.crystal.HasValue)
            {
                dupResult = r;
                break;
            }
        }

        Assert.IsTrue(dupResult.HasValue, "중복 결과가 발생하지 않았습니다 (시드 조정 필요).");
        Assert.IsTrue(dupResult.Value.crystal.HasValue, "중복임에도 crystal이 null입니다.");
        Assert.Greater(dupResult.Value.crystalAmount, 0, "crystalAmount가 0 이하입니다.");

        // 결정 지갑에도 반영됐어야 함
        Assert.Greater(crystals.Get(dupResult.Value.crystal.Value), 0,
            "CrystalWallet에 결정이 적립되지 않았습니다.");
    }
}
