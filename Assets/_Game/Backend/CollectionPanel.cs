using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 도감(Collection) 패널 — 그리드 카드 뷰 + 필터/정렬 바.
/// 단일 책임 분리:
///   · CollectionPanel — 진입점, 뷰 상태 관리, FilterBar 빌드
///   · CodexCardBuilder — 카드 생성 정적 유틸리티 (별도 파일)
/// </summary>
public class CollectionPanel : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────
    [SerializeField] RectTransform contentRoot;   // 스크롤 컨텐츠 영역
    [SerializeField] RectTransform filterBarRoot; // 필터 버튼 행을 붙일 영역 (없으면 코드로 생성)

    // ── 뷰 상태 ───────────────────────────────────────────────────────────
    Continent?  _filterContinent = null;   // null = 전체
    Rarity?     _filterRarity    = null;   // null = 전체
    bool        _ownedOnly       = false;
    SortMode    _sort            = SortMode.DexNumber;

    // ── 데이터 ────────────────────────────────────────────────────────────
    CharacterDatabase _db;

    // ── 카드 선택 콜백 (Task 4에서 연결) ─────────────────────────────────
    Action<CharacterDef, OwnedCharacter> _onCardSelected;

    // ── 필터 버튼 참조 (활성 상태 토글용) ────────────────────────────────
    readonly List<(Button btn, Image bg, TextMeshProUGUI lbl)> _continentBtns = new List<(Button, Image, TextMeshProUGUI)>();
    readonly List<(Button btn, Image bg, TextMeshProUGUI lbl)> _rarityBtns    = new List<(Button, Image, TextMeshProUGUI)>();
    readonly List<(Button btn, Image bg, TextMeshProUGUI lbl)> _sortBtns      = new List<(Button, Image, TextMeshProUGUI)>();
    (Button btn, Image bg, TextMeshProUGUI lbl) _ownedOnlyBtn;

    // ── 정렬 열거 ─────────────────────────────────────────────────────────
    enum SortMode { DexNumber, Rarity, Name }

    // ─────────────────────────────────────────────────────────────────────
    // Unity 생명주기
    // ─────────────────────────────────────────────────────────────────────

    void OnEnable()
    {
        _db = Resources.Load<CharacterDatabase>("CharacterDatabase");

        BuildFilterBar();
        Refresh();

        if (MetaState.IsInitialized)
            MetaState.Roster.OnChanged += Refresh;
    }

    void OnDisable()
    {
        if (MetaState.IsInitialized)
            MetaState.Roster.OnChanged -= Refresh;
        UIManager.Close();
    }

    // ─────────────────────────────────────────────────────────────────────
    // 공용 API
    // ─────────────────────────────────────────────────────────────────────

    public void OnCloseClicked() => gameObject.SetActive(false);

    /// <summary>Task 4 DetailView에서 연결할 카드 선택 콜백 설정.</summary>
    public void SetOnCardSelected(Action<CharacterDef, OwnedCharacter> cb) => _onCardSelected = cb;

    // ─────────────────────────────────────────────────────────────────────
    // 필터 바 빌드
    // ─────────────────────────────────────────────────────────────────────

    void BuildFilterBar()
    {
        // filterBarRoot가 Inspector에 연결되지 않은 경우 스킵 (contentRoot 상위에 자동 생성 불가)
        if (filterBarRoot == null) return;

        // 이미 빌드되어 있으면 재빌드하지 않음
        if (filterBarRoot.childCount > 0) return;

        _continentBtns.Clear();
        _rarityBtns.Clear();
        _sortBtns.Clear();

        const float ROW_H = 28f;
        const float PAD   = 4f;

        // ── 행 1: 대륙 필터 ─────────────────────────────────────────────
        var row1 = CreateFilterRow(filterBarRoot, 0f, ROW_H, PAD);
        AddFilterButton(row1, "[전체]", true,
            () => { _filterContinent = null; UpdateContinentButtonStates(); Refresh(); },
            out var r1allBtn);
        _continentBtns.Add(r1allBtn);

        var continents = new (string label, Continent value)[]
        {
            ("[물리]", Continent.Physics),
            ("[화학]", Continent.Chemistry),
            ("[생명]", Continent.Biology),
            ("[지구]", Continent.EarthSci),
            ("[수학]", Continent.Math),
            ("[정보]", Continent.Info),
        };
        foreach (var (label, value) in continents)
        {
            var capturedValue = value;
            AddFilterButton(row1, label, false,
                () => { _filterContinent = capturedValue; UpdateContinentButtonStates(); Refresh(); },
                out var entry);
            _continentBtns.Add(entry);
        }

        // ── 행 2: 등급 필터 + 보유 토글 ─────────────────────────────────
        var row2 = CreateFilterRow(filterBarRoot, -(ROW_H + PAD), ROW_H, PAD);
        AddFilterButton(row2, "[전체]", true,
            () => { _filterRarity = null; UpdateRarityButtonStates(); Refresh(); },
            out var r2allBtn);
        _rarityBtns.Add(r2allBtn);

        foreach (Rarity rar in Enum.GetValues(typeof(Rarity)))
        {
            var capturedRar = rar;
            AddFilterButton(row2, $"[{rar}]", false,
                () => { _filterRarity = capturedRar; UpdateRarityButtonStates(); Refresh(); },
                out var entry);
            _rarityBtns.Add(entry);
        }

        AddFilterButton(row2, "[보유만]", false,
            () => { _ownedOnly = !_ownedOnly; UpdateOwnedOnlyButtonState(); Refresh(); },
            out _ownedOnlyBtn);

        // ── 행 3: 정렬 ──────────────────────────────────────────────────
        var row3 = CreateFilterRow(filterBarRoot, -((ROW_H + PAD) * 2f), ROW_H, PAD);
        var sortItems = new (string label, SortMode mode)[]
        {
            ("[도감번호]", SortMode.DexNumber),
            ("[등급]",    SortMode.Rarity),
            ("[이름]",    SortMode.Name),
        };
        foreach (var (label, mode) in sortItems)
        {
            var capturedMode = mode;
            AddFilterButton(row3, label, mode == SortMode.DexNumber,
                () => { _sort = capturedMode; UpdateSortButtonStates(); Refresh(); },
                out var entry);
            _sortBtns.Add(entry);
        }

        // 컨테이너 높이
        filterBarRoot.sizeDelta = new Vector2(0, (ROW_H + PAD) * 3f + PAD);
    }

    // ─────────────────────────────────────────────────────────────────────
    // 그리드 갱신
    // ─────────────────────────────────────────────────────────────────────

    void Refresh()
    {
        if (contentRoot == null || !MetaState.IsInitialized) return;

        // 기존 카드 제거
        while (contentRoot.childCount > 0)
            UnityEngine.Object.Destroy(contentRoot.GetChild(0).gameObject);

        if (_db == null) return;

        var roster = MetaState.Roster;
        var allDefs = _db.All;
        if (allDefs == null || allDefs.Length == 0)
        {
            BuildEmpty("등록된 캐릭터가 없습니다.");
            return;
        }

        // ── 필터 ────────────────────────────────────────────────────────
        IEnumerable<CharacterDef> defs = allDefs.Where(d => d != null);

        if (_filterContinent.HasValue)
            defs = defs.Where(d => d.continent == _filterContinent.Value);

        if (_filterRarity.HasValue)
            defs = defs.Where(d => d.rarity == _filterRarity.Value);

        if (_ownedOnly)
            defs = defs.Where(d => roster.Has(d.id));

        // ── 정렬 ────────────────────────────────────────────────────────
        defs = _sort switch
        {
            SortMode.DexNumber => defs.OrderBy(d => d.dexNumber == 0 ? int.MaxValue : d.dexNumber),
            SortMode.Rarity    => defs.OrderByDescending(d => (int)d.rarity)
                                      .ThenBy(d => d.dexNumber == 0 ? int.MaxValue : d.dexNumber),
            SortMode.Name      => defs.OrderBy(d => d.nameKo ?? d.id),
            _                  => defs,
        };

        var defList = defs.ToList();

        if (defList.Count == 0)
        {
            BuildEmpty("조건에 맞는 캐릭터가 없습니다.");
            return;
        }

        // ── 그리드 레이아웃 계산 ─────────────────────────────────────────
        float panelW = contentRoot.rect.width;
        if (panelW <= 0f) panelW = 400f; // fallback (OnEnable 직후 rect 미확정 대비)

        const float CARD_W   = CodexCardBuilder.CARD_W;
        const float CARD_H   = CodexCardBuilder.CARD_H;
        const float GAP      = CodexCardBuilder.CARD_GAP;
        const float PAD_SIDE = 8f;
        const float PAD_TOP  = 8f;

        int cols = Mathf.Max(3, Mathf.FloorToInt((panelW - PAD_SIDE * 2f + GAP) / (CARD_W + GAP)));
        float totalW    = cols * CARD_W + (cols - 1) * GAP;
        float startX    = (panelW - totalW) * 0.5f;  // 중앙 정렬

        int rows = Mathf.CeilToInt((float)defList.Count / cols);
        float contentH = PAD_TOP + rows * CARD_H + (rows - 1) * GAP + PAD_TOP;
        contentRoot.sizeDelta = new Vector2(0, contentH);

        // ── 카드 배치 ────────────────────────────────────────────────────
        for (int i = 0; i < defList.Count; i++)
        {
            var def   = defList[i];
            var owned = roster.Get(def.id);

            int col = i % cols;
            int row = i / cols;

            float x = startX + col * (CARD_W + GAP) + CARD_W * 0.5f;
            float y = -(PAD_TOP + row * (CARD_H + GAP) + CARD_H * 0.5f);

            var cardRt = CodexCardBuilder.BuildCard(contentRoot, def, owned, _onCardSelected);
            cardRt.anchorMin        = new Vector2(0f, 1f);
            cardRt.anchorMax        = new Vector2(0f, 1f);
            cardRt.pivot            = new Vector2(0.5f, 0.5f);
            cardRt.anchoredPosition = new Vector2(x, y);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // 버튼 상태 갱신
    // ─────────────────────────────────────────────────────────────────────

    void UpdateContinentButtonStates()
    {
        // 인덱스 0 = [전체], 1~6 = Physics~Info
        var continentValues = new Continent?[]
        {
            null,
            Continent.Physics, Continent.Chemistry, Continent.Biology,
            Continent.EarthSci, Continent.Math, Continent.Info,
        };
        for (int i = 0; i < _continentBtns.Count && i < continentValues.Length; i++)
            SetButtonActive(_continentBtns[i].bg, _continentBtns[i].lbl, _filterContinent == continentValues[i]);
    }

    void UpdateRarityButtonStates()
    {
        // 인덱스 0 = [전체], 1~5 = N/R/SR/SSR/UR
        Rarity?[] rarityValues = { null, Rarity.N, Rarity.R, Rarity.SR, Rarity.SSR, Rarity.UR };
        for (int i = 0; i < _rarityBtns.Count - 1 && i < rarityValues.Length; i++) // -1: 보유 버튼 제외
            SetButtonActive(_rarityBtns[i].bg, _rarityBtns[i].lbl, _filterRarity == rarityValues[i]);
        // 보유 버튼은 _ownedOnlyBtn에 별도 관리
    }

    void UpdateOwnedOnlyButtonState()
    {
        SetButtonActive(_ownedOnlyBtn.bg, _ownedOnlyBtn.lbl, _ownedOnly);
    }

    void UpdateSortButtonStates()
    {
        SortMode[] sortModes = { SortMode.DexNumber, SortMode.Rarity, SortMode.Name };
        for (int i = 0; i < _sortBtns.Count && i < sortModes.Length; i++)
            SetButtonActive(_sortBtns[i].bg, _sortBtns[i].lbl, _sort == sortModes[i]);
    }

    static void SetButtonActive(Image bg, TextMeshProUGUI lbl, bool active)
    {
        if (bg == null) return;
        bg.color = active
            ? new Color(UITheme.TextPrimary.r, UITheme.TextPrimary.g, UITheme.TextPrimary.b, 0.25f)
            : new Color(UITheme.TextSecondary.r, UITheme.TextSecondary.g, UITheme.TextSecondary.b, 0.12f);
        if (lbl != null)
            lbl.color = active ? UITheme.TextPrimary : UITheme.TextSecondary;
    }

    // ─────────────────────────────────────────────────────────────────────
    // 필터 바 UI 헬퍼
    // ─────────────────────────────────────────────────────────────────────

    static RectTransform CreateFilterRow(RectTransform parent, float yPos, float height, float pad)
    {
        var go = new GameObject("FilterRow", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, yPos);
        rt.sizeDelta        = new Vector2(0f, height);

        // 수평 레이아웃
        var hLayout = go.AddComponent<HorizontalLayoutGroup>();
        hLayout.spacing            = 4f;
        hLayout.padding            = new RectOffset((int)pad, (int)pad, 2, 2);
        hLayout.childAlignment     = TextAnchor.MiddleLeft;
        hLayout.childForceExpandWidth  = false;
        hLayout.childForceExpandHeight = true;
        hLayout.childControlWidth  = false;
        hLayout.childControlHeight = true;

        return rt;
    }

    static void AddFilterButton(
        RectTransform row,
        string label,
        bool startActive,
        Action onClick,
        out (Button btn, Image bg, TextMeshProUGUI lbl) result)
    {
        var go = new GameObject("Btn_" + label, typeof(RectTransform));
        go.transform.SetParent(row, false);
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(MeasureButtonWidth(label), 24f);

        var bg = go.AddComponent<Image>();
        bg.color = startActive
            ? new Color(UITheme.TextPrimary.r, UITheme.TextPrimary.g, UITheme.TextPrimary.b, 0.25f)
            : new Color(UITheme.TextSecondary.r, UITheme.TextSecondary.g, UITheme.TextSecondary.b, 0.12f);

        var txtGO = new GameObject("Label", typeof(RectTransform));
        txtGO.transform.SetParent(go.transform, false);
        var txtRt = (RectTransform)txtGO.transform;
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(4f, 0f);
        txtRt.offsetMax = new Vector2(-4f, 0f);
        var txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text      = label;
        txt.fontSize  = UITheme.FontCaption;
        txt.color     = startActive ? UITheme.TextPrimary : UITheme.TextSecondary;
        txt.alignment = TextAlignmentOptions.Center;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.onClick.AddListener(() => onClick?.Invoke());

        result = (btn, bg, txt);
    }

    // 라벨 문자 수로 대략적인 버튼 폭 계산
    static float MeasureButtonWidth(string label)
    {
        // 한글 1자 ≈ 11px, 영문 ≈ 7px, 대괄호 포함 최소 40px
        return Mathf.Max(40f, label.Length * 9f + 8f);
    }

    // ─────────────────────────────────────────────────────────────────────
    // 빈 상태 메시지
    // ─────────────────────────────────────────────────────────────────────

    void BuildEmpty(string msg)
    {
        var go = new GameObject("Empty", typeof(RectTransform));
        go.transform.SetParent(contentRoot, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, 0f);
        rt.sizeDelta        = new Vector2(0f, 80f);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text      = msg;
        t.fontSize  = UITheme.FontBody;
        t.color     = UITheme.TextSecondary;
        t.alignment = TextAlignmentOptions.Center;
        contentRoot.sizeDelta = new Vector2(0, 80f);
    }
}
