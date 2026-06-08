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

    /// <summary>skillId 로 스킬 해금 문제 검색. 빈 문자열이나 null이면 null 반환.</summary>
    public ProblemDef BySkillId(string skillId)
    {
        if (string.IsNullOrEmpty(skillId)) return null;
        foreach (var p in problems)
            if (p != null && p.skillId == skillId) return p;
        return null;
    }

    /// <summary>
    /// 레벨업용 문제 중 지정 난이도를 랜덤 반환.
    /// skillId가 비어 있어야 레벨업 문제로 간주한다. 없으면 null.
    /// </summary>
    public ProblemDef RandomByDifficulty(ProblemDifficulty difficulty)
    {
        var candidates = new System.Collections.Generic.List<ProblemDef>();
        foreach (var p in problems)
        {
            if (p != null && string.IsNullOrEmpty(p.skillId) && p.difficulty == difficulty)
                candidates.Add(p);
        }
        if (candidates.Count == 0) return null;
        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    /// <summary>에디터 전용 — CharacterSeedSetup 에서 호출.</summary>
    public void SetProblems(ProblemDef[] defs) => problems = defs;
}
