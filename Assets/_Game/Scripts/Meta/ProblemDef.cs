using UnityEngine;

/// <summary>
/// 스킬 해금에 필요한 개념 문제 1개의 정의 (설계 §5-2).
/// SkillDef.id 와 1:1 연결. ProblemDatabase 를 통해 조회한다.
/// </summary>
[CreateAssetMenu(menuName = "MSRPG/Problem Definition", fileName = "New Problem")]
public class ProblemDef : ScriptableObject
{
    [Header("연결")]
    public string id;
    public string skillId; // 스킬 해금용. 레벨업 문제는 비워둠

    [Header("레벨업 난이도 (skillId가 비어 있을 때 유효)")]
    public ProblemDifficulty difficulty;

    [Header("문제")]
    public ProblemType type;
    [TextArea(2, 6)]
    public string prompt;

    [Header("객관식 (MultipleChoice)")]
    public string[] choices       = new string[4];
    public int      correctIndex  = 0; // choices 배열 인덱스

    [Header("주관식 (FreeInput)")]
    [Tooltip("모두 정규화(소문자·trim·공백압축) 후 비교. 여러 정답 허용.")]
    public string[] acceptedAnswers = new string[0];

    [Header("정답 후 설명 (선택)")]
    [TextArea(1, 4)]
    public string explanation;
}
