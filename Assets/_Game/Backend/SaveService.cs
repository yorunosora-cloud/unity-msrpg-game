// ⚠️ PlayFab SDK가 필요합니다.

using System;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

/// <summary>
/// PlayerStats를 PlayFab UserData(JSON)로 저장·복원합니다.
/// 로그인 성공 이후에만 호출하세요.
/// </summary>
public static class SaveService
{
    const string STATS_KEY = "stats";

    // ── 저장 ──────────────────────────────────────────────────────────────────

    /// <summary>현재 PlayerStats를 서버에 저장합니다.</summary>
    public static void Save(PlayerStats stats,
                            Action       onDone  = null,
                            Action<string> onError = null)
    {
        string json = JsonUtility.ToJson(stats.Export());

        PlayFabClientAPI.UpdateUserData(
            new UpdateUserDataRequest
            {
                Data = new Dictionary<string, string> { { STATS_KEY, json } }
            },
            _ =>
            {
                Debug.Log("[SaveService] 저장 완료");
                onDone?.Invoke();
            },
            err =>
            {
                Debug.LogWarning($"[SaveService] 저장 실패: {err.GenerateErrorReport()}");
                onError?.Invoke(err.GenerateErrorReport());
            });
    }

    // ── 복원 ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// 서버에서 PlayerStats를 불러와 복원합니다.
    /// 저장 데이터가 없으면 초기값을 그대로 유지합니다(신규 플레이어).
    /// </summary>
    public static void Load(PlayerStats    stats,
                            Action         onDone  = null,
                            Action<string> onError = null)
    {
        PlayFabClientAPI.GetUserData(
            new GetUserDataRequest { Keys = new List<string> { STATS_KEY } },
            result =>
            {
                if (result.Data != null && result.Data.TryGetValue(STATS_KEY, out var entry))
                {
                    StatsData data = JsonUtility.FromJson<StatsData>(entry.Value);
                    stats.LoadState(data);
                    Debug.Log("[SaveService] 복원 완료");
                }
                else
                {
                    Debug.Log("[SaveService] 저장 데이터 없음 — 신규 플레이어 기본값 유지");
                }
                onDone?.Invoke();
            },
            err =>
            {
                Debug.LogWarning($"[SaveService] 복원 실패: {err.GenerateErrorReport()}");
                onError?.Invoke(err.GenerateErrorReport());
            });
    }
}
