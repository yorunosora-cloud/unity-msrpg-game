using NUnit.Framework;
using UnityEngine;

/// <summary>스킬 해금(문제 풀이) 순수 로직 EditMode 테스트.</summary>
public class ProblemTests
{
    // ════════════════════════════════════════════════════════════════════════
    // 헬퍼
    // ════════════════════════════════════════════════════════════════════════

    static ProblemDef MakeMultiChoice(int correctIndex = 1)
    {
        var p = ScriptableObject.CreateInstance<ProblemDef>();
        p.id           = "test_prob";
        p.skillId      = "test_skill";
        p.type         = ProblemType.MultipleChoice;
        p.prompt       = "테스트 문제";
        p.choices      = new[] { "오답1", "정답", "오답2", "오답3" };
        p.correctIndex = correctIndex;
        return p;
    }

    static ProblemDef MakeFreeInput(params string[] answers)
    {
        var p = ScriptableObject.CreateInstance<ProblemDef>();
        p.id              = "test_prob_fi";
        p.skillId         = "test_skill_fi";
        p.type            = ProblemType.FreeInput;
        p.prompt          = "단답형 테스트";
        p.acceptedAnswers = answers;
        return p;
    }

    static CharacterDef MakeDef(params SkillDef[] skills)
    {
        var d = ScriptableObject.CreateInstance<CharacterDef>();
        d.id        = "test_char";
        d.nameKo    = "테스트";
        d.rarity    = Rarity.N;
        d.continent = Continent.Physics;
        d.skills    = skills;
        d.baseStats = new CharacterStats { hp = 200, atk = 50, def = 10, spd = 10, mp = 100 };
        return d;
    }

    static SkillDef MakeSkill(string id, int mpCost = 20)
    {
        var s = ScriptableObject.CreateInstance<SkillDef>();
        s.id      = id;
        s.nameKo  = id;
        s.mpCost  = mpCost;
        s.cooldown = 1f;
        s.effectKind = SkillEffectKind.Strike;
        return s;
    }

    static CombatCharacter MakeChar(params SkillDef[] skills)
    {
        var oc = new OwnedCharacter { id = "test", level = 1 };
        return new CombatCharacter(MakeDef(skills), oc);
    }

    static CombatCharacter MakeCharWithOwned(OwnedCharacter oc, params SkillDef[] skills)
        => new CombatCharacter(MakeDef(skills), oc);

    // ════════════════════════════════════════════════════════════════════════
    // ProblemChecker — 객관식
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void MultipleChoice_CorrectIndex_ReturnsTrue()
    {
        var prob = MakeMultiChoice(correctIndex: 2);
        Assert.IsTrue(ProblemChecker.Check(prob, "", 2));
    }

    [Test]
    public void MultipleChoice_WrongIndex_ReturnsFalse()
    {
        var prob = MakeMultiChoice(correctIndex: 2);
        Assert.IsFalse(ProblemChecker.Check(prob, "", 0));
        Assert.IsFalse(ProblemChecker.Check(prob, "", 3));
    }

    [Test]
    public void MultipleChoice_NullDef_ReturnsFalse()
    {
        Assert.IsFalse(ProblemChecker.Check(null, "", 0));
    }

    // ════════════════════════════════════════════════════════════════════════
    // ProblemChecker — 주관식
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void FreeInput_ExactMatch_ReturnsTrue()
    {
        var prob = MakeFreeInput("미토콘드리아");
        Assert.IsTrue(ProblemChecker.Check(prob, "미토콘드리아", -1));
    }

    [Test]
    public void FreeInput_Whitespace_Normalized()
    {
        var prob = MakeFreeInput("m/s");
        Assert.IsTrue(ProblemChecker.Check(prob, "  m/s  ", -1));
    }

    [Test]
    public void FreeInput_CaseInsensitive()
    {
        var prob = MakeFreeInput("mitochondria");
        Assert.IsTrue(ProblemChecker.Check(prob, "MITOCHONDRIA", -1));
        Assert.IsTrue(ProblemChecker.Check(prob, "Mitochondria", -1));
    }

    [Test]
    public void FreeInput_MultipleSpaceCollapsed()
    {
        var prob = MakeFreeInput("f ma");
        Assert.IsTrue(ProblemChecker.Check(prob, "f  ma", -1));
    }

    [Test]
    public void FreeInput_AlternativeAnswer_ReturnsTrue()
    {
        var prob = MakeFreeInput("미토콘드리아", "mitochondria");
        Assert.IsTrue(ProblemChecker.Check(prob, "mitochondria", -1));
    }

    [Test]
    public void FreeInput_WrongAnswer_ReturnsFalse()
    {
        var prob = MakeFreeInput("미토콘드리아");
        Assert.IsFalse(ProblemChecker.Check(prob, "핵", -1));
        Assert.IsFalse(ProblemChecker.Check(prob, "", -1));
    }

    // ════════════════════════════════════════════════════════════════════════
    // IsUnlocked — 기본 폴백
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void IsUnlocked_EmptyList_Index0_ReturnsTrue()
    {
        var s0 = MakeSkill("s0");
        var s1 = MakeSkill("s1");
        var oc = new OwnedCharacter { id = "c" }; // unlockedSkillIds 비어있음
        var ch = MakeCharWithOwned(oc, s0, s1);

        Assert.IsTrue(ch.IsUnlocked(0));
    }

    [Test]
    public void IsUnlocked_EmptyList_Index1_ReturnsFalse()
    {
        var s0 = MakeSkill("s0");
        var s1 = MakeSkill("s1");
        var oc = new OwnedCharacter { id = "c" };
        var ch = MakeCharWithOwned(oc, s0, s1);

        Assert.IsFalse(ch.IsUnlocked(1));
    }

    [Test]
    public void IsUnlocked_OutOfRange_ReturnsFalse()
    {
        var ch = MakeChar(MakeSkill("s0"));
        Assert.IsFalse(ch.IsUnlocked(99));
    }

    // ════════════════════════════════════════════════════════════════════════
    // Unlock — 해금 후 상태
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Unlock_Index_ThenIsUnlockedTrue()
    {
        var s0 = MakeSkill("s0");
        var s1 = MakeSkill("s1");
        var oc = new OwnedCharacter { id = "c" };
        var ch = MakeCharWithOwned(oc, s0, s1);

        ch.Unlock(1);

        Assert.IsTrue(ch.IsUnlocked(1));
    }

    [Test]
    public void Unlock_ById_ThenIsUnlockedTrue()
    {
        var s0 = MakeSkill("sk_a");
        var s1 = MakeSkill("sk_b");
        var oc = new OwnedCharacter { id = "c" };
        var ch = MakeCharWithOwned(oc, s0, s1);

        ch.Unlock("sk_b");

        Assert.IsTrue(ch.IsUnlocked(1));
    }

    [Test]
    public void Unlock_Duplicate_DoesNotAddTwice()
    {
        var s0 = MakeSkill("s0");
        var oc = new OwnedCharacter { id = "c" };
        var ch = MakeCharWithOwned(oc, s0);

        ch.Unlock(0);
        ch.Unlock(0);

        Assert.AreEqual(1, oc.unlockedSkillIds.Count);
    }

    // ════════════════════════════════════════════════════════════════════════
    // CanCast — 잠금 게이팅
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void CanCast_LockedSkill_ReturnsFalse()
    {
        var s0 = MakeSkill("s0", mpCost: 10);
        var s1 = MakeSkill("s1", mpCost: 10);
        var oc = new OwnedCharacter { id = "c" };
        var ch = MakeCharWithOwned(oc, s0, s1); // s1 잠김

        Assert.IsFalse(ch.CanCast(1));
    }

    [Test]
    public void CanCast_AfterUnlock_WithSufficientMp_ReturnsTrue()
    {
        var s0 = MakeSkill("s0", mpCost: 10);
        var s1 = MakeSkill("s1", mpCost: 10);
        var oc = new OwnedCharacter { id = "c" };
        var ch = MakeCharWithOwned(oc, s0, s1);

        ch.Unlock(1);

        Assert.IsTrue(ch.CanCast(1));
    }

    [Test]
    public void CanCast_DefaultSkill_Index0_ReturnsTrue()
    {
        var s0 = MakeSkill("s0", mpCost: 10);
        var oc = new OwnedCharacter { id = "c" }; // 빈 목록 → 기본 해금 폴백
        var ch = MakeCharWithOwned(oc, s0);

        Assert.IsTrue(ch.CanCast(0));
    }
}
