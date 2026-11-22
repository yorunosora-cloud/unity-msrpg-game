using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>설계도 18-C — Stage C: 도감 상세의 "개념 해설" 섹션(2단 구조·개념 문제 정답 시 심화 해금) 스모크 테스트.</summary>
public class CodexDetailUiTests
{
    GameObject _root;
    RectTransform _container;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("TestCanvas", typeof(RectTransform), typeof(Canvas));
        var c = new GameObject("Content", typeof(RectTransform));
        c.transform.SetParent(_root.transform, false);
        _container = (RectTransform)c.transform;
        CodexConceptSection.ResetSessionState();
        _correctCalls = 0;
    }

    [TearDown]
    public void TearDown() { if (_root != null) Object.DestroyImmediate(_root); }

    static CharacterDef Def(int questions = 1)
    {
        var d = ScriptableObject.CreateInstance<CharacterDef>();
        d.id = "edu"; d.nameKo = "교육"; d.rarity = Rarity.R; d.continent = Continent.Physics;
        d.baseStats = new CharacterStats { hp = 10, atk = 1, def = 1, spd = 1, mp = 1 };
        d.skills = new SkillDef[0];
        d.basicExplanation = "기본 해설 본문";
        d.advancedExplanation = "심화 해설 본문";
        d.realPrinciple = "실제 원리 본문";
        d.sourceRef = "교과서 3단원";
        var qs = new ConceptQuestion[questions];
        for (int i = 0; i < questions; i++)
            qs[i] = new ConceptQuestion { question = "문제" + (i + 1), choices = new[] { "보기A", "보기B", "보기C", "보기D" }, answerIndex = 1 };
        d.conceptQuestions = qs;
        return d;
    }

    /// <summary>섹션을 container 맨 위에 붙인다(저장 콜백 = 호출 횟수만 센다).</summary>
    int _correctCalls;
    void Build(RectTransform container, CharacterDef def, OwnedCharacter owned, System.Action onChanged = null)
        => CodexConceptSection.Build(container, def, owned, 0f, () => _correctCalls++, onChanged);

    string AllText() => string.Join("\n", _container.GetComponentsInChildren<TMP_Text>(true).Select(t => t.text));
    Button[] Choices() => _container.GetComponentsInChildren<Button>(true).Where(b => b.name.StartsWith("Choice_")).OrderBy(b => b.name).ToArray();

    [Test]
    public void NotOwned_ShowsNoEducationContent()
    {
        Build(_container, Def(), null);
        StringAssert.DoesNotContain("기본 해설 본문", AllText());
        Assert.AreEqual(0, Choices().Length);
    }

    [Test]
    public void Owned_Locked_ShowsBasicAndQuestionAndChoices_ButNotAdvanced()
    {
        var owned = new OwnedCharacter { id = "edu" };
        Build(_container, Def(), owned);
        string t = AllText();
        StringAssert.Contains("기본 해설 본문", t);
        StringAssert.Contains("잠김", t);
        StringAssert.Contains("문제1", t);
        StringAssert.DoesNotContain("심화 해설 본문", t);
        StringAssert.DoesNotContain("실제 원리 본문", t);
        Assert.AreEqual(4, Choices().Length);
    }

    [Test]
    public void CorrectAnswer_UnlocksAndNotifies()
    {
        var d = Def(); var owned = new OwnedCharacter { id = "edu" };
        int changed = 0;
        Build(_container, d, owned, () => changed++);
        Choices()[1].onClick.Invoke();                       // 정답 = 인덱스 1
        Assert.IsTrue(owned.advancedCodexUnlocked);
        Assert.AreEqual(1, _correctCalls, "정답이면 저장 콜백이 호출된다");
        Assert.AreEqual(1, changed, "상세를 다시 그리도록 알린다");
    }

    [Test]
    public void WrongAnswer_KeepsLocked_Notifies_AndShowsFeedbackWithNextQuestion()
    {
        var d = Def(questions: 2); var owned = new OwnedCharacter { id = "edu" };
        int changed = 0;
        System.Action rebuild = null;
        rebuild = () => { changed++; foreach (Transform c in _container) Object.DestroyImmediate(c.gameObject); Build(_container, d, owned, rebuild); };
        Build(_container, d, owned, rebuild);

        StringAssert.Contains("문제1", AllText());
        Choices()[0].onClick.Invoke();                       // 오답
        Assert.IsFalse(owned.advancedCodexUnlocked);
        Assert.AreEqual(0, _correctCalls, "오답은 저장하지 않는다");
        Assert.AreEqual(1, changed);
        string t = AllText();
        StringAssert.Contains("오답", t);
        StringAssert.Contains("문제2", t, "오답이면 다음 문제로 넘어간다");
        StringAssert.DoesNotContain("심화 해설 본문", t);
    }

    [Test]
    public void Unlocked_ShowsAdvancedPrincipleAndSource_WithoutChoices()
    {
        var owned = new OwnedCharacter { id = "edu", advancedCodexUnlocked = true };
        Build(_container, Def(), owned);
        string t = AllText();
        StringAssert.Contains("기본 해설 본문", t);
        StringAssert.Contains("심화 해설 본문", t);
        StringAssert.Contains("실제 원리 본문", t);
        StringAssert.Contains("교과서 3단원", t);
        StringAssert.DoesNotContain("잠김", t);
        Assert.AreEqual(0, Choices().Length);
    }

    [Test]
    public void NoQuestions_AdvancedOpenImmediately()
    {
        var owned = new OwnedCharacter { id = "edu" };
        Build(_container, Def(questions: 0), owned);
        string t = AllText();
        StringAssert.Contains("심화 해설 본문", t);
        Assert.AreEqual(0, Choices().Length);
    }

    [Test]
    public void CharacterWithoutEducationContent_AddsNoConceptSection()
    {
        var d = ScriptableObject.CreateInstance<CharacterDef>();
        d.id = "plain"; d.nameKo = "평범"; d.rarity = Rarity.N; d.baseStats = new CharacterStats { hp = 1, atk = 1, def = 1, spd = 1, mp = 1 };
        d.skills = new SkillDef[0];
        Build(_container, d, new OwnedCharacter { id = "plain" });
        Assert.IsNull(_container.Find("Sec_Concept"));
    }

    [Test]
    public void ConceptSection_ExistsForOwnedCharacterWithContent()
    {
        Build(_container, Def(), new OwnedCharacter { id = "edu" });
        Assert.IsNotNull(_container.Find("Sec_Concept"));
    }
}
