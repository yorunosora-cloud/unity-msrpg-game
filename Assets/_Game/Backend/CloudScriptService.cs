// ⚠️ PlayFab SDK가 필요합니다.

using System;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.Json;

/// <summary>
/// PlayFab Legacy CloudScript 호출 래퍼.
/// 로그인 세션 티켓은 SDK가 자동 첨부합니다.
/// </summary>
public static class CloudScriptService
{
    /// <summary>
    /// CloudScript 함수를 호출합니다.
    /// </summary>
    /// <param name="functionName">handlers.{functionName}으로 등록된 함수명</param>
    /// <param name="args">함수에 전달할 인자 (익명 객체 가능)</param>
    /// <param name="onOk">성공 시 콜백 — FunctionResult JsonObject 전달</param>
    /// <param name="onErr">실패 시 콜백 — 에러 메시지 전달</param>
    public static void Execute(string functionName, object args,
        Action<JsonObject> onOk, Action<string> onErr)
    {
        if (!PlayFabClientAPI.IsClientLoggedIn())
        {
            onErr?.Invoke("로그인 필요");
            return;
        }

        PlayFabClientAPI.ExecuteCloudScript(
            new ExecuteCloudScriptRequest
            {
                FunctionName          = functionName,
                FunctionParameter     = args,
                GeneratePlayStreamEvent = false,
            },
            result =>
            {
                if (result.Error != null)
                {
                    Debug.LogWarning($"[CloudScript] {functionName} 함수 오류: {result.Error.Message}");
                    onErr?.Invoke(result.Error.Message);
                    return;
                }
                onOk?.Invoke(result.FunctionResult as JsonObject);
            },
            err =>
            {
                string msg = err.GenerateErrorReport();
                Debug.LogWarning($"[CloudScript] {functionName} 호출 실패: {msg}");
                onErr?.Invoke(msg);
            });
    }
}
