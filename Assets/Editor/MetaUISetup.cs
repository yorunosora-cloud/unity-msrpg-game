using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor.Events;
using TMPro;

/// <summary>
/// MSRPG > Setup Meta UI 메뉴.
/// Mesoria 씬에 재화 HUD·가챠·컬렉션·관리자 패널을 Canvas 오버레이로 추가합니다.
/// 기존 씬 오브젝트를 삭제하지 않으므로 반복 실행 안전.
/// </summary>
public static class MetaUISetup
{
    const string FONT_SDF_PATH = "Assets/_Game/Art/Fonts/malgun SDF.asset";
    const string SCENE_PATH    = "Assets/_Game/Scenes/Mesoria.unity";
    const string CANVAS_NAME   = "MetaCanvas";

    [MenuItem("MSRPG/Setup Meta UI")]
    public static void Run()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_SDF_PATH);
        EnsureTmpDefaultFont(font);

        // Mesoria 씬 열기
        if (!System.IO.File.Exists(SCENE_PATH.Replace("Assets", Application.dataPath)))
        {
            Debug.LogError("[MetaUISetup] Mesoria.unity 씬이 없습니다. 먼저 MSRPG > Setup Mesoria Scene을 실행하세요.");
            return;
        }
        EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);

        // 기존 MetaCanvas 제거 (재실행 안전)
        var existing = GameObject.Find(CANVAS_NAME);
        if (existing != null) Object.DestroyImmediate(existing);

        // ── Canvas ──────────────────────────────────────────────────────────
        var canvasGO = new GameObject(CANVAS_NAME);
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode        = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder      = 10; // 기존 UI 위에 표시
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // MetaPanelController 부착 (항상 활성)
        var controller = canvasGO.AddComponent<MetaPanelController>();

        // ── CurrencyHud (상단 전체폭 고정) ──────────────────────────────
        var hudGO = new GameObject("CurrencyHud");
        hudGO.transform.SetParent(canvasGO.transform, false);
        var hudRt = hudGO.AddComponent<RectTransform>();
        hudRt.anchorMin       = new Vector2(0f, 1f);
        hudRt.anchorMax       = new Vector2(1f, 1f);
        hudRt.pivot           = new Vector2(0.5f, 1f);
        hudRt.sizeDelta       = new Vector2(0f, 70f);
        hudRt.anchoredPosition = Vector2.zero;
        var hudBg  = hudGO.AddComponent<Image>();
        hudBg.color = new Color(0f, 0f, 0f, 0.6f);
        var hud    = hudGO.AddComponent<CurrencyHud>();

        var goldText     = CreateLabel(hudGO.transform, "GoldText",     "골드 5,000", 26, new Vector2(-380f, 0f), font);
        var paperText    = CreateLabel(hudGO.transform, "PaperText",    "논문 30",    26, new Vector2(-120f, 0f), font);
        var focusText    = CreateLabel(hudGO.transform, "FocusText",    "집중 120",   26, new Vector2( 120f, 0f), font);
        var fragmentText = CreateLabel(hudGO.transform, "FragmentText", "조각 10",    26, new Vector2( 380f, 0f), font);

        var hudSo = new SerializedObject(hud);
        hudSo.FindProperty("goldText").objectReferenceValue     = goldText.GetComponent<TMP_Text>();
        hudSo.FindProperty("paperText").objectReferenceValue    = paperText.GetComponent<TMP_Text>();
        hudSo.FindProperty("focusText").objectReferenceValue    = focusText.GetComponent<TMP_Text>();
        hudSo.FindProperty("fragmentText").objectReferenceValue = fragmentText.GetComponent<TMP_Text>();
        hudSo.ApplyModifiedProperties();

        // ── CollectionPanel (도감, C키, 중앙 숨김) ──────────────────────
        var collPanel = CreateCentrePanel(canvasGO.transform, "CollectionPanel", new Vector2(700f, 950f));
        collPanel.SetActive(false);
        CreateLabel(collPanel.transform, "Title",   "도감  [C]",   44, new Vector2(0f, 390f), font);
        var collListText = CreateLabel(collPanel.transform, "ListText", "", 26, new Vector2(0f, 0f), font);
        var closeBtnC    = CreateButton(collPanel.transform, "CloseBtn", "닫기  [C]", new Vector2(0f, -400f), font, 28, 0.4f);

        var collCmp = collPanel.AddComponent<CollectionPanel>();
        var cSo = new SerializedObject(collCmp);
        cSo.FindProperty("listText").objectReferenceValue = collListText.GetComponent<TMP_Text>();
        cSo.ApplyModifiedProperties();
        UnityEventTools.AddVoidPersistentListener(closeBtnC.GetComponent<Button>().onClick, collCmp.OnCloseClicked);

        // ── InventoryPanel (인벤토리, I키, 중앙 숨김) ───────────────────
        var invPanel = CreateCentrePanel(canvasGO.transform, "InventoryPanel", new Vector2(700f, 950f));
        invPanel.SetActive(false);
        CreateLabel(invPanel.transform, "Title",   "인벤토리  [I]", 44, new Vector2(0f, 390f), font);
        var invListText = CreateLabel(invPanel.transform, "ListText", "", 26, new Vector2(0f, 0f), font);
        var closeBtnI   = CreateButton(invPanel.transform, "CloseBtn", "닫기  [I]", new Vector2(0f, -400f), font, 28, 0.4f);

        var invCmp = invPanel.AddComponent<InventoryPanel>();
        var iSo = new SerializedObject(invCmp);
        iSo.FindProperty("listText").objectReferenceValue = invListText.GetComponent<TMP_Text>();
        iSo.ApplyModifiedProperties();
        UnityEventTools.AddVoidPersistentListener(closeBtnI.GetComponent<Button>().onClick, invCmp.OnCloseClicked);

        // ── AdminPanel (F1키, 중앙 숨김) ─────────────────────────────────
        var adminPanel = CreateCentrePanel(canvasGO.transform, "AdminPanel", new Vector2(700f, 1000f));
        adminPanel.SetActive(false);
        CreateLabel(adminPanel.transform, "Title", "관리자 패널  [F1]", 40, new Vector2(0f, 450f), font);

        var addGoldBtn      = CreateButton(adminPanel.transform, "AddGoldBtn",      "+골드 1,000",    new Vector2(0f,  340f), font, 26);
        var addPaperBtn     = CreateButton(adminPanel.transform, "AddPaperBtn",     "+논문 100",      new Vector2(0f,  260f), font, 26);
        var addFocusBtn     = CreateButton(adminPanel.transform, "AddFocusBtn",     "+집중력 100",    new Vector2(0f,  180f), font, 26);
        var addFragmentBtn  = CreateButton(adminPanel.transform, "AddFragmentBtn",  "+조각 50",       new Vector2(0f,  100f), font, 26);
        var giveCrystalsBtn = CreateButton(adminPanel.transform, "GiveCrystalsBtn", "+결정(전종 10)", new Vector2(0f,   20f), font, 26);

        var charInput = CreateInput(adminPanel.transform, "CharacterIdInput", "캐릭터 ID 입력", new Vector2(-60f, -60f), font);
        var giveBtn   = CreateButton(adminPanel.transform, "GiveCharacterBtn", "지급", new Vector2(260f, -60f), font, 26);
        ((RectTransform)giveBtn.transform).sizeDelta = new Vector2(150f, 65f);

        var giveAllCharsBtn = CreateButton(adminPanel.transform, "GiveAllCharsBtn", "전체 캐릭터 지급", new Vector2(0f, -120f), font, 26);

        var roll1Btn  = CreateButton(adminPanel.transform, "Roll1Btn",  "가챠 1회 (무료)", new Vector2(-160f, -210f), font, 26);
        var roll10Btn = CreateButton(adminPanel.transform, "Roll10Btn", "10연 (무료)",      new Vector2( 160f, -210f), font, 26);
        ((RectTransform)roll1Btn.transform).sizeDelta  = new Vector2(310f, 65f);
        ((RectTransform)roll10Btn.transform).sizeDelta = new Vector2(310f, 65f);

        var resetBtn = CreateButton(adminPanel.transform, "ResetBtn", "계정 초기화", new Vector2(-200f, -300f), font, 24, 0.5f);
        var saveBtn  = CreateButton(adminPanel.transform, "SaveBtn",  "강제 저장",   new Vector2(   0f, -300f), font, 24);
        var loadBtn  = CreateButton(adminPanel.transform, "LoadBtn",  "불러오기",    new Vector2( 200f, -300f), font, 24);
        ((RectTransform)resetBtn.transform).sizeDelta = new Vector2(250f, 60f);
        ((RectTransform)saveBtn.transform).sizeDelta  = new Vector2(220f, 60f);
        ((RectTransform)loadBtn.transform).sizeDelta  = new Vector2(200f, 60f);

        var statusAdmin = CreateLabel(adminPanel.transform, "Status", "", 24, new Vector2(0f, -400f), font);
        var closeBtnA   = CreateButton(adminPanel.transform, "CloseBtn", "닫기  [F1]", new Vector2(0f, -490f), font, 28, 0.4f);

        var adminCmp = adminPanel.AddComponent<AdminPanel>();
        var aSo = new SerializedObject(adminCmp);
        aSo.FindProperty("addGoldButton").objectReferenceValue       = addGoldBtn.GetComponent<Button>();
        aSo.FindProperty("addPaperButton").objectReferenceValue      = addPaperBtn.GetComponent<Button>();
        aSo.FindProperty("addFocusButton").objectReferenceValue      = addFocusBtn.GetComponent<Button>();
        aSo.FindProperty("addFragmentButton").objectReferenceValue   = addFragmentBtn.GetComponent<Button>();
        aSo.FindProperty("giveCrystalsButton").objectReferenceValue  = giveCrystalsBtn.GetComponent<Button>();
        aSo.FindProperty("characterIdInput").objectReferenceValue    = charInput.GetComponent<TMP_InputField>();
        aSo.FindProperty("giveCharacterButton").objectReferenceValue    = giveBtn.GetComponent<Button>();
        aSo.FindProperty("giveAllCharactersButton").objectReferenceValue = giveAllCharsBtn.GetComponent<Button>();
        aSo.FindProperty("debugRollOneButton").objectReferenceValue  = roll1Btn.GetComponent<Button>();
        aSo.FindProperty("debugRollTenButton").objectReferenceValue  = roll10Btn.GetComponent<Button>();
        aSo.FindProperty("resetAccountButton").objectReferenceValue  = resetBtn.GetComponent<Button>();
        aSo.FindProperty("forceSaveButton").objectReferenceValue     = saveBtn.GetComponent<Button>();
        aSo.FindProperty("forceLoadButton").objectReferenceValue     = loadBtn.GetComponent<Button>();
        aSo.FindProperty("statusText").objectReferenceValue          = statusAdmin.GetComponent<TMP_Text>();
        aSo.FindProperty("closeButton").objectReferenceValue         = closeBtnA.GetComponent<Button>();
        aSo.ApplyModifiedProperties();

        UnityEventTools.AddVoidPersistentListener(addGoldBtn.GetComponent<Button>().onClick,      adminCmp.OnAddGold);
        UnityEventTools.AddVoidPersistentListener(addPaperBtn.GetComponent<Button>().onClick,     adminCmp.OnAddPaper);
        UnityEventTools.AddVoidPersistentListener(addFocusBtn.GetComponent<Button>().onClick,     adminCmp.OnAddFocus);
        UnityEventTools.AddVoidPersistentListener(addFragmentBtn.GetComponent<Button>().onClick,  adminCmp.OnAddFragment);
        UnityEventTools.AddVoidPersistentListener(giveCrystalsBtn.GetComponent<Button>().onClick, adminCmp.OnGiveCrystals);
        UnityEventTools.AddVoidPersistentListener(giveBtn.GetComponent<Button>().onClick,             adminCmp.OnGiveCharacter);
        UnityEventTools.AddVoidPersistentListener(giveAllCharsBtn.GetComponent<Button>().onClick,     adminCmp.OnGiveAllCharacters);
        UnityEventTools.AddVoidPersistentListener(roll1Btn.GetComponent<Button>().onClick,            adminCmp.OnDebugRollOne);
        UnityEventTools.AddVoidPersistentListener(roll10Btn.GetComponent<Button>().onClick,       adminCmp.OnDebugRollTen);
        UnityEventTools.AddVoidPersistentListener(resetBtn.GetComponent<Button>().onClick,        adminCmp.OnResetAccount);
        UnityEventTools.AddVoidPersistentListener(saveBtn.GetComponent<Button>().onClick,         adminCmp.OnForceSave);
        UnityEventTools.AddVoidPersistentListener(loadBtn.GetComponent<Button>().onClick,         adminCmp.OnForceLoad);
        UnityEventTools.AddVoidPersistentListener(closeBtnA.GetComponent<Button>().onClick,       adminCmp.OnCloseClicked);

        // ── RnEPanel (K키, R&E) ─────────────────────────────────────────
        // 허브 구조: 3열 그리드 → 카드 클릭 → 개인 창
        //   [레벨업] → 난이도 선택 → 자원 소모 → 문제(최대 3시도) → EXP
        //   [스킬 연구] → 전체폭 스킬 목록 → 잠금 클릭 → 문제 → 해금
        var skillPanel = CreateCentrePanel(canvasGO.transform, "RnEPanel", new Vector2(1000f, 950f));
        skillPanel.SetActive(false);

        // 제목 (상단 좌측)
        {
            var g = new GameObject("Title"); g.transform.SetParent(skillPanel.transform, false);
            var r = g.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0f,1f); r.anchorMax = new Vector2(1f,1f);
            r.offsetMin = new Vector2(14f,-68f); r.offsetMax = new Vector2(-155f,0f);
            var t = g.AddComponent<TextMeshProUGUI>();
            t.text = "R&E  [K]"; t.fontSize = 36; t.color = Color.white;
            t.alignment = TextAlignmentOptions.MidlineLeft;
            if (font != null) t.font = font;
        }

        // 닫기 버튼 (상단 우측)
        var closeBtnKGO = new GameObject("CloseBtn");
        closeBtnKGO.transform.SetParent(skillPanel.transform, false);
        {
            var r = closeBtnKGO.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(1f,1f); r.anchorMax = new Vector2(1f,1f);
            r.pivot = new Vector2(1f,1f);
            r.offsetMin = new Vector2(-145f,-66f); r.offsetMax = new Vector2(-8f,-4f);
            closeBtnKGO.AddComponent<Image>().color = new Color(0.22f,0.22f,0.32f,0.95f);
            closeBtnKGO.AddComponent<Button>();
            var tg = new GameObject("Text"); tg.transform.SetParent(closeBtnKGO.transform, false);
            var tr = tg.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.sizeDelta = Vector2.zero;
            var t = tg.AddComponent<TextMeshProUGUI>();
            t.text = "닫기  [K]"; t.fontSize = 22; t.color = Color.white;
            t.alignment = TextAlignmentOptions.Center;
            if (font != null) t.font = font;
        }

        // 캐릭터 그리드 스크롤뷰 (초기: 전체폭, 3열)
        var charSvGO = new GameObject("CharScrollView");
        charSvGO.transform.SetParent(skillPanel.transform, false);
        var charSvRt = charSvGO.AddComponent<RectTransform>();
        charSvRt.anchorMin = new Vector2(0f, 0f);
        charSvRt.anchorMax = new Vector2(1f, 1f);
        charSvRt.offsetMin = new Vector2(10f,  65f);
        charSvRt.offsetMax = new Vector2(-10f, -70f);
        charSvGO.AddComponent<Image>().color = new Color(0.04f, 0.04f, 0.07f, 0.85f);
        var charSv = charSvGO.AddComponent<ScrollRect>();
        charSv.horizontal = false;

        var charVpGO = new GameObject("Viewport");
        charVpGO.transform.SetParent(charSvGO.transform, false);
        var charVpRt = charVpGO.AddComponent<RectTransform>();
        charVpRt.anchorMin = Vector2.zero; charVpRt.anchorMax = Vector2.one;
        charVpRt.offsetMin = Vector2.zero; charVpRt.offsetMax = Vector2.zero;
        charVpGO.AddComponent<RectMask2D>();

        var charContentGO = new GameObject("Content");
        charContentGO.transform.SetParent(charVpGO.transform, false);
        var charContentRt = charContentGO.AddComponent<RectTransform>();
        charContentRt.anchorMin = new Vector2(0f, 1f);
        charContentRt.anchorMax = new Vector2(1f, 1f);
        charContentRt.pivot     = new Vector2(0.5f, 1f);
        charContentRt.sizeDelta = Vector2.zero;
        var rneGridLayout = charContentGO.AddComponent<GridLayoutGroup>();
        rneGridLayout.cellSize        = new Vector2(316f, 200f);
        rneGridLayout.spacing         = new Vector2(8f, 8f);
        rneGridLayout.padding         = new RectOffset(8, 8, 8, 8);
        rneGridLayout.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        rneGridLayout.constraintCount = 3;
        rneGridLayout.startAxis       = GridLayoutGroup.Axis.Horizontal;
        charContentGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        charSv.viewport = charVpRt;
        charSv.content  = charContentRt;

        // 상세 패널 (허브, 카드 선택 시 우측 표시)
        var detailPanelGO = new GameObject("DetailPanel");
        detailPanelGO.transform.SetParent(skillPanel.transform, false);
        {
            var r = detailPanelGO.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0f, 0f); r.anchorMax = new Vector2(1f, 1f);
            r.offsetMin = new Vector2(410f, 65f); r.offsetMax = new Vector2(-10f, -70f);
            detailPanelGO.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.10f, 0.97f);
        }
        detailPanelGO.SetActive(false);

        // 초상화 (상단 40%, 좌측 30%)
        var detailPortraitGO = new GameObject("PortraitBox");
        detailPortraitGO.transform.SetParent(detailPanelGO.transform, false);
        var detailPortrait = (Image)null;
        {
            var r = detailPortraitGO.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0f, 0.60f); r.anchorMax = new Vector2(0.30f, 1f);
            r.offsetMin = new Vector2(8f, 8f); r.offsetMax = new Vector2(-4f, -8f);
            detailPortrait = detailPortraitGO.AddComponent<Image>();
            detailPortrait.color = Color.gray;
        }

        // 정보 텍스트 (상단 40%, 우측 70%)
        TMP_Text MakeDetailText(string dName, string dTxt, float xMin, float yMin, float yMax,
                                int fs, Color col)
        {
            var g = new GameObject(dName); g.transform.SetParent(detailPanelGO.transform, false);
            var r = g.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(xMin, yMin); r.anchorMax = new Vector2(1f, yMax);
            r.offsetMin = new Vector2(4f, 2f); r.offsetMax = new Vector2(-8f, -2f);
            var t = g.AddComponent<TextMeshProUGUI>();
            t.text = dTxt; t.fontSize = fs; t.color = col;
            t.alignment = TextAlignmentOptions.MidlineLeft;
            if (font != null) t.font = font;
            return t;
        }

        var detailNameTxt      = MakeDetailText("CharNameText",   "--", 0.30f, 0.87f, 1.00f, 24, Color.white);
        var detailContinentTxt = MakeDetailText("ContinentText",  "--", 0.30f, 0.80f, 0.87f, 18, new Color(0.8f,0.9f,1f));
        var detailLevelTxt     = MakeDetailText("LevelText",      "--", 0.30f, 0.73f, 0.80f, 18, Color.white);
        var detailExpTxt       = MakeDetailText("ExpText",        "--", 0.30f, 0.66f, 0.73f, 16, new Color(0.7f,1f,0.7f));
        var detailMaterialTxt  = MakeDetailText("MaterialText",   "--", 0.30f, 0.60f, 0.66f, 16, new Color(1f,0.85f,0.5f));

        // 난이도 선택 패널 (콘텐츠 영역 0.12~0.60, 기본 비활성)
        var difficultyPanelGO = new GameObject("DifficultyPanel");
        difficultyPanelGO.transform.SetParent(detailPanelGO.transform, false);
        {
            var r = difficultyPanelGO.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0f, 0.12f); r.anchorMax = new Vector2(1f, 0.60f);
            r.offsetMin = new Vector2(8f, 4f); r.offsetMax = new Vector2(-8f, -4f);
            difficultyPanelGO.AddComponent<Image>().color = new Color(0.04f,0.04f,0.07f,0.7f);
        }
        difficultyPanelGO.SetActive(false);

        // 난이도 헤더
        {
            var g = new GameObject("DiffHeader"); g.transform.SetParent(difficultyPanelGO.transform, false);
            var r = g.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0f,0.80f); r.anchorMax = new Vector2(1f,1f);
            r.offsetMin = new Vector2(8f,0f); r.offsetMax = new Vector2(-8f,-4f);
            var t = g.AddComponent<TextMeshProUGUI>();
            t.text = "난이도 선택  (자원 소모 → 문제 풀이 → EXP 획득)";
            t.fontSize = 18; t.color = new Color(0.9f,0.9f,0.7f);
            t.alignment = TextAlignmentOptions.MidlineLeft;
            if (font != null) t.font = font;
        }

        // 하/중/상 버튼
        var difficultyBtns = new Button[3];
        var difficultyLbls = new TMP_Text[3];
        float[] diffYMin = { 0f,    0.27f, 0.54f };
        float[] diffYMax = { 0.26f, 0.53f, 0.79f };
        var diffColors = new Color[]
        {
            new Color(0.2f,0.5f,0.2f,0.9f),
            new Color(0.5f,0.4f,0.1f,0.9f),
            new Color(0.5f,0.1f,0.1f,0.9f),
        };
        for (int i = 0; i < 3; i++)
        {
            var db = new GameObject("DiffBtn_" + i);
            db.transform.SetParent(difficultyPanelGO.transform, false);
            var r = db.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0f, diffYMin[i]);
            r.anchorMax = new Vector2(1f, diffYMax[i]);
            r.offsetMin = new Vector2(8f, 2f); r.offsetMax = new Vector2(-8f, -2f);
            db.AddComponent<Image>().color = diffColors[i];
            difficultyBtns[i] = db.AddComponent<Button>();

            var lGO = new GameObject("Label"); lGO.transform.SetParent(db.transform, false);
            var lr = lGO.AddComponent<RectTransform>();
            lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
            lr.offsetMin = new Vector2(10f,0f); lr.offsetMax = new Vector2(-10f,0f);
            var lt = lGO.AddComponent<TextMeshProUGUI>();
            lt.text = "--"; lt.fontSize = 20; lt.color = Color.white;
            lt.alignment = TextAlignmentOptions.MidlineLeft;
            if (font != null) lt.font = font;
            difficultyLbls[i] = lt;
        }

        // 스킬 목록 패널 (콘텐츠 영역 0.12~0.60, 전체폭, 기본 비활성)
        var skillListPanelGO = new GameObject("SkillListPanel");
        skillListPanelGO.transform.SetParent(detailPanelGO.transform, false);
        {
            var r = skillListPanelGO.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0f, 0.12f); r.anchorMax = new Vector2(1f, 0.60f);
            r.offsetMin = new Vector2(8f, 4f); r.offsetMax = new Vector2(-8f, -4f);
            skillListPanelGO.AddComponent<Image>().color = new Color(0.04f,0.04f,0.07f,0.8f);
        }
        skillListPanelGO.SetActive(false);

        // 스킬 목록 헤더
        {
            var g = new GameObject("SkillHeader"); g.transform.SetParent(skillListPanelGO.transform, false);
            var r = g.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0f,1f); r.anchorMax = new Vector2(1f,1f);
            r.offsetMin = new Vector2(8f,-38f); r.offsetMax = new Vector2(-8f,0f);
            var t = g.AddComponent<TextMeshProUGUI>();
            t.text = "스킬 목록  (잠금 클릭 → 문제 풀이 → 해금)";
            t.fontSize = 18; t.color = new Color(0.7f,0.85f,1f);
            t.alignment = TextAlignmentOptions.MidlineLeft;
            if (font != null) t.font = font;
        }

        // 스킬 스크롤뷰 (전체 폭)
        var skillSvGO2 = new GameObject("SkillScrollView");
        skillSvGO2.transform.SetParent(skillListPanelGO.transform, false);
        var skillContentRt = (RectTransform)null;
        {
            var r = skillSvGO2.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0f,0f); r.anchorMax = new Vector2(1f,1f);
            r.offsetMin = new Vector2(0f,0f); r.offsetMax = new Vector2(0f,-40f);
            skillSvGO2.AddComponent<Image>().color = new Color(0,0,0,0);
            var sv = skillSvGO2.AddComponent<ScrollRect>();
            sv.horizontal = false;

            var vpGO = new GameObject("Viewport");
            vpGO.transform.SetParent(skillSvGO2.transform, false);
            var vpRt = vpGO.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero; vpRt.offsetMax = Vector2.zero;
            vpGO.AddComponent<RectMask2D>();

            var cGO = new GameObject("Content");
            cGO.transform.SetParent(vpGO.transform, false);
            var cRt = cGO.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0f,1f); cRt.anchorMax = new Vector2(1f,1f);
            cRt.pivot = new Vector2(0.5f,1f); cRt.sizeDelta = Vector2.zero;
            var vl = cGO.AddComponent<VerticalLayoutGroup>();
            vl.childForceExpandWidth = true; vl.childForceExpandHeight = false;
            vl.spacing = 4f; vl.padding = new RectOffset(4,4,4,4);
            cGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sv.viewport = vpRt; sv.content = cRt;
            skillContentRt = cRt;
        }

        // ── 스킬 정보 패널 (스킬 선택 시 표시, detailPanel 위에 overlay) ────
        var skillInfoPanelGO = new GameObject("SkillInfoPanel");
        skillInfoPanelGO.transform.SetParent(detailPanelGO.transform, false);
        {
            var r = skillInfoPanelGO.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0f, 0.12f); r.anchorMax = new Vector2(1f, 0.60f);
            r.offsetMin = new Vector2(8f, 4f); r.offsetMax = new Vector2(-8f, -4f);
            skillInfoPanelGO.AddComponent<Image>().color = new Color(0.05f, 0.07f, 0.13f, 0.98f);
        }
        skillInfoPanelGO.SetActive(false);

        // 스킬 이름 (상단 20%)
        var skillInfoNameGO = new GameObject("SkillInfoName");
        skillInfoNameGO.transform.SetParent(skillInfoPanelGO.transform, false);
        TMP_Text skillInfoNameTxt;
        {
            var r = skillInfoNameGO.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0f, 0.80f); r.anchorMax = new Vector2(1f, 1f);
            r.offsetMin = new Vector2(14f, 0f); r.offsetMax = new Vector2(-14f, -4f);
            skillInfoNameTxt = skillInfoNameGO.AddComponent<TextMeshProUGUI>();
            skillInfoNameTxt.fontSize = 22; skillInfoNameTxt.color = Color.white;
            skillInfoNameTxt.fontStyle = FontStyles.Bold;
            skillInfoNameTxt.alignment = TextAlignmentOptions.MidlineLeft;
            if (font != null) skillInfoNameTxt.font = font;
        }

        // 스탯·설명 텍스트 (중간 50%)
        var skillInfoStatsGO = new GameObject("SkillInfoStats");
        skillInfoStatsGO.transform.SetParent(skillInfoPanelGO.transform, false);
        TMP_Text skillInfoStatsTxt;
        {
            var r = skillInfoStatsGO.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0f, 0.30f); r.anchorMax = new Vector2(1f, 0.80f);
            r.offsetMin = new Vector2(14f, 4f); r.offsetMax = new Vector2(-14f, 0f);
            skillInfoStatsTxt = skillInfoStatsGO.AddComponent<TextMeshProUGUI>();
            skillInfoStatsTxt.fontSize = 17;
            skillInfoStatsTxt.color = new Color(0.85f, 0.92f, 1f);
            skillInfoStatsTxt.alignment = TextAlignmentOptions.TopLeft;
            skillInfoStatsTxt.textWrappingMode = TextWrappingModes.Normal;
            if (font != null) skillInfoStatsTxt.font = font;
        }

        // [연구 시작] 버튼 (하단 좌측 58%)
        var skillResearchStartBtnGO = new GameObject("SkillResearchStartBtn");
        skillResearchStartBtnGO.transform.SetParent(skillInfoPanelGO.transform, false);
        {
            var r = skillResearchStartBtnGO.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0f, 0.02f); r.anchorMax = new Vector2(0.58f, 0.28f);
            r.offsetMin = new Vector2(8f, 2f); r.offsetMax = new Vector2(-4f, -2f);
            skillResearchStartBtnGO.AddComponent<Image>().color = new Color(0.2f, 0.55f, 0.25f, 1f);
            skillResearchStartBtnGO.AddComponent<Button>();
            var tg = new GameObject("Label"); tg.transform.SetParent(skillResearchStartBtnGO.transform, false);
            var tr = tg.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.sizeDelta = Vector2.zero;
            var lt = tg.AddComponent<TextMeshProUGUI>();
            lt.text = "연구 시작"; lt.fontSize = 20; lt.color = Color.white;
            lt.alignment = TextAlignmentOptions.Center;
            if (font != null) lt.font = font;
        }

        // [목록으로] 버튼 (하단 우측 38%)
        var skillInfoBackBtnGO = new GameObject("SkillInfoBackBtn");
        skillInfoBackBtnGO.transform.SetParent(skillInfoPanelGO.transform, false);
        {
            var r = skillInfoBackBtnGO.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0.62f, 0.02f); r.anchorMax = new Vector2(1f, 0.28f);
            r.offsetMin = new Vector2(4f, 2f); r.offsetMax = new Vector2(-8f, -2f);
            skillInfoBackBtnGO.AddComponent<Image>().color = new Color(0.30f, 0.30f, 0.40f, 1f);
            skillInfoBackBtnGO.AddComponent<Button>();
            var tg = new GameObject("Label"); tg.transform.SetParent(skillInfoBackBtnGO.transform, false);
            var tr = tg.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.sizeDelta = Vector2.zero;
            var lt = tg.AddComponent<TextMeshProUGUI>();
            lt.text = "목록으로"; lt.fontSize = 20; lt.color = Color.white;
            lt.alignment = TextAlignmentOptions.Center;
            if (font != null) lt.font = font;
        }

        // 우하단 액션 버튼: [레벨업] [스킬 연구] (0~12%)
        var lvUpModeBtnGO = new GameObject("LevelUpModeBtn");
        lvUpModeBtnGO.transform.SetParent(detailPanelGO.transform, false);
        {
            var r = lvUpModeBtnGO.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0f, 0.01f); r.anchorMax = new Vector2(0.48f, 0.11f);
            r.offsetMin = new Vector2(8f, 0f); r.offsetMax = new Vector2(-4f, 0f);
            lvUpModeBtnGO.AddComponent<Image>().color = new Color(0.2f,0.6f,0.25f,1f);
            lvUpModeBtnGO.AddComponent<Button>();
            var tg = new GameObject("Label"); tg.transform.SetParent(lvUpModeBtnGO.transform, false);
            var tr = tg.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.sizeDelta = Vector2.zero;
            var lt = tg.AddComponent<TextMeshProUGUI>();
            lt.text = "레벨업"; lt.fontSize = 20; lt.color = Color.white;
            lt.alignment = TextAlignmentOptions.Center;
            if (font != null) lt.font = font;
        }

        var skillModeBtnGO = new GameObject("SkillModeBtn");
        skillModeBtnGO.transform.SetParent(detailPanelGO.transform, false);
        {
            var r = skillModeBtnGO.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0.52f, 0.01f); r.anchorMax = new Vector2(1f, 0.11f);
            r.offsetMin = new Vector2(4f, 0f); r.offsetMax = new Vector2(-8f, 0f);
            skillModeBtnGO.AddComponent<Image>().color = new Color(0.2f,0.35f,0.65f,1f);
            skillModeBtnGO.AddComponent<Button>();
            var tg = new GameObject("Label"); tg.transform.SetParent(skillModeBtnGO.transform, false);
            var tr = tg.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.sizeDelta = Vector2.zero;
            var lt = tg.AddComponent<TextMeshProUGUI>();
            lt.text = "스킬 연구"; lt.fontSize = 20; lt.color = Color.white;
            lt.alignment = TextAlignmentOptions.Center;
            if (font != null) lt.font = font;
        }

        // 문제 오버레이 (K 패널 전체 덮기)
        var problemOverlayGO = new GameObject("ProblemOverlay");
        problemOverlayGO.transform.SetParent(skillPanel.transform, false);
        {
            var r = problemOverlayGO.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0f,0f); r.anchorMax = new Vector2(1f,1f);
            r.offsetMin = new Vector2(10f,65f); r.offsetMax = new Vector2(-10f,-70f);
            problemOverlayGO.AddComponent<Image>().color = new Color(0.03f,0.03f,0.06f,0.97f);
        }
        problemOverlayGO.SetActive(false);

        // 문제 텍스트 (상단 35%)
        var promptGO = new GameObject("PromptText"); promptGO.transform.SetParent(problemOverlayGO.transform, false);
        var promptText = (TMP_Text)null;
        {
            var r = promptGO.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0f,0.65f); r.anchorMax = new Vector2(1f,1f);
            r.offsetMin = new Vector2(16f,4f); r.offsetMax = new Vector2(-16f,-8f);
            promptText = promptGO.AddComponent<TextMeshProUGUI>();
            promptText.fontSize = 22; promptText.color = Color.white;
            promptText.alignment = TextAlignmentOptions.TopLeft;
            if (font != null) promptText.font = font;
        }

        // 시도 카운터 (상단 우측)
        var attemptGO = new GameObject("AttemptText"); attemptGO.transform.SetParent(problemOverlayGO.transform, false);
        var attemptText = (TMP_Text)null;
        {
            var r = attemptGO.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0.6f,0.90f); r.anchorMax = new Vector2(1f,1f);
            r.offsetMin = new Vector2(0f,-4f); r.offsetMax = new Vector2(-16f,-4f);
            attemptText = attemptGO.AddComponent<TextMeshProUGUI>();
            attemptText.fontSize = 20; attemptText.color = new Color(1f,0.9f,0.5f);
            attemptText.alignment = TextAlignmentOptions.MidlineRight;
            if (font != null) attemptText.font = font;
        }

        // 객관식 영역 (중간 50%)
        var mcArea = new GameObject("MultipleChoiceArea"); mcArea.transform.SetParent(problemOverlayGO.transform, false);
        var choiceButtons = new Button[4];
        var choiceLabels  = new TMP_Text[4];
        {
            var r = mcArea.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0f,0.15f); r.anchorMax = new Vector2(1f,0.65f);
            r.offsetMin = new Vector2(12f,0f); r.offsetMax = new Vector2(-12f,0f);
            for (int i = 0; i < 4; i++)
            {
                float yMin = 1f - (i+1)*0.25f, yMax = 1f - i*0.25f;
                var cb = new GameObject("Choice" + i); cb.transform.SetParent(mcArea.transform, false);
                var cbr = cb.AddComponent<RectTransform>();
                cbr.anchorMin = new Vector2(0f,yMin); cbr.anchorMax = new Vector2(1f,yMax);
                cbr.offsetMin = new Vector2(0f,2f); cbr.offsetMax = new Vector2(0f,-2f);
                cb.AddComponent<Image>().color = new Color(0.2f,0.35f,0.6f,0.9f);
                choiceButtons[i] = cb.AddComponent<Button>();
                var lGO = new GameObject("Label"); lGO.transform.SetParent(cb.transform, false);
                var lr = lGO.AddComponent<RectTransform>();
                lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
                lr.offsetMin = new Vector2(10f,0f); lr.offsetMax = new Vector2(-6f,0f);
                var lt = lGO.AddComponent<TextMeshProUGUI>();
                lt.text = "보기 " + (i+1); lt.fontSize = 20; lt.color = Color.white;
                lt.alignment = TextAlignmentOptions.MidlineLeft;
                if (font != null) lt.font = font;
                choiceLabels[i] = lt;
            }
        }

        // 주관식 영역
        var fiArea = new GameObject("FreeInputArea"); fiArea.transform.SetParent(problemOverlayGO.transform, false);
        var answerInputGO = (GameObject)null; var submitBtnGO = (GameObject)null;
        {
            var r = fiArea.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0f,0.22f); r.anchorMax = new Vector2(1f,0.62f);
            r.offsetMin = new Vector2(12f,0f); r.offsetMax = new Vector2(-12f,0f);
            answerInputGO = CreateInput(fiArea.transform, "AnswerInput", "답 입력...", Vector2.zero, font);
            ((RectTransform)answerInputGO.transform).anchorMin = new Vector2(0f,0.52f);
            ((RectTransform)answerInputGO.transform).anchorMax = new Vector2(1f,0.90f);
            ((RectTransform)answerInputGO.transform).offsetMin = Vector2.zero;
            ((RectTransform)answerInputGO.transform).offsetMax = Vector2.zero;
            submitBtnGO = CreateButton(fiArea.transform, "SubmitBtn", "제출", Vector2.zero, font, 26);
            ((RectTransform)submitBtnGO.transform).anchorMin = new Vector2(0f,0.04f);
            ((RectTransform)submitBtnGO.transform).anchorMax = new Vector2(1f,0.46f);
            ((RectTransform)submitBtnGO.transform).offsetMin = Vector2.zero;
            ((RectTransform)submitBtnGO.transform).offsetMax = Vector2.zero;
        }
        fiArea.SetActive(false);

        // 피드백 텍스트
        var fbGO = new GameObject("FeedbackText"); fbGO.transform.SetParent(problemOverlayGO.transform, false);
        var fbText = (TMP_Text)null;
        {
            var r = fbGO.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0f,0.08f); r.anchorMax = new Vector2(1f,0.14f);
            r.offsetMin = new Vector2(12f,0f); r.offsetMax = new Vector2(-12f,0f);
            fbText = fbGO.AddComponent<TextMeshProUGUI>();
            fbText.fontSize = 22; fbText.color = Color.white; fbText.alignment = TextAlignmentOptions.Center;
            if (font != null) fbText.font = font;
        }
        fbGO.SetActive(false);

        // 해설 텍스트
        var exGO = new GameObject("ExplanationText"); exGO.transform.SetParent(problemOverlayGO.transform, false);
        var exText = (TMP_Text)null;
        {
            var r = exGO.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0f,0.02f); r.anchorMax = new Vector2(1f,0.08f);
            r.offsetMin = new Vector2(12f,0f); r.offsetMax = new Vector2(-12f,0f);
            exText = exGO.AddComponent<TextMeshProUGUI>();
            exText.fontSize = 18; exText.color = new Color(0.8f,1f,0.8f);
            exText.alignment = TextAlignmentOptions.MidlineLeft;
            if (font != null) exText.font = font;
        }
        exGO.SetActive(false);

        // 문제 닫기 버튼 (우상단)
        var closeProbBtnGO = new GameObject("CloseProblemBtn");
        closeProbBtnGO.transform.SetParent(problemOverlayGO.transform, false);
        {
            var r = closeProbBtnGO.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(1f,1f); r.anchorMax = new Vector2(1f,1f);
            r.pivot = new Vector2(1f,1f);
            r.offsetMin = new Vector2(-130f,-52f); r.offsetMax = new Vector2(-6f,-6f);
            closeProbBtnGO.AddComponent<Image>().color = new Color(0.5f,0.15f,0.15f,0.95f);
            closeProbBtnGO.AddComponent<Button>();
            var tg = new GameObject("Label"); tg.transform.SetParent(closeProbBtnGO.transform, false);
            var tr = tg.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.sizeDelta = Vector2.zero;
            var ct = tg.AddComponent<TextMeshProUGUI>();
            ct.text = "문제 닫기"; ct.fontSize = 20; ct.color = Color.white;
            ct.alignment = TextAlignmentOptions.Center;
            if (font != null) ct.font = font;
        }

        skillPanel.SetActive(false);

        // RnEPanel 컴포넌트 연결
        var srp   = skillPanel.AddComponent<RnEPanel>();
        var srpSo = new SerializedObject(srp);

        // 그리드
        srpSo.FindProperty("charScrollViewRt").objectReferenceValue = charSvRt;
        srpSo.FindProperty("charGridContent").objectReferenceValue  = charContentRt;
        srpSo.FindProperty("gridLayout").objectReferenceValue       = rneGridLayout;

        // 개인 창 정보
        srpSo.FindProperty("detailPanel").objectReferenceValue         = detailPanelGO;
        srpSo.FindProperty("detailPortrait").objectReferenceValue      = detailPortrait;
        srpSo.FindProperty("detailNameText").objectReferenceValue      = detailNameTxt;
        srpSo.FindProperty("detailContinentText").objectReferenceValue = detailContinentTxt;
        srpSo.FindProperty("detailLevelText").objectReferenceValue     = detailLevelTxt;
        srpSo.FindProperty("detailExpText").objectReferenceValue       = detailExpTxt;
        srpSo.FindProperty("detailMaterialText").objectReferenceValue  = detailMaterialTxt;

        // 허브 버튼
        srpSo.FindProperty("levelUpModeButton").objectReferenceValue = lvUpModeBtnGO.GetComponent<Button>();
        srpSo.FindProperty("skillModeButton").objectReferenceValue   = skillModeBtnGO.GetComponent<Button>();

        // 난이도 패널
        srpSo.FindProperty("difficultyPanel").objectReferenceValue = difficultyPanelGO;
        var dbArr = srpSo.FindProperty("difficultyButtons");
        dbArr.arraySize = 3;
        for (int i = 0; i < 3; i++)
            dbArr.GetArrayElementAtIndex(i).objectReferenceValue = difficultyBtns[i];
        var dlArr = srpSo.FindProperty("difficultyLabels");
        dlArr.arraySize = 3;
        for (int i = 0; i < 3; i++)
            dlArr.GetArrayElementAtIndex(i).objectReferenceValue = difficultyLbls[i];

        // 스킬 목록 패널
        srpSo.FindProperty("skillListPanel").objectReferenceValue   = skillListPanelGO;
        srpSo.FindProperty("skillListContent").objectReferenceValue = skillContentRt;

        // 스킬 정보 패널
        srpSo.FindProperty("skillInfoPanel").objectReferenceValue           = skillInfoPanelGO;
        srpSo.FindProperty("skillInfoNameText").objectReferenceValue        = skillInfoNameTxt;
        srpSo.FindProperty("skillInfoStatsText").objectReferenceValue       = skillInfoStatsTxt;
        srpSo.FindProperty("skillResearchStartButton").objectReferenceValue = skillResearchStartBtnGO.GetComponent<Button>();
        srpSo.FindProperty("skillInfoBackButton").objectReferenceValue      = skillInfoBackBtnGO.GetComponent<Button>();

        // 문제 오버레이
        srpSo.FindProperty("problemOverlay").objectReferenceValue     = problemOverlayGO;
        srpSo.FindProperty("promptText").objectReferenceValue         = promptText;
        srpSo.FindProperty("attemptText").objectReferenceValue        = attemptText;
        srpSo.FindProperty("multipleChoiceArea").objectReferenceValue = mcArea;
        srpSo.FindProperty("freeInputArea").objectReferenceValue      = fiArea;
        srpSo.FindProperty("answerInput").objectReferenceValue        = answerInputGO.GetComponent<TMP_InputField>();
        srpSo.FindProperty("submitButton").objectReferenceValue       = submitBtnGO.GetComponent<Button>();
        srpSo.FindProperty("feedbackText").objectReferenceValue       = fbText;
        srpSo.FindProperty("explanationText").objectReferenceValue    = exText;
        srpSo.FindProperty("closeProblemButton").objectReferenceValue = closeProbBtnGO.GetComponent<Button>();
        srpSo.FindProperty("closeButton").objectReferenceValue        = closeBtnKGO.GetComponent<Button>();

        var cbArr = srpSo.FindProperty("choiceButtons");
        cbArr.arraySize = 4;
        for (int i = 0; i < 4; i++)
            cbArr.GetArrayElementAtIndex(i).objectReferenceValue = choiceButtons[i];
        var clArr = srpSo.FindProperty("choiceLabels");
        clArr.arraySize = 4;
        for (int i = 0; i < 4; i++)
            clArr.GetArrayElementAtIndex(i).objectReferenceValue = choiceLabels[i];
        srpSo.ApplyModifiedProperties();

        UnityEventTools.AddVoidPersistentListener(closeBtnKGO.GetComponent<Button>().onClick, srp.OnCloseClicked);

        // MetaPanelController 참조 연결
        var ctrlSo = new SerializedObject(controller);
        ctrlSo.FindProperty("collectionPanel").objectReferenceValue = collPanel;
        ctrlSo.FindProperty("inventoryPanel").objectReferenceValue  = invPanel;
        ctrlSo.FindProperty("rnePanel").objectReferenceValue        = skillPanel;
        ctrlSo.FindProperty("adminPanel").objectReferenceValue      = adminPanel;
        ctrlSo.ApplyModifiedProperties();

        // 씬 저장
        EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene(), SCENE_PATH);
        AssetDatabase.Refresh();

        Debug.Log("[MSRPG] Meta UI 설정 완료! Mesoria 씬에 MetaCanvas가 추가됐습니다.");
    }

    // ── UI 생성 헬퍼 ──────────────────────────────────────────────────────

    static void EnsureTmpDefaultFont(TMP_FontAsset font)
    {
        if (font == null) return;
        var settings = Resources.Load<TMPro.TMP_Settings>("TMP Settings");
        if (settings == null) return;
        var so = new SerializedObject(settings);
        var prop = so.FindProperty("m_defaultFontAsset");
        if (prop != null && prop.objectReferenceValue == null)
        {
            prop.objectReferenceValue = font;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    static GameObject CreateStrip(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = size;
        rt.anchoredPosition = pos;
        return go;
    }

    static GameObject CreateCentrePanel(Transform parent, string name, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = size;
        rt.anchoredPosition = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.07f, 0.07f, 0.12f, 0.97f);
        return go;
    }

    static GameObject CreateLabel(Transform parent, string name, string text,
                                   int fontSize, Vector2 pos, TMP_FontAsset font)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(650f, 60f);
        rt.anchoredPosition = pos;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text      = text;
        t.fontSize  = fontSize;
        t.color     = Color.white;
        t.alignment = TextAlignmentOptions.Center;
        if (font != null) t.font = font;
        return go;
    }

    static GameObject CreateButton(Transform parent, string name, string label,
                                    Vector2 pos, TMP_FontAsset font,
                                    int fontSize = 32, float bgAlpha = 1f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(580f, 70f);
        rt.anchoredPosition = pos;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.25f, 0.5f, 1f, bgAlpha);
        go.AddComponent<Button>();

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var trt = textGO.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero;
        var t = textGO.AddComponent<TextMeshProUGUI>();
        t.text      = label;
        t.fontSize  = fontSize;
        t.color     = bgAlpha > 0.3f ? Color.white : new Color(0.6f, 0.8f, 1f);
        t.alignment = TextAlignmentOptions.Center;
        if (font != null) t.font = font;

        return go;
    }

    static GameObject CreateInput(Transform parent, string name, string placeholder,
                                   Vector2 pos, TMP_FontAsset font)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(400f, 65f);
        rt.anchoredPosition = pos;
        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.25f);
        var field = go.AddComponent<TMP_InputField>();

        var areaGO = new GameObject("Text Area");
        areaGO.transform.SetParent(go.transform, false);
        var areaRT = areaGO.AddComponent<RectTransform>();
        areaRT.anchorMin = Vector2.zero; areaRT.anchorMax = Vector2.one;
        areaRT.offsetMin = new Vector2(10f, 4f); areaRT.offsetMax = new Vector2(-10f, -4f);
        areaGO.AddComponent<RectMask2D>();

        var phGO = new GameObject("Placeholder");
        phGO.transform.SetParent(areaGO.transform, false);
        var phRT = phGO.AddComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one; phRT.sizeDelta = Vector2.zero;
        var phText = phGO.AddComponent<TextMeshProUGUI>();
        phText.text = placeholder; phText.fontSize = 24;
        phText.color = new Color(1,1,1,0.4f); phText.fontStyle = FontStyles.Italic;
        phText.alignment = TextAlignmentOptions.MidlineLeft;
        if (font != null) phText.font = font;

        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(areaGO.transform, false);
        var txtRT = txtGO.AddComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one; txtRT.sizeDelta = Vector2.zero;
        var tmpText = txtGO.AddComponent<TextMeshProUGUI>();
        tmpText.fontSize = 24; tmpText.color = Color.white;
        tmpText.alignment = TextAlignmentOptions.MidlineLeft;
        if (font != null) tmpText.font = font;

        field.textViewport  = areaRT;
        field.textComponent = tmpText;
        field.placeholder   = phText;
        return go;
    }
}
