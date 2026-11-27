using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>23-A 단계 C — skill_counters.csv 및 효과·수정자 신규 열의 매핑·검증.</summary>
public class CounterCsvTests
{
    const string Cat = CharacterCsvBuilder.Catalog;
    const string Skl = CharacterCsvBuilder.Skills;
    const string Eff = CharacterCsvBuilder.Effects;
    const string Pas = CharacterCsvBuilder.Passives;
    const string Mod = CharacterCsvBuilder.Modifiers;
    const string Cnt = CharacterCsvBuilder.Counters;

    static ImportResult Build(params (string sheet, string csv)[] sheets)
    {
        var d = new Dictionary<string, string>();
        foreach (var s in sheets) d[s.sheet] = s.csv;
        return CharacterCsvBuilder.Build(d);
    }

    static readonly (string, string) OneChar = (Cat, "id,nameKo,continent,rarity\nhero_r,영웅,Physics,R\nother_r,다른,Physics,R\n");
    static readonly (string, string) Skills = (Skl,
        "skillId,ownerId,slot,nameKo,skillType,startUnlocked,effectKind,milestoneLevel\n" +
        "hero_q,hero_r,0,기본,Active,true,Strike,3\n" +
        "hero_p,hero_r,1,패시브,Passive,false,Strike,3\n" +
        "other_q,other_r,0,기본,Active,true,Strike,3\n");
    static readonly (string, string) PassiveRow = (Pas, "skillId,trigger\nhero_p,None\n");

    const string CntHeader = "counterId,ownerId,maxStacks,milestoneMaxStacks,decay,decaySeconds,gainOn,resetOn,milestoneSkillId,gainSkillId\n";

    static void NoErrors(ImportResult r) => Assert.IsFalse(r.report.HasErrors, string.Join("\n", r.report.Errors));
    static void HasError(ImportResult r, string contains)
    {
        Assert.IsTrue(r.report.HasErrors, "오류가 있어야 한다");
        Assert.IsTrue(r.report.Errors.Any(e => e.Contains(contains)), "기대 문구 '" + contains + "' — 실제:\n" + string.Join("\n", r.report.Errors));
    }

    // ── skill_counters.csv ───────────────────────────────────────────────

    [Test]
    public void Counters_MapFlagsDecayAndMilestone()
    {
        var r = Build(OneChar, Skills, PassiveRow, (Cnt, CntHeader +
            "heat,hero_r,5,7,ResetAfterIdle,5,OnHitTaken;OnSkillCast,,hero_p\n" +
            "combo,hero_r,5,6,None,0,OnHit,OnHitTaken,hero_q\n"));
        NoErrors(r);
        var c = r.characters.Single(x => x.id == "hero_r");
        Assert.AreEqual(2, c.counters.Length);
        var heat = c.counters.Single(x => x.id == "heat");
        Assert.AreEqual((5, 7, CounterDecay.ResetAfterIdle, 5f, "hero_p"), (heat.maxStacks, heat.milestoneMaxStacks, heat.decay, heat.decaySeconds, heat.milestoneSkillId));
        Assert.AreEqual(CounterTrigger.OnHitTaken | CounterTrigger.OnSkillCast, heat.gainOn);
        Assert.AreEqual(CounterTrigger.OnHitTaken, c.counters.Single(x => x.id == "combo").resetOn);
        Assert.IsTrue(r.characters.Single(x => x.id == "other_r").counters == null || r.characters.Single(x => x.id == "other_r").counters.Length == 0);
    }

    [Test]
    public void Counters_GainSkillId_MapsAndMustBelongToTheOwner()
    {
        var ok = Build(OneChar, Skills, PassiveRow, (Cnt, CntHeader + "run,hero_r,5,,None,0,OnHit,OnHitTaken,,hero_q\n"));
        NoErrors(ok);
        Assert.AreEqual("hero_q", ok.characters.Single(x => x.id == "hero_r").counters[0].gainSkillId);
        HasError(Build(OneChar, Skills, PassiveRow, (Cnt, CntHeader + "run,hero_r,5,,None,0,OnHit,,,other_q\n")), "other_q");
    }

    [Test]
    public void Counters_UnknownTriggerName_IsAnError()
        => HasError(Build(OneChar, Skills, PassiveRow, (Cnt, CntHeader + "x,hero_r,3,,None,0,OnTeleport,,\n")), "OnTeleport");

    [Test]
    public void Counters_UnknownOwner_IsAnError()
        => HasError(Build(OneChar, Skills, PassiveRow, (Cnt, CntHeader + "x,ghost_r,3,,None,0,OnHit,,\n")), "ghost_r");

    [Test]
    public void Counters_DuplicateId_ForSameOwner_IsAnError()
        => HasError(Build(OneChar, Skills, PassiveRow, (Cnt, CntHeader + "x,hero_r,3,,None,0,OnHit,,\nx,hero_r,3,,None,0,OnHit,,\n")), "중복");

    [Test]
    public void Counters_MaxStacksMustBePositive()
        => HasError(Build(OneChar, Skills, PassiveRow, (Cnt, CntHeader + "x,hero_r,0,,None,0,OnHit,,\n")), "maxStacks");

    [Test]
    public void Counters_ReservedIds_AreRejected()
    {
        HasError(Build(OneChar, Skills, PassiveRow, (Cnt, CntHeader + "@recent,hero_r,3,,None,0,OnHit,,\n")), "예약");
        HasError(Build(OneChar, Skills, PassiveRow, (Cnt, CntHeader + "target:ac,hero_r,3,,None,0,OnHit,,\n")), "예약");
    }

    [Test]
    public void Counters_MilestoneSkill_MustBelongToTheOwner_AndBeGivenWithMilestoneMax()
    {
        HasError(Build(OneChar, Skills, PassiveRow, (Cnt, CntHeader + "x,hero_r,3,5,None,0,OnHit,,other_q\n")), "other_q");
        HasError(Build(OneChar, Skills, PassiveRow, (Cnt, CntHeader + "x,hero_r,3,5,None,0,OnHit,,\n")), "milestoneSkillId");
    }

    // ── skill_effects.csv 신규 열 ────────────────────────────────────────

    const string EffHeader = "skillId,order,type,target,value,counterId,counterMode,counterCoeff,milestoneCounterCoeff,counterConsume,requireCounterFull,counterWindow\n";

    [Test]
    public void Effects_MapCounterColumns()
    {
        var r = Build(OneChar, Skills, PassiveRow, (Cnt, CntHeader + "free,hero_r,5,,None,0,MovingSecond,,\n"), (Eff, EffHeader +
            "hero_q,1,Damage,EnemyArea,1,free,Quadratic,0.1,0.12,true,false,\n" +
            "hero_q,2,Stun,EnemyArea,,free,,,,false,true,\n" +
            "hero_q,3,Damage,EnemyArea,1,@recent,Linear,0.15,0.2,false,false,5\n" +
            "hero_q,4,Damage,EnemyArea,1,target:ac,Linear,0.1,,true,false,\n"));
        NoErrors(r);
        var e = r.skills.Single(s => s.id == "hero_q").effects;
        Assert.AreEqual(("free", CounterMode.Quadratic, 0.1f, 0.12f, true, false), (e[0].counterId, e[0].counterMode, e[0].counterCoeff, e[0].milestoneCounterCoeff, e[0].counterConsume, e[0].requireCounterFull));
        Assert.IsTrue(e[1].requireCounterFull);
        Assert.AreEqual(("@recent", 5f), (e[2].counterId, e[2].counterWindow));
        Assert.AreEqual("target:ac", e[3].counterId);
    }

    [Test]
    public void Effects_UnknownCounterId_IsAnError()
        => HasError(Build(OneChar, Skills, PassiveRow, (Eff, EffHeader + "hero_q,1,Damage,EnemyArea,1,nope,Linear,0.1,,false,false,\n")), "nope");

    [Test]
    public void Effects_CounterOfAnotherCharacter_IsAnError()
        => HasError(Build(OneChar, Skills, PassiveRow, (Cnt, CntHeader + "theirs,other_r,5,,None,0,OnHit,,\n"),
            (Eff, EffHeader + "hero_q,1,Damage,EnemyArea,1,theirs,Linear,0.1,,false,false,\n")), "theirs");

    [Test]
    public void Effects_ModeWithoutCounterId_IsAnError()
        => HasError(Build(OneChar, Skills, PassiveRow, (Eff, EffHeader + "hero_q,1,Damage,EnemyArea,1,,Linear,0.1,,false,false,\n")), "counterId");

    [Test]
    public void Effects_BareTargetPrefix_IsAnError()
        => HasError(Build(OneChar, Skills, PassiveRow, (Eff, EffHeader + "hero_q,1,Damage,EnemyArea,1,target:,Linear,0.1,,false,false,\n")), "target:");

    // ── skill_modifiers.csv: source=Counter ───────────────────────────────

    const string ModHeader = "skillId,order,source,stat,value,counterId\n";

    [Test]
    public void Modifiers_CounterSource_MapsCounterId()
    {
        var r = Build(OneChar, Skills, PassiveRow, (Cnt, CntHeader + "heat,hero_r,5,,None,0,OnHitTaken,,\n"),
            (Mod, ModHeader + "hero_p,1,Counter,SkillPower,0.04,heat\n"));
        NoErrors(r);
        var m = r.skills.Single(s => s.id == "hero_p").passive.modifiers.Single();
        Assert.AreEqual((ModifierSource.Counter, StatKind.SkillPower, 0.04f, "heat"), (m.source, m.stat, m.value, m.counterId));
    }

    [Test]
    public void Modifiers_CounterSource_RequiresAKnownCounter()
    {
        HasError(Build(OneChar, Skills, PassiveRow, (Mod, ModHeader + "hero_p,1,Counter,SkillPower,0.04,\n")), "counterId");
        HasError(Build(OneChar, Skills, PassiveRow, (Mod, ModHeader + "hero_p,1,Counter,SkillPower,0.04,ghost\n")), "ghost");
    }
}
