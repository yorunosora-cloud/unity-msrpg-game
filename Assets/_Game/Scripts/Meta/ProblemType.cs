/// <summary>문제 형식 (설계 §5-2).</summary>
public enum ProblemType
{
    MultipleChoice, // 4지선다 객관식
    FreeInput,      // 주관식 단답형
}

/// <summary>레벨업 문제 난이도. 하/중/상 = EXP 보상 80/200/500.</summary>
public enum ProblemDifficulty
{
    Low,  // 하
    Mid,  // 중
    High, // 상
}

/// <summary>
/// ProblemDifficulty 관련 상수를 한 곳에서 관리한다.
/// RnEPanel 과 MetaUISetup 양쪽에서 재사용.
/// </summary>
public static class ProblemDifficultyInfo
{
    /// <summary>난이도별 정답 EXP 보상 (하80/중200/상500).</summary>
    public static int ExpReward(ProblemDifficulty d) => d switch
    {
        ProblemDifficulty.Low  => 80,
        ProblemDifficulty.Mid  => 200,
        ProblemDifficulty.High => 500,
        _                      => 80,
    };

    /// <summary>기본 비용(level×5)에 곱할 배수 (하×1/중×2/상×3).</summary>
    public static int CostMultiplier(ProblemDifficulty d) => d switch
    {
        ProblemDifficulty.Low  => 1,
        ProblemDifficulty.Mid  => 2,
        ProblemDifficulty.High => 3,
        _                      => 1,
    };

    /// <summary>한글 라벨 (하/중/상).</summary>
    public static string Label(ProblemDifficulty d) => d switch
    {
        ProblemDifficulty.Low  => "하",
        ProblemDifficulty.Mid  => "중",
        ProblemDifficulty.High => "상",
        _                      => "하",
    };
}
