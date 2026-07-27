// ⚠️ PlayFab SDK가 필요합니다.

using System;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

/// <summary>
/// MetaState(Wallet·Roster·GachaState·Crystals·StudyMats)를 PlayFab UserData에 저장·복원합니다.
/// 로그인 이후에만 호출하세요.
/// </summary>
public static class MetaSaveService
{
    const string KEY_WALLET   = "wallet";
    const string KEY_ROSTER   = "roster";
    const string KEY_GACHA    = "gacha";
    const string KEY_CRYSTALS = "crystals";
    const string KEY_STUDY    = "studyMats";
    const string KEY_MAILBOX  = "mailbox";

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

    /// <summary>UserData(Wallet·Roster·GachaState·Crystals·StudyMats)를 PlayFab에서 복원합니다.</summary>
    public static void Load(Action onDone = null, Action<string> onError = null)
    {
        if (!PlayFabClientAPI.IsClientLoggedIn()) { onDone?.Invoke(); return; }

        PlayFabClientAPI.GetUserData(
            new GetUserDataRequest
            {
                Keys = new List<string> { KEY_WALLET, KEY_ROSTER, KEY_GACHA, KEY_CRYSTALS, KEY_STUDY, KEY_MAILBOX }
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

                    if (d.TryGetValue(KEY_MAILBOX, out var mb))
                        MetaState.Mailbox.LoadState(
                            JsonUtility.FromJson<MailboxData>(mb.Value));
                }

                Debug.Log("[MetaSave] 메타 복원 완료");
                onDone?.Invoke();
            },
            err =>
            {
                Debug.LogWarning($"[MetaSave] 복원 실패: {err.GenerateErrorReport()}");
                onError?.Invoke(err.GenerateErrorReport());
            });
    }

    // ── 우편함 (독립 저장/재조회) ────────────────────────────────────────
    // KEY_MAILBOX는 Save()의 배치 Dictionary에 절대 포함하지 않는다 — 관리자가
    // AdminSendMail로 발송한 우편이 플레이어의 일상적인 메타 자동저장에 덮이지
    // 않도록 하는 핵심 안전장치다 (설계 §14 결정 6-a).

    /// <summary>우편함만 저장. 일반 배치 Save()와 완전히 분리된 메서드.</summary>
    public static void SaveMailbox(Action onDone = null, Action<string> onError = null)
    {
        if (!MetaState.IsInitialized) { onDone?.Invoke(); return; }
        if (!PlayFabClientAPI.IsClientLoggedIn()) { onDone?.Invoke(); return; }

        var data = new Dictionary<string, string>
        {
            { KEY_MAILBOX, JsonUtility.ToJson(MetaState.Mailbox.Export()) },
        };

        PlayFabClientAPI.UpdateUserData(
            new UpdateUserDataRequest { Data = data },
            _ =>
            {
                Debug.Log("[MetaSave] 우편함 저장 완료");
                onDone?.Invoke();
            },
            err =>
            {
                Debug.LogWarning($"[MetaSave] 우편함 저장 실패: {err.GenerateErrorReport()}");
                onError?.Invoke(err.GenerateErrorReport());
            });
    }

    /// <summary>우편함을 서버에서 재조회. 우편함 패널 Open 시 호출해 관리자가 그 사이 보낸 우편을 반영한다.</summary>
    public static void RefreshMailbox(Action onDone = null, Action<string> onError = null)
    {
        if (!PlayFabClientAPI.IsClientLoggedIn()) { onDone?.Invoke(); return; }

        PlayFabClientAPI.GetUserData(
            new GetUserDataRequest { Keys = new List<string> { KEY_MAILBOX } },
            result =>
            {
                if (result.Data != null && result.Data.TryGetValue(KEY_MAILBOX, out var mb))
                    MetaState.Mailbox.LoadState(JsonUtility.FromJson<MailboxData>(mb.Value));
                else
                    MetaState.Mailbox.LoadState(null);

                onDone?.Invoke();
            },
            err =>
            {
                Debug.LogWarning($"[MetaSave] 우편함 재조회 실패: {err.GenerateErrorReport()}");
                onError?.Invoke(err.GenerateErrorReport());
            });
    }
}
