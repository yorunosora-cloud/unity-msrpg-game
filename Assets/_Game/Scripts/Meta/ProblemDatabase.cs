using UnityEngine;

/// <summary>
/// 모든 ProblemDef 를 담는 데이터베이스.
/// Resources/ProblemDatabase.asset 으로 저장해 Resources.Load 로 접근한다.
/// </summary>
[CreateAssetMenu(menuName = "MSRPG/Problem Database", fileName = "ProblemDatabase")]
public class ProblemDatabase : ScriptableObject
{
    [SerializeField] ProblemDef[] problems = new ProblemDef[0];

    public ProblemDef[] All => problems;

    /// <summary>skillId 로 문제 검색. 없으면 null.</summary>
    public ProblemDef BySkillId(string skillId)
    {
        foreach (var p in problems)
            if (p != null && p.skillId == skillId) return p;
        return null;
    }

    /// <summary>에디터 전용 — CharacterSeedSetup 에서 호출.</summary>
    public void SetProblems(ProblemDef[] defs) => problems = defs;
}
