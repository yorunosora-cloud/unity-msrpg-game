using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>23-F — 속성 열(catalog attackElement, effects elementMask/Invert, modifiers requireElement) 매핑·검증.</summary>
public class ElementCsvTests
{
    const string Cat = CharacterCsvBuilder.Catalog;
    const string Skl = CharacterCsvBuilder.Skills;
    const string Eff = CharacterCsvBuilder.Effects;
    const string Pas = CharacterCsvBuilder.Passives;
    const string Mod = CharacterCsvBuilder.Modifiers;

    static ImportResult Build(params (string sheet, string csv)[] sheets)
    {
        var d = new Dictionary<string, string>();
        foreach (var s in sheets) d[s.sheet] = s.csv;
        return CharacterCsvBuilder.Build(d);
    }

    static void NoErrors(ImportResult r) => Assert.IsFalse(r.report.HasErrors, string.Join("\n", r.report.Errors));
    static void HasError(ImportResult r, string contains)
    {
        Assert.IsTrue(r.report.HasErrors, "오류가 있어야 한다");
        Assert.IsTrue(r.report.Errors.Any(e => e.Contains(contains)), "기대 문구 '" + contains + "' — 실제:\n" + string.Join("\n", r.report.Errors));
    }

    static readonly (string, string) Skills = (Skl, "skillId,ownerId,slot,nameKo,skillType,startUnlocked,effectKind\nhero_q,hero_r,0,기본,Active,true,Strike\nhero_p,hero_r,1,패시브,Passive,false,Strike\n");
    static readonly (string, string) PassiveRow = (Pas, "skillId,trigger\nhero_p,PartyPresence\n");

    [Test]
    public void Catalog_AttackElement_MapsFlags_AndRejectsUnknownNames()
    {
        var ok = Build((Cat, "id,nameKo,continent,rarity,attackElement\nhero_r,영웅,Physics,R,Electric;Light\n"));
        NoErrors(ok);
        Assert.AreEqual(AttackElement.Electric | AttackElement.Light, ok.characters.Single().attackElement);
        HasError(Build((Cat, "id,nameKo,continent,rarity,attackElement\nhero_r,영웅,Physics,R,Plasma\n")), "Plasma");
    }

    [Test]
    public void Effects_ElementMaskAndInvert_MapOnDamageTakenMult_AndAreRejectedElsewhere()
    {
        const string h = "skillId,order,type,target,value,duration,elementMask,elementInvert\n";
        var ok = Build((Cat, "id,nameKo,continent,rarity\nhero_r,영웅,Physics,R\n"), Skills, PassiveRow,
            (Eff, h + "hero_q,1,DamageTakenMult,Self,0.9,6,Metal,true\n"));
        NoErrors(ok);
        var e = ok.skills.Single(s => s.id == "hero_q").effects.Single();
        Assert.AreEqual((AttackElement.Metal, true), (e.elementMask, e.elementInvert));
        HasError(Build((Cat, "id,nameKo,continent,rarity\nhero_r,영웅,Physics,R\n"), Skills, PassiveRow,
            (Eff, h + "hero_q,1,Damage,EnemyArea,1,,Metal,\n")), "DamageTakenMult");
        HasError(Build((Cat, "id,nameKo,continent,rarity\nhero_r,영웅,Physics,R\n"), Skills, PassiveRow,
            (Eff, h + "hero_q,1,DamageTakenMult,Self,0.9,6,,true\n")), "elementMask");
    }

    [Test]
    public void Modifiers_RequireElement_MapsAndValidates()
    {
        const string h = "skillId,order,source,stat,value,partyWide,requireElement,requireElementInvert\n";
        var ok = Build((Cat, "id,nameKo,continent,rarity\nhero_r,영웅,Physics,R\n"), Skills, PassiveRow,
            (Mod, h + "hero_p,1,PartyPresence,Atk,1.2,true,Electric,\nhero_p,2,PartyPresence,Atk,1.07,true,Electric,true\n"));
        NoErrors(ok);
        var m = ok.skills.Single(s => s.id == "hero_p").passive.modifiers;
        Assert.AreEqual((AttackElement.Electric, false), (m[0].requireElement, m[0].requireElementInvert));
        Assert.AreEqual((AttackElement.Electric, true), (m[1].requireElement, m[1].requireElementInvert));
        HasError(Build((Cat, "id,nameKo,continent,rarity\nhero_r,영웅,Physics,R\n"), Skills, PassiveRow,
            (Mod, h + "hero_p,1,PartyPresence,Atk,1.2,true,,true\n")), "requireElement");
    }
}
