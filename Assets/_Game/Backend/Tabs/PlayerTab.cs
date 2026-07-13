// ⚠️ PlayFab SDK가 필요합니다.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 관리자 패널 C탭 — 플레이어 목록(실제 PlayFab 데이터).
/// CloudScript AdminGetPlayers로 조회. 행별 [관리] 버튼으로 PlayerManagePanel을 열어
/// 정지/해제·삭제·캐릭터 지급/회수·계정 초기화를 그 패널에서 처리한다(행은 조회 전용).
/// </summary>
public class PlayerTab : MonoBehaviour
{
    [SerializeField] TMP_Dropdown       sortDropdown;
    [SerializeField] Button             orderButton;
    [SerializeField] Button             refreshButton;
    [SerializeField] RectTransform      contentRoot;
    [SerializeField] AdminPanel         owner;
    [SerializeField] PlayerManagePanel  managePanel;

    const float ROW_H = 60f;

    readonly List<PlayerDTO> _rows = new List<PlayerDTO>();
    bool _ascending = true;
    bool _busy      = false;
    bool _loaded    = false;

    // ── 생명주기 ──────────────────────────────────────────────────────────

    void Start()
    {
        if (sortDropdown != null)
        {
            sortDropdown.ClearOptions();
            sortDropdown.AddOptions(new List<string> { "이름", "PlayFabId", "마지막 로그인", "가입일" });
            sortDropdown.onValueChanged.AddListener(_ => Rebuild());
        }
        if (orderButton   != null) orderButton.onClick.AddListener(ToggleOrder);
        if (refreshButton != null) refreshButton.onClick.AddListener(LoadPlayers);
    }

    void OnEnable()
    {
        if (!_loaded) LoadPlayers();
        else          Rebuild();
    }

    // ── 데이터 로드 ───────────────────────────────────────────────────────

    void LoadPlayers()
    {
        if (_busy) return;
        _busy = true;
        owner?.ShowStatus("플레이어 목록 불러오는 중...", Color.white);

        PlayerAdminService.FetchPlayers(
            list =>
            {
                _busy   = false;
                _loaded = true;
                _rows.Clear();
                _rows.AddRange(list);
                owner?.ShowStatus($"플레이어 {_rows.Count}명 로드 완료", Color.green);
                Rebuild();
            },
            err =>
            {
                _busy = false;
                owner?.ShowStatus($"목록 로드 실패: {err}", Color.red);
            });
    }

    // ── 정렬 ──────────────────────────────────────────────────────────────

    void ToggleOrder()
    {
        _ascending = !_ascending;
        Rebuild();
    }

    List<PlayerDTO> SortedRows()
    {
        var key = (PlayerSortKey)(sortDropdown != null ? sortDropdown.value : 0);
        return PlayerSort.Sort(_rows, key, _ascending);
    }

    // ── 목록 재생성 ───────────────────────────────────────────────────────

    void Rebuild()
    {
        if (contentRoot == null) return;

        foreach (Transform child in contentRoot)
            Destroy(child.gameObject);

        if (orderButton != null)
        {
            var lbl = orderButton.GetComponentInChildren<TMP_Text>();
            if (lbl != null) lbl.text = _ascending ? "오름차순 ▲" : "내림차순 ▼";
        }

        var sorted = SortedRows();
        for (int i = 0; i < sorted.Count; i++)
            CreateRow(sorted[i], i);

        contentRoot.sizeDelta = new Vector2(contentRoot.sizeDelta.x, sorted.Count * ROW_H);
    }

    void CreateRow(PlayerDTO row, int rowIdx)
    {
        var rowGO = new GameObject($"Row_{row.playFabId}");
        rowGO.transform.SetParent(contentRoot, false);
        var rowRT = rowGO.AddComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(0f, 1f);
        rowRT.anchorMax = new Vector2(1f, 1f);
        rowRT.pivot     = new Vector2(0.5f, 1f);
        rowRT.sizeDelta = new Vector2(0f, ROW_H - 4f);
        rowRT.anchoredPosition = new Vector2(0f, -rowIdx * ROW_H);

        var rowBg = rowGO.AddComponent<Image>();
        rowBg.color = rowIdx % 2 == 0 ? UITheme.PanelBgMid : UITheme.PanelBgDark;
        if (row.IsBanned) rowBg.color = new Color(0.45f, 0.15f, 0.15f);

        AddText(rowGO.transform, row.displayName, TextAlignmentOptions.MidlineLeft,
            new Vector2(0f, 0f), new Vector2(0.30f, 1f), new Vector2(8f, 0f), Vector2.zero)
            .color = UITheme.TextPrimary;

        AddText(rowGO.transform, row.playFabId, TextAlignmentOptions.Midline,
            new Vector2(0.30f, 0f), new Vector2(0.55f, 1f), Vector2.zero, Vector2.zero)
            .color = UITheme.TextSecondary;

        AddText(rowGO.transform, FormatDate(row.LastLoginUtc), TextAlignmentOptions.Midline,
            new Vector2(0.55f, 0f), new Vector2(0.70f, 1f), Vector2.zero, Vector2.zero)
            .color = UITheme.TextSecondary;

        AddText(rowGO.transform, FormatDate(row.CreatedUtc), TextAlignmentOptions.Midline,
            new Vector2(0.70f, 0f), new Vector2(0.85f, 1f), Vector2.zero, Vector2.zero)
            .color = UITheme.TextSecondary;

        var manageBtn = AddButton(rowGO.transform, "관리", UITheme.BtnPrimary,
            new Vector2(0.85f, 0f), new Vector2(1.00f, 1f));
        manageBtn.onClick.AddListener(() => managePanel?.Open(row, LoadPlayers));
    }

    // ── 내부 헬퍼 ─────────────────────────────────────────────────────────

    static string FormatDate(DateTime utc) => utc == DateTime.MinValue ? "-" : utc.ToLocalTime().ToString("yyyy-MM-dd");

    TMP_Text AddText(Transform parent, string text, TextAlignmentOptions align,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text      = text;
        t.fontSize  = UITheme.FontBody + 1;
        t.color     = UITheme.TextPrimary;
        t.alignment = align;
        return t;
    }

    Button AddButton(Transform parent, string text, Color bg, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject("Btn");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = new Vector2(2f, 4f);
        rt.offsetMax = new Vector2(-2f, -4f);

        var image = go.AddComponent<Image>();
        image.color = bg;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = image;

        var lblGO = new GameObject("Label");
        lblGO.transform.SetParent(go.transform, false);
        var lrt = lblGO.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
        var label = lblGO.AddComponent<TextMeshProUGUI>();
        label.text      = text;
        label.fontSize  = UITheme.FontCaption + 2;
        label.color     = UITheme.TextPrimary;
        label.alignment = TextAlignmentOptions.Center;

        return btn;
    }
}
