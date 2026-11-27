using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>23-G — skill_effects.csv 계산 경로 열의 매핑·검증.</summary>
public class ComputationCsvTests
{
    const string Cat = CharacterCsvBuilder.Catalog;
    const string Skl = CharacterCsvBuilder.Skills;
    const string Eff = CharacterCsvBuilder.Effects;

    static ImportResult Build(params (string sheet, string csv)[] sheets)
    {
        var d = new Dictionary<string, string>();
        foreach (var s in sheets) d[s.sheet] = s.csv;
        return CharacterCsvBuilder.Build(d);
    }

    static readonly (string, string) Chars = (Cat, "id,nameKo,continent,rarity\nhero_r,영웅,Physics,R\n");
    static readonly (string, string) Skills = (Skl, "skillId,ownerId,slot,nameKo,skillType,startUnlocked,effectKind\nhero_q,hero_r,0,기본,Active,true,Strike\n");

    const string H = "skillId,order,type,target,value,duration,damageBasis,ignoreDefense,scaleBy,scaleCoeff,scaleCap,milestoneScaleCap,critMult,critHalfAngle,branchGroup,branchId\n";

    static void NoErrors(ImportResult r) => Assert.IsFalse(r.report.HasErrors, string.Join("\n", r.report.Errors));
    static void HasError(ImportResult r, string contains)
    {
        Assert.IsTrue(r.report.HasErrors, "오류가 있어야 한다");
        Assert.IsTrue(r.report.Errors.Any(e => e.Contains(contains)), "기대 문구 '" + contains + "' — 실제:\n" + string.Join("\n", r.report.Errors));
    }

    [Test]
    public void Columns_MapOnDamageEffects()
    {
        var r = Build(Chars, Skills, (Eff, H +
            "hero_q,1,Damage,EnemyArea,1,,TargetAtk,true,SpeedDiff,0.01,1,1.3,2.5,10,,\n"));
        NoErrors(r);
        var e = r.skills.Single().effects.Single();
        Assert.AreEqual((DamageBasis.TargetAtk, true, ScaleBy.SpeedDiff, 0.01f, 1f, 1.3f, 2.5f, 10f),
            (e.damageBasis, e.ignoreDefense, e.scaleBy, e.scaleCoeff, e.scaleCap, e.milestoneScaleCap, e.critMult, e.critHalfAngle));
    }

    [Test]
    public void Columns_Defaults_AreNeutral()
    {
        var r = Build(Chars, Skills, (Eff, H + "hero_q,1,Damage,EnemyArea,1,,,,,,,,,,,\n"));
        NoErrors(r);
        var e = r.skills.Single().effects.Single();
        Assert.AreEqual((DamageBasis.CasterAtk, false, ScaleBy.None, 0f), (e.damageBasis, e.ignoreDefense, e.scaleBy, e.critMult));
    }

    [Test]
    public void UnknownEnumValues_AreErrors()
    {
        HasError(Build(Chars, Skills, (Eff, H + "hero_q,1,Damage,EnemyArea,1,,Nope,,,,,,,,,\n")), "Nope");
        HasError(Build(Chars, Skills, (Eff, H + "hero_q,1,Damage,EnemyArea,1,,,,Warp,,,,,,,\n")), "Warp");
    }

    [Test]
    public void DamageOnlyColumns_AreRejectedOnOtherEffectTypes()
    {
        HasError(Build(Chars, Skills, (Eff, H + "hero_q,1,Slow,EnemyArea,0.8,3,,true,,,,,,,,\n")), "Damage");
        HasError(Build(Chars, Skills, (Eff, H + "hero_q,1,Stun,EnemyArea,,2,,,Height,,1,,,,,\n")), "Damage");
    }

    [Test]
    public void Crit_RequiresAHalfAngle_AndScaleRequiresACap()
    {
        HasError(Build(Chars, Skills, (Eff, H + "hero_q,1,Damage,EnemyArea,1,,,,,,,,2.5,0,,\n")), "critHalfAngle");
        HasError(Build(Chars, Skills, (Eff, H + "hero_q,1,Damage,EnemyArea,1,,,,MoveSpeedRatio,,0,,,,,\n")), "scaleCap");
    }

    [Test]
    public void Branch_GroupAndIdMustComeTogether_WithAtLeastTwoBranches()
    {
        HasError(Build(Chars, Skills, (Eff, H + "hero_q,1,Damage,EnemyArea,1,,,,,,,,,,g,\n")), "branchId");
        HasError(Build(Chars, Skills, (Eff, H + "hero_q,1,Damage,EnemyArea,1,,,,,,,,,,,a\n")), "branchGroup");
        HasError(Build(Chars, Skills, (Eff, H + "hero_q,1,Damage,EnemyArea,1,,,,,,,,,,g,a\nhero_q,2,Slow,EnemyArea,0.8,3,,,,,,,,,g,a\n")), "분기");
        NoErrors(Build(Chars, Skills, (Eff, H + "hero_q,1,Damage,EnemyArea,1,,,,,,,,,,g,a\nhero_q,2,Slow,EnemyArea,0.8,3,,,,,,,,,g,b\n")));
    }
}
