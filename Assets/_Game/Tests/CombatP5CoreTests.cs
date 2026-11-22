using NUnit.Framework;
using UnityEngine;

/// <summary>설계도 18-A — Stage A 순수 코어: TagRule · TagRuleTable · PrecisionKillGrader · SkillUnlockCondition · DerivedQuantity.</summary>
public class CombatP5CoreTests
{
    static TagRule Rule(string id, string attr, bool high, float baseMult, float coeff, float refValue, float cap)
        => new TagRule { tagId = id, displayKo = id, targetAttr = attr, directionHigh = high,
                         baseMult = baseMult, coeffPerUnit = coeff, refValue = refValue, capMult = cap };

    // ════════════════════════════════════════════════════════════════════════
    // TagRule.Evaluate
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Tag_High_AtRef_IsBase()
    {
        var r = Rule("고온", "temp", true, 1f, 0.001f, 300f, 2f);
        Assert.AreEqual(1f, r.Evaluate(new AttributeProfile { temp = 300 }), 0.0001f);
    }

    [Test]
    public void Tag_High_AboveRef_ScalesLinearly()
    {
        var r = Rule("고온", "temp", true, 1f, 0.001f, 300f, 2f);
        Assert.AreEqual(1.6f, r.Evaluate(new AttributeProfile { temp = 900 }), 0.0001f);
    }

    [Test]
    public void Tag_High_BelowRef_ClampsToBase()
    {
        var r = Rule("고온", "temp", true, 1f, 0.001f, 300f, 2f);
        Assert.AreEqual(1f, r.Evaluate(new AttributeProfile { temp = 100 }), 0.0001f, "기본 배율 아래로 내려가지 않음");
    }

    [Test]
    public void Tag_High_HugeValue_ClampsToCap()
    {
        var r = Rule("고온", "temp", true, 1f, 0.001f, 300f, 2f);
        Assert.AreEqual(2f, r.Evaluate(new AttributeProfile { temp = 100000 }), 0.0001f);
    }

    [Test]
    public void Tag_Low_BelowRef_ScalesUp_AboveRef_ClampsToBase()
    {
        var r = Rule("저온", "temp", false, 1f, 0.002f, 300f, 1.8f);
        Assert.AreEqual(1.4f, r.Evaluate(new AttributeProfile { temp = 100 }), 0.0001f);
        Assert.AreEqual(1f,   r.Evaluate(new AttributeProfile { temp = 400 }), 0.0001f);
        Assert.AreEqual(1.8f, r.Evaluate(new AttributeProfile { temp = -300 }), 0.0001f, "상한 1.8");
    }

    [Test]
    public void Tag_ReadsTheRightAttribute()
    {
        var a = new AttributeProfile { mass = 500, velocity = 90, temp = 10, charge = -20 };
        Assert.AreEqual(1f + 0.005f * 400f, Rule("m", "mass", true, 1f, 0.005f, 100f, 9f).Evaluate(a), 0.0001f);
        Assert.AreEqual(1f + 0.01f * 40f,   Rule("v", "velocity", true, 1f, 0.01f, 50f, 9f).Evaluate(a), 0.0001f);
        Assert.AreEqual(1f + 0.01f * 20f,   Rule("c", "charge", false, 1f, 0.01f, 0f, 9f).Evaluate(a), 0.0001f, "음전하: 0 − (−20)");
    }

    [Test]
    public void Tag_UnknownAttr_ReadsZero()
    {
        var r = Rule("x", "nope", true, 1f, 0.01f, 0f, 2f);
        Assert.AreEqual(1f, r.Evaluate(new AttributeProfile { mass = 999 }), 0.0001f);
    }

    [Test]
    public void Tag_FormulaKo_ShowsRealCoefficients_BothDirections()
    {
        var hi = Rule("고온", "temp", true, 1f, 0.001f, 300f, 2f);
        var lo = Rule("저온", "temp", false, 1f, 0.002f, 300f, 1.8f);
        string h = hi.FormulaKo();
        string l = lo.FormulaKo();
        StringAssert.Contains("0.001", h);
        StringAssert.Contains("대상 온도 − 300", h);
        StringAssert.Contains("2", h);
        StringAssert.Contains("300 − 대상 온도", l, "low는 기준값에서 대상값을 뺀다");
        StringAssert.Contains("1.8", l);
    }

    [Test]
    public void Tag_LabelKo_MapsAttributes()
    {
        Assert.AreEqual("질량", TagRule.LabelKo("mass"));
        Assert.AreEqual("속도", TagRule.LabelKo("velocity"));
        Assert.AreEqual("온도", TagRule.LabelKo("temp"));
        Assert.AreEqual("전하", TagRule.LabelKo("charge"));
    }

    // ════════════════════════════════════════════════════════════════════════
    // TagRuleTable
    // ════════════════════════════════════════════════════════════════════════

    [TearDown]
    public void TearDown() => TagRuleTable.ResetForTests();

    [Test]
    public void Table_DefaultPhysicsRules_HasEightTags()
    {
        var rules = TagRuleTable.DefaultPhysicsRules();
        Assert.AreEqual(8, rules.Length);
        foreach (var id in new[] { "고온", "저온", "고속", "저속", "고질량", "저질량", "양전하", "음전하" })
            Assert.IsNotNull(System.Array.Find(rules, r => r.tagId == id), id);
    }

    [Test]
    public void Table_DefaultHighTemp_MatchesDocValues()
    {
        var hot = System.Array.Find(TagRuleTable.DefaultPhysicsRules(), r => r.tagId == "고온");
        Assert.AreEqual(1.6f, hot.Evaluate(new AttributeProfile { temp = 900 }), 0.0001f);
    }

    [Test]
    public void Table_Find_UnknownReturnsNull()
    {
        TagRuleTable.SetForTests(new[] { Rule("a", "mass", true, 1f, 0.1f, 0f, 2f) });
        Assert.IsNotNull(TagRuleTable.Current.Find("a"));
        Assert.IsNull(TagRuleTable.Current.Find("zzz"));
    }

    [Test]
    public void Table_Current_FallsBackToBuiltInPhysicsWhenNoAsset()
    {
        TagRuleTable.ResetForTests();
        Assert.IsNotNull(TagRuleTable.Current.Find("고온"), "에셋이 없어도 기본 물리 태그를 쓴다(임포터 이전)");
    }

    [Test]
    public void Table_Multiplier_MultipliesTagsAndIgnoresUnknown()
    {
        TagRuleTable.SetForTests(new[]
        {
            Rule("a", "temp", true, 1f, 0.001f, 300f, 2f),
            Rule("b", "mass", true, 1f, 0.01f, 0f, 3f),
        });
        var attrs = new AttributeProfile { temp = 800, mass = 20 };      // a=1.5, b=1.2
        Assert.AreEqual(1.8f, TagRuleTable.Multiplier(new[] { "a", "b", "없는태그" }, attrs), 0.0001f);
        Assert.AreEqual(1f,   TagRuleTable.Multiplier(null, attrs), 0.0001f);
        Assert.AreEqual(1f,   TagRuleTable.Multiplier(new string[0], attrs), 0.0001f);
    }

    // ════════════════════════════════════════════════════════════════════════
    // PrecisionKillGrader
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Grade_ExactKill_IsPerfect()
        => Assert.AreEqual(2.0f, PrecisionKillGrader.Grade(100, 100), 0.0001f);

    [Test]
    public void Grade_BoundariesAtTenAndThirtyPercent()
    {
        Assert.AreEqual(1.5f, PrecisionKillGrader.Grade(100, 110), 0.0001f, "오버킬 10% = 우수");
        Assert.AreEqual(1.2f, PrecisionKillGrader.Grade(100, 111), 0.0001f);
        Assert.AreEqual(1.2f, PrecisionKillGrader.Grade(100, 130), 0.0001f, "오버킬 30% = 양호");
        Assert.AreEqual(1.0f, PrecisionKillGrader.Grade(100, 131), 0.0001f);
        Assert.AreEqual(1.0f, PrecisionKillGrader.Grade(100, 500), 0.0001f);
    }

    [Test]
    public void Grade_NonPositiveHp_IsNormal()
        => Assert.AreEqual(1.0f, PrecisionKillGrader.Grade(0, 5), 0.0001f);

    // ════════════════════════════════════════════════════════════════════════
    // SkillUnlockCondition
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Unlock_Empty_IsAlways()
        => Assert.AreEqual(SkillUnlockConditionType.Always, SkillUnlockCondition.Parse("").type);

    [Test]
    public void Unlock_ParsesAllKnownTypes()
    {
        var d = SkillUnlockCondition.Parse("DamageTakenPercent:0.5");
        Assert.AreEqual(SkillUnlockConditionType.DamageTakenPercent, d.type);
        Assert.AreEqual(0.5f, d.numericParam, 0.0001f);

        var p = SkillUnlockCondition.Parse("PromotionRankAtLeast:3");
        Assert.AreEqual(SkillUnlockConditionType.PromotionRankAtLeast, p.type);
        Assert.AreEqual(3f, p.numericParam, 0.0001f);

        var s = SkillUnlockCondition.Parse("StoryFlag:quantum_continent_unlocked");
        Assert.AreEqual(SkillUnlockConditionType.StoryFlag, s.type);
        Assert.AreEqual("quantum_continent_unlocked", s.stringParam);
    }

    [Test]
    public void Unlock_TyposAndMalformed_FallBackToAlways()
    {
        Assert.AreEqual(SkillUnlockConditionType.Always, SkillUnlockCondition.Parse("Foo:1").type);
        Assert.AreEqual(SkillUnlockConditionType.Always, SkillUnlockCondition.Parse("DamageTakenPercent:abc").type);
        Assert.AreEqual(SkillUnlockConditionType.Always, SkillUnlockCondition.Parse("StoryFlag").type);
        Assert.AreEqual(SkillUnlockConditionType.Always, SkillUnlockCondition.Parse(null).type);
    }

    // ════════════════════════════════════════════════════════════════════════
    // DerivedQuantity
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Derived_MomentumAndKineticEnergy()
    {
        var a = new AttributeProfile { mass = 10, velocity = 3 };
        Assert.AreEqual(30f, DerivedQuantity.Momentum(a), 0.0001f);
        Assert.AreEqual(45f, DerivedQuantity.KineticEnergy(a), 0.0001f);
    }
}
