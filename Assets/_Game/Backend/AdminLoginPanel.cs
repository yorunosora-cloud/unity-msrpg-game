using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using PlayFab.Json;

/// <summary>
/// 관리자 비밀 키 입력 패널.
/// AccountPanel의 "관리자 로그인" 버튼으로 열리며, CloudScript VerifyAdminKey로 검증.
/// 성공 시 세션 한정 MetaState.IsAdmin = true → AdminPanel 열기.
/// 게임 재시작 시 IsAdmin은 false로 초기화됨(세션 메모리에만 유지).
/// </summary>
public class AdminLoginPanel : MonoBehaviour
{
    [Header("패널")]
    [SerializeField] GameObject panel;

    [Header("입력")]
    [SerializeField] TMP_InputField keyInput;   // contentType = Password (MetaUISetup에서 설정)

    [Header("버튼")]
    [SerializeField] Button confirmButton;
    [SerializeField] Button closeButton;

    [Header("상태")]
    [SerializeField] TMP_Text statusText;

    bool _pending;  // CloudScript 호출 중 중복 요청 방지
    string _pendingKey; // CloudScript 콜백에서 MetaState.AdminKey에 반영하기 위해 임시 보관

    // ── 공개 메서드 ───────────────────────────────────────────────────────

    public void Open()
    {
        if (keyInput != null) keyInput.text = "";
        SetStatus("", Color.white);
        _pending = false;
        panel.SetActive(true);
    }

    public void Close()
    {
        panel.SetActive(false);
        UIManager.Close();
    }

    // ── 생명주기 ──────────────────────────────────────────────────────────

    void Start()
    {
        panel.SetActive(false);

        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);
        if (closeButton   != null) closeButton.onClick.AddListener(Close);
        if (keyInput      != null) keyInput.onSubmit.AddListener(_ => OnConfirm());
    }

    void Update()
    {
        // Enter 키 제출 지원 (TMP onSubmit이 포커스 잃으면 동작 안 할 때 보완)
        if (panel.activeSelf
            && !_pending
            && Keyboard.current != null
            && Keyboard.current.enterKey.wasPressedThisFrame)
        {
            OnConfirm();
        }
    }

    // ── 내부 ──────────────────────────────────────────────────────────────

    void OnConfirm()
    {
        if (_pending) return;

        string key = keyInput != null ? keyInput.text.Trim() : "";
        if (string.IsNullOrEmpty(key))
        {
            SetStatus("비밀 키를 입력하세요.", Color.yellow);
            return;
        }

        _pending = true;
        _pendingKey = key;
        SetStatus("확인 중...", Color.white);
        if (confirmButton != null) confirmButton.interactable = false;

        CloudScriptService.Execute(
            "VerifyAdminKey",
            new { key },
            OnCloudScriptOk,
            OnCloudScriptErr);
    }

    void OnCloudScriptOk(JsonObject result)
    {
        _pending = false;
        if (confirmButton != null) confirmButton.interactable = true;

        // { "success": true/false }
        bool success = false;
        if (result != null && result.TryGetValue("success", out var val))
            bool.TryParse(val.ToString(), out success);

        if (success)
        {
            MetaState.IsAdmin  = true;
            MetaState.AdminKey = _pendingKey;
            panel.SetActive(false);
            UIManager.Close();

            // AdminPanel은 별도 캔버스(MetaCanvas)에 있으므로 런타임 탐색
            var ap = FindFirstObjectByType<AdminPanel>(FindObjectsInactive.Include);
            if (ap != null)
            {
                UIManager.TryOpen();
                ap.gameObject.SetActive(true);
            }
        }
        else
        {
            SetStatus("잘못된 키입니다.", Color.red);
        }
    }

    void OnCloudScriptErr(string err)
    {
        _pending = false;
        if (confirmButton != null) confirmButton.interactable = true;
        SetStatus($"검증 실패: {err}", Color.red);
        // 패널 유지 (설계도 6장 — 네트워크 오류 시 닫히지 않음)
    }

    void SetStatus(string msg, Color color)
    {
        if (statusText == null) return;
        statusText.text  = msg;
        statusText.color = color;
    }
}
