// ⚠️ PlayFab SDK가 필요합니다.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 관리자 패널 C탭 — 플레이어 1명 전용 상세 패널.
/// PlayerTab 목록의 [관리] 버튼으로 열림. 정지/해제(1클릭) · 삭제·계정 초기화(2단계 확인) ·
/// 캐릭터 지급/회수(검색 가능한 목록, CharacterTab과 동일한 패턴을 원격 대상에 적용)를 다룬다.
/// </summary>
public class PlayerManagePanel : MonoBehaviour
{
    [SerializeField] TMP_Text       titleLabel;
    [SerializeField] Button         banButton;
    [SerializeField] Button         deleteButton;
    [SerializeField] Button         resetButton;
    [SerializeField] Button         mailButton;
    [SerializeField] TMP_InputField searchInput;
    [SerializeField] RectTransform  contentRoot;
    [SerializeField] Button         closeButton;
    [SerializeField] AdminPanel     owner;
    [SerializeField] MailSendForm   mailForm;

    const float ROW_H = 60f;

    PlayerDTO _target;
    Action    _onAccountChanged;
    readonly HashSet<string> _ownedIds = new HashSet<string>();
    CharacterDatabase _db;

    bool _busy;
    bool _deleteConfirming;
    bool _resetConfirming;

    TMP_Text _banLabel;
    Image    _banImage;
    TMP_Text _deleteLabel;
    Image    _deleteImage;
    TMP_Text _resetLabel;
    Image    _resetImage;

    // ── 생명주기 ──────────────────────────────────────────────────────────

    void Awake()
    {
        if (banButton    != null) { _banLabel    = banButton.GetComponentInChildren<TMP_Text>(); _banImage    = banButton.GetComponent<Image>(); }
        if (deleteButton != null) { _deleteLabel = deleteButton.GetComponentInChildren<TMP_Text>(); _deleteImage = deleteButton.GetComponent<Image>(); }
        if (resetButton  != null) { _resetLabel  = resetButton.GetComponentInChildren<TMP_Text>(); _resetImage  = resetButton.GetComponent<Image>(); }

        if (banButton    != null) banButton.onClick.AddListener(OnBanClicked);
        if (deleteButton != null) deleteButton.onClick.AddListener(OnDeleteClicked);
        if (resetButton  != null) resetButton.onClick.AddListener(OnResetClicked);
        if (mailButton   != null) mailButton.onClick.AddListener(OnMailClicked);
        if (closeButton  != null) closeButton.onClick.AddListener(Close);
        if (searchInput  != null) searchInput.onValueChanged.AddListener(_ => RebuildCharList());
    }

    // ── 열기/닫기 ─────────────────────────────────────────────────────────

    public void Open(PlayerDTO target, Action onAccountChanged)
    {
        _target           = target;
        _onAccountChanged = onAccountChanged;
        _deleteConfirming = false;
        _resetConfirming  = false;
        _busy             = false;

        if (_db == null) _db = Resources.Load<CharacterDatabase>("CharacterDatabase");
        if (searchInput != null) searchInput.text = "";

        gameObject.SetActive(true);

        if (titleLabel != null) titleLabel.text = $"{Label(_target)} 관리 ({_target.playFabId})";
        RefreshBanVisual();
        ResetDeleteVisual();
        ResetResetVisual();
        SetInteractable(true);

        _ownedIds.Clear();
        RebuildCharList();
        LoadRoster();
    }

    void Close() => gameObject.SetActive(false);

    // ── 보유 캐릭터 조회 ─────────────────────────────────────────────────

    void LoadRoster()
    {
        owner?.ShowStatus("보유 캐릭터 조회 중...", Color.white);
        PlayerAdminService.FetchPlayerRoster(_target.playFabId,
            set =>
            {
                _ownedIds.Clear();
                _ownedIds.UnionWith(set);
                owner?.ShowStatus("보유 캐릭터 조회 완료", Color.green);
                RebuildCharList();
            },
            err => owner?.ShowStatus($"보유 캐릭터 조회 실패: {err}", Color.red));
    }

    // ── 정지/해제 ─────────────────────────────────────────────────────────

    void OnBanClicked()
    {
        if (_busy) return;
        _busy = true;
        SetInteractable(false);
        bool banning = !_target.IsBanned;
        owner?.ShowStatus($"{Label(_target)} {(banning ? "정지" : "해제")} 중...", Color.white);

        Action onOk = () =>
        {
            _busy = false;
            _target.bannedUntil = banning ? "9999-12-31T23:59:59Z" : "";
            SetInteractable(true);
            RefreshBanVisual();
            owner?.ShowStatus($"{Label(_target)} {(banning ? "정지됨" : "정지 해제됨")}", Color.green);
        };
        Action<string> onErr = err =>
        {
            _busy = false;
            SetInteractable(true);
            owner?.ShowStatus($"{(banning ? "정지" : "해제")} 실패: {err}", Color.red);
        };

        if (banning) PlayerAdminService.BanPlayer(_target.playFabId, onOk, onErr);
        else         PlayerAdminService.UnbanPlayer(_target.playFabId, onOk, onErr);
    }

    void RefreshBanVisual()
    {
        if (_target == null) return;
        bool banned = _target.IsBanned;
        if (_banLabel != null) _banLabel.text  = banned ? "정지 해제" : "정지";
        if (_banImage != null) _banImage.color = banned ? UITheme.BtnSuccess : UITheme.BtnNeutral;
    }

    // ── 우편 발송 ─────────────────────────────────────────────────────────

    void OnMailClicked() => mailForm?.Open(_target);

    // ── 삭제 (2단계 확인) ────────────────────────────────────────────────

    void OnDeleteClicked()
    {
        if (_busy) return;

        if (!_deleteConfirming)
        {
            _deleteConfirming = true;
            if (_deleteLabel != null) _deleteLabel.text  = "확인?";
            if (_deleteImage != null) _deleteImage.color = new Color(0.95f, 0.25f, 0.25f);
            return;
        }

        _busy = true;
        SetInteractable(false);
        owner?.ShowStatus($"{Label(_target)} 삭제 중...", Color.white);

        PlayerAdminService.DeletePlayer(_target.playFabId,
            () =>
            {
                _busy = false;
                owner?.ShowStatus($"{Label(_target)} 삭제 완료", Color.green);
                _onAccountChanged?.Invoke();
                Close();
            },
            err =>
            {
                _busy = false;
                SetInteractable(true);
                ResetDeleteVisual();
                owner?.ShowStatus($"삭제 실패: {err}", Color.red);
            });
    }

    void ResetDeleteVisual()
    {
        _deleteConfirming = false;
        if (_deleteLabel != null) _deleteLabel.text  = "삭제";
        if (_deleteImage != null) _deleteImage.color = UITheme.BtnDanger;
    }

    // ── 계정 초기화 (2단계 확인) ─────────────────────────────────────────

    void OnResetClicked()
    {
        if (_busy) return;

        if (!_resetConfirming)
        {
            _resetConfirming = true;
            if (_resetLabel != null) _resetLabel.text  = "확인?";
            if (_resetImage != null) _resetImage.color = new Color(0.95f, 0.25f, 0.25f);
            return;
        }

        _busy = true;
        SetInteractable(false);
        owner?.ShowStatus($"{Label(_target)} 계정 초기화 중...", Color.white);

        PlayerAdminService.ResetAccount(_target.playFabId,
            () =>
            {
                _busy = false;
                SetInteractable(true);
                ResetResetVisual();
                _ownedIds.Clear();
                RebuildCharList();
                owner?.ShowStatus($"{Label(_target)} 계정 초기화 완료", Color.green);
                _onAccountChanged?.Invoke();
            },
            err =>
            {
                _busy = false;
                SetInteractable(true);
                ResetResetVisual();
                owner?.ShowStatus($"계정 초기화 실패: {err}", Color.red);
            });
    }

    void ResetResetVisual()
    {
        _resetConfirming = false;
        if (_resetLabel != null) _resetLabel.text  = "계정 초기화";
        if (_resetImage != null) _resetImage.color = UITheme.BtnDanger;
    }

    // ── 캐릭터 목록 (지급/회수) ──────────────────────────────────────────

    void RebuildCharList()
    {
        if (contentRoot == null || _db == null) return;

        foreach (Transform child in contentRoot)
            Destroy(child.gameObject);

        string q = searchInput != null ? searchInput.text.Trim().ToLower() : "";
        var filtered = new List<CharacterDef>();
        foreach (var def in _db.All)
        {
            if (def == null) continue;
            if (string.IsNullOrEmpty(q)
                || def.nameKo.ToLower().Contains(q)
                || def.nameEn.ToLower().Contains(q))
                filtered.Add(def);
        }

        for (int i = 0; i < filtered.Count; i++)
            CreateCharRow(filtered[i], i);

        contentRoot.sizeDelta = new Vector2(contentRoot.sizeDelta.x, filtered.Count * ROW_H);
    }

    void CreateCharRow(CharacterDef def, int rowIdx)
    {
        bool has = _ownedIds.Contains(def.id);

        var rowGO = new GameObject($"Row_{def.id}");
        rowGO.transform.SetParent(contentRoot, false);
        var rowRT = rowGO.AddComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(0f, 1f);
        rowRT.anchorMax = new Vector2(1f, 1f);
        rowRT.pivot     = new Vector2(0.5f, 1f);
        rowRT.sizeDelta = new Vector2(0f, ROW_H - 4f);
        rowRT.anchoredPosition = new Vector2(0f, -rowIdx * ROW_H);

        rowGO.AddComponent<Image>().color =
            rowIdx % 2 == 0 ? UITheme.PanelBgMid : UITheme.PanelBgDark;

        AddText(rowGO.transform, def.nameKo, TextAlignmentOptions.MidlineLeft,
            new Vector2(0f, 0f), new Vector2(0.38f, 1f), new Vector2(8f, 0f), Vector2.zero)
            .color = UITheme.TextPrimary;

        AddText(rowGO.transform, def.rarity.ToString(), TextAlignmentOptions.Midline,
            new Vector2(0.38f, 0f), new Vector2(0.58f, 1f), Vector2.zero, Vector2.zero)
            .color = UITheme.TextSecondary;

        var hasText = AddText(rowGO.transform, has ? "✓" : "─", TextAlignmentOptions.Midline,
            new Vector2(0.58f, 0f), new Vector2(0.68f, 1f), Vector2.zero, Vector2.zero);
        hasText.color = has ? Color.green : UITheme.TextDisabled;

        var btnGO = new GameObject("ActionBtn");
        btnGO.transform.SetParent(rowGO.transform, false);
        var btnRT = btnGO.AddComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.69f, 0.08f); btnRT.anchorMax = new Vector2(1f, 0.92f);
        btnRT.offsetMin = new Vector2(4f, 0f); btnRT.offsetMax = new Vector2(-4f, 0f);
        btnGO.AddComponent<Image>().color = has ? UITheme.BtnDanger : UITheme.BtnSuccess;
        var btn = btnGO.AddComponent<Button>();

        var lblGO = new GameObject("Label");
        lblGO.transform.SetParent(btnGO.transform, false);
        var lblRT = lblGO.AddComponent<RectTransform>();
        lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one; lblRT.sizeDelta = Vector2.zero;
        var lblTxt = lblGO.AddComponent<TextMeshProUGUI>();
        lblTxt.text      = has ? "회수" : "지급";
        lblTxt.fontSize  = UITheme.FontBody;
        lblTxt.color     = UITheme.TextPrimary;
        lblTxt.alignment = TextAlignmentOptions.Midline;

        var defId   = def.id;
        var defName = def.nameKo;
        btn.onClick.AddListener(() => OnToggleCharacter(defId, defName, has));
    }

    void OnToggleCharacter(string charId, string charName, bool currentlyHas)
    {
        if (_busy) return;
        _busy = true;
        SetInteractable(false);
        owner?.ShowStatus($"{charName} {(currentlyHas ? "회수" : "지급")} 중...", Color.white);

        Action onOk = () =>
        {
            _busy = false;
            if (currentlyHas) _ownedIds.Remove(charId);
            else              _ownedIds.Add(charId);
            SetInteractable(true);
            RebuildCharList();
            owner?.ShowStatus($"{charName} {(currentlyHas ? "회수" : "지급")} 완료", Color.green);
        };
        Action<string> onErr = err =>
        {
            _busy = false;
            SetInteractable(true);
            owner?.ShowStatus($"{(currentlyHas ? "회수" : "지급")} 실패: {err}", Color.red);
        };

        if (currentlyHas) PlayerAdminService.RevokeCharacter(_target.playFabId, charId, onOk, onErr);
        else              PlayerAdminService.GiveCharacter(_target.playFabId, charId, onOk, onErr);
    }

    // ── 내부 헬퍼 ─────────────────────────────────────────────────────────

    void SetInteractable(bool on)
    {
        if (banButton    != null) banButton.interactable    = on;
        if (deleteButton != null) deleteButton.interactable = on;
        if (resetButton  != null) resetButton.interactable  = on;
    }

    static string Label(PlayerDTO p) => string.IsNullOrEmpty(p.displayName) ? p.playFabId : p.displayName;

    TMP_Text AddText(Transform parent, string text, TextAlignmentOptions align,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
        rt.sizeDelta = Vector2.zero;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text      = text;
        t.fontSize  = UITheme.FontBody + 1;
        t.color     = UITheme.TextPrimary;
        t.alignment = align;
        return t;
    }
}
