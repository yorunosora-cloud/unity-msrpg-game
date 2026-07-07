using System.Collections.Generic;

/// <summary>
/// 로컬 시드 문제와 TitleData 델타(추가/수정 + 삭제표시)를 병합하는 순수 함수.
/// 네트워크·ScriptableObject 자산 I/O와 분리해 단위 테스트 가능하게 한다.
/// </summary>
public static class ProblemMerge
{
    public static ProblemDef[] Apply(ProblemDef[] seedProblems, ProblemStore store)
    {
        var byId = new Dictionary<string, ProblemDef>();
        if (seedProblems != null)
            foreach (var p in seedProblems)
                if (p != null && !string.IsNullOrEmpty(p.id))
                    byId[p.id] = p;

        if (store != null && store.items != null)
            foreach (var dto in store.items)
            {
                if (dto == null || string.IsNullOrEmpty(dto.id)) continue;
                var def = UnityEngine.ScriptableObject.CreateInstance<ProblemDef>();
                dto.ApplyTo(def);
                byId[dto.id] = def;
            }

        if (store != null && store.deletedIds != null)
            foreach (var id in store.deletedIds)
                if (!string.IsNullOrEmpty(id))
                    byId.Remove(id);

        var result = new ProblemDef[byId.Count];
        byId.Values.CopyTo(result, 0);
        return result;
    }
}
