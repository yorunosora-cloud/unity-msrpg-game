using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// MSRPG > Import Character CSVs (설계도 18 §8, 18-B).
/// Resources/Data의 관계형 CSV 시트를 읽어 CharacterDef/SkillDef/TagRuleTable 에셋을 생성·제자리 갱신하고
/// CharacterDatabase에 병합한다. 파싱·매핑·검증은 런타임의 CharacterCsvBuilder가 하며, 이 클래스는 파일 I/O와 에셋 쓰기만 한다.
/// 오류가 하나라도 있으면 에셋을 전혀 쓰지 않는다.
/// </summary>
public static class CharacterCsvImporter
{
    const string CSV_DIR   = "Assets/_Game/Resources/Data";
    const string CHAR_DIR  = "Assets/_Game/Data/Characters";
    const string SKILL_DIR = "Assets/_Game/Data/Skills";
    const string TAG_PATH  = "Assets/_Game/Resources/TagRuleTable.asset";
    const string COMBO_PATH = "Assets/_Game/Resources/SynergyComboTable.asset";
    const string DB_PATH   = "Assets/_Game/Resources/CharacterDatabase.asset";
    const string PROBLEM_DB_PATH = "Assets/_Game/Resources/ProblemDatabase.asset";
    const string LABEL     = "CsvImported";     // 임포터가 만든 에셋 표시(고아 감지·시드와 구분)

    [MenuItem("MSRPG/Import Character CSVs")]
    public static void Run() => Import(batch: false);

    /// <summary>배치 모드(-executeMethod CharacterCsvImporter.RunBatch). 오류가 있으면 종료 코드 1.</summary>
    public static void RunBatch()
    {
        bool ok = Import(batch: true);
        EditorApplication.Exit(ok ? 0 : 1);
    }

    static bool Import(bool batch)
    {
        // 1. 시트 읽기
        var sheets = new Dictionary<string, string>();
        foreach (var name in CharacterCsvBuilder.AllSheets)
        {
            string path = Path.Combine(CsvFullDir(), name);
            if (File.Exists(path)) sheets[name] = File.ReadAllText(path, Encoding.UTF8);
        }

        // 2. 빌드·검증 (오류가 있으면 중단)
        var res = CharacterCsvBuilder.Build(sheets);
        foreach (var w in res.report.Warnings) Debug.LogWarning("[CSV 임포트] " + w);
        if (res.report.HasErrors)
        {
            foreach (var e in res.report.Errors) Debug.LogError("[CSV 임포트] " + e);
            Debug.LogError($"[CSV 임포트] 오류 {res.report.Errors.Count}건 — 에셋을 쓰지 않았습니다.");
            if (!batch) EditorUtility.DisplayDialog("CSV 임포트 실패", $"오류 {res.report.Errors.Count}건. Console을 확인하세요.", "확인");
            return false;
        }

        EnsureDir(CHAR_DIR); EnsureDir(SKILL_DIR); EnsureDir(Path.GetDirectoryName(TAG_PATH).Replace('\\', '/'));

        // 3. 에셋 쓰기: 태그 → 스킬 → 캐릭터
        WriteTagRules(res.tagRules);

        var skillAssets = new Dictionary<SkillDef, SkillDef>();
        foreach (var s in res.skills)
            skillAssets[s] = Upsert(s, $"{SKILL_DIR}/{s.id}.asset", out _);

        WriteCombos(res.combos, skillAssets);

        var charAssets = new List<CharacterDef>();
        var importedIds = new HashSet<string>();
        foreach (var c in res.characters)
        {
            c.skills = c.skills.Select(s => skillAssets[s]).ToArray();           // 메모리 SkillDef → 에셋 참조
            var asset = Upsert(c, $"{CHAR_DIR}/{c.id}.asset", out bool wasSeedOrOther);
            if (wasSeedOrOther) WarnOverwrite(asset);
            charAssets.Add(asset);
            importedIds.Add(c.id);
        }

        // 4. DB 병합·경고
        MergeCharacterDatabase(charAssets);
        WarnOrphans(importedIds, new HashSet<string>(res.skills.Select(s => s.id)));
        WarnLockedSkills(res.skills);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[CSV 임포트] 완료 — 캐릭터 {res.characters.Count}, 스킬 {res.skills.Count}, 태그 {res.tagRules.Length}, 연계 {res.combos.Count}, 경고 {res.report.Warnings.Count}건");
        if (!batch)
            EditorUtility.DisplayDialog("CSV 임포트 완료", $"캐릭터 {res.characters.Count} · 스킬 {res.skills.Count} · 태그 {res.tagRules.Length}\n경고 {res.report.Warnings.Count}건 (Console 확인)", "확인");
        return true;
    }

    // ── 에셋 쓰기 ─────────────────────────────────────────────────────────

    static string CsvFullDir() => Path.Combine(Application.dataPath, "_Game/Resources/Data");

    static void EnsureDir(string assetDir) => Directory.CreateDirectory(assetDir.Replace("Assets", Application.dataPath));

    /// <summary>같은 경로의 에셋이 있으면 제자리 갱신(GUID·참조 유지), 없으면 생성. 반환 = 에셋 인스턴스.</summary>
    static T Upsert<T>(T src, string path, out bool existedWithoutLabel) where T : ScriptableObject
    {
        existedWithoutLabel = false;
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(src, path);
            AssetDatabase.SetLabels(src, new[] { LABEL });
            return src;
        }

        existedWithoutLabel = !AssetDatabase.GetLabels(existing).Contains(LABEL);
        EditorUtility.CopySerialized(src, existing);
        AssetDatabase.SetLabels(existing, new[] { LABEL });
        EditorUtility.SetDirty(existing);
        return existing;
    }

    static void WriteCombos(List<SynergyComboDef> combos, Dictionary<SkillDef, SkillDef> skillAssets)
    {
        if (combos == null || combos.Count == 0) return;
        var tmp = ScriptableObject.CreateInstance<SynergyComboTable>();
        tmp.name = "SynergyComboTable";
        tmp.combos = combos.Select(c => new SynergyComboDef
        {
            id = c.id, comboName = c.comboName, formula = c.formula, markId = c.markId,
            consumerId = c.consumerId, triggerSkillId = c.triggerSkillId, comboSkill = skillAssets[c.comboSkill],   // 메모리 SkillDef → 에셋 참조
        }).ToArray();
        Upsert(tmp, COMBO_PATH, out _);
    }

    static void WriteTagRules(TagRule[] rules)
    {
        if (rules == null || rules.Length == 0) return;
        var tmp = ScriptableObject.CreateInstance<TagRuleTable>();
        tmp.name = "TagRuleTable";
        tmp.rules = rules;
        Upsert(tmp, TAG_PATH, out _);
    }

    static void MergeCharacterDatabase(List<CharacterDef> imported)
    {
        var db = AssetDatabase.LoadAssetAtPath<CharacterDatabase>(DB_PATH);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<CharacterDatabase>();
            AssetDatabase.CreateAsset(db, DB_PATH);
        }

        var list = db.All.Where(c => c != null).ToList();                       // 기존 항목·순서 유지
        foreach (var c in imported.OrderBy(c => c.dexNumber).ThenBy(c => c.id))
            if (!list.Contains(c)) list.Add(c);

        var so  = new SerializedObject(db);
        var arr = so.FindProperty("characters");
        arr.arraySize = list.Count;
        for (int i = 0; i < list.Count; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(db);
    }

    // ── 경고 ──────────────────────────────────────────────────────────────

    static void WarnOverwrite(CharacterDef asset)
        => Debug.LogWarning($"[CSV 임포트] 기존 비-임포트(시드) 에셋 '{asset.id}'를 덮어썼습니다. 스킬 id가 달라지면 기존 세이브의 해금·숙련도 기록이 고아가 됩니다.");

    static void WarnOrphans(HashSet<string> charIds, HashSet<string> skillIds)
    {
        foreach (var guid in AssetDatabase.FindAssets("l:" + LABEL + " t:CharacterDef", new[] { CHAR_DIR }))
        {
            var c = AssetDatabase.LoadAssetAtPath<CharacterDef>(AssetDatabase.GUIDToAssetPath(guid));
            if (c != null && !charIds.Contains(c.id))
                Debug.LogWarning($"[CSV 임포트] CSV에서 사라진 캐릭터 에셋 '{c.id}'가 남아 있습니다(자동 삭제하지 않음).");
        }
        foreach (var guid in AssetDatabase.FindAssets("l:" + LABEL + " t:SkillDef", new[] { SKILL_DIR }))
        {
            var s = AssetDatabase.LoadAssetAtPath<SkillDef>(AssetDatabase.GUIDToAssetPath(guid));
            if (s != null && !skillIds.Contains(s.id))
                Debug.LogWarning($"[CSV 임포트] CSV에서 사라진 스킬 에셋 '{s.id}'가 남아 있습니다(자동 삭제하지 않음).");
        }
    }

    /// <summary>startUnlocked=false인 액티브 스킬에 해금 문제가 없으면 영구히 잠긴다 — 경고.</summary>
    static void WarnLockedSkills(List<SkillDef> skills)
    {
        var pdb = AssetDatabase.LoadAssetAtPath<ProblemDatabase>(PROBLEM_DB_PATH);
        var solvable = new HashSet<string>();
        if (pdb != null) foreach (var p in pdb.All) if (p != null && !string.IsNullOrEmpty(p.skillId)) solvable.Add(p.skillId);

        foreach (var s in skills)
            if (s.skillType == SkillKind.Active && !s.startUnlocked && !solvable.Contains(s.id))
                Debug.LogWarning($"[CSV 임포트] 스킬 '{s.id}'는 startUnlocked=false인데 해금 문제가 ProblemDatabase에 없어 사용할 수 없습니다.");
    }
}
