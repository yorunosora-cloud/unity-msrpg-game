using NUnit.Framework;
using UnityEngine;

/// <summary>설계도 18-C — Stage A: 2단 도감 로직 · 개념 문제 · 콤보 보상 · 호감도 질문 데이터.</summary>
public class EducationTests
{
    static CharacterDef Def(params ConceptQuestion[] questions)
    {
        var d = ScriptableObject.CreateInstance<CharacterDef>();
        d.id = "edu"; d.nameKo = "교육";
        d.basicExplanation = "기본 해설"; d.advancedExplanation = "심화 해설";
        d.realPrinciple = "실제 원리"; d.sourceRef = "교과서";
        d.conceptQuestions = questions;
        return d;
    }

    static ConceptQuestion Q(int answer = 1, int choices = 4)
    {
        var c = new string[choices];
        for (int i = 0; i < choices; i++) c[i] = "보기" + (i + 1);
        return new ConceptQuestion { question = "문제", choices = c, answerIndex = answer };
    }

    // ════════════════════════════════════════════════════════════════════════
    // 스키마
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Owned_AdvancedFlag_DefaultsFalse_AndOldSavesLoad()
    {
        Assert.IsFalse(new OwnedCharacter().advancedCodexUnlocked);
        var old = JsonUtility.FromJson<OwnedCharacter>("{\"id\":\"x\",\"level\":2,\"exp\":5}");
        Assert.IsFalse(old.advancedCodexUnlocked, "구 세이브에는 필드가 없어도 false로 로드");
        Assert.AreEqual(2, old.level);
    }

    [Test]
    public void Owned_AdvancedFlag_SurvivesJsonRoundTrip()
    {
        var o = new OwnedCharacter { id = "x", advancedCodexUnlocked = true };
        var back = JsonUtility.FromJson<OwnedCharacter>(JsonUtility.ToJson(o));
        Assert.IsTrue(back.advancedCodexUnlocked);
    }

    [Test]
    public void CharacterDef_EducationFields_DefaultEmpty()
    {
        var d = ScriptableObject.CreateInstance<CharacterDef>();
        Assert.IsTrue(string.IsNullOrEmpty(d.basicExplanation));
        Assert.IsTrue(d.conceptQuestions == null || d.conceptQuestions.Length == 0);
        Assert.IsTrue(d.affinityQuestions == null || d.affinityQuestions.Length == 0);
    }

    // ════════════════════════════════════════════════════════════════════════
    // CodexConcept
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Advanced_NotOwned_IsNeverOpen()
        => Assert.IsFalse(CodexConcept.IsAdvancedOpen(Def(Q()), null));

    [Test]
    public void Advanced_LockedUntilQuestionAnsweredCorrectly()
    {
        var d = Def(Q(answer: 2));
        var o = new OwnedCharacter { id = "edu" };
        Assert.IsFalse(CodexConcept.IsAdvancedOpen(d, o));

        Assert.IsFalse(CodexConcept.TryAnswer(d, o, 0, 0), "오답");
        Assert.IsFalse(o.advancedCodexUnlocked);
        Assert.IsFalse(CodexConcept.IsAdvancedOpen(d, o));

        Assert.IsTrue(CodexConcept.TryAnswer(d, o, 0, 2), "정답");
        Assert.IsTrue(o.advancedCodexUnlocked);
        Assert.IsTrue(CodexConcept.IsAdvancedOpen(d, o));
    }

    [Test]
    public void Advanced_NoQuestions_IsNotLockedForever()
    {
        var o = new OwnedCharacter { id = "edu" };
        Assert.IsTrue(CodexConcept.IsAdvancedOpen(Def(), o), "문제가 없으면 심화 해설을 잠그지 않는다");
        Assert.IsFalse(CodexConcept.HasQuestion(Def()));
    }

    [Test]
    public void TryAnswer_InvalidIndexes_ReturnFalseWithoutThrowing()
    {
        var d = Def(Q());
        var o = new OwnedCharacter { id = "edu" };
        Assert.IsFalse(CodexConcept.TryAnswer(d, o, 5, 0));
        Assert.IsFalse(CodexConcept.TryAnswer(d, o, -1, 0));
        Assert.IsFalse(CodexConcept.TryAnswer(d, o, 0, 9));
        Assert.IsFalse(CodexConcept.TryAnswer(d, o, 0, -1));
        Assert.IsFalse(CodexConcept.TryAnswer(null, o, 0, 0));
        Assert.IsFalse(CodexConcept.TryAnswer(d, null, 0, 1));
        Assert.IsFalse(o.advancedCodexUnlocked);
    }

    [Test]
    public void TryAnswer_AlreadyUnlocked_StaysUnlocked_EvenOnWrongAnswer()
    {
        var d = Def(Q(answer: 1));
        var o = new OwnedCharacter { id = "edu", advancedCodexUnlocked = true };
        Assert.IsFalse(CodexConcept.TryAnswer(d, o, 0, 3));
        Assert.IsTrue(o.advancedCodexUnlocked, "한 번 열린 심화 해설은 다시 잠기지 않는다");
    }

    [Test]
    public void PickQuestion_CyclesThroughQuestions_AndHandlesNone()
    {
        var d = Def(Q(0), Q(1), Q(2));
        Assert.AreEqual(0, CodexConcept.PickQuestionIndex(d, 0));
        Assert.AreEqual(1, CodexConcept.PickQuestionIndex(d, 1));
        Assert.AreEqual(2, CodexConcept.PickQuestionIndex(d, 2));
        Assert.AreEqual(0, CodexConcept.PickQuestionIndex(d, 3), "순환");
        Assert.AreEqual(-1, CodexConcept.PickQuestionIndex(Def(), 0));
        Assert.AreEqual(0, CodexConcept.PickQuestionIndex(d, -1), "음수 시도 횟수는 0으로");
    }

    [Test]
    public void AnyQuestionCorrect_Unlocks()
    {
        var d = Def(Q(answer: 0), Q(answer: 3));
        var o = new OwnedCharacter { id = "edu" };
        Assert.IsTrue(CodexConcept.TryAnswer(d, o, 1, 3));
        Assert.IsTrue(o.advancedCodexUnlocked);
    }

    [Test]
    public void HasEducationContent_DetectsAnyField()
    {
        Assert.IsTrue(CodexConcept.HasContent(Def()));
        var empty = ScriptableObject.CreateInstance<CharacterDef>();
        Assert.IsFalse(CodexConcept.HasContent(empty));
        empty.basicExplanation = "x";
        Assert.IsTrue(CodexConcept.HasContent(empty));
    }

    // ════════════════════════════════════════════════════════════════════════
    // 콤보 보상 · 호감도 질문
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void ComboReward_AddsBonusToTheElementsWallet()
    {
        var w = new StudyMaterialWallet();
        int granted = ComboReward.Award(w, Continent.Physics);
        Assert.AreEqual(ComboReward.ComboBonusAmount, granted);
        Assert.AreEqual(ComboReward.ComboBonusAmount, w.Get(Continent.Physics));
        Assert.AreEqual(0, w.Get(Continent.Chemistry));
        Assert.Greater(ComboReward.ComboBonusAmount, 0);
    }

    [Test]
    public void ComboReward_NullWallet_IsSafe()
        => Assert.AreEqual(0, ComboReward.Award(null, Continent.Physics));

    [Test]
    public void AffinityQuestion_IsConceptPick()
    {
        var q = new AffinityQuestion { question = "?", choices = new[] { "가", "나" }, conceptPick = 1 };
        Assert.IsTrue(q.IsConceptPick(1));
        Assert.IsFalse(q.IsConceptPick(0));
        Assert.IsFalse(q.IsConceptPick(-1));
    }
}
