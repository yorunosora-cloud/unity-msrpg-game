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

        // ── SkillResearchPanel (K키, 스킬 연구 전용) ────────────────────
        var skillPanel = CreateCentrePanel(canvasGO.transform, "SkillResearchPanel", new Vector2(1000f, 950f));
        skillPanel.SetActive(false);

        // 제목
        CreateLabel(skillPanel.transform, "Title", "스킬 연구  [K]", 40, new Vector2(0f, 420f), font);

        // ── 좌측 캐릭터 목록 (스크롤 뷰) ──────────────────────────────
        var charScrollGO = new GameObject("CharScrollView");
        charScrollGO.transform.SetParent(skillPanel.transform, false);
        var charScrollRt = charScrollGO.AddComponent<RectTransform>();
        charScrollRt.anchorMin = new Vector2(0f, 0f);
        charScrollRt.anchorMax = new Vector2(0f, 1f);
        charScrollRt.offsetMin = new Vector2(20f,  50f);
        charScrollRt.offsetMax = new Vector2(320f, -60f);
        charScrollGO.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.8f);
        var charSv = charScrollGO.AddComponent<ScrollRect>();
        charSv.horizontal = false;

        var charViewport = new GameObject("Viewport");
        charViewport.transform.SetParent(charScrollGO.transform, false);
        var charVpRt = charViewport.AddComponent<RectTransform>();
        charVpRt.anchorMin = Vector2.zero; charVpRt.anchorMax = Vector2.one;
        charVpRt.offsetMin = Vector2.zero; charVpRt.offsetMax = Vector2.zero;
        charViewport.AddComponent<RectMask2D>();

        var charContent = new GameObject("Content");
        charContent.transform.SetParent(charViewport.transform, false);
        var charContentRt = charContent.AddComponent<RectTransform>();
        charContentRt.anchorMin = new Vector2(0f, 1f);
        charContentRt.anchorMax = new Vector2(1f, 1f);
        charContentRt.pivot     = new Vector2(0.5f, 1f);
        charContentRt.sizeDelta = new Vector2(0f, 0f);
        var charLayout = charContent.AddComponent<VerticalLayoutGroup>();
        charLayout.childForceExpandWidth  = true;
        charLayout.childForceExpandHeight = false;
        charLayout.spacing = 4f;
        charLayout.padding = new RectOffset(4, 4, 4, 4);
        charContent.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        charSv.viewport = charVpRt;
        charSv.content  = charContentRt;

        // ── 중앙 스킬 목록 ─────────────────────────────────────────────
        var skillScrollGO = new GameObject("SkillScrollView");
        skillScrollGO.transform.SetParent(skillPanel.transform, false);
        var skillScrollRt = skillScrollGO.AddComponent<RectTransform>();
        skillScrollRt.anchorMin = new Vector2(0f, 0f);
        skillScrollRt.anchorMax = new Vector2(0f, 1f);
        skillScrollRt.offsetMin = new Vector2(330f,  50f);
        skillScrollRt.offsetMax = new Vector2(640f, -60f);
        skillScrollGO.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.8f);
        var skillSv = skillScrollGO.AddComponent<ScrollRect>();
        skillSv.horizontal = false;

        var skillViewport = new GameObject("Viewport");
        skillViewport.transform.SetParent(skillScrollGO.transform, false);
        var skillVpRt = skillViewport.AddComponent<RectTransform>();
        skillVpRt.anchorMin = Vector2.zero; skillVpRt.anchorMax = Vector2.one;
        skillVpRt.offsetMin = Vector2.zero; skillVpRt.offsetMax = Vector2.zero;
        skillViewport.AddComponent<RectMask2D>();

        var skillContent = new GameObject("Content");
        skillContent.transform.SetParent(skillViewport.transform, false);
        var skillContentRt = skillContent.AddComponent<RectTransform>();
        skillContentRt.anchorMin = new Vector2(0f, 1f);
        skillContentRt.anchorMax = new Vector2(1f, 1f);
        skillContentRt.pivot     = new Vector2(0.5f, 1f);
        skillContentRt.sizeDelta = new Vector2(0f, 0f);
        var skillLayout = skillContent.AddComponent<VerticalLayoutGroup>();
        skillLayout.childForceExpandWidth  = true;
        skillLayout.childForceExpandHeight = false;
        skillLayout.spacing = 4f;
        skillLayout.padding = new RectOffset(4, 4, 4, 4);
        skillContent.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        skillSv.viewport = skillVpRt;
        skillSv.content  = skillContentRt;

        // ── 우측 문제 영역 ─────────────────────────────────────────────
        var problemArea = new GameObject("ProblemArea");
        problemArea.transform.SetParent(skillPanel.transform, false);
        var paRt = problemArea.AddComponent<RectTransform>();
        paRt.anchorMin = new Vector2(0f, 0f);
        paRt.anchorMax = new Vector2(0f, 1f);
        paRt.offsetMin = new Vector2(650f,  50f);
        paRt.offsetMax = new Vector2(980f, -60f);
        problemArea.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.8f);

        // 문제 텍스트
        var promptGO = new GameObject("PromptText");
        promptGO.transform.SetParent(problemArea.transform, false);
        var promptRt = promptGO.AddComponent<RectTransform>();
        promptRt.anchorMin        = new Vector2(0f, 0.65f);
        promptRt.anchorMax        = new Vector2(1f, 1f);
        promptRt.offsetMin        = new Vector2(12f, 0f);
        promptRt.offsetMax        = new Vector2(-12f, -8f);
        var promptText = promptGO.AddComponent<TextMeshProUGUI>();
        promptText.text      = "스킬을 선택하면 문제가 나타납니다.";
        promptText.fontSize  = 22;
        promptText.color     = Color.white;
        promptText.alignment = TextAlignmentOptions.TopLeft;
        if (font != null) promptText.font = font;

        // 객관식 영역
        var mcArea = new GameObject("MultipleChoiceArea");
        mcArea.transform.SetParent(problemArea.transform, false);
        var mcRt = mcArea.AddComponent<RectTransform>();
        mcRt.anchorMin = new Vector2(0f, 0.10f);
        mcRt.anchorMax = new Vector2(1f, 0.65f);
        mcRt.offsetMin = new Vector2(8f, 0f);
        mcRt.offsetMax = new Vector2(-8f, 0f);

        var choiceButtons = new Button[4];
        var choiceLabels  = new TMP_Text[4];
        for (int i = 0; i < 4; i++)
        {
            float yAnchorMin = 1f - (i + 1) * 0.25f;
            float yAnchorMax = 1f - i * 0.25f;
            var cbGO = new GameObject($"Choice{i}");
            cbGO.transform.SetParent(mcArea.transform, false);
            var cbRt = cbGO.AddComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(0f, yAnchorMin);
            cbRt.anchorMax = new Vector2(1f, yAnchorMax);
            cbRt.offsetMin = new Vector2(0f, 2f);
            cbRt.offsetMax = new Vector2(0f, -2f);
            cbGO.AddComponent<Image>().color = new Color(0.2f, 0.35f, 0.6f, 0.9f);
            choiceButtons[i] = cbGO.AddComponent<Button>();

            var cbTextGO = new GameObject("Label");
            cbTextGO.transform.SetParent(cbGO.transform, false);
            var cbTextRt = cbTextGO.AddComponent<RectTransform>();
            cbTextRt.anchorMin = Vector2.zero; cbTextRt.anchorMax = Vector2.one;
            cbTextRt.offsetMin = new Vector2(10f, 0f); cbTextRt.offsetMax = new Vector2(-6f, 0f);
            var cbText = cbTextGO.AddComponent<TextMeshProUGUI>();
            cbText.text      = $"보기 {i + 1}";
            cbText.fontSize  = 20;
            cbText.color     = Color.white;
            cbText.alignment = TextAlignmentOptions.MidlineLeft;
            if (font != null) cbText.font = font;
            choiceLabels[i] = cbText;
        }

        // 주관식 영역
        var fiArea = new GameObject("FreeInputArea");
        fiArea.transform.SetParent(problemArea.transform, false);
        var fiRt = fiArea.AddComponent<RectTransform>();
        fiRt.anchorMin = new Vector2(0f, 0.10f);
        fiRt.anchorMax = new Vector2(1f, 0.55f);
        fiRt.offsetMin = new Vector2(8f, 0f);
        fiRt.offsetMax = new Vector2(-8f, 0f);

        var answerInputGO = CreateInput(fiArea.transform, "AnswerInput", "답 입력...", new Vector2(0f, 60f), font);
        ((RectTransform)answerInputGO.transform).anchorMin = new Vector2(0f, 0.55f);
        ((RectTransform)answerInputGO.transform).anchorMax = new Vector2(1f, 0.85f);
        ((RectTransform)answerInputGO.transform).offsetMin = Vector2.zero;
        ((RectTransform)answerInputGO.transform).offsetMax = Vector2.zero;

        var submitBtnGO = CreateButton(fiArea.transform, "SubmitBtn", "제출", new Vector2(0f, 0f), font, 26);
        ((RectTransform)submitBtnGO.transform).anchorMin = new Vector2(0f, 0.10f);
        ((RectTransform)submitBtnGO.transform).anchorMax = new Vector2(1f, 0.45f);
        ((RectTransform)submitBtnGO.transform).offsetMin = Vector2.zero;
        ((RectTransform)submitBtnGO.transform).offsetMax = Vector2.zero;

        fiArea.SetActive(false);

        // 피드백 텍스트
        var fbGO = new GameObject("FeedbackText");
        fbGO.transform.SetParent(problemArea.transform, false);
        var fbRt = fbGO.AddComponent<RectTransform>();
        fbRt.anchorMin = new Vector2(0f, 0.02f);
        fbRt.anchorMax = new Vector2(1f, 0.12f);
        fbRt.offsetMin = new Vector2(8f, 0f); fbRt.offsetMax = new Vector2(-8f, 0f);
        var fbText = fbGO.AddComponent<TextMeshProUGUI>();
        fbText.fontSize  = 20; fbText.color = Color.white; fbText.alignment = TextAlignmentOptions.Center;
        if (font != null) fbText.font = font;
        fbGO.SetActive(false);

        // 정답 해설 텍스트
        var exGO = new GameObject("ExplanationText");
        exGO.transform.SetParent(problemArea.transform, false);
        var exRt = exGO.AddComponent<RectTransform>();
        exRt.anchorMin = new Vector2(0f, 0.55f);
        exRt.anchorMax = new Vector2(1f, 0.65f);
        exRt.offsetMin = new Vector2(8f, 0f); exRt.offsetMax = new Vector2(-8f, 0f);
        var exText = exGO.AddComponent<TextMeshProUGUI>();
        exText.fontSize  = 19; exText.color = new Color(0.8f, 1f, 0.8f); exText.alignment = TextAlignmentOptions.TopLeft;
        if (font != null) exText.font = font;
        exGO.SetActive(false);

        problemArea.SetActive(false);

        // 닫기 버튼
        var closeBtnK = CreateButton(skillPanel.transform, "CloseBtn", "닫기  [K]",
                                     new Vector2(0f, -440f), font, 28, 0.4f);
        skillPanel.SetActive(false);

        // SkillResearchPanel 컴포넌트 연결
        var srp   = skillPanel.AddComponent<SkillResearchPanel>();
        var srpSo = new SerializedObject(srp);
        srpSo.FindProperty("charListContent").objectReferenceValue     = charContentRt;
        srpSo.FindProperty("skillListContent").objectReferenceValue    = skillContentRt;
        srpSo.FindProperty("problemArea").objectReferenceValue         = problemArea;
        srpSo.FindProperty("promptText").objectReferenceValue          = promptText;
        srpSo.FindProperty("multipleChoiceArea").objectReferenceValue  = mcArea;
        srpSo.FindProperty("freeInputArea").objectReferenceValue       = fiArea;
        srpSo.FindProperty("answerInput").objectReferenceValue         = answerInputGO.GetComponent<TMP_InputField>();
        srpSo.FindProperty("submitButton").objectReferenceValue        = submitBtnGO.GetComponent<Button>();
        srpSo.FindProperty("feedbackText").objectReferenceValue        = fbText;
        srpSo.FindProperty("explanationText").objectReferenceValue     = exText;
        srpSo.FindProperty("closeButton").objectReferenceValue         = closeBtnK.GetComponent<Button>();

        // choiceButtons / choiceLabels 배열 주입
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

        // ── MetaPanelController 참조 연결 ───────────────────────────────
        var ctrlSo = new SerializedObject(controller);
        ctrlSo.FindProperty("collectionPanel").objectReferenceValue      = collPanel;
        ctrlSo.FindProperty("inventoryPanel").objectReferenceValue       = invPanel;
        ctrlSo.FindProperty("skillResearchPanel").objectReferenceValue   = skillPanel;
        ctrlSo.FindProperty("adminPanel").objectReferenceValue           = adminPanel;
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
