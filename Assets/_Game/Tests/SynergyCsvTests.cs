using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>23-B 단계 B3 — skill_combos.csv 매핑·검증.</summary>
public class SynergyCsvTests
{
    const string Cat = CharacterCsvBuilder.Catalog;
    const string Skl = CharacterCsvBuilder.Skills;
    const string Eff = CharacterCsvBuilder.Effects;
    const string Cmb = CharacterCsvBuilder.Combos;

    static ImportResult Build(params (string sheet, string csv)[] sheets)
    {
        var d = new Dictionary<string, string>();
        foreach (var s in sheets) d[s.sheet] = s.csv;
        return CharacterCsvBuilder.Build(d);
    }

    static readonly (string, string) Chars = (Cat, "id,nameKo,continent,rarity\nmarker_r,표식자,Physics,R\nhero_r,영웅,Physics,R\n");
    static readonly (string, string) Skills = (Skl,
        "skillId,ownerId,slot,nameKo,skillType,startUnlocked,effectKind\n" +
        "mark_s,marker_r,1,표식,Active,false,Mark\n" +
        "hero_q,hero_r,0,기본,Active,true,Strike\n" +
        "hero_t,hero_r,2,트리거,Active,false,Strike\n" +
        "hero_combo,hero_r,0,연계기,Combo,false,Strike\n" +
        "marker_combo,marker_r,0,남의 연계기,Combo,false,Strike\n" +
        "hero_plain,hero_r,3,일반,Active,false,Strike\n");
    static readonly (string, string) MarkEffect = (Eff, "skillId,order,type,target,duration,markId\nmark_s,1,Mark,EnemyArea,8,disp\n");

    const string H = "comboId,comboName,formula,markId,consumerId,triggerSkillId,comboSkillId\n";

    static void NoErrors(ImportResult r) => Assert.IsFalse(r.report.HasErrors, string.Join("\n", r.report.Errors));
    static void HasError(ImportResult r, string contains)
    {
        Assert.IsTrue(r.report.HasErrors, "오류가 있어야 한다");
        Assert.IsTrue(r.report.Errors.Any(e => e.Contains(contains)), "기대 문구 '" + contains + "' — 실제:\n" + string.Join("\n", r.report.Errors));
    }

    [Test]
    public void Combos_MapAllFields_AndResolveTheComboSkill()
    {
        var r = Build(Chars, Skills, MarkEffect, (Cmb, H + "combo_x,속도 도출,v = Δx/Δt,disp,hero_r,hero_t,hero_combo\n"));
        NoErrors(r);
        var c = r.combos.Single();
        Assert.AreEqual(("combo_x", "속도 도출", "v = Δx/Δt", "disp", "hero_r", "hero_t"), (c.id, c.comboName, c.formula, c.markId, c.consumerId, c.triggerSkillId));
        Assert.AreSame(r.skills.Single(s => s.id == "hero_combo"), c.comboSkill, "연계기는 같은 빌드의 SkillDef를 가리킨다");
        Assert.AreEqual(SkillKind.Combo, c.comboSkill.skillType);
    }

    [Test]
    public void Combo_UnknownConsumer_IsAnError()
        => HasError(Build(Chars, Skills, MarkEffect, (Cmb, H + "c,n,f,disp,ghost_r,hero_t,hero_combo\n")), "ghost_r");

    [Test]
    public void Combo_TriggerSkill_MustExistAndBelongToTheConsumer()
    {
        HasError(Build(Chars, Skills, MarkEffect, (Cmb, H + "c,n,f,disp,hero_r,nope,hero_combo\n")), "nope");
        HasError(Build(Chars, Skills, MarkEffect, (Cmb, H + "c,n,f,disp,hero_r,mark_s,hero_combo\n")), "mark_s");
    }

    [Test]
    public void Combo_ComboSkill_MustBeTheConsumersCombo_Kind()
    {
        HasError(Build(Chars, Skills, MarkEffect, (Cmb, H + "c,n,f,disp,hero_r,hero_t,nope\n")), "nope");
        HasError(Build(Chars, Skills, MarkEffect, (Cmb, H + "c,n,f,disp,hero_r,hero_t,hero_plain\n")), "Combo");
        HasError(Build(Chars, Skills, MarkEffect, (Cmb, H + "c,n,f,disp,hero_r,hero_t,marker_combo\n")), "marker_combo");
    }

    [Test]
    public void Combo_DuplicateIdOrTriggerPair_IsAnError()
    {
        HasError(Build(Chars, Skills, MarkEffect, (Cmb, H + "c,n,f,disp,hero_r,hero_t,hero_combo\nc,n2,f,disp,hero_r,hero_t,hero_combo\n")), "중복");
    }

    [Test]
    public void Combo_RequiresMarkId_AndWarnsWhenNoSkillAppliesIt()
    {
        HasError(Build(Chars, Skills, MarkEffect, (Cmb, H + "c,n,f,,hero_r,hero_t,hero_combo\n")), "markId");
        var r = Build(Chars, Skills, (Cmb, H + "c,n,f,typo_mark,hero_r,hero_t,hero_combo\n"));
        NoErrors(r);
        Assert.IsTrue(r.report.Warnings.Any(w => w.Contains("typo_mark")), "어떤 스킬도 걸지 않는 표식은 경고");
    }

    [Test]
    public void ComboSkillKind_IsNotCastableFromSlotKeys_AndNeedsNoPassiveRow()
    {
        var r = Build(Chars, Skills, MarkEffect, (Cmb, H + "c,n,f,disp,hero_r,hero_t,hero_combo\n"));
        NoErrors(r);
        var hero = r.characters.Single(x => x.id == "hero_r");
        var cc = new CombatCharacter(hero, new OwnedCharacter { id = "hero_r", level = 1 });
        for (int i = 0; i < hero.skills.Length; i++) cc.Unlock(i);
        Assert.AreEqual(hero.skills.ToList().FindIndex(s => s.id == "hero_q"), cc.IndexForSlot(0), "슬롯 0은 기본기, 같은 슬롯의 Combo는 키로 발동하지 않는다");
    }
}
