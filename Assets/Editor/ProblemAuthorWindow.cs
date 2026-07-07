using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 문제(Problem) 저작 전용 에디터 창. MSRPG > Problem Author.
/// 로컬 .asset 저장 + TitleData 발행(원시 UnityWebRequest로 PlayFab REST를
/// 직접 호출 — PlayFabHttp는 Play 모드 코루틴에 의존하므로 에디터 단독으로는
/// 응답을 받을 수 없어 SDK를 우회한다).
/// </summary>
public class ProblemAuthorWindow : EditorWindow
{
    const string PROBLEM_DIR     = "Assets/_Game/Data/Problems";
    const string PROBLEM_DB_PATH = "Assets/_Game/Resources/ProblemDatabase.asset";
    const string CHAR_DB_PATH    = "Assets/_Game/Resources/CharacterDatabase.asset";
    const string PREF_SECRET_KEY = "MSRPG_PlayFabDevSecretKey";

    static readonly Continent[] _subjects =
    {
        Continent.Physics, Continent.Chemistry, Continent.Biology,
        Continent.EarthSci, Continent.Math, Continent.Info,
    };

    string   _id = "";
    int      _purpose;
    int      _subjectIdx;
    string   _country = "";
    int      _skillIdx;
    int      _difficulty;
    int      _type;
    string   _prompt = "";
    string[] _choices = new string[4];
    int      _correctIndex;
    string   _acceptedAnswersRaw = "";
    string   _explanation = "";

    string[] _skillLabels = new string[0];
    string[] _skillIds    = new string[0];

    string _secretKey = "";
    string _status    = "";

    [MenuItem("MSRPG/Problem Author")]
    public static void Open() => GetWindow<ProblemAuthorWindow>("Problem Author");

    void OnEnable()
    {
        _secretKey = EditorPrefs.GetString(PREF_SECRET_KEY, "");
        RefreshSkillList();
    }

    void RefreshSkillList()
    {
        var db = AssetDatabase.LoadAssetAtPath<CharacterDatabase>(CHAR_DB_PATH);
        var labels = new List<string>();
        var ids    = new List<string>();
        if (db != null)
        {
            foreach (var def in db.All)
            {
                if (def == null || def.skills == null) continue;
                foreach (var skill in def.skills)
                {
                    if (skill == null) continue;
                    labels.Add($"{def.nameKo} - {skill.nameKo}");
                    ids.Add(skill.id);
                }
            }
        }
        _skillLabels = labels.ToArray();
        _skillIds    = ids.ToArray();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("문제 저작", EditorStyles.boldLabel);
        _id = EditorGUILayout.TextField("ID (비우면 자동 생성)", _id);

        _purpose = EditorGUILayout.Popup("용도", _purpose, new[] { "레벨업", "스킬" });
        if (_purpose == 0)
        {
            _subjectIdx = EditorGUILayout.Popup("과목", _subjectIdx, LabelsFor(_subjects));
            _country    = EditorGUILayout.TextField("국가(슬러그)", _country);
        }
        else
        {
            if (GUILayout.Button("스킬 목록 새로고침")) RefreshSkillList();
            _skillIdx = EditorGUILayout.Popup("스킬", _skillIdx,
                _skillLabels.Length > 0 ? _skillLabels : new[] { "(스킬 없음)" });
        }

        _difficulty = EditorGUILayout.Popup("난이도", _difficulty, new[] { "하", "중", "상" });
        _type       = EditorGUILayout.Popup("유형", _type, new[] { "객관식", "주관식" });

        EditorGUILayout.LabelField("지문");
        _prompt = EditorGUILayout.TextArea(_prompt, GUILayout.Height(50));

        if (_type == 0)
        {
            for (int i = 0; i < 4; i++)
                _choices[i] = EditorGUILayout.TextField($"보기 {i + 1}", _choices[i]);
            _correctIndex = EditorGUILayout.Popup("정답", _correctIndex,
                new[] { "보기 1", "보기 2", "보기 3", "보기 4" });
        }
        else
        {
            _acceptedAnswersRaw = EditorGUILayout.TextField("정답(콤마 구분)", _acceptedAnswersRaw);
        }

        EditorGUILayout.LabelField("해설");
        _explanation = EditorGUILayout.TextArea(_explanation, GUILayout.Height(40));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("PlayFab 발행 (선택)", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        _secretKey = EditorGUILayout.PasswordField("Developer Secret Key", _secretKey);
        if (EditorGUI.EndChangeCheck())
            EditorPrefs.SetString(PREF_SECRET_KEY, _secretKey);

        EditorGUILayout.Space();
        if (GUILayout.Button("로컬 .asset로 저장")) SaveLocalAsset();
        if (GUILayout.Button("이 문제를 TitleData에 발행")) PublishOne();
        if (GUILayout.Button("현재 DB 전체를 TitleData로 발행(백필)")) PublishAll();

        if (!string.IsNullOrEmpty(_status))
            EditorGUILayout.HelpBox(_status, MessageType.Info);
    }

    static string[] LabelsFor(Continent[] subjects)
    {
        var arr = new string[subjects.Length];
        for (int i = 0; i < subjects.Length; i++) arr[i] = ProblemTab.ContinentLabel(subjects[i]);
        return arr;
    }

    ProblemDTO BuildDto()
    {
        string id = string.IsNullOrWhiteSpace(_id) ? "usr_" + Guid.NewGuid().ToString("N").Substring(0, 10) : _id.Trim();
        var dto = new ProblemDTO
        {
            id           = id,
            difficulty   = _difficulty,
            type         = _type,
            prompt       = _prompt,
            choices      = (string[])_choices.Clone(),
            correctIndex = _correctIndex,
            explanation  = _explanation,
        };

        if (_purpose == 1)
        {
            dto.skillId = _skillIds.Length > 0 && _skillIdx < _skillIds.Length ? _skillIds[_skillIdx] : "";
            dto.subject = 0;
            dto.country = "";
        }
        else
        {
            dto.skillId = "";
            dto.subject = (int)_subjects[_subjectIdx];
            dto.country = _country.Trim();
        }

        dto.acceptedAnswers = !string.IsNullOrWhiteSpace(_acceptedAnswersRaw)
            ? Array.ConvertAll(_acceptedAnswersRaw.Split(','), s => s.Trim())
            : new string[0];

        return dto;
    }

    // ── 로컬 저장 ─────────────────────────────────────────────────────────

    void SaveLocalAsset()
    {
        var dto = BuildDto();
        Directory.CreateDirectory(PROBLEM_DIR.Replace("Assets", Application.dataPath));

        string path = $"{PROBLEM_DIR}/{dto.id}.asset";
        var def = AssetDatabase.LoadAssetAtPath<ProblemDef>(path);
        bool isNew = def == null;
        if (isNew) def = ScriptableObject.CreateInstance<ProblemDef>();
        dto.ApplyTo(def);

        if (isNew) AssetDatabase.CreateAsset(def, path);
        else       EditorUtility.SetDirty(def);

        var db = AssetDatabase.LoadAssetAtPath<ProblemDatabase>(PROBLEM_DB_PATH);
        if (db != null)
        {
            var so = new SerializedObject(db);
            var problemsProp = so.FindProperty("problems");
            bool alreadyIn = false;
            for (int i = 0; i < problemsProp.arraySize; i++)
                if (problemsProp.GetArrayElementAtIndex(i).objectReferenceValue == def) { alreadyIn = true; break; }
            if (!alreadyIn)
            {
                problemsProp.arraySize++;
                problemsProp.GetArrayElementAtIndex(problemsProp.arraySize - 1).objectReferenceValue = def;
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(db);
        }

        AssetDatabase.SaveAssets();
        _id     = dto.id;
        _status = $"로컬 저장 완료: {path}";
    }

    // ── TitleData 발행 ────────────────────────────────────────────────────

    void PublishOne()
    {
        if (string.IsNullOrEmpty(_secretKey)) { _status = "Developer Secret Key를 입력하세요."; return; }
        try
        {
            var store = FetchStore();
            var dto   = BuildDto();
            UpsertIntoStore(store, dto);
            PushStore(store);
            _id     = dto.id;
            _status = $"TitleData 발행 완료: {dto.id}";
        }
        catch (Exception e)
        {
            _status = $"발행 실패: {e.Message}";
        }
    }

    void PublishAll()
    {
        if (string.IsNullOrEmpty(_secretKey)) { _status = "Developer Secret Key를 입력하세요."; return; }
        var db = AssetDatabase.LoadAssetAtPath<ProblemDatabase>(PROBLEM_DB_PATH);
        if (db == null) { _status = "ProblemDatabase.asset을 찾을 수 없습니다."; return; }

        try
        {
            var store = new ProblemStore { items = new ProblemDTO[db.All.Length], deletedIds = new string[0] };
            for (int i = 0; i < db.All.Length; i++)
                store.items[i] = ProblemDTO.FromDef(db.All[i]);
            PushStore(store);
            _status = $"전체 발행 완료: {store.items.Length}개";
        }
        catch (Exception e)
        {
            _status = $"전체 발행 실패: {e.Message}";
        }
    }

    static void UpsertIntoStore(ProblemStore store, ProblemDTO dto)
    {
        var items = new List<ProblemDTO>(store.items ?? new ProblemDTO[0]);
        int idx = items.FindIndex(p => p != null && p.id == dto.id);
        if (idx >= 0) items[idx] = dto; else items.Add(dto);
        store.items = items.ToArray();

        var deleted = new List<string>(store.deletedIds ?? new string[0]);
        deleted.Remove(dto.id);
        store.deletedIds = deleted.ToArray();
    }

    ProblemStore FetchStore()
    {
        string titleId = PlayFab.PlayFabSettings.TitleId;
        string url  = $"https://{titleId}.playfabapi.com/Admin/GetTitleData";
        string body = JsonUtility.ToJson(new GetTitleDataReq { Keys = new[] { "problems" } });
        string resp = SendRequest(url, body);

        var env = JsonUtility.FromJson<GetTitleDataEnvelope>(resp);
        if (env?.data?.Data == null || string.IsNullOrEmpty(env.data.Data.problems))
            return new ProblemStore();
        return JsonUtility.FromJson<ProblemStore>(env.data.Data.problems);
    }

    void PushStore(ProblemStore store)
    {
        string titleId = PlayFab.PlayFabSettings.TitleId;
        string url  = $"https://{titleId}.playfabapi.com/Admin/SetTitleData";
        string body = JsonUtility.ToJson(new SetTitleDataReq { Key = "problems", Value = JsonUtility.ToJson(store) });
        SendRequest(url, body);
    }

    string SendRequest(string url, string jsonBody)
    {
        var req = new UnityWebRequest(url, "POST");
        byte[] raw = Encoding.UTF8.GetBytes(jsonBody);
        req.uploadHandler   = new UploadHandlerRaw(raw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("X-SecretKey", _secretKey);

        var op = req.SendWebRequest();
        while (!op.isDone) { /* 에디터 도구 — 블로킹 대기(짧은 요청이라 허용) */ }

        if (req.result != UnityWebRequest.Result.Success)
            throw new Exception($"{req.responseCode}: {req.downloadHandler.text}");

        return req.downloadHandler.text;
    }

    [Serializable] class GetTitleDataReq { public string[] Keys; }
    [Serializable] class TitleDataPayload { public string problems; }
    [Serializable] class GetTitleDataBody { public TitleDataPayload Data; }
    [Serializable] class GetTitleDataEnvelope { public int code; public string status; public GetTitleDataBody data; }
    [Serializable] class SetTitleDataReq { public string Key; public string Value; }
}
