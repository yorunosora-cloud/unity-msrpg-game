// ⚠️ PlayFab SDK가 필요합니다.

using System;
using System.Collections.Generic;
using UnityEngine;
using PlayFab.Json;

/// <summary>관리자 전용 플레이어 조회·삭제·정지 CloudScript 래퍼.</summary>
public static class PlayerAdminService
{
    public static void FetchPlayers(Action<List<PlayerDTO>> onDone, Action<string> onError)
    {
        string key = MetaState.AdminKey;
        if (string.IsNullOrEmpty(key)) { onError?.Invoke("관리자 키가 없습니다. 관리자 로그인을 먼저 하세요."); return; }

        CloudScriptService.Execute(
            "AdminGetPlayers",
            new { key },
            result =>
            {
                if (!IsSuccess(result)) { onError?.Invoke(ErrorText(result)); return; }
                if (!result.TryGetValue("playersJson", out var pj) || pj == null)
                { onError?.Invoke("응답에 playersJson이 없습니다."); return; }

                PlayerListDTO wrapper;
                try { wrapper = JsonUtility.FromJson<PlayerListDTO>(pj.ToString()); }
                catch (Exception e) { onError?.Invoke($"목록 파싱 실패: {e.Message}"); return; }

                var list = new List<PlayerDTO>();
                if (wrapper?.items != null) list.AddRange(wrapper.items);
                onDone?.Invoke(list);
            },
            onError);
    }

    public static void DeletePlayer(string playFabId, Action onDone, Action<string> onError)
        => AdminAction("AdminDeletePlayer", playFabId, onDone, onError);

    public static void BanPlayer(string playFabId, Action onDone, Action<string> onError)
        => AdminAction("AdminBanPlayer", playFabId, onDone, onError);

    public static void UnbanPlayer(string playFabId, Action onDone, Action<string> onError)
        => AdminAction("AdminUnbanPlayer", playFabId, onDone, onError);

    public static void ResetAccount(string playFabId, Action onDone, Action<string> onError)
        => AdminAction("AdminResetAccount", playFabId, onDone, onError);

    /// <summary>대상 계정의 보유 캐릭터 id 목록을 조회합니다.</summary>
    public static void FetchPlayerRoster(string playFabId, Action<HashSet<string>> onDone, Action<string> onError)
    {
        string key = MetaState.AdminKey;
        if (string.IsNullOrEmpty(key))       { onError?.Invoke("관리자 키가 없습니다. 관리자 로그인을 먼저 하세요."); return; }
        if (string.IsNullOrEmpty(playFabId)) { onError?.Invoke("PlayFabId가 비어 있습니다."); return; }

        CloudScriptService.Execute(
            "AdminGetPlayerRoster",
            new { key, playFabId },
            result =>
            {
                if (!IsSuccess(result)) { onError?.Invoke(ErrorText(result)); return; }
                if (!result.TryGetValue("ownedJson", out var oj) || oj == null)
                { onError?.Invoke("응답에 ownedJson이 없습니다."); return; }

                RosterIdsDTO wrapper;
                try { wrapper = JsonUtility.FromJson<RosterIdsDTO>(oj.ToString()); }
                catch (Exception e) { onError?.Invoke($"보유 목록 파싱 실패: {e.Message}"); return; }

                var set = new HashSet<string>();
                if (wrapper?.ids != null) set.UnionWith(wrapper.ids);
                onDone?.Invoke(set);
            },
            onError);
    }

    public static void GiveCharacter(string playFabId, string charId, Action onDone, Action<string> onError)
        => CharAction("AdminGiveCharacter", playFabId, charId, onDone, onError);

    public static void RevokeCharacter(string playFabId, string charId, Action onDone, Action<string> onError)
        => CharAction("AdminRevokeCharacter", playFabId, charId, onDone, onError);

    static void CharAction(string functionName, string playFabId, string charId, Action onDone, Action<string> onError)
    {
        string key = MetaState.AdminKey;
        if (string.IsNullOrEmpty(key))       { onError?.Invoke("관리자 키가 없습니다. 관리자 로그인을 먼저 하세요."); return; }
        if (string.IsNullOrEmpty(playFabId)) { onError?.Invoke("PlayFabId가 비어 있습니다."); return; }
        if (string.IsNullOrEmpty(charId))    { onError?.Invoke("charId가 비어 있습니다."); return; }

        CloudScriptService.Execute(
            functionName, new { key, playFabId, charId },
            result =>
            {
                if (!IsSuccess(result)) { onError?.Invoke(ErrorText(result)); return; }
                onDone?.Invoke();
            },
            onError);
    }

    /// <summary>대상 계정 우편함에 우편(재화/결정 첨부 또는 메시지)을 발송합니다.</summary>
    public static void SendMail(string playFabId, string title, string body,
        MailAttachmentType attachType, int kindIndex, int amount,
        Action onOk, Action<string> onErr)
    {
        string key = MetaState.AdminKey;
        if (string.IsNullOrEmpty(key))       { onErr?.Invoke("관리자 키가 없습니다. 관리자 로그인을 먼저 하세요."); return; }
        if (string.IsNullOrEmpty(playFabId)) { onErr?.Invoke("PlayFabId가 비어 있습니다."); return; }
        if (string.IsNullOrEmpty(title))     { onErr?.Invoke("제목을 입력하세요."); return; }

        CloudScriptService.Execute(
            "AdminSendMail",
            new { key, playFabId, title, body, attachType = (int)attachType, kindIndex, amount },
            result =>
            {
                if (!IsSuccess(result)) { onErr?.Invoke(ErrorText(result)); return; }
                onOk?.Invoke();
            },
            onErr);
    }

    static void AdminAction(string functionName, string playFabId, Action onDone, Action<string> onError)
    {
        string key = MetaState.AdminKey;
        if (string.IsNullOrEmpty(key))       { onError?.Invoke("관리자 키가 없습니다. 관리자 로그인을 먼저 하세요."); return; }
        if (string.IsNullOrEmpty(playFabId)) { onError?.Invoke("PlayFabId가 비어 있습니다."); return; }

        CloudScriptService.Execute(
            functionName, new { key, playFabId },
            result =>
            {
                if (!IsSuccess(result)) { onError?.Invoke(ErrorText(result)); return; }
                onDone?.Invoke();
            },
            onError);
    }

    static bool IsSuccess(JsonObject result)
        => result != null && result.TryGetValue("success", out var val)
        && bool.TryParse(val.ToString(), out var b) && b;

    /// <summary>CloudScript가 { success:false, error:"..." } 로 반환한 상세 메시지를 꺼낸다. 없으면 일반 메시지.</summary>
    static string ErrorText(JsonObject result)
    {
        if (result != null && result.TryGetValue("error", out var err) && err != null)
            return err.ToString();
        return "서버가 요청을 거부했습니다.";
    }
}
