using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 관리자 패널 B탭 — 캐릭터 보유 현황 및 지급/회수.
/// CharacterDatabase 전체 목록, Roster 상태 실시간 반영.
/// </summary>
public class CharacterTab : MonoBehaviour
{
    [SerializeField] TMP_InputField searchInput;
    [SerializeField] Button         giveAllButton;
    [SerializeField] RectTransform  contentRoot;
    [SerializeField] AdminPanel     owner;

    CharacterDatabase _db;
    const float ROW_H = 60f;

    // ── 생명주기 ──────────────────────────────────────────────────────────

    void Start()
    {
        _db = Resources.Load<CharacterDatabase>("CharacterDatabase");
        if (searchInput   != null) searchInput.onValueChanged.AddListener(_ => Rebuild());
        if (giveAllButton != null) giveAllButton.onClick.AddListener(OnGiveAll);
    }

    void OnEnable()
    {
        if (_db == null) _db = Resources.Load<CharacterDatabase>("CharacterDatabase");
        if (MetaState.IsInitialized)
            MetaState.Roster.OnChanged += Rebuild;
        Rebuild();
    }

    void OnDisable()
    {
        if (MetaState.IsInitialized)
            MetaState.Roster.OnChanged -= Rebuild;
    }

    // ── 목록 재생성 ───────────────────────────────────────────────────────

    void Rebuild()
    {
        if (contentRoot == null || _db == null) return;

        foreach (Transform child in contentRoot)
            Object.Destroy(child.gameObject);

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
            CreateRow(filtered[i], i);

        contentRoot.sizeDelta = new Vector2(contentRoot.sizeDelta.x, filtered.Count * ROW_H);
    }

    void CreateRow(CharacterDef def, int rowIdx)
    {
        bool has = MetaState.IsInitialized && MetaState.Roster.Has(def.id);

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

        // 이름
        AddText(rowGO.transform, def.nameKo, TextAlignmentOptions.MidlineLeft,
            new Vector2(0f, 0f), new Vector2(0.38f, 1f), new Vector2(8f, 0f), Vector2.zero)
            .color = UITheme.TextPrimary;

        // 희귀도
        AddText(rowGO.transform, def.rarity.ToString(), TextAlignmentOptions.Midline,
            new Vector2(0.38f, 0f), new Vector2(0.58f, 1f), Vector2.zero, Vector2.zero)
            .color = UITheme.TextSecondary;

        // 보유 ✓/─
        var hasText = AddText(rowGO.transform, has ? "✓" : "─", TextAlignmentOptions.Midline,
            new Vector2(0.58f, 0f), new Vector2(0.68f, 1f), Vector2.zero, Vector2.zero);
        hasText.color = has ? Color.green : UITheme.TextDisabled;

        // 지급/회수 버튼
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

        var defId = def.id;
        var defName = def.nameKo;
        btn.onClick.AddListener(() =>
        {
            if (!MetaState.IsInitialized) return;
            if (MetaState.Roster.Has(defId))
                MetaState.Roster.Remove(defId);
            else
                MetaState.Roster.Add(defId);
            // Roster.OnChanged → Rebuild() 자동 호출
            MetaSaveService.Save(
                ()  => owner?.ShowStatus($"{defName} 저장 완료", Color.green),
                err => owner?.ShowStatus($"저장 실패: {err}", Color.red));
        });
    }

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
        t.fontSize  = UITheme.FontBody + 2;
        t.color     = UITheme.TextPrimary;
        t.alignment = align;
        return t;
    }

    // ── 전체 지급 ─────────────────────────────────────────────────────────

    void OnGiveAll()
    {
        if (!MetaState.IsInitialized || _db == null) return;
        int count = 0;
        foreach (var def in _db.All)
        {
            if (def == null) continue;
            if (MetaState.Roster.Add(def.id)) count++;
        }
        MetaSaveService.Save(
            ()  => owner?.ShowStatus($"전체 {count}명 지급 완료", Color.green),
            err => owner?.ShowStatus($"저장 실패: {err}", Color.red));
    }
}
