using System.Text.RegularExpressions;

/// <summary>
/// 문제 정답 여부를 판단하는 순수 로직. UI 의존 없음 → EditMode 테스트 가능.
/// </summary>
public static class ProblemChecker
{
    /// <summary>
    /// 정답 여부 반환.
    /// 객관식: selectedIndex 가 correctIndex 와 일치하면 true.
    /// 주관식: answer 를 정규화한 값이 acceptedAnswers 중 하나와 일치하면 true.
    /// </summary>
    public static bool Check(ProblemDef def, string answer, int selectedIndex)
    {
        if (def == null) return false;

        if (def.type == ProblemType.MultipleChoice)
            return selectedIndex == def.correctIndex;

        // FreeInput — 정규화 비교
        string normalized = Normalize(answer);
        foreach (var accepted in def.acceptedAnswers)
            if (Normalize(accepted) == normalized) return true;
        return false;
    }

    static string Normalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        s = s.Trim().ToLowerInvariant();
        s = Regex.Replace(s, @"\s+", " ");
        return s;
    }
}
