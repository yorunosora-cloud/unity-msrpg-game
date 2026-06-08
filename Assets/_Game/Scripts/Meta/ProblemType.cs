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
