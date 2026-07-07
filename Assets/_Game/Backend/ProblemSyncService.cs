// ⚠️ PlayFab SDK가 필요합니다.

using System;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.Json;

/// <summary>
/// TitleData "problems" 델타를 로컬 ProblemDatabase에 병합하고,
/// 관리자의 추가/삭제를 CloudScript(UpsertProblem/DeleteProblem)로 반영한다.
/// </summary>
public static class ProblemSyncService
{
    const string TITLE_DATA_KEY = "problems";

    /// <summary>부팅 시 1회 호출 — TitleData를 받아 로컬 시드와 병합한다.</summary>
    public static void Load(Action onDone = null, Action<string> onError = null)
    {
        var db = Resources.Load<ProblemDatabase>("ProblemDatabase");
        if (db == null) { onDone?.Invoke(); return; }
        if (!PlayFabClientAPI.IsClientLoggedIn()) { onDone?.Invoke(); return; }

        PlayFabClientAPI.GetTitleData(
            new GetTitleDataRequest { Keys = new List<string> { TITLE_DATA_KEY } },
            result =>
            {
                ProblemStore store = null;
                if (result.Data != null
                    && result.Data.TryGetValue(TITLE_DATA_KEY, out var json)
                    && !string.IsNullOrEmpty(json))
                {
                    store = JsonUtility.FromJson<ProblemStore>(json);
                }

                db.SetProblems(ProblemMerge.Apply(db.All, store));
                Debug.Log($"[ProblemSync] 문제 동기화 완료 (총 {db.All.Length}개)");
                onDone?.Invoke();
            },
            err =>
            {
                Debug.LogWarning($"[ProblemSync] 로드 실패: {err.GenerateErrorReport()}");
                onError?.Invoke(err.GenerateErrorReport());
            });
    }

    /// <summary>문제 추가/수정. 서버 성공 시 현재 세션의 ProblemDatabase에도 즉시 반영한다.</summary>
    public static void Upsert(ProblemDef def, Action onDone = null, Action<string> onError = null)
    {
        string key = MetaState.AdminKey;
        if (string.IsNullOrEmpty(key)) { onError?.Invoke("관리자 키가 없습니다. 관리자 로그인을 먼저 하세요."); return; }

        var dto = ProblemDTO.FromDef(def);
        CloudScriptService.Execute(
            "UpsertProblem",
            new { key, problem = dto },
            result =>
            {
                if (!IsSuccess(result)) { onError?.Invoke("서버가 요청을 거부했습니다."); return; }
                Resources.Load<ProblemDatabase>("ProblemDatabase")?.Upsert(def);
                onDone?.Invoke();
            },
            onError);
    }

    /// <summary>문제 삭제. 서버 성공 시 현재 세션의 ProblemDatabase에서도 즉시 제거한다.</summary>
    public static void Delete(string id, Action onDone = null, Action<string> onError = null)
    {
        string key = MetaState.AdminKey;
        if (string.IsNullOrEmpty(key)) { onError?.Invoke("관리자 키가 없습니다. 관리자 로그인을 먼저 하세요."); return; }

        CloudScriptService.Execute(
            "DeleteProblem",
            new { key, id },
            result =>
            {
                if (!IsSuccess(result)) { onError?.Invoke("서버가 요청을 거부했습니다."); return; }
                Resources.Load<ProblemDatabase>("ProblemDatabase")?.Remove(id);
                onDone?.Invoke();
            },
            onError);
    }

    static bool IsSuccess(JsonObject result)
        => result != null
        && result.TryGetValue("success", out var val)
        && bool.TryParse(val.ToString(), out var b)
        && b;
}
