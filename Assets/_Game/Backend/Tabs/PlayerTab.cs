using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 관리자 패널 C탭 — 플레이어 목록(정렬 전용, 로컬 더미 데이터).
/// 실제 계정 조회·조작(CloudScript)은 다음 라운드에서 구현.
/// </summary>
public class PlayerTab : MonoBehaviour
{
    /// <summary>플레이어 목록 행 1개(로컬 더미 데이터 스키마 — CloudScript 연동 시 그대로 재사용 가능).</summary>
    class AdminPlayerRow
    {
        public string   playFabId;
        public string   displayName;
        public DateTime lastLogin;
        public int      characterCount; // 보유 캐릭터 수
        public Rarity   bestRarity;     // 보유 캐릭터 중 최고 등급
        public int      bestLevel;      // 그 캐릭터의 레벨
        public int      bestExp;        // 그 캐릭터의 경험치
    }

    enum SortKey { Name, PlayFabId, LastLogin, CharacterCount }

    [SerializeField] TMP_Dropdown  sortDropdown;
    [SerializeField] Button        orderButton;
    [SerializeField] RectTransform contentRoot;
    [SerializeField] AdminPanel    owner;

    const float ROW_H = 60f;

    readonly List<AdminPlayerRow> _rows = new List<AdminPlayerRow>();
    bool _ascending = true;

    // ── 생명주기 ──────────────────────────────────────────────────────────

    void Start()
    {
        BuildDummyRows();

        if (sortDropdown != null)
        {
            sortDropdown.ClearOptions();
            sortDropdown.AddOptions(new List<string> { "이름", "PlayFabId", "마지막 로그인", "캐릭터 보유순" });
            sortDropdown.onValueChanged.AddListener(_ => Rebuild());
        }
        if (orderButton != null) orderButton.onClick.AddListener(ToggleOrder);
    }

    void OnEnable() => Rebuild();

    // ── 더미 데이터 ───────────────────────────────────────────────────────

    void BuildDummyRows()
    {
        if (_rows.Count > 0) return; // 재진입 시 중복 생성 방지

        var now = DateTime.Now;
        _rows.Add(new AdminPlayerRow { playFabId = "PF-0001", displayName = "뉴턴팬",   lastLogin = now.AddHours(-1),  characterCount = 7, bestRarity = Rarity.UR,  bestLevel = 42, bestExp = 900 });
        _rows.Add(new AdminPlayerRow { playFabId = "PF-0002", displayName = "화학러버", lastLogin = now.AddDays(-2),   characterCount = 7, bestRarity = Rarity.UR,  bestLevel = 42, bestExp = 500 }); // characterCount·rarity·level 동점, exp만 다름
        _rows.Add(new AdminPlayerRow { playFabId = "PF-0003", displayName = "생명과학도", lastLogin = now.AddMinutes(-30), characterCount = 7, bestRarity = Rarity.UR, bestLevel = 30, bestExp = 999 }); // count·rarity 동점, level 다름
        _rows.Add(new AdminPlayerRow { playFabId = "PF-0004", displayName = "지구별",   lastLogin = now.AddDays(-10),  characterCount = 7, bestRarity = Rarity.SSR, bestLevel = 50, bestExp = 999 }); // count 동점, rarity 다름
        _rows.Add(new AdminPlayerRow { playFabId = "PF-0005", displayName = "수학왕",   lastLogin = now.AddDays(-1),   characterCount = 3, bestRarity = Rarity.SR,  bestLevel = 20, bestExp = 100 });
        _rows.Add(new AdminPlayerRow { playFabId = "PF-0006", displayName = "정보처리사", lastLogin = now.AddHours(-5), characterCount = 1, bestRarity = Rarity.N,   bestLevel = 5,  bestExp = 10  });
        _rows.Add(new AdminPlayerRow { playFabId = "PF-0007", displayName = "신규가입", lastLogin = now.AddMinutes(-2), characterCount = 0, bestRarity = Rarity.N,  bestLevel = 1,  bestExp = 0   });
    }

    // ── 정렬 ──────────────────────────────────────────────────────────────

    void ToggleOrder()
    {
        _ascending = !_ascending;
        Rebuild();
    }

    List<AdminPlayerRow> SortedRows()
    {
        var key = (SortKey)(sortDropdown != null ? sortDropdown.value : 0);
        var list = new List<AdminPlayerRow>(_rows);

        Comparison<AdminPlayerRow> cmp = key switch
        {
            SortKey.Name          => (a, b) => string.Compare(a.displayName, b.displayName, StringComparison.Ordinal),
            SortKey.PlayFabId     => (a, b) => string.Compare(a.playFabId, b.playFabId, StringComparison.Ordinal),
            SortKey.LastLogin     => (a, b) => a.lastLogin.CompareTo(b.lastLogin),
            SortKey.CharacterCount=> CompareByCharacterOwnership,
            _                     => (a, b) => 0,
        };

        list.Sort(cmp);
        if (!_ascending) list.Reverse();
        return list;
    }

    /// <summary>
    /// "캐릭터 보유순" 다중 키 비교: 보유수 → 최고등급 → 레벨 → 경험치.
    /// 앞 키가 같으면 다음 키로 넘어가며, 첫 비영값을 반환한다.
    /// </summary>
    static int CompareByCharacterOwnership(AdminPlayerRow a, AdminPlayerRow b)
    {
        int c = a.characterCount.CompareTo(b.characterCount);
        if (c != 0) return c;
        c = ((int)a.bestRarity).CompareTo((int)b.bestRarity);
        if (c != 0) return c;
        c = a.bestLevel.CompareTo(b.bestLevel);
        if (c != 0) return c;
        return a.bestExp.CompareTo(b.bestExp);
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

    void CreateRow(AdminPlayerRow row, int rowIdx)
    {
        var rowGO = new GameObject($"Row_{row.playFabId}");
        rowGO.transform.SetParent(contentRoot, false);
        var rowRT = rowGO.AddComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(0f, 1f);
        rowRT.anchorMax = new Vector2(1f, 1f);
        rowRT.pivot     = new Vector2(0.5f, 1f);
        rowRT.sizeDelta = new Vector2(0f, ROW_H - 4f);
        rowRT.anchoredPosition = new Vector2(0f, -rowIdx * ROW_H);

        rowGO.AddComponent<Image>().color =
            rowIdx % 2 == 0 ? UITheme.PanelBgMid : UITheme.PanelBgDark;

        AddText(rowGO.transform, row.displayName, TextAlignmentOptions.MidlineLeft,
            new Vector2(0f, 0f), new Vector2(0.34f, 1f), new Vector2(8f, 0f), Vector2.zero)
            .color = UITheme.TextPrimary;

        AddText(rowGO.transform, row.playFabId, TextAlignmentOptions.Midline,
            new Vector2(0.34f, 0f), new Vector2(0.58f, 1f), Vector2.zero, Vector2.zero)
            .color = UITheme.TextSecondary;

        AddText(rowGO.transform, row.lastLogin.ToString("yyyy-MM-dd"), TextAlignmentOptions.Midline,
            new Vector2(0.58f, 0f), new Vector2(0.82f, 1f), Vector2.zero, Vector2.zero)
            .color = UITheme.TextSecondary;

        AddText(rowGO.transform, $"보유 {row.characterCount}", TextAlignmentOptions.MidlineRight,
            new Vector2(0.82f, 0f), new Vector2(1f, 1f), Vector2.zero, new Vector2(-8f, 0f))
            .color = UITheme.TextPrimary;
    }

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
}
