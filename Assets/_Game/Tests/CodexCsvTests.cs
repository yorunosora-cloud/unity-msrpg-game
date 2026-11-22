using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>설계도 18-C — Stage B: character_codex.csv 매핑·검증.</summary>
public class CodexCsvTests
{
    const string Cat = CharacterCsvBuilder.Catalog;
    const string Cdx = CharacterCsvBuilder.Codex;

    static ImportResult Build(params (string sheet, string csv)[] sheets)
    {
        var d = new Dictionary<string, string>();
        foreach (var s in sheets) d[s.sheet] = s.csv;
        return CharacterCsvBuilder.Build(d);
    }

    static readonly (string, string) OneChar = (Cat,
        "id,nameKo,continent,rarity\nhero_r,영웅,Physics,R\n");

    const string Header = "ownerId,basicExplanation,advancedExplanation,realPrinciple,sourceRef,conceptQ,conceptChoices,conceptAnswer,affinityQ,affinityChoices,affinityConceptPick\n";

    static void NoErrors(ImportResult r) => Assert.IsFalse(r.report.HasErrors, string.Join("\n", r.report.Errors));

    [Test]
    public void Codex_MapsTextQuestionsAndAffinity_ConvertingToZeroBasedIndexes()
    {
        var r = Build(OneChar, (Cdx, Header +
            "hero_r,기본,심화,원리,교과서,\"관성이란?\",\"가;나;다;라\",3,\"어느 쪽?\",\"왼쪽;오른쪽\",2\n"));
        NoErrors(r);
        var c = r.characters.Single();
        Assert.AreEqual("기본", c.basicExplanation);
        Assert.AreEqual("심화", c.advancedExplanation);
        Assert.AreEqual("원리", c.realPrinciple);
        Assert.AreEqual("교과서", c.sourceRef);
        Assert.AreEqual(1, c.conceptQuestions.Length);
        Assert.AreEqual("관성이란?", c.conceptQuestions[0].question);
        CollectionAssert.AreEqual(new[] { "가", "나", "다", "라" }, c.conceptQuestions[0].choices);
        Assert.AreEqual(2, c.conceptQuestions[0].answerIndex, "CSV 3번 → 인덱스 2");
        Assert.AreEqual(1, c.affinityQuestions.Length);
        Assert.AreEqual("어느 쪽?", c.affinityQuestions[0].question);
        Assert.AreEqual(1, c.affinityQuestions[0].conceptPick, "CSV 2번 → 인덱스 1");
        Assert.IsTrue(c.affinityQuestions[0].IsConceptPick(1));
    }

    [Test]
    public void Codex_MultipleRowsPerOwner_AccumulateQuestions_TextComesFromFirstRow()
    {
        var r = Build(OneChar, (Cdx, Header +
            "hero_r,기본,심화,원리,출처,Q1,a;b,1,,,\n" +
            "hero_r,,,,,Q2,c;d;e,3,A1,x;y,1\n" +
            "hero_r,,,,,,,,A2,p;q,2\n"));
        NoErrors(r);
        var c = r.characters.Single();
        Assert.AreEqual("기본", c.basicExplanation);
        Assert.AreEqual(2, c.conceptQuestions.Length);
        Assert.AreEqual("Q2", c.conceptQuestions[1].question);
        Assert.AreEqual(2, c.conceptQuestions[1].answerIndex);
        Assert.AreEqual(2, c.affinityQuestions.Length);
        Assert.AreEqual("A2", c.affinityQuestions[1].question);
    }

    [Test]
    public void Codex_DuplicateTextDefinition_IsAnError()
    {
        var r = Build(OneChar, (Cdx, Header + "hero_r,기본1,,,,,,,,,\nhero_r,기본2,,,,,,,,,\n"));
        Assert.IsTrue(r.report.HasErrors);
        StringAssert.Contains("basicExplanation", string.Join("\n", r.report.Errors));
        StringAssert.Contains("[character_codex.csv:3]", string.Join("\n", r.report.Errors));
    }

    [Test]
    public void Codex_UnknownOwner_IsAnError()
    {
        var r = Build(OneChar, (Cdx, Header + "nobody,기본,,,,,,,,,\n"));
        Assert.IsTrue(r.report.HasErrors);
        StringAssert.Contains("nobody", string.Join("\n", r.report.Errors));
    }

    [Test]
    public void Codex_ConceptQuestion_Validation()
    {
        var oneChoice = Build(OneChar, (Cdx, Header + "hero_r,,,,,Q,only,1,,,\n"));
        Assert.IsTrue(oneChoice.report.HasErrors, "선택지 2개 미만");
        var zeroAnswer = Build(OneChar, (Cdx, Header + "hero_r,,,,,Q,a;b,0,,,\n"));
        Assert.IsTrue(zeroAnswer.report.HasErrors, "정답 번호는 1부터");
        var tooBig = Build(OneChar, (Cdx, Header + "hero_r,,,,,Q,a;b,3,,,\n"));
        Assert.IsTrue(tooBig.report.HasErrors, "정답 번호가 선택지 수 초과");
        var orphan = Build(OneChar, (Cdx, Header + "hero_r,,,,,,a;b,1,,,\n"));
        Assert.IsTrue(orphan.report.HasErrors, "문제 없이 선택지만 있음");
    }

    [Test]
    public void Codex_AffinityQuestion_MustBeTwoChoices_WithValidPick()
    {
        var three = Build(OneChar, (Cdx, Header + "hero_r,,,,,,,,Q,a;b;c,1\n"));
        Assert.IsTrue(three.report.HasErrors, "호감도 질문은 2지선다");
        var badPick = Build(OneChar, (Cdx, Header + "hero_r,,,,,,,,Q,a;b,3\n"));
        Assert.IsTrue(badPick.report.HasErrors);
        var ok = Build(OneChar, (Cdx, Header + "hero_r,,,,,,,,Q,a;b,2\n"));
        NoErrors(ok);
    }

    [Test]
    public void Codex_RowWithOnlyOwner_IsHarmless()
    {
        var r = Build(OneChar, (Cdx, Header + "hero_r,,,,,,,,,,\n"));
        NoErrors(r);
        Assert.IsTrue(string.IsNullOrEmpty(r.characters.Single().basicExplanation));
    }

    [Test]
    public void Codex_MissingRequiredColumn_AndUnknownColumn()
    {
        var missing = Build(OneChar, (Cdx, "basicExplanation\nx\n"));
        Assert.IsTrue(missing.report.HasErrors);
        var extra = Build(OneChar, (Cdx, "ownerId,basicExplanation,zzz\nhero_r,기본,1\n"));
        NoErrors(extra);
        Assert.AreEqual(1, extra.report.Warnings.Count);
    }

    [Test]
    public void NoCodexSheet_LeavesEducationFieldsEmpty()
    {
        var r = Build(OneChar);
        NoErrors(r);
        Assert.IsTrue(string.IsNullOrEmpty(r.characters.Single().basicExplanation));
        Assert.IsTrue(r.characters.Single().conceptQuestions == null || r.characters.Single().conceptQuestions.Length == 0);
    }

    [Test]
    public void AllSheets_IncludesCodex()
        => CollectionAssert.Contains(CharacterCsvBuilder.AllSheets, CharacterCsvBuilder.Codex);

    [Test]
    public void BuiltConcept_WorksWithCodexConcept()
    {
        var r = Build(OneChar, (Cdx, Header + "hero_r,기본,심화,원리,출처,Q,a;b;c;d,2,,,\n"));
        NoErrors(r);
        var def = r.characters.Single();
        var owned = new OwnedCharacter { id = def.id };
        Assert.IsFalse(CodexConcept.IsAdvancedOpen(def, owned));
        Assert.IsFalse(CodexConcept.TryAnswer(def, owned, 0, 0));
        Assert.IsTrue(CodexConcept.TryAnswer(def, owned, 0, 1), "CSV 정답 2번 = 인덱스 1");
        Assert.IsTrue(CodexConcept.IsAdvancedOpen(def, owned));
    }
}
