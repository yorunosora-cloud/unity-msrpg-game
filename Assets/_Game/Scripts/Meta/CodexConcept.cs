/// <summary>
/// 2단 도감의 순수 로직 (설계도 18 §3-12, 18-C). UI 의존 없음.
/// 기본 해설은 항상 열려 있고, 심화 해설(+실제 원리·출처)은 개념 문제를 맞히면 열린다.
/// </summary>
public static class CodexConcept
{
    public static bool HasQuestion(CharacterDef def)
        => def != null && def.conceptQuestions != null && def.conceptQuestions.Length > 0;

    /// <summary>교육 문구·문제 중 하나라도 있으면 true.</summary>
    public static bool HasContent(CharacterDef def)
        => def != null && (!string.IsNullOrEmpty(def.basicExplanation) || !string.IsNullOrEmpty(def.advancedExplanation)
                           || !string.IsNullOrEmpty(def.realPrinciple) || HasQuestion(def));

    /// <summary>
    /// 심화 해설이 열려 있는지. 소유하지 않으면 false, 개념 문제가 없는 캐릭터는 잠그지 않는다(영구 잠김 방지).
    /// </summary>
    public static bool IsAdvancedOpen(CharacterDef def, OwnedCharacter owned)
    {
        if (def == null || owned == null) return false;
        return owned.advancedCodexUnlocked || !HasQuestion(def);
    }

    /// <summary>시도 횟수에 따라 보여 줄 문제 번호(순환). 문제가 없으면 -1.</summary>
    public static int PickQuestionIndex(CharacterDef def, int attempt)
    {
        if (!HasQuestion(def)) return -1;
        if (attempt < 0) attempt = 0;
        return attempt % def.conceptQuestions.Length;
    }

    /// <summary>
    /// 정답이면 true를 반환하고 심화 해설을 연다(플래그 설정, 저장은 호출자 책임). 오답·잘못된 인덱스는 false.
    /// 한 번 열린 플래그는 오답으로 다시 잠기지 않는다.
    /// </summary>
    public static bool TryAnswer(CharacterDef def, OwnedCharacter owned, int questionIndex, int choiceIndex)
    {
        if (!HasQuestion(def) || owned == null) return false;
        if (questionIndex < 0 || questionIndex >= def.conceptQuestions.Length) return false;

        var q = def.conceptQuestions[questionIndex];
        if (q.choices == null || choiceIndex < 0 || choiceIndex >= q.choices.Length) return false;
        if (choiceIndex != q.answerIndex) return false;

        owned.advancedCodexUnlocked = true;
        return true;
    }
}
