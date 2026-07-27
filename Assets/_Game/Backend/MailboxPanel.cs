using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 우편함 UI (설계 §14, Phase 3). 계정 패널(ESC) 하단 버튼으로 열리며, Open() 직전에
/// MetaSaveService.RefreshMailbox()로 서버 최신 상태를 반영한다(AccountPanel.OnMailboxClicked).
/// 목록 행은 미리보기만 보여주고, 행을 클릭하면 상세창(MailDetail)이 열려 제목·전체 본문을
/// 먼저 확인한 뒤 [수령]/[확인]을 누르도록 한다(클릭 즉시 읽음 처리되던 이전 동작 개선).
/// </summary>
public class MailboxPanel : MonoBehaviour
{
    [Header("패널")]
    [SerializeField] GameObject panel;

    [Header("목록")]
    [SerializeField] RectTransform contentRoot;

    [Header("버튼")]
    [SerializeField] Button closeButton;
    [SerializeField] Button claimAllButton;

    [Header("상세창")]
    [SerializeField] GameObject      detailPanel;
    [SerializeField] TMP_Text        detailTitle;
    [SerializeField] TMP_Text        detailBody;
    [SerializeField] RectTransform   detailBodyContent;
    [SerializeField] TMP_Text        detailAttach;
    [SerializeField] Button          detailActionButton;
    [SerializeField] TMP_Text        detailActionLabel;
    [SerializeField] Image           detailActionBg;
    [SerializeField] Button          detailCloseButton;

    const float ROW_H = 100f;
    const float ROW_GAP = 6f;
    const float DETAIL_BODY_MIN_H = 120f;

    string _openMailId; // 현재 상세창에 열려있는 우편 id (없으면 null)

    // ── 생명주기 ──────────────────────────────────────────────────────────

    void Awake()
    {
        if (closeButton       != null) closeButton.onClick.AddListener(Close);
        if (claimAllButton    != null) claimAllButton.onClick.AddListener(OnClaimAllClicked);
        if (detailCloseButton != null) detailCloseButton.onClick.AddListener(CloseDetail);
        if (detailActionButton!= null) detailActionButton.onClick.AddListener(OnDetailAction);
    }

    // ── 공개 메서드 ───────────────────────────────────────────────────────

    /// <summary>패널을 연다. 호출 전에 MetaSaveService.RefreshMailbox()로 최신화하는 것을 권장.</summary>
    public void Open()
    {
        panel.SetActive(true);
        CloseDetail();
        Rebuild();
    }

    public void Close()
    {
        CloseDetail();
        panel.SetActive(false);
        UIManager.Close();
    }

    // ── 목록 갱신 ─────────────────────────────────────────────────────────

    void Rebuild()
    {
        if (contentRoot == null || !MetaState.IsInitialized) return;

        while (contentRoot.childCount > 0)
            Destroy(contentRoot.GetChild(0).gameObject);

        var mails = MetaState.Mailbox.Mails.OrderByDescending(m => m.sentUtc).ToList();

        if (mails.Count == 0)
        {
            BuildEmptyRow();
            contentRoot.sizeDelta = new Vector2(0, 90f);
            return;
        }

        float y = 0f;
        for (int i = 0; i < mails.Count; i++)
        {
            BuildRow(mails[i], y);
            y -= ROW_H + ROW_GAP;
        }
        contentRoot.sizeDelta = new Vector2(0, Mathf.Abs(y));
    }

    void OnClaimAllClicked()
    {
        MetaState.Mailbox.ClaimAll();
        MetaSaveService.SaveMailbox();
        Rebuild();
    }

    // ── 행 생성 (미리보기만 — 클릭 시 상세창) ────────────────────────────

    void BuildEmptyRow()
    {
        var go = new GameObject("Empty", typeof(RectTransform));
        go.transform.SetParent(contentRoot, false);
        SetRowRect((RectTransform)go.transform, 0f, 90f);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text      = "받은 우편이 없습니다.";
        t.fontSize  = UITheme.FontBody;
        t.color     = UITheme.TextSecondary;
        t.alignment = TextAlignmentOptions.Center;
    }

    void BuildRow(MailItem mail, float yPos)
    {
        bool pending = mail.attachType == MailAttachmentType.None ? !mail.read : !mail.claimed;

        var row = new GameObject("Row_" + mail.id, typeof(RectTransform));
        row.transform.SetParent(contentRoot, false);
        SetRowRect((RectTransform)row.transform, yPos, ROW_H);
        var rowBg = row.AddComponent<Image>();
        rowBg.color = pending ? UITheme.PanelBgMid : UITheme.PanelBgDark;
        var rowBtn = row.AddComponent<Button>();
        rowBtn.targetGraphic = rowBg;
        string id = mail.id;
        rowBtn.onClick.AddListener(() => OpenDetail(id));

        // 제목
        var titleGO = new GameObject("Title", typeof(RectTransform));
        titleGO.transform.SetParent(row.transform, false);
        var titleRt = (RectTransform)titleGO.transform;
        titleRt.anchorMin = new Vector2(0f, 1f); titleRt.anchorMax = new Vector2(0.72f, 1f);
        titleRt.pivot     = new Vector2(0f, 1f);
        titleRt.anchoredPosition = new Vector2(14f, -8f);
        titleRt.sizeDelta = new Vector2(-14f, 32f);
        var titleTxt = titleGO.AddComponent<TextMeshProUGUI>();
        titleTxt.text      = mail.title;
        titleTxt.fontSize  = UITheme.FontH2;
        titleTxt.color     = UITheme.TextPrimary;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.alignment = TextAlignmentOptions.MidlineLeft;
        titleTxt.overflowMode = TextOverflowModes.Ellipsis;

        // 미리보기 (본문 첫 줄 또는 첨부 요약, 한 줄로 잘림)
        var previewGO = new GameObject("Preview", typeof(RectTransform));
        previewGO.transform.SetParent(row.transform, false);
        var previewRt = (RectTransform)previewGO.transform;
        previewRt.anchorMin = new Vector2(0f, 1f); previewRt.anchorMax = new Vector2(0.72f, 1f);
        previewRt.pivot     = new Vector2(0f, 1f);
        previewRt.anchoredPosition = new Vector2(14f, -42f);
        previewRt.sizeDelta = new Vector2(-14f, 44f);
        var previewTxt = previewGO.AddComponent<TextMeshProUGUI>();
        previewTxt.text      = BuildPreview(mail);
        previewTxt.fontSize  = UITheme.FontBody;
        previewTxt.color     = UITheme.TextSecondary;
        previewTxt.alignment = TextAlignmentOptions.TopLeft;
        previewTxt.overflowMode = TextOverflowModes.Ellipsis;
        previewTxt.enableWordWrapping = false;

        // 상태 표시 (누르는 버튼 아님 — 정보 표시용 pill)
        var pillGO = new GameObject("StatusPill", typeof(RectTransform));
        pillGO.transform.SetParent(row.transform, false);
        var pillRt = (RectTransform)pillGO.transform;
        pillRt.anchorMin = new Vector2(0.72f, 0f); pillRt.anchorMax = new Vector2(1f, 1f);
        pillRt.offsetMin = new Vector2(6f, 10f); pillRt.offsetMax = new Vector2(-10f, -10f);
        var pillBg = pillGO.AddComponent<Image>();

        var pillLblGO = new GameObject("Label", typeof(RectTransform));
        pillLblGO.transform.SetParent(pillGO.transform, false);
        var pillLblRt = (RectTransform)pillLblGO.transform;
        pillLblRt.anchorMin = Vector2.zero; pillLblRt.anchorMax = Vector2.one; pillLblRt.sizeDelta = Vector2.zero;
        var pillLblTxt = pillLblGO.AddComponent<TextMeshProUGUI>();
        pillLblTxt.fontSize  = UITheme.FontBody;
        pillLblTxt.color     = UITheme.TextPrimary;
        pillLblTxt.alignment = TextAlignmentOptions.Center;

        bool hasAttach = mail.attachType != MailAttachmentType.None;
        if (hasAttach)
        {
            pillBg.color    = mail.claimed ? UITheme.PanelBgDark : UITheme.BtnSuccess;
            pillLblTxt.text = mail.claimed ? "수령완료" : "수령 가능";
        }
        else
        {
            pillBg.color    = mail.read ? UITheme.PanelBgDark : UITheme.BtnNeutral;
            pillLblTxt.text = mail.read ? "확인함" : "새 메시지";
        }
    }

    // ── 상세창 ────────────────────────────────────────────────────────────

    /// <summary>목록 행 클릭 시 상세창을 열어 제목·전체 본문·첨부를 보여준다. 읽음/수령은 하지 않는다.</summary>
    void OpenDetail(string mailId)
    {
        var mail = Find(mailId);
        if (mail == null || detailPanel == null) return;

        _openMailId = mailId;

        if (detailTitle != null) detailTitle.text = mail.title;
        if (detailBody  != null) detailBody.text  = string.IsNullOrEmpty(mail.body) ? "(본문 없음)" : mail.body;
        if (detailAttach != null) detailAttach.text = AttachSummary(mail) ?? "";

        // 여러 줄 본문 스크롤 — content 높이를 텍스트 실제 높이에 맞춘다.
        if (detailBody != null && detailBodyContent != null)
        {
            detailBody.ForceMeshUpdate();
            float h = Mathf.Max(DETAIL_BODY_MIN_H, detailBody.preferredHeight + 20f);
            detailBodyContent.sizeDelta = new Vector2(detailBodyContent.sizeDelta.x, h);
        }

        bool hasAttach = mail.attachType != MailAttachmentType.None;
        bool pending   = hasAttach ? !mail.claimed : !mail.read;

        if (detailActionBg    != null) detailActionBg.color    = pending ? (hasAttach ? UITheme.BtnSuccess : UITheme.BtnNeutral) : UITheme.PanelBgDark;
        if (detailActionLabel != null) detailActionLabel.text  = hasAttach ? (mail.claimed ? "수령완료" : "수령") : (mail.read ? "확인함" : "확인");
        if (detailActionButton!= null) detailActionButton.interactable = pending;

        detailPanel.SetActive(true);
    }

    void OnDetailAction()
    {
        if (string.IsNullOrEmpty(_openMailId)) return;
        var mail = Find(_openMailId);
        if (mail == null) { CloseDetail(); return; }

        bool ok = mail.attachType != MailAttachmentType.None
            ? MetaState.Mailbox.Claim(_openMailId)
            : MetaState.Mailbox.MarkRead(_openMailId);

        if (!ok) return; // 이미 처리됨 — 멱등

        MetaSaveService.SaveMailbox();
        CloseDetail();
        Rebuild();
    }

    void CloseDetail()
    {
        _openMailId = null;
        if (detailPanel != null) detailPanel.SetActive(false);
    }

    // ── 텍스트 헬퍼 ───────────────────────────────────────────────────────

    /// <summary>목록 행 미리보기: 본문 첫 줄 우선, 없으면 첨부 요약.</summary>
    static string BuildPreview(MailItem mail)
    {
        if (!string.IsNullOrEmpty(mail.body))
        {
            int nl = mail.body.IndexOf('\n');
            return nl >= 0 ? mail.body.Substring(0, nl) : mail.body;
        }
        return AttachSummary(mail) ?? "";
    }

    /// <summary>첨부 요약 문구 (예: "첨부: 골드 100"). 첨부 없으면 null.</summary>
    static string AttachSummary(MailItem mail) => mail.attachType switch
    {
        MailAttachmentType.Currency   => $"첨부: {CurrencyLabel((CurrencyKind)mail.kindIndex)} {mail.amount:N0}",
        MailAttachmentType.Crystal    => $"첨부: {CrystalCatalog.ContinentLabel((CrystalKind)mail.kindIndex)} {CrystalCatalog.DisplayName((CrystalKind)mail.kindIndex)} {mail.amount:N0}",
        MailAttachmentType.Consumable => $"첨부: 소비 아이템 x{mail.amount:N0}",
        _                             => null,
    };

    static string CurrencyLabel(CurrencyKind kind) => kind switch
    {
        CurrencyKind.Gold     => "골드",
        CurrencyKind.Paper    => "논문",
        CurrencyKind.Focus    => "집중력",
        CurrencyKind.Fragment => "조각",
        _                     => kind.ToString(),
    };

    MailItem Find(string id)
    {
        foreach (var m in MetaState.Mailbox.Mails)
            if (m.id == id) return m;
        return null;
    }

    static void SetRowRect(RectTransform rt, float yPos, float height)
    {
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, yPos);
        rt.sizeDelta        = new Vector2(0f, height);
    }
}
