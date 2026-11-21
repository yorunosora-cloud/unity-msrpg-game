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
}
