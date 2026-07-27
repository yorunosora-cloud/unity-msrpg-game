// ⚠️ PlayFab SDK가 필요합니다.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 우편 발송 폼 오버레이 (설계 §14). PlayerManagePanel의 자식으로,
/// ProblemFormPanel과 동일한 오버레이 패턴을 따른다. [우편 발송] 버튼으로 열림.
/// </summary>
public class MailSendForm : MonoBehaviour
{
    [SerializeField] TMP_InputField titleInput;
    [SerializeField] TMP_InputField bodyInput;
    [SerializeField] TMP_InputField amountInput;
    [SerializeField] TMP_Dropdown   attachDropdown;
    [SerializeField] TMP_Dropdown   kindDropdown;
    [SerializeField] Button         sendButton;
    [SerializeField] Button         cancelButton;
    [SerializeField] TMP_Text       statusText;

    static readonly List<string> AttachOptions   = new List<string> { "없음(메시지)", "재화", "결정" };
    static readonly List<string> CurrencyOptions = new List<string> { "골드", "논문", "집중력", "조각" };

    PlayerDTO _target;
    bool _busy;

    // ── 생명주기 ──────────────────────────────────────────────────────────

    void Awake()
    {
        if (attachDropdown != null)
        {
            attachDropdown.ClearOptions();
            attachDropdown.AddOptions(AttachOptions);
            attachDropdown.onValueChanged.AddListener(_ => RefreshKindOptions());
        }
        if (sendButton   != null) sendButton.onClick.AddListener(OnSendClicked);
        if (cancelButton != null) cancelButton.onClick.AddListener(Close);
    }

    // ── 열기/닫기 ─────────────────────────────────────────────────────────

    public void Open(PlayerDTO target)
    {
        _target = target;
        _busy   = false;

        if (titleInput     != null) titleInput.text     = "";
        if (bodyInput       != null) bodyInput.text      = "";
        if (amountInput     != null) amountInput.text    = "";
        if (attachDropdown != null) attachDropdown.value = 0;
        SetStatus("", Color.white);
        SetInteractable(true);

        gameObject.SetActive(true);
    }

    void Close() => gameObject.SetActive(false);

    // ── 첨부 종류 드롭다운 갱신 ──────────────────────────────────────────

    /// <summary>attachDropdown 선택(없음/재화/결정)에 맞춰 kindDropdown 옵션·활성 상태를 갱신한다.</summary>
    void RefreshKindOptions()
    {
        if (kindDropdown == null || attachDropdown == null) return;

        int attach = attachDropdown.value; // 0 없음 / 1 재화 / 2 결정
        kindDropdown.ClearOptions();

        bool hasAttachment = attach != 0;
        kindDropdown.interactable = hasAttachment;
        if (amountInput != null) amountInput.interactable = hasAttachment;
        if (!hasAttachment) return;

        if (attach == 1)
        {
            kindDropdown.AddOptions(CurrencyOptions);
        }
        else // 2 == 결정
        {
            var options = new List<string>();
            foreach (CrystalKind k in System.Enum.GetValues(typeof(CrystalKind)))
                options.Add($"{CrystalCatalog.ContinentLabel(k)} {CrystalCatalog.DisplayName(k)}");
            kindDropdown.AddOptions(options);
        }
        kindDropdown.value = 0;
    }

    // ── 발송 ──────────────────────────────────────────────────────────────

    void OnSendClicked()
    {
        if (_busy || _target == null) return;

        string title = titleInput != null ? titleInput.text.Trim() : "";
        if (string.IsNullOrEmpty(title)) { SetStatus("제목을 입력하세요.", Color.yellow); return; }

        var attachType = (MailAttachmentType)(attachDropdown != null ? attachDropdown.value : 0);
        int kindIndex  = kindDropdown != null ? kindDropdown.value : 0;
        int amount     = 0;
        if (attachType != MailAttachmentType.None)
        {
            if (amountInput == null || !int.TryParse(amountInput.text, out amount) || amount <= 0)
            { SetStatus("수량을 올바르게 입력하세요.", Color.yellow); return; }
        }

        string body = bodyInput != null ? bodyInput.text : "";

        _busy = true;
        SetInteractable(false);
        SetStatus("발송 중...", Color.white);

        PlayerAdminService.SendMail(_target.playFabId, title, body, attachType, kindIndex, amount,
            () =>
            {
                _busy = false;
                SetInteractable(true);
                SetStatus("발송 완료", Color.green);
                if (titleInput  != null) titleInput.text  = "";
                if (bodyInput   != null) bodyInput.text   = "";
                if (amountInput != null) amountInput.text = "";
            },
            err =>
            {
                _busy = false;
                SetInteractable(true);
                SetStatus($"발송 실패: {err}", Color.red);
            });
    }

    // ── 내부 헬퍼 ─────────────────────────────────────────────────────────

    void SetInteractable(bool on)
    {
        if (sendButton     != null) sendButton.interactable     = on;
        if (cancelButton   != null) cancelButton.interactable   = on;
        if (titleInput     != null) titleInput.interactable     = on;
        if (bodyInput       != null) bodyInput.interactable      = on;
        if (attachDropdown != null) attachDropdown.interactable = on;

        // kindDropdown·amountInput의 활성 여부는 첨부타입 선택에 종속된다.
        if (on) RefreshKindOptions();
        else
        {
            if (kindDropdown != null) kindDropdown.interactable = false;
            if (amountInput  != null) amountInput.interactable  = false;
        }
    }

    void SetStatus(string msg, Color color)
    {
        if (statusText == null) return;
        statusText.text  = msg;
        statusText.color = color;
    }
}
