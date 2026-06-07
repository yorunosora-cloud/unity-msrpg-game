using NUnit.Framework;
using UnityEngine;

/// <summary>CombatCharacter · Party EditMode 테스트.</summary>
public class PartyTests
{
    // ════════════════════════════════════════════════════════════════════════
    // 헬퍼
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>테스트용 CharacterDef ScriptableObject 생성.</summary>
    static CharacterDef MakeDef(Rarity rarity = Rarity.N, Continent continent = Continent.Physics)
    {
        var def = ScriptableObject.CreateInstance<CharacterDef>();
        def.id        = "test_" + Random.Range(0, 9999);
        def.nameKo    = "테스트";
        def.rarity    = rarity;
        def.continent = continent;
        def.baseStats = new CharacterStats { hp = 100, atk = 10, def = 5, spd = 5, mp = 50 };
        return def;
    }

    static OwnedCharacter MakeOwned(int level = 1) =>
        new OwnedCharacter { id = "test", level = level, exp = 0 };

    static CombatCharacter MakeCharacter(Rarity rarity = Rarity.N, int level = 1)
        => new CombatCharacter(MakeDef(rarity), MakeOwned(level));

    // ════════════════════════════════════════════════════════════════════════
    // CombatCharacter — 스탯
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void CombatCharacter_Stats_ReflectStatGrowth()
    {
        var def   = MakeDef();
        var owned = MakeOwned(level: 3);
        var ch    = new CombatCharacter(def, owned);

        var expected = StatGrowth.ComputeStats(def, 3);
        Assert.AreEqual(expected.atk, ch.Atk);
        Assert.AreEqual(expected.def, ch.Def);
        Assert.AreEqual(expected.hp,  ch.MaxHp);
        Assert.AreEqual(expected.mp,  ch.MaxMp);
    }

    [Test]
    public void CombatCharacter_InitialHp_EqualMaxHp()
    {
        var ch = MakeCharacter();
        Assert.AreEqual(ch.MaxHp, ch.Hp);
    }

    // ════════════════════════════════════════════════════════════════════════
    // CombatCharacter — HP/MP 조작
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void CombatCharacter_Damage_ReducesHp()
    {
        var ch = MakeCharacter();
        ch.Damage(20);
        Assert.AreEqual(ch.MaxHp - 20, ch.Hp);
    }

    [Test]
    public void CombatCharacter_Damage_ClampsToZero()
    {
        var ch = MakeCharacter();
        ch.Damage(ch.MaxHp + 9999);
        Assert.AreEqual(0, ch.Hp);
    }

    [Test]
    public void CombatCharacter_IsDowned_WhenHpZero()
    {
        var ch = MakeCharacter();
        ch.Damage(ch.MaxHp);
        Assert.IsTrue(ch.IsDowned);
    }

    [Test]
    public void CombatCharacter_NotDowned_WhenHpAboveZero()
    {
        var ch = MakeCharacter();
        ch.Damage(ch.MaxHp - 1);
        Assert.IsFalse(ch.IsDowned);
    }

    [Test]
    public void CombatCharacter_Heal_ClampsToMaxHp()
    {
        var ch = MakeCharacter();
        ch.Damage(10);
        ch.Heal(9999);
        Assert.AreEqual(ch.MaxHp, ch.Hp);
    }

    [Test]
    public void CombatCharacter_UseMp_DeductsAmount()
    {
        var ch = MakeCharacter();
        int before = ch.Mp;
        bool ok = ch.UseMp(10);
        Assert.IsTrue(ok);
        Assert.AreEqual(before - 10, ch.Mp);
    }

    [Test]
    public void CombatCharacter_UseMp_FailsWhenInsufficient()
    {
        var ch = MakeCharacter();
        bool ok = ch.UseMp(ch.Mp + 1);
        Assert.IsFalse(ok);
    }

    [Test]
    public void CombatCharacter_RestoreFull_RecovershpAndMp()
    {
        var ch = MakeCharacter();
        ch.Damage(50);
        ch.UseMp(20);
        ch.RestoreFull();
        Assert.AreEqual(ch.MaxHp, ch.Hp);
        Assert.AreEqual(ch.MaxMp, ch.Mp);
    }

    // ════════════════════════════════════════════════════════════════════════
    // CombatCharacter — EXP / 레벨업
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void CombatCharacter_GainExp_LevelsUp()
    {
        var ch = MakeCharacter();
        Assert.AreEqual(1, ch.Level);

        ch.GainExp(ch.NextExp); // 임계값 정확히 지급 → 레벨 2
        Assert.AreEqual(2, ch.Level);
    }

    [Test]
    public void CombatCharacter_GainExp_RestoresHpOnLevelUp()
    {
        var ch = MakeCharacter();
        ch.Damage(30);
        ch.GainExp(ch.NextExp);
        // 레벨업 시 HP 풀 회복
        Assert.AreEqual(ch.MaxHp, ch.Hp);
    }

    [Test]
    public void CombatCharacter_GainExp_DoesNotExceedLevelCap()
    {
        // N 등급 상한 = 40
        var def   = MakeDef(Rarity.N);
        var owned = MakeOwned(level: 40);
        var ch    = new CombatCharacter(def, owned);

        ch.GainExp(999999);
        Assert.AreEqual(40, ch.Level);
    }

    [Test]
    public void CombatCharacter_OnChanged_FiredOnDamage()
    {
        var ch    = MakeCharacter();
        string last = null;
        ch.OnChanged += r => last = r;

        ch.Damage(5);
        Assert.AreEqual("hp", last);
    }

    [Test]
    public void CombatCharacter_OnChanged_FiredWithLevelup()
    {
        var ch    = MakeCharacter();
        string last = null;
        ch.OnChanged += r => last = r;

        ch.GainExp(ch.NextExp);
        Assert.AreEqual("levelup", last);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Party — 전환·기절·전멸
    // ════════════════════════════════════════════════════════════════════════

    static Party MakeParty(int count = 3)
    {
        var members = new CombatCharacter[count];
        for (int i = 0; i < count; i++) members[i] = MakeCharacter();
        return new Party(members);
    }

    [Test]
    public void Party_Initial_ActiveIsFirst()
    {
        var party = MakeParty();
        Assert.AreEqual(0, party.ActiveIndex);
        Assert.AreSame(party.Members[0], party.Active);
    }

    [Test]
    public void Party_SwitchTo_ChangesActiveIndex()
    {
        var party = MakeParty();
        bool ok = party.SwitchTo(1);
        Assert.IsTrue(ok);
        Assert.AreEqual(1, party.ActiveIndex);
    }

    [Test]
    public void Party_SwitchTo_RefusesOutOfRange()
    {
        var party = MakeParty(2);
        bool ok = party.SwitchTo(5);
        Assert.IsFalse(ok);
    }

    [Test]
    public void Party_SwitchTo_RefusesDownedMember()
    {
        var party = MakeParty();
        party.Members[1].Damage(party.Members[1].MaxHp); // 기절
        bool ok = party.SwitchTo(1);
        Assert.IsFalse(ok);
    }

    [Test]
    public void Party_SwitchTo_RefusesSameIndex()
    {
        var party = MakeParty();
        bool ok = party.SwitchTo(0); // 이미 활성
        Assert.IsFalse(ok);
    }

    [Test]
    public void Party_AutoSwitchToNext_SkipsDownedMembers()
    {
        var party = MakeParty(3);
        party.Members[1].Damage(party.Members[1].MaxHp); // 1번 기절
        bool ok = party.AutoSwitchToNext();
        Assert.IsTrue(ok);
        Assert.AreEqual(2, party.ActiveIndex); // 1 건너뛰고 2로
    }

    [Test]
    public void Party_AutoSwitchToNext_ReturnsFalseWhenAllDowned()
    {
        var party = MakeParty(2);
        party.Members[0].Damage(party.Members[0].MaxHp);
        party.Members[1].Damage(party.Members[1].MaxHp);
        bool ok = party.AutoSwitchToNext();
        Assert.IsFalse(ok);
    }

    [Test]
    public void Party_AllDowned_TrueWhenEverybodyDowned()
    {
        var party = MakeParty(2);
        party.Members[0].Damage(party.Members[0].MaxHp);
        party.Members[1].Damage(party.Members[1].MaxHp);
        Assert.IsTrue(party.AllDowned);
    }

    [Test]
    public void Party_AllDowned_FalseWhenOneSurvives()
    {
        var party = MakeParty(3);
        party.Members[0].Damage(party.Members[0].MaxHp);
        party.Members[2].Damage(party.Members[2].MaxHp);
        Assert.IsFalse(party.AllDowned);
    }

    [Test]
    public void Party_RestoreAll_RecovershpAndResetsActiveIndex()
    {
        var party = MakeParty(3);
        party.SwitchTo(2);
        party.Members[0].Damage(party.Members[0].MaxHp);
        party.Members[1].Damage(party.Members[1].MaxHp);

        party.RestoreAll();

        Assert.AreEqual(0, party.ActiveIndex);
        Assert.IsFalse(party.AllDowned);
        foreach (var m in party.Members)
            Assert.AreEqual(m.MaxHp, m.Hp);
    }

    [Test]
    public void Party_OnActiveChanged_FiredOnSwitch()
    {
        var party   = MakeParty();
        int fired   = 0;
        party.OnActiveChanged += () => fired++;
        party.SwitchTo(1);
        Assert.AreEqual(1, fired);
    }
}
