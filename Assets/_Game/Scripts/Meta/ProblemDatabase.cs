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

    /// <summary>
    /// skillId 로 스킬 해금 문제 검색(첫 매칭 1개). 빈 문자열이나 null이면 null 반환.
    /// [Deprecated] 스킬 해금·레벨업이 풀을 공유하도록 통합되어
    /// <see cref="RandomBySkillDifficulty"/>(skillId, Low)로 대체됨. 하위 호환용으로만 유지.
    /// </summary>
    public ProblemDef BySkillId(string skillId)
    {
        if (string.IsNullOrEmpty(skillId)) return null;
        foreach (var p in problems)
            if (p != null && p.skillId == skillId) return p;
        return null;
    }

    /// <summary>skillId 에 속한 모든 문제(전 난이도 포함)를 반환한다. 문제탭 카운트 집계용.</summary>
    public ProblemDef[] AllBySkill(string skillId)
    {
        var list = new System.Collections.Generic.List<ProblemDef>();
        if (string.IsNullOrEmpty(skillId)) return list.ToArray();
        foreach (var p in problems)
            if (p != null && p.skillId == skillId) list.Add(p);
        return list.ToArray();
    }

    /// <summary>
    /// 스킬 해금·스킬 레벨업 공용 풀에서 skillId+난이도가 일치하는 문제 중 랜덤 1개를 반환한다.
    /// 후보가 없으면 null.
    /// </summary>
    public ProblemDef RandomBySkillDifficulty(string skillId, ProblemDifficulty difficulty)
    {
        if (string.IsNullOrEmpty(skillId)) return null;
        var candidates = new System.Collections.Generic.List<ProblemDef>();
        foreach (var p in problems)
            if (p != null && p.skillId == skillId && p.difficulty == difficulty)
                candidates.Add(p);
        if (candidates.Count == 0) return null;
        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
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

    /// <summary>id로 문제 검색. 없으면 null. (문제탭 편집 폼 프리필용)</summary>
    public ProblemDef ById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (var p in problems)
            if (p != null && p.id == id) return p;
        return null;
    }

    /// <summary>id가 같으면 교체, 없으면 추가한다. (CloudScript 쓰기 성공 후 세션 즉시 반영용)</summary>
    public void Upsert(ProblemDef def)
    {
        if (def == null || string.IsNullOrEmpty(def.id)) return;
        var list = new System.Collections.Generic.List<ProblemDef>(problems);
        int idx = list.FindIndex(p => p != null && p.id == def.id);
        if (idx >= 0) list[idx] = def; else list.Add(def);
        problems = list.ToArray();
    }

    /// <summary>id가 일치하는 문제를 제거한다. 없으면 아무 일도 하지 않는다.</summary>
    public void Remove(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        var list = new System.Collections.Generic.List<ProblemDef>(problems);
        list.RemoveAll(p => p == null || p.id == id);
        problems = list.ToArray();
    }

    /// <summary>에디터 전용 — CharacterSeedSetup 에서 호출.</summary>
    public void SetProblems(ProblemDef[] defs) => problems = defs;
}
