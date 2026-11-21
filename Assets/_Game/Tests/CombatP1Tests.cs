using NUnit.Framework;
using UnityEngine;

/// <summary>설계도 22 P1 — 슬롯 해석·기본기·파티 5명·전환 쿨타임·대기 캐릭터 틱 테스트 (MSRPG.Tests 어셈블리, PlayMode 플랫폼으로 실행).</summary>
public class CombatP1Tests
{
    // ════════════════════════════════════════════════════════════════════════
    // 헬퍼
    // ════════════════════════════════════════════════════════════════════════

    static SkillDef MakeSkill(string id, int slot = -1, string formReq = null,
                              int mp = 0, float cd = 1f, float range = 3f,
                              float halfAngle = 60f, float mult = 1f)
    {
        var s = ScriptableObject.CreateInstance<SkillDef>();
        s.id               = id;
        s.nameKo           = id;
        s.slot             = slot;
        s.formReq          = formReq;
        s.effectKind       = SkillEffectKind.Strike;
        s.mpCost           = mp;
        s.cooldown         = cd;
        s.range            = range;
        s.halfAngle        = halfAngle;
        s.damageMultiplier = mult;
        return s;
    }

    static CharacterDef MakeDef(SkillDef[] skills = null)
    {
        var def = ScriptableObject.CreateInstance<CharacterDef>();
        def.id        = "p1_" + Random.Range(0, 99999);
        def.nameKo    = "테스트";
        def.rarity    = Rarity.N;
        def.continent = Continent.Physics;
        def.skills    = skills;
        def.baseStats = new CharacterStats { hp = 200, atk = 50, def = 10, spd = 10, mp = 100 };
        return def;
    }

    static CombatCharacter MakeChar(SkillDef[] skills = null)
        => new CombatCharacter(MakeDef(skills), new OwnedCharacter { id = "test", level = 1, exp = 0 });

    // ════════════════════════════════════════════════════════════════════════
    // SkillSlots
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void SkillSlots_KeyLabel_MapsSlotsToKeys()
    {
        Assert.AreEqual("Q", SkillSlots.KeyLabel(0));
        Assert.AreEqual("R", SkillSlots.KeyLabel(1));
        Assert.AreEqual("T", SkillSlots.KeyLabel(2));
        Assert.AreEqual("F", SkillSlots.KeyLabel(3));
        Assert.AreEqual("V", SkillSlots.KeyLabel(4));
        Assert.AreEqual("G", SkillSlots.KeyLabel(5));
        Assert.AreEqual("B", SkillSlots.KeyLabel(6));
    }

    [Test]
    public void SkillSlots_KeyLabel_OutOfRange_ReturnsQuestionMark()
    {
        Assert.AreEqual("?", SkillSlots.KeyLabel(-1));
        Assert.AreEqual("?", SkillSlots.KeyLabel(7));
    }

    [Test]
    public void SkillSlots_LegacySlot_IsIndexPlusOne()
    {
        Assert.AreEqual(1, SkillSlots.LegacySlot(0));
        Assert.AreEqual(6, SkillSlots.LegacySlot(5));
    }

    [Test]
    public void SkillDef_Defaults_SlotIsLegacyMinusOne_FormReqEmpty()
    {
        var s = ScriptableObject.CreateInstance<SkillDef>();
        Assert.AreEqual(-1, s.slot);
        Assert.IsTrue(string.IsNullOrEmpty(s.formReq));
    }

    // ════════════════════════════════════════════════════════════════════════
    // CombatCharacter — 슬롯 해석
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void SlotOf_Legacy_IsIndexPlusOne()
    {
        var ch = MakeChar(new[] { MakeSkill("a"), MakeSkill("b") });
        Assert.AreEqual(1, ch.SlotOf(0));
        Assert.AreEqual(2, ch.SlotOf(1));
    }

    [Test]
    public void SlotOf_ExplicitSlot_IsHonored()
    {
        var ch = MakeChar(new[] { MakeSkill("basic", slot: 0), MakeSkill("t", slot: 2) });
        Assert.AreEqual(0, ch.SlotOf(0));
        Assert.AreEqual(2, ch.SlotOf(1));
    }

    [Test]
    public void SlotOf_NoSkill_ReturnsMinusOne()
    {
        var ch = MakeChar(new[] { MakeSkill("a") });
        Assert.AreEqual(-1, ch.SlotOf(5));
        Assert.AreEqual(-1, ch.SlotOf(-1));
    }

    [Test]
    public void IndexForSlot_Legacy_ResolvesByIndexPlusOne()
    {
        var ch = MakeChar(new[] { MakeSkill("a"), MakeSkill("b") });
        Assert.AreEqual(0,  ch.IndexForSlot(1));
        Assert.AreEqual(1,  ch.IndexForSlot(2));
        Assert.AreEqual(-1, ch.IndexForSlot(0), "레거시 캐릭터는 슬롯 0(기본기)이 비어 있다");
        Assert.AreEqual(-1, ch.IndexForSlot(3));
    }

    [Test]
    public void IndexForSlot_ExplicitSlots_ResolvesBySlot()
    {
        var ch = MakeChar(new[] { MakeSkill("basic", slot: 0), MakeSkill("t", slot: 2) });
        Assert.AreEqual(0,  ch.IndexForSlot(0));
        Assert.AreEqual(1,  ch.IndexForSlot(2));
        Assert.AreEqual(-1, ch.IndexForSlot(1));
    }

    [Test]
    public void IndexForSlot_FormReqMismatch_IsSkipped()
    {
        // P1: CurrentFormId는 항상 "" → formReq가 있는 스킬은 해석되지 않는다
        var ch = MakeChar(new[] { MakeSkill("p", slot: 1, formReq: "Particle"), MakeSkill("any", slot: 1) });
        Assert.AreEqual(1, ch.IndexForSlot(1), "formReq가 맞지 않는 스킬은 건너뛰고 다음 스킬을 찾는다");
    }

    [Test]
    public void IndexForSlot_NullSkillsArray_ReturnsMinusOne()
    {
        var ch = MakeChar(null);
        Assert.AreEqual(-1, ch.IndexForSlot(1));
    }

    // ════════════════════════════════════════════════════════════════════════
    // BasicAttack — 기본기(슬롯 0) 수치 해석
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void BasicAttack_NoSlot0Skill_UsesDefaults()
    {
        var ch   = MakeChar(new[] { MakeSkill("a") });   // 레거시: 슬롯 1
        var spec = BasicAttack.Resolve(ch, 2.2f, 60f, 0.5f);
        Assert.AreEqual(2.2f, spec.range);
        Assert.AreEqual(60f,  spec.halfAngle);
        Assert.AreEqual(1f,   spec.damageMultiplier);
        Assert.AreEqual(0.5f, spec.cooldown);
    }

    [Test]
    public void BasicAttack_Slot0Skill_UsesSkillValues()
    {
        var basic = MakeSkill("basic", slot: 0, cd: 0.8f, range: 3.5f, halfAngle: 40f, mult: 1.2f);
        var ch    = MakeChar(new[] { basic });
        var spec  = BasicAttack.Resolve(ch, 2.2f, 60f, 0.5f);
        Assert.AreEqual(3.5f, spec.range);
        Assert.AreEqual(40f,  spec.halfAngle);
        Assert.AreEqual(1.2f, spec.damageMultiplier);
        Assert.AreEqual(0.8f, spec.cooldown);
    }

    [Test]
    public void BasicAttack_NullCharacter_UsesDefaults()
    {
        var spec = BasicAttack.Resolve(null, 2.2f, 60f, 0.5f);
        Assert.AreEqual(2.2f, spec.range);
        Assert.AreEqual(1f,   spec.damageMultiplier);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Party — 5명·전환 쿨타임·대기 캐릭터 틱
    // ════════════════════════════════════════════════════════════════════════

    static Party MakeParty(int count)
    {
        var members = new CombatCharacter[count];
        for (int i = 0; i < count; i++) members[i] = MakeChar();
        return new Party(members);
    }

    [Test]
    public void Party_MaxSize_IsFive()
    {
        Assert.AreEqual(5, Party.MaxSize);
    }

    [Test]
    public void Party_Constructor_KeepsAtMostFiveMembers()
    {
        var party = MakeParty(7);
        Assert.AreEqual(5, party.Members.Count);
    }

    [Test]
    public void Party_SwitchTo_FifthMember_Works()
    {
        var party = MakeParty(5);
        Assert.IsTrue(party.SwitchTo(4));
        Assert.AreEqual(4, party.ActiveIndex);
    }

    [Test]
    public void Party_SwitchTo_StartsOneSecondCooldown()
    {
        var party = MakeParty(3);
        Assert.IsTrue(party.SwitchTo(1));
        Assert.AreEqual(Party.SwitchCooldown, party.SwitchCooldownRemaining, 0.0001f);
        Assert.IsFalse(party.SwitchTo(2), "쿨타임 중에는 수동 전환 불가");
        Assert.AreEqual(1, party.ActiveIndex);
    }

    [Test]
    public void Party_SwitchTo_AfterCooldownElapsed_Works()
    {
        var party = MakeParty(3);
        party.SwitchTo(1);
        party.Tick(1f);
        Assert.AreEqual(0f, party.SwitchCooldownRemaining, 0.0001f);
        Assert.IsTrue(party.SwitchTo(2));
    }

    [Test]
    public void Party_AutoSwitchToNext_IgnoresSwitchCooldown()
    {
        var party = MakeParty(3);
        party.SwitchTo(1);                                   // 쿨타임 시작
        party.Members[1].Damage(party.Members[1].MaxHp);     // 활성 기절
        Assert.IsTrue(party.AutoSwitchToNext(), "기절로 인한 자동 교체는 쿨타임과 무관");
        Assert.AreEqual(2, party.ActiveIndex);
    }

    [Test]
    public void Party_AutoSwitchToNext_DoesNotStartCooldown()
    {
        var party = MakeParty(3);
        party.Members[0].Damage(party.Members[0].MaxHp);
        party.AutoSwitchToNext();
        Assert.AreEqual(0f, party.SwitchCooldownRemaining, 0.0001f);
    }

    [Test]
    public void Party_RestoreAll_ResetsSwitchCooldown()
    {
        var party = MakeParty(3);
        party.SwitchTo(1);
        party.RestoreAll();
        Assert.AreEqual(0f, party.SwitchCooldownRemaining, 0.0001f);
    }

    [Test]
    public void Party_Tick_AdvancesBenchMemberCooldowns()
    {
        var skill  = MakeSkill("a", cd: 3f);
        var active = MakeChar();
        var bench  = MakeChar(new[] { skill });
        var party  = new Party(new[] { active, bench });

        bench.CastSkill(0);                                   // 대기 멤버의 쿨타임 시작(3초)
        Assert.AreEqual(3f, bench.CooldownRemaining(0), 0.0001f);

        party.Tick(2f);
        Assert.AreEqual(1f, bench.CooldownRemaining(0), 0.0001f, "대기 중에도 쿨타임이 흐른다");
    }

    [Test]
    public void Party_RegenMpTick_RecoversBenchMp_ButNotDownedMp()
    {
        var active = MakeChar();
        var bench  = MakeChar();
        var downed = MakeChar();
        var party  = new Party(new[] { active, bench, downed });

        bench.UseMp(50);
        downed.UseMp(50);
        downed.Damage(downed.MaxHp);                          // 기절
        int benchBefore  = bench.Mp;
        int downedBefore = downed.Mp;

        party.RegenMpTick(0.03f, 0.5f);

        Assert.Greater(bench.Mp, benchBefore, "대기 멤버도 MP 자연회복");
        Assert.AreEqual(downedBefore, downed.Mp, "기절한 멤버는 회복하지 않는다");
    }
}
