// ⚠️ PlayFab SDK가 필요합니다.

using System;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

/// <summary>
/// MetaState(Wallet·Roster·GachaState·IsAdmin)를 PlayFab UserData에 저장·복원합니다.
/// 기존 SaveService와 동일한 패턴. 로그인 이후에만 호출하세요.
/// </summary>
public static class MetaSaveService
{
    const string KEY_WALLET   = "wallet";
    const string KEY_ROSTER   = "roster";
    const string KEY_GACHA    = "gacha";
    const string KEY_CRYSTALS = "crystals";
    const string KEY_STUDY    = "studyMats";
    const string KEY_ADMIN    = "isAdmin";

    // ── 저장 ──────────────────────────────────────────────────────────────

    public static void Save(Action onDone = null, Action<string> onError = null)
    {
        if (!MetaState.IsInitialized) { onDone?.Invoke(); return; }
        if (!PlayFabClientAPI.IsClientLoggedIn()) { onDone?.Invoke(); return; }

        var data = new Dictionary<string, string>
        {
            { KEY_WALLET,   JsonUtility.ToJson(MetaState.Wallet.Export())          },
            { KEY_ROSTER,   JsonUtility.ToJson(MetaState.Roster.Export())          },
            { KEY_GACHA,    JsonUtility.ToJson(MetaState.GachaState.Export())      },
            { KEY_CRYSTALS, JsonUtility.ToJson(MetaState.Crystals.Export())        },
            { KEY_STUDY,    JsonUtility.ToJson(MetaState.StudyMaterials.Export())  },
        };

        PlayFabClientAPI.UpdateUserData(
            new UpdateUserDataRequest { Data = data },
            _ =>
            {
                Debug.Log("[MetaSave] 저장 완료");
                onDone?.Invoke();
            },
            err =>
            {
                Debug.LogWarning($"[MetaSave] 저장 실패: {err.GenerateErrorReport()}");
                onError?.Invoke(err.GenerateErrorReport());
            });
    }

    // ── 복원 ──────────────────────────────────────────────────────────────

    /// <summary>UserData 복원 후 ReadOnlyData에서 isAdmin 플래그를 읽습니다.</summary>
    public static void Load(Action onDone = null, Action<string> onError = null)
    {
        PlayFabClientAPI.GetUserData(
            new GetUserDataRequest
            {
                Keys = new List<string> { KEY_WALLET, KEY_ROSTER, KEY_GACHA, KEY_CRYSTALS, KEY_STUDY }
            },
            result =>
            {
                var d = result.Data;
                if (d != null)
                {
                    if (d.TryGetValue(KEY_WALLET, out var w))
                        MetaState.Wallet.LoadState(
                            JsonUtility.FromJson<WalletData>(w.Value));

                    if (d.TryGetValue(KEY_ROSTER, out var r))
                        MetaState.Roster.LoadState(
                            JsonUtility.FromJson<RosterData>(r.Value));

                    if (d.TryGetValue(KEY_GACHA, out var g))
                        MetaState.GachaState.LoadState(
                            JsonUtility.FromJson<GachaStateData>(g.Value));

                    if (d.TryGetValue(KEY_CRYSTALS, out var c))
                        MetaState.Crystals.LoadState(
                            JsonUtility.FromJson<CrystalWalletData>(c.Value));

                    if (d.TryGetValue(KEY_STUDY, out var sm))
                        MetaState.StudyMaterials.LoadState(
                            JsonUtility.FromJson<StudyMaterialData>(sm.Value));
                }

                Debug.Log("[MetaSave] 메타 복원 완료");
                LoadAdminFlag(onDone, onError);
            },
            err =>
            {
                Debug.LogWarning($"[MetaSave] 복원 실패: {err.GenerateErrorReport()}");
                onError?.Invoke(err.GenerateErrorReport());
            });
    }

    // ── admin 플래그 (ReadOnlyData) ───────────────────────────────────────

    static void LoadAdminFlag(Action onDone, Action<string> onError)
    {
        PlayFabClientAPI.GetUserReadOnlyData(
            new GetUserDataRequest { Keys = new List<string> { KEY_ADMIN } },
            result =>
            {
                MetaState.IsAdmin =
                    result.Data != null &&
                    result.Data.TryGetValue(KEY_ADMIN, out var v) &&
                    v.Value.ToLower() == "true";

                onDone?.Invoke();
            },
            err =>
            {
                // admin 확인 실패는 치명적이지 않음 — false 처리 후 계속
                Debug.LogWarning($"[MetaSave] isAdmin 확인 실패: {err.GenerateErrorReport()}");
                MetaState.IsAdmin = false;
                onDone?.Invoke();
            });
    }
}
