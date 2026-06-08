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

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_SDF_PATH);

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
        hudRt.anchorMin       = new Vector2(0f, 1f);   // 상단 전체폭 앵커
        hudRt.anchorMax       = new Vector2(1f, 1f);
        hudRt.pivot           = new Vector2(0.5f, 1f);
        hudRt.sizeDelta       = new Vector2(0f, 70f);  // 너비는 화면에 맞춤
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

        var roll1Btn  = CreateButton(adminPanel.transform, "Roll1Btn",  "가챠 1회 (무료)", new Vector2(-160f, -150f), font, 26);
        var roll10Btn = CreateButton(adminPanel.transform, "Roll10Btn", "10연 (무료)",      new Vector2( 160f, -150f), font, 26);
        ((RectTransform)roll1Btn.transform).sizeDelta  = new Vector2(310f, 65f);
        ((RectTransform)roll10Btn.transform).sizeDelta = new Vector2(310f, 65f);

        var resetBtn = CreateButton(adminPanel.transform, "ResetBtn", "계정 초기화", new Vector2(-200f, -240f), font, 24, 0.5f);
        var saveBtn  = CreateButton(adminPanel.transform, "SaveBtn",  "강제 저장",   new Vector2(   0f, -240f), font, 24);
        var loadBtn  = CreateButton(adminPanel.transform, "LoadBtn",  "불러오기",    new Vector2( 200f, -240f), font, 24);
        ((RectTransform)resetBtn.transform).sizeDelta = new Vector2(250f, 60f);
        ((RectTransform)saveBtn.transform).sizeDelta  = new Vector2(220f, 60f);
        ((RectTransform)loadBtn.transform).sizeDelta  = new Vector2(200f, 60f);

        var statusAdmin = CreateLabel(adminPanel.transform, "Status", "", 24, new Vector2(0f, -340f), font);
        var closeBtnA   = CreateButton(adminPanel.transform, "CloseBtn", "닫기  [F1]", new Vector2(0f, -430f), font, 28, 0.4f);

        var adminCmp = adminPanel.AddComponent<AdminPanel>();
        var aSo = new SerializedObject(adminCmp);
        aSo.FindProperty("addGoldButton").objectReferenceValue       = addGoldBtn.GetComponent<Button>();
        aSo.FindProperty("addPaperButton").objectReferenceValue      = addPaperBtn.GetComponent<Button>();
        aSo.FindProperty("addFocusButton").objectReferenceValue      = addFocusBtn.GetComponent<Button>();
        aSo.FindProperty("addFragmentButton").objectReferenceValue   = addFragmentBtn.GetComponent<Button>();
        aSo.FindProperty("giveCrystalsButton").objectReferenceValue  = giveCrystalsBtn.GetComponent<Button>();
        aSo.FindProperty("characterIdInput").objectReferenceValue    = charInput.GetComponent<TMP_InputField>();
        aSo.FindProperty("giveCharacterButton").objectReferenceValue = giveBtn.GetComponent<Button>();
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
        UnityEventTools.AddVoidPersistentListener(giveBtn.GetComponent<Button>().onClick,         adminCmp.OnGiveCharacter);
        UnityEventTools.AddVoidPersistentListener(roll1Btn.GetComponent<Button>().onClick,        adminCmp.OnDebugRollOne);
        UnityEventTools.AddVoidPersistentListener(roll10Btn.GetComponent<Button>().onClick,       adminCmp.OnDebugRollTen);
        UnityEventTools.AddVoidPersistentListener(resetBtn.GetComponent<Button>().onClick,        adminCmp.OnResetAccount);
        UnityEventTools.AddVoidPersistentListener(saveBtn.GetComponent<Button>().onClick,         adminCmp.OnForceSave);
        UnityEventTools.AddVoidPersistentListener(loadBtn.GetComponent<Button>().onClick,         adminCmp.OnForceLoad);
        UnityEventTools.AddVoidPersistentListener(closeBtnA.GetComponent<Button>().onClick,       adminCmp.OnCloseClicked);

        // ── RnEPanel (K키, R&E — 캐릭터 육성) ──────────────────────────
        // 레이아웃: 좌(520px 캐릭터 그리드, 행당 3칸) | 우(460px 탭+콘텐츠)
        var skillPanel = CreateCentrePanel(canvasGO.transform, "RnEPanel", new Vector2(1000f, 950f));
        skillPanel.SetActive(false);

        // 제목
        CreateLabel(skillPanel.transform, "Title", "R&E  [K]", 40, new Vector2(0f, 420f), font);

        // ── 좌측: 캐릭터 그리드 스크롤뷰 (520px, GridLayoutGroup 3열) ──
        var charSvGO = new GameObject("CharScrollView");
        charSvGO.transform.SetParent(skillPanel.transform, false);
        var charSvRt = charSvGO.AddComponent<RectTransform>();
        // anchorMin=(0,0) anchorMax=(0,1) → 패널 좌측에 고정, offsetMin/Max로 위치/폭 지정
        charSvRt.anchorMin = new Vector2(0f, 0f);
        charSvRt.anchorMax = new Vector2(0f, 1f);
        charSvRt.offsetMin = new Vector2(10f,  55f);   // 좌:10, 하:55
        charSvRt.offsetMax = new Vector2(530f, -65f);  // 우:530(=520px 폭), 상:-65
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
        charContentRt.sizeDelta = new Vector2(0f, 0f);
        // GridLayoutGroup: 3열, 각 카드 160×160
        var gridLayout = charContentGO.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize        = new Vector2(160f, 160f);
        gridLayout.spacing         = new Vector2(8f, 8f);
        gridLayout.padding         = new RectOffset(8, 8, 8, 8);
        gridLayout.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 3;
        gridLayout.startAxis       = GridLayoutGroup.Axis.Horizontal;
        charContentGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        charSv.viewport = charVpRt;
        charSv.content  = charContentRt;

        // ── 우측: 탭+콘텐츠 영역 컨테이너 (캐릭터 선택 전 숨김) ──────
        var tabAreaGO = new GameObject("TabArea");
        tabAreaGO.transform.SetParent(skillPanel.transform, false);
        var tabAreaRt = tabAreaGO.AddComponent<RectTransform>();
        tabAreaRt.anchorMin = new Vector2(0f, 0f);
        tabAreaRt.anchorMax = new Vector2(0f, 1f);
        tabAreaRt.offsetMin = new Vector2(540f,  55f);
        tabAreaRt.offsetMax = new Vector2(990f, -65f);
        tabAreaGO.SetActive(false);  // 캐릭터 선택 전 숨김

        // 탭 버튼 2개 (tabArea 자식)
        var levelUpTabGO = new GameObject("LevelUpTabBtn");
        levelUpTabGO.transform.SetParent(tabAreaGO.transform, false);
        var levelUpTabRt = levelUpTabGO.AddComponent<RectTransform>();
        levelUpTabRt.anchorMin        = new Vector2(0f, 1f);
        levelUpTabRt.anchorMax        = new Vector2(0.5f, 1f);
        levelUpTabRt.pivot            = new Vector2(0.5f, 1f);
        levelUpTabRt.sizeDelta        = new Vector2(-4f, 55f);
        levelUpTabRt.anchoredPosition = new Vector2(0f, 0f);
        var levelUpTabBg = levelUpTabGO.AddComponent<Image>();
        levelUpTabBg.color = new Color(0.25f, 0.5f, 1f, 1f);
        var levelUpTabBtn = levelUpTabGO.AddComponent<Button>();
        var luTabTxtGO = new GameObject("Text");
        luTabTxtGO.transform.SetParent(levelUpTabGO.transform, false);
        var luTabTxtRt = luTabTxtGO.AddComponent<RectTransform>();
        luTabTxtRt.anchorMin = Vector2.zero; luTabTxtRt.anchorMax = Vector2.one; luTabTxtRt.sizeDelta = Vector2.zero;
        var luTabLbl = luTabTxtGO.AddComponent<TextMeshProUGUI>();
        luTabLbl.text = "[레벨업]"; luTabLbl.fontSize = 24; luTabLbl.color = Color.white;
        luTabLbl.alignment = TextAlignmentOptions.Center;
        if (font != null) luTabLbl.font = font;

        var skillTabGO = new GameObject("SkillTabBtn");
        skillTabGO.transform.SetParent(tabAreaGO.transform, false);
        var skillTabRt = skillTabGO.AddComponent<RectTransform>();
        skillTabRt.anchorMin        = new Vector2(0.5f, 1f);
        skillTabRt.anchorMax        = new Vector2(1f,   1f);
        skillTabRt.pivot            = new Vector2(0.5f, 1f);
        skillTabRt.sizeDelta        = new Vector2(-4f, 55f);
        skillTabRt.anchoredPosition = new Vector2(0f, 0f);
        var skillTabBg = skillTabGO.AddComponent<Image>();
        skillTabBg.color = new Color(0.15f, 0.18f, 0.28f, 1f);
        var skillTabBtn = skillTabGO.AddComponent<Button>();
        var skTabTxtGO = new GameObject("Text");
        skTabTxtGO.transform.SetParent(skillTabGO.transform, false);
        var skTabTxtRt = skTabTxtGO.AddComponent<RectTransform>();
        skTabTxtRt.anchorMin = Vector2.zero; skTabTxtRt.anchorMax = Vector2.one; skTabTxtRt.sizeDelta = Vector2.zero;
        var skTabLbl = skTabTxtGO.AddComponent<TextMeshProUGUI>();
        skTabLbl.text = "[스킬 연구]"; skTabLbl.fontSize = 24; skTabLbl.color = Color.white;
        skTabLbl.alignment = TextAlignmentOptions.Center;
        if (font != null) skTabLbl.font = font;

        // ── 레벨업 탭 콘텐츠 (tabArea 자식, 기본 active) ─────────────
        var levelUpArea = new GameObject("LevelUpArea");
        levelUpArea.transform.SetParent(tabAreaGO.transform, false);
        var luaRt = levelUpArea.AddComponent<RectTransform>();
        luaRt.anchorMin = new Vector2(0f, 0f);
        luaRt.anchorMax = new Vector2(1f, 1f);
        luaRt.offsetMin = new Vector2(0f, 0f);
        luaRt.offsetMax = new Vector2(0f, -60f);  // 탭 버튼(55px) 아래부터
        levelUpArea.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.8f);

        // 레벨/EXP 정보 텍스트
        var levelInfoGO = new GameObject("LevelInfoText");
        levelInfoGO.transform.SetParent(levelUpArea.transform, false);
        var levelInfoRt = levelInfoGO.AddComponent<RectTransform>();
        levelInfoRt.anchorMin        = new Vector2(0f, 1f);
        levelInfoRt.anchorMax        = new Vector2(1f, 1f);
        levelInfoRt.pivot            = new Vector2(0.5f, 1f);
        levelInfoRt.sizeDelta        = new Vector2(0f, 55f);
        levelInfoRt.anchoredPosition = new Vector2(0f, -8f);
        var levelInfoText = levelInfoGO.AddComponent<TextMeshProUGUI>();
        levelInfoText.text      = "레벨 정보";
        levelInfoText.fontSize  = 22;
        levelInfoText.color     = Color.white;
        levelInfoText.alignment = TextAlignmentOptions.Center;
        if (font != null) levelInfoText.font = font;

        // 난이도 버튼 3개
        var diffLowBtn  = CreateButton(levelUpArea.transform, "DiffLowBtn",  "하  (무료)", new Vector2(0f, 190f), font, 24);
        var diffMidBtn  = CreateButton(levelUpArea.transform, "DiffMidBtn",  "중  (무료)", new Vector2(0f, 110f), font, 24);
        var diffHighBtn = CreateButton(levelUpArea.transform, "DiffHighBtn", "상  (무료)", new Vector2(0f,  30f), font, 24);
        ((RectTransform)diffLowBtn.transform).sizeDelta  = new Vector2(380f, 62f);
        ((RectTransform)diffMidBtn.transform).sizeDelta  = new Vector2(380f, 62f);
        ((RectTransform)diffHighBtn.transform).sizeDelta = new Vector2(380f, 62f);

        // ── 스킬 연구 탭 콘텐츠 (tabArea 자식, 기본 inactive) ─────────
        var skillArea = new GameObject("SkillArea");
        skillArea.transform.SetParent(tabAreaGO.transform, false);
        var saRt = skillArea.AddComponent<RectTransform>();
        saRt.anchorMin = new Vector2(0f, 0f);
        saRt.anchorMax = new Vector2(1f, 1f);
        saRt.offsetMin = new Vector2(0f, 0f);
        saRt.offsetMax = new Vector2(0f, -60f);
        skillArea.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.8f);

        var skillSvGO = new GameObject("SkillScrollView");
        skillSvGO.transform.SetParent(skillArea.transform, false);
        var skillSvRt = skillSvGO.AddComponent<RectTransform>();
        skillSvRt.anchorMin = Vector2.zero; skillSvRt.anchorMax = new Vector2(1f, 0.55f);
        skillSvRt.offsetMin = Vector2.zero; skillSvRt.offsetMax = Vector2.zero;
        skillSvGO.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        var skillSv2 = skillSvGO.AddComponent<ScrollRect>();
        skillSv2.horizontal = false;

        var skillVpGO = new GameObject("Viewport");
        skillVpGO.transform.SetParent(skillSvGO.transform, false);
        var skillVpRt = skillVpGO.AddComponent<RectTransform>();
        skillVpRt.anchorMin = Vector2.zero; skillVpRt.anchorMax = Vector2.one;
        skillVpRt.offsetMin = Vector2.zero; skillVpRt.offsetMax = Vector2.zero;
        skillVpGO.AddComponent<RectMask2D>();

        var skillContentGO = new GameObject("Content");
        skillContentGO.transform.SetParent(skillVpGO.transform, false);
        var skillContentRt = skillContentGO.AddComponent<RectTransform>();
        skillContentRt.anchorMin = new Vector2(0f, 1f);
        skillContentRt.anchorMax = new Vector2(1f, 1f);
        skillContentRt.pivot     = new Vector2(0.5f, 1f);
        skillContentRt.sizeDelta = new Vector2(0f, 0f);
        var skillLayout = skillContentGO.AddComponent<VerticalLayoutGroup>();
        skillLayout.childForceExpandWidth = true; skillLayout.childForceExpandHeight = false;
        skillLayout.spacing = 4f; skillLayout.padding = new RectOffset(4, 4, 4, 4);
        skillContentGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        skillSv2.viewport = skillVpRt; skillSv2.content = skillContentRt;

        skillArea.SetActive(false);

        // ── 문제 영역 (tabArea 자식, 하단 45%) ────────────────────────
        var problemArea = new GameObject("ProblemArea");
        problemArea.transform.SetParent(tabAreaGO.transform, false);
        var paRt = problemArea.AddComponent<RectTransform>();
        paRt.anchorMin = new Vector2(0f, 0f);
        paRt.anchorMax = new Vector2(1f, 0.45f);
        paRt.offsetMin = new Vector2(0f, 0f);
        paRt.offsetMax = new Vector2(0f, 0f);
        problemArea.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.8f);

        var promptGO = new GameObject("PromptText");
        promptGO.transform.SetParent(problemArea.transform, false);
        var promptRt = promptGO.AddComponent<RectTransform>();
        promptRt.anchorMin = new Vector2(0f, 0.60f); promptRt.anchorMax = new Vector2(1f, 1f);
        promptRt.offsetMin = new Vector2(12f, 0f); promptRt.offsetMax = new Vector2(-12f, -8f);
        var promptText = promptGO.AddComponent<TextMeshProUGUI>();
        promptText.text = ""; promptText.fontSize = 20; promptText.color = Color.white;
        promptText.alignment = TextAlignmentOptions.TopLeft;
        if (font != null) promptText.font = font;

        var mcArea = new GameObject("MultipleChoiceArea");
        mcArea.transform.SetParent(problemArea.transform, false);
        var mcRt = mcArea.AddComponent<RectTransform>();
        mcRt.anchorMin = new Vector2(0f, 0.08f); mcRt.anchorMax = new Vector2(1f, 0.60f);
        mcRt.offsetMin = new Vector2(8f, 0f); mcRt.offsetMax = new Vector2(-8f, 0f);

        var choiceButtons = new Button[4];
        var choiceLabels  = new TMP_Text[4];
        for (int i = 0; i < 4; i++)
        {
            float yMin = 1f - (i + 1) * 0.25f;
            float yMax = 1f - i * 0.25f;
            var cbGO = new GameObject($"Choice{i}");
            cbGO.transform.SetParent(mcArea.transform, false);
            var cbRt = cbGO.AddComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(0f, yMin); cbRt.anchorMax = new Vector2(1f, yMax);
            cbRt.offsetMin = new Vector2(0f, 2f); cbRt.offsetMax = new Vector2(0f, -2f);
            cbGO.AddComponent<Image>().color = new Color(0.2f, 0.35f, 0.6f, 0.9f);
            choiceButtons[i] = cbGO.AddComponent<Button>();
            var cbTxtGO = new GameObject("Label");
            cbTxtGO.transform.SetParent(cbGO.transform, false);
            var cbTxtRt = cbTxtGO.AddComponent<RectTransform>();
            cbTxtRt.anchorMin = Vector2.zero; cbTxtRt.anchorMax = Vector2.one;
            cbTxtRt.offsetMin = new Vector2(10f, 0f); cbTxtRt.offsetMax = new Vector2(-6f, 0f);
            var cbTxt = cbTxtGO.AddComponent<TextMeshProUGUI>();
            cbTxt.text = $"보기 {i + 1}"; cbTxt.fontSize = 19; cbTxt.color = Color.white;
            cbTxt.alignment = TextAlignmentOptions.MidlineLeft;
            if (font != null) cbTxt.font = font;
            choiceLabels[i] = cbTxt;
        }

        var fiArea = new GameObject("FreeInputArea");
        fiArea.transform.SetParent(problemArea.transform, false);
        var fiRt = fiArea.AddComponent<RectTransform>();
        fiRt.anchorMin = new Vector2(0f, 0.08f); fiRt.anchorMax = new Vector2(1f, 0.56f);
        fiRt.offsetMin = new Vector2(8f, 0f); fiRt.offsetMax = new Vector2(-8f, 0f);

        var answerInputGO = CreateInput(fiArea.transform, "AnswerInput", "답 입력...", Vector2.zero, font);
        ((RectTransform)answerInputGO.transform).anchorMin = new Vector2(0f, 0.55f);
        ((RectTransform)answerInputGO.transform).anchorMax = new Vector2(1f, 0.88f);
        ((RectTransform)answerInputGO.transform).offsetMin = Vector2.zero;
        ((RectTransform)answerInputGO.transform).offsetMax = Vector2.zero;

        var submitBtnGO = CreateButton(fiArea.transform, "SubmitBtn", "제출", Vector2.zero, font, 26);
        ((RectTransform)submitBtnGO.transform).anchorMin = new Vector2(0f, 0.10f);
        ((RectTransform)submitBtnGO.transform).anchorMax = new Vector2(1f, 0.46f);
        ((RectTransform)submitBtnGO.transform).offsetMin = Vector2.zero;
        ((RectTransform)submitBtnGO.transform).offsetMax = Vector2.zero;

        fiArea.SetActive(false);

        var fbGO = new GameObject("FeedbackText");
        fbGO.transform.SetParent(problemArea.transform, false);
        var fbRt = fbGO.AddComponent<RectTransform>();
        fbRt.anchorMin = new Vector2(0f, 0.01f); fbRt.anchorMax = new Vector2(1f, 0.10f);
        fbRt.offsetMin = new Vector2(8f, 0f); fbRt.offsetMax = new Vector2(-8f, 0f);
        var fbText = fbGO.AddComponent<TextMeshProUGUI>();
        fbText.fontSize = 20; fbText.color = Color.white; fbText.alignment = TextAlignmentOptions.Center;
        if (font != null) fbText.font = font;
        fbGO.SetActive(false);

        var exGO = new GameObject("ExplanationText");
        exGO.transform.SetParent(problemArea.transform, false);
        var exRt = exGO.AddComponent<RectTransform>();
        exRt.anchorMin = new Vector2(0f, 0.56f); exRt.anchorMax = new Vector2(1f, 0.62f);
        exRt.offsetMin = new Vector2(8f, 0f); exRt.offsetMax = new Vector2(-8f, 0f);
        var exText = exGO.AddComponent<TextMeshProUGUI>();
        exText.fontSize = 18; exText.color = new Color(0.8f, 1f, 0.8f);
        exText.alignment = TextAlignmentOptions.TopLeft;
        if (font != null) exText.font = font;
        exGO.SetActive(false);

        problemArea.SetActive(false);

        // 닫기 버튼
        var closeBtnK = CreateButton(skillPanel.transform, "CloseBtn", "닫기  [K]",
                                     new Vector2(0f, -440f), font, 28, 0.4f);
        skillPanel.SetActive(false);

        // ── RnEPanel 컴포넌트 연결 ────────────────────────────────────
        var srp   = skillPanel.AddComponent<RnEPanel>();
        var srpSo = new SerializedObject(srp);
        srpSo.FindProperty("charGridContent").objectReferenceValue    = charContentRt;
        srpSo.FindProperty("tabArea").objectReferenceValue            = tabAreaGO;
        srpSo.FindProperty("levelUpTabBtn").objectReferenceValue      = levelUpTabBtn;
        srpSo.FindProperty("skillTabBtn").objectReferenceValue        = skillTabBtn;
        srpSo.FindProperty("levelUpTabBg").objectReferenceValue       = levelUpTabBg;
        srpSo.FindProperty("skillTabBg").objectReferenceValue         = skillTabBg;
        srpSo.FindProperty("levelUpArea").objectReferenceValue        = levelUpArea;
        srpSo.FindProperty("levelInfoText").objectReferenceValue      = levelInfoText;
        srpSo.FindProperty("diffLowBtn").objectReferenceValue         = diffLowBtn.GetComponent<Button>();
        srpSo.FindProperty("diffMidBtn").objectReferenceValue         = diffMidBtn.GetComponent<Button>();
        srpSo.FindProperty("diffHighBtn").objectReferenceValue        = diffHighBtn.GetComponent<Button>();
        srpSo.FindProperty("skillArea").objectReferenceValue          = skillArea;
        srpSo.FindProperty("skillListContent").objectReferenceValue   = skillContentRt;
        srpSo.FindProperty("problemArea").objectReferenceValue        = problemArea;
        srpSo.FindProperty("promptText").objectReferenceValue         = promptText;
        srpSo.FindProperty("multipleChoiceArea").objectReferenceValue = mcArea;
        srpSo.FindProperty("freeInputArea").objectReferenceValue      = fiArea;
        srpSo.FindProperty("answerInput").objectReferenceValue        = answerInputGO.GetComponent<TMP_InputField>();
        srpSo.FindProperty("submitButton").objectReferenceValue       = submitBtnGO.GetComponent<Button>();
        srpSo.FindProperty("feedbackText").objectReferenceValue       = fbText;
        srpSo.FindProperty("explanationText").objectReferenceValue    = exText;
        srpSo.FindProperty("closeButton").objectReferenceValue        = closeBtnK.GetComponent<Button>();

        var cbArr = srpSo.FindProperty("choiceButtons");
        cbArr.arraySize = 4;
        for (int i = 0; i < 4; i++)
            cbArr.GetArrayElementAtIndex(i).objectReferenceValue = choiceButtons[i];
        var clArr = srpSo.FindProperty("choiceLabels");
        clArr.arraySize = 4;
        for (int i = 0; i < 4; i++)
            clArr.GetArrayElementAtIndex(i).objectReferenceValue = choiceLabels[i];
        srpSo.ApplyModifiedProperties();

        UnityEventTools.AddVoidPersistentListener(closeBtnK.GetComponent<Button>().onClick, srp.OnCloseClicked);

        // ── MetaPanelController 참조 연결 ────────────────────────────
        var ctrlSo = new SerializedObject(controller);
        ctrlSo.FindProperty("collectionPanel").objectReferenceValue = collPanel;
        ctrlSo.FindProperty("inventoryPanel").objectReferenceValue  = invPanel;
        ctrlSo.FindProperty("rnePanel").objectReferenceValue        = skillPanel;
        ctrlSo.FindProperty("adminPanel").objectReferenceValue      = adminPanel;
        ctrlSo.ApplyModifiedProperties();

        // ── 씬 저장 ─────────────────────────────────────────────────────
        EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene(), SCENE_PATH);
        AssetDatabase.Refresh();

        Debug.Log("[MSRPG] ✅ Meta UI 설정 완료! Mesoria 씬에 MetaCanvas가 추가됐습니다.");
    }

    // ── UI 생성 헬퍼 ──────────────────────────────────────────────────────

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
