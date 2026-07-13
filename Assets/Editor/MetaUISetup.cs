using System.Collections.Generic;
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
        hudBg.color = new Color(UITheme.PanelBgDark.r, UITheme.PanelBgDark.g,
                                UITheme.PanelBgDark.b, 0.88f);
        var hud    = hudGO.AddComponent<CurrencyHud>();

        var goldText     = UIKit.Label(hudGO.transform, "GoldText",     "골드 5,000",
            UIKit.TextLevel.Body, new Vector2(-380f, 0f));
        var paperText    = UIKit.Label(hudGO.transform, "PaperText",    "논문 30",
            UIKit.TextLevel.Body, new Vector2(-120f, 0f));
        var focusText    = UIKit.Label(hudGO.transform, "FocusText",    "집중 120",
            UIKit.TextLevel.Body, new Vector2( 120f, 0f));
        var fragmentText = UIKit.Label(hudGO.transform, "FragmentText", "조각 10",
            UIKit.TextLevel.Body, new Vector2( 380f, 0f));

        var hudSo = new SerializedObject(hud);
        hudSo.FindProperty("goldText").objectReferenceValue     = goldText.GetComponent<TMP_Text>();
        hudSo.FindProperty("paperText").objectReferenceValue    = paperText.GetComponent<TMP_Text>();
        hudSo.FindProperty("focusText").objectReferenceValue    = focusText.GetComponent<TMP_Text>();
        hudSo.FindProperty("fragmentText").objectReferenceValue = fragmentText.GetComponent<TMP_Text>();
        hudSo.ApplyModifiedProperties();

        // ── CollectionPanel (도감, C키, 중앙 숨김) ──────────────────────
        var collPanel = UIKit.Panel(canvasGO.transform, "CollectionPanel", new Vector2(700f, 950f));
        collPanel.SetActive(false);
        UIKit.Label(collPanel.transform, "Title", "도감  [C]", UIKit.TextLevel.H1, new Vector2(0f, 415f));
        UIKit.Divider(collPanel.transform, new Vector2(0f, 378f), 660f);
        var collScroll  = UIKit.ScrollList(collPanel.transform, "CharList", Vector2.zero, new Vector2(676f, 730f));
        var closeBtnC   = UIKit.Button(collPanel.transform, "CloseBtn", "닫기  [C]",
            UIKit.BtnKind.Neutral, new Vector2(0f, -420f), new Vector2(300f, 65f));

        var collCmp = collPanel.AddComponent<CollectionPanel>();
        var cSo = new SerializedObject(collCmp);
        cSo.FindProperty("contentRoot").objectReferenceValue = collScroll.content;
        cSo.ApplyModifiedProperties();
        UnityEventTools.AddVoidPersistentListener(closeBtnC.GetComponent<Button>().onClick, collCmp.OnCloseClicked);

        // ── InventoryPanel (인벤토리, I키, 중앙 숨김) ───────────────────
        var invPanel = UIKit.Panel(canvasGO.transform, "InventoryPanel", new Vector2(700f, 950f));
        invPanel.SetActive(false);
        UIKit.Label(invPanel.transform, "Title", "인벤토리  [Tab]", UIKit.TextLevel.H1, new Vector2(0f, 415f));
        UIKit.Divider(invPanel.transform, new Vector2(0f, 378f), 660f);
        var invScroll   = UIKit.ScrollList(invPanel.transform, "CrystalList", Vector2.zero, new Vector2(676f, 730f));
        var closeBtnI   = UIKit.Button(invPanel.transform, "CloseBtn", "닫기  [Tab]",
            UIKit.BtnKind.Neutral, new Vector2(0f, -420f), new Vector2(300f, 65f));

        var invCmp = invPanel.AddComponent<InventoryPanel>();
        var iSo = new SerializedObject(invCmp);
        iSo.FindProperty("contentRoot").objectReferenceValue = invScroll.content;
        iSo.ApplyModifiedProperties();
        UnityEventTools.AddVoidPersistentListener(closeBtnI.GetComponent<Button>().onClick, invCmp.OnCloseClicked);

        // ── AdminPanel (F1키, 중앙 숨김) — 4탭 구조 ─────────────────────
        var adminPanel = UIKit.Panel(canvasGO.transform, "AdminPanel", new Vector2(700f, 1200f));
        adminPanel.SetActive(false);
        UIKit.Label(adminPanel.transform, "Title", "관리자 패널  [F1]", UIKit.TextLevel.H1, new Vector2(0f, 545f));
        UIKit.Divider(adminPanel.transform, new Vector2(0f, 508f), 660f);

        // 탭바 버튼 4개 (x: -247, -82, 82, 247)
        var tab0Btn = UIKit.Button(adminPanel.transform, "Tab0Btn", "커맨드",   UIKit.BtnKind.Primary, new Vector2(-247f, 460f), new Vector2(155f, 55f), UITheme.FontBody + 2);
        var tab1Btn = UIKit.Button(adminPanel.transform, "Tab1Btn", "캐릭터",   UIKit.BtnKind.Neutral, new Vector2( -82f, 460f), new Vector2(155f, 55f), UITheme.FontBody + 2);
        var tab2Btn = UIKit.Button(adminPanel.transform, "Tab2Btn", "플레이어", UIKit.BtnKind.Neutral, new Vector2(  82f, 460f), new Vector2(155f, 55f), UITheme.FontBody + 2);
        var tab3Btn = UIKit.Button(adminPanel.transform, "Tab3Btn", "문제",     UIKit.BtnKind.Neutral, new Vector2( 247f, 460f), new Vector2(155f, 55f), UITheme.FontBody + 2);
        UIKit.Divider(adminPanel.transform, new Vector2(0f, 428f), 660f);

        // ── CommandPanel (탭0) ─────────────────────────────────────────
        var commandPanel = new GameObject("CommandPanel");
        commandPanel.transform.SetParent(adminPanel.transform, false);
        {
            var rt = commandPanel.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(680f, 750f);
            rt.anchoredPosition = new Vector2(0f, 45f);
        }

        // 재화 지급
        UIKit.Label(commandPanel.transform, "CurrencyHdr", "■  재화 지급", UIKit.TextLevel.Body, new Vector2(-140f, 340f));
        var kindDropGO    = UIKit.Dropdown(commandPanel.transform, "KindDropdown",
            new System.Collections.Generic.List<string> { "골드", "논문", "집중력", "조각" },
            new Vector2(0f, 280f), new Vector2(420f, 55f));
        var amountInputGO = UIKit.Input(commandPanel.transform,  "AmountInput",  "금액 (기본 1000)", new Vector2(-75f, 210f), new Vector2(270f, 55f));
        var giveCurrBtn   = UIKit.Button(commandPanel.transform, "GiveCurrBtn",  "지급",  UIKit.BtnKind.Success, new Vector2(145f, 210f), new Vector2(130f, 55f));

        // 레벨 조작
        UIKit.Divider(commandPanel.transform, new Vector2(0f, 170f), 640f);
        UIKit.Label(commandPanel.transform, "LevelHdr", "■  레벨 조작", UIKit.TextLevel.Body, new Vector2(-140f, 140f));
        var charDropGO    = UIKit.Dropdown(commandPanel.transform, "CharDropdown",
            new System.Collections.Generic.List<string>(),
            new Vector2(0f, 80f), new Vector2(420f, 55f));
        var levelInputGO  = UIKit.Input(commandPanel.transform,  "LevelInput",   "레벨 값", new Vector2(-75f, 10f), new Vector2(270f, 55f));
        var applyLevelBtn = UIKit.Button(commandPanel.transform, "ApplyLevelBtn","적용",  UIKit.BtnKind.Primary, new Vector2(145f, 10f), new Vector2(130f, 55f));

        // 퀘스트 강제완료
        UIKit.Divider(commandPanel.transform, new Vector2(0f, -30f), 640f);
        UIKit.Label(commandPanel.transform, "QuestHdr", "■  퀘스트 강제완료", UIKit.TextLevel.Body, new Vector2(-100f, -60f));
        var questInputGO  = UIKit.Input(commandPanel.transform,  "QuestInput",     "퀘스트 ID",  new Vector2(-85f,  -130f), new Vector2(340f, 55f));
        var forceQuestBtn = UIKit.Button(commandPanel.transform, "ForceQuestBtn",  "강제 완료", UIKit.BtnKind.Neutral, new Vector2(185f, -130f), new Vector2(150f, 55f));

        // 가챠 디버그
        UIKit.Divider(commandPanel.transform, new Vector2(0f, -165f), 640f);
        UIKit.Label(commandPanel.transform, "GachaHdr", "■  가챠 디버그", UIKit.TextLevel.Body, new Vector2(-140f, -195f));
        var roll1BtnGO  = UIKit.Button(commandPanel.transform, "Roll1Btn",  "가챠 1회 (무료)", UIKit.BtnKind.Neutral, new Vector2(-155f, -255f), new Vector2(290f, 60f));
        var roll10BtnGO = UIKit.Button(commandPanel.transform, "Roll10Btn", "10연 (무료)",     UIKit.BtnKind.Neutral, new Vector2( 155f, -255f), new Vector2(290f, 60f));

        // 계정
        UIKit.Divider(commandPanel.transform, new Vector2(0f, -295f), 640f);
        UIKit.Label(commandPanel.transform, "AccountHdr", "■  계정", UIKit.TextLevel.Body, new Vector2(-175f, -320f));
        var resetBtnGO = UIKit.Button(commandPanel.transform, "ResetBtn", "초기화",  UIKit.BtnKind.Danger,   new Vector2(-190f, -375f), new Vector2(180f, 55f));
        var saveBtnGO  = UIKit.Button(commandPanel.transform, "SaveBtn",  "저장",    UIKit.BtnKind.Primary,  new Vector2(   0f, -375f), new Vector2(180f, 55f));
        var loadBtnGO  = UIKit.Button(commandPanel.transform, "LoadBtn",  "불러오기",UIKit.BtnKind.Neutral,  new Vector2( 190f, -375f), new Vector2(180f, 55f));

        var cmdCmp = commandPanel.AddComponent<CommandTab>();
        var cmdSo  = new SerializedObject(cmdCmp);
        cmdSo.FindProperty("kindDropdown").objectReferenceValue      = kindDropGO.GetComponent<TMP_Dropdown>();
        cmdSo.FindProperty("amountInput").objectReferenceValue       = amountInputGO.GetComponent<TMP_InputField>();
        cmdSo.FindProperty("giveCurrencyButton").objectReferenceValue= giveCurrBtn.GetComponent<Button>();
        cmdSo.FindProperty("charDropdown").objectReferenceValue      = charDropGO.GetComponent<TMP_Dropdown>();
        cmdSo.FindProperty("levelInput").objectReferenceValue        = levelInputGO.GetComponent<TMP_InputField>();
        cmdSo.FindProperty("applyLevelButton").objectReferenceValue  = applyLevelBtn.GetComponent<Button>();
        cmdSo.FindProperty("questIdInput").objectReferenceValue      = questInputGO.GetComponent<TMP_InputField>();
        cmdSo.FindProperty("forceQuestButton").objectReferenceValue  = forceQuestBtn.GetComponent<Button>();
        cmdSo.FindProperty("rollOneButton").objectReferenceValue     = roll1BtnGO.GetComponent<Button>();
        cmdSo.FindProperty("rollTenButton").objectReferenceValue     = roll10BtnGO.GetComponent<Button>();
        cmdSo.FindProperty("resetButton").objectReferenceValue       = resetBtnGO.GetComponent<Button>();
        cmdSo.FindProperty("saveButton").objectReferenceValue        = saveBtnGO.GetComponent<Button>();
        cmdSo.FindProperty("loadButton").objectReferenceValue        = loadBtnGO.GetComponent<Button>();
        cmdSo.ApplyModifiedProperties();

        // ── CharacterPanel (탭1) ───────────────────────────────────────
        var characterPanel = new GameObject("CharacterPanel");
        characterPanel.transform.SetParent(adminPanel.transform, false);
        characterPanel.SetActive(false);
        {
            var rt = characterPanel.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(680f, 750f);
            rt.anchoredPosition = new Vector2(0f, 45f);
        }

        var searchInputGO  = UIKit.Input(characterPanel.transform,  "SearchInput", "이름 검색",  new Vector2(-90f, 325f), new Vector2(445f, 55f));
        var giveAllBtnGO   = UIKit.Button(characterPanel.transform, "GiveAllBtn",  "전체 지급", UIKit.BtnKind.Success, new Vector2(228f, 325f), new Vector2(170f, 55f));
        var charScrollList = UIKit.ScrollList(characterPanel.transform, "CharList", new Vector2(0f, -30f), new Vector2(660f, 615f));

        var charTabCmp = characterPanel.AddComponent<CharacterTab>();
        var charTabSo  = new SerializedObject(charTabCmp);
        charTabSo.FindProperty("searchInput").objectReferenceValue  = searchInputGO.GetComponent<TMP_InputField>();
        charTabSo.FindProperty("giveAllButton").objectReferenceValue= giveAllBtnGO.GetComponent<Button>();
        charTabSo.FindProperty("contentRoot").objectReferenceValue  = charScrollList.content;
        charTabSo.ApplyModifiedProperties();

        // ── PlayerPanel (탭2) — 실제 PlayFab 플레이어 목록 + 정지/삭제 ──────
        var playerPanel = new GameObject("PlayerPanel");
        playerPanel.transform.SetParent(adminPanel.transform, false);
        playerPanel.SetActive(false);
        {
            var rt = playerPanel.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(680f, 750f);
            rt.anchoredPosition = new Vector2(0f, 45f);
        }

        var sortDropGO = UIKit.Dropdown(playerPanel.transform, "SortDropdown",
            new List<string> { "이름", "PlayFabId", "마지막 로그인", "가입일" },
            new Vector2(-150f, 325f), new Vector2(280f, 55f));
        var orderBtnGO = UIKit.Button(playerPanel.transform, "OrderBtn", "오름차순 ▲",
            UIKit.BtnKind.Neutral, new Vector2(105f, 325f), new Vector2(150f, 55f));
        var refreshBtnGO = UIKit.Button(playerPanel.transform, "RefreshBtn", "새로고침",
            UIKit.BtnKind.Primary, new Vector2(258f, 325f), new Vector2(130f, 55f));
        var playerScroll = UIKit.ScrollList(playerPanel.transform, "PlayerList",
            new Vector2(0f, -30f), new Vector2(660f, 615f));

        UIKit.Label(playerPanel.transform, "NoticeLabel",
            "행의 [관리]로 해당 플레이어의 캐릭터 지급/회수·정지·삭제·초기화를 할 수 있습니다. 삭제·초기화는 되돌릴 수 없어 두 번 눌러 확인합니다.",
            UIKit.TextLevel.Caption, new Vector2(0f, -365f));

        var playerTabCmp = playerPanel.AddComponent<PlayerTab>();
        var playerTabSo  = new SerializedObject(playerTabCmp);
        playerTabSo.FindProperty("sortDropdown").objectReferenceValue  = sortDropGO.GetComponent<TMP_Dropdown>();
        playerTabSo.FindProperty("orderButton").objectReferenceValue   = orderBtnGO.GetComponent<Button>();
        playerTabSo.FindProperty("refreshButton").objectReferenceValue = refreshBtnGO.GetComponent<Button>();
        playerTabSo.FindProperty("contentRoot").objectReferenceValue   = playerScroll.content;
        playerTabSo.ApplyModifiedProperties();

        // ── PlayerManagePanel — 플레이어 1명 전용 상세 패널(정지/삭제/초기화 + 캐릭터 지급·회수) ──
        var managePanelGO = UIKit.Panel(adminPanel.transform, "PlayerManagePanel",
            new Vector2(680f, 750f), new Vector2(0f, 45f));
        managePanelGO.SetActive(false);

        var manageTitle = UIKit.Label(managePanelGO.transform, "Title", "플레이어 관리",
            UIKit.TextLevel.H2, new Vector2(0f, 335f), new Vector2(640f, 50f));

        var manageBanBtnGO    = UIKit.Button(managePanelGO.transform, "BanBtn",    "정지",
            UIKit.BtnKind.Neutral, new Vector2(-220f, 265f), new Vector2(200f, 55f));
        var manageDeleteBtnGO = UIKit.Button(managePanelGO.transform, "DeleteBtn", "삭제",
            UIKit.BtnKind.Danger,  new Vector2(0f, 265f),    new Vector2(200f, 55f));
        var manageResetBtnGO  = UIKit.Button(managePanelGO.transform, "ResetBtn",  "계정 초기화",
            UIKit.BtnKind.Danger,  new Vector2(220f, 265f),  new Vector2(200f, 55f));

        var manageSearchGO = UIKit.Input(managePanelGO.transform, "SearchInput", "캐릭터 검색",
            new Vector2(0f, 195f), new Vector2(420f, 55f));

        var manageScroll = UIKit.ScrollList(managePanelGO.transform, "CharList",
            new Vector2(0f, -70f), new Vector2(660f, 470f));

        var manageCloseBtnGO = UIKit.Button(managePanelGO.transform, "CloseBtn", "닫기",
            UIKit.BtnKind.Neutral, new Vector2(0f, -345f), new Vector2(220f, 60f));

        var managePanelCmp = managePanelGO.AddComponent<PlayerManagePanel>();
        var managePanelSo  = new SerializedObject(managePanelCmp);
        managePanelSo.FindProperty("titleLabel").objectReferenceValue   = manageTitle.GetComponent<TMP_Text>();
        managePanelSo.FindProperty("banButton").objectReferenceValue    = manageBanBtnGO.GetComponent<Button>();
        managePanelSo.FindProperty("deleteButton").objectReferenceValue = manageDeleteBtnGO.GetComponent<Button>();
        managePanelSo.FindProperty("resetButton").objectReferenceValue  = manageResetBtnGO.GetComponent<Button>();
        managePanelSo.FindProperty("searchInput").objectReferenceValue  = manageSearchGO.GetComponent<TMP_InputField>();
        managePanelSo.FindProperty("contentRoot").objectReferenceValue  = manageScroll.content;
        managePanelSo.FindProperty("closeButton").objectReferenceValue  = manageCloseBtnGO.GetComponent<Button>();
        managePanelSo.ApplyModifiedProperties();

        playerTabSo.FindProperty("managePanel").objectReferenceValue = managePanelCmp;
        playerTabSo.ApplyModifiedProperties();

        // ── ProblemPanel (탭3) — 문제 분류 트리 + 추가/수정/삭제 폼 ─────
        var problemPanel = new GameObject("ProblemPanel");
        problemPanel.transform.SetParent(adminPanel.transform, false);
        problemPanel.SetActive(false);
        {
            var rt = problemPanel.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(680f, 750f);
            rt.anchoredPosition = new Vector2(0f, 45f);
        }

        var addProblemBtnGO = UIKit.Button(problemPanel.transform, "AddProblemBtn", "＋ 새 문제",
            UIKit.BtnKind.Success, new Vector2(0f, 325f), new Vector2(300f, 55f));
        var problemScroll = UIKit.ScrollList(problemPanel.transform, "ProblemTree", new Vector2(0f, -40f), new Vector2(660f, 615f));

        var problemTabCmp = problemPanel.AddComponent<ProblemTab>();
        var problemTabSo  = new SerializedObject(problemTabCmp);
        problemTabSo.FindProperty("contentRoot").objectReferenceValue = problemScroll.content;
        problemTabSo.FindProperty("addButton").objectReferenceValue   = addProblemBtnGO.GetComponent<Button>();

        // ── ProblemFormPanel (문제탭 하위 오버레이) ──────────────────────
        var formPanelGO = new GameObject("ProblemFormPanel");
        formPanelGO.transform.SetParent(problemPanel.transform, false);
        formPanelGO.SetActive(false);
        {
            var rt = formPanelGO.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(660f, 700f);
            rt.anchoredPosition = Vector2.zero;
        }
        formPanelGO.AddComponent<Image>().color = UITheme.PanelBgDark;

        var idLabelGO = UIKit.Label(formPanelGO.transform, "IdLabel", "새 문제",
            UIKit.TextLevel.Caption, new Vector2(0f, 320f), new Vector2(640f, 30f));

        var purposeDropGO = UIKit.Dropdown(formPanelGO.transform, "PurposeDropdown", new List<string>(),
            new Vector2(-165f, 280f), new Vector2(310f, 50f));
        var difficultyDropGO = UIKit.Dropdown(formPanelGO.transform, "DifficultyDropdown", new List<string>(),
            new Vector2(165f, 280f), new Vector2(310f, 50f));

        var subjectDropGO = UIKit.Dropdown(formPanelGO.transform, "SubjectDropdown", new List<string>(),
            new Vector2(-165f, 225f), new Vector2(310f, 50f));
        var countryInputGO = UIKit.Input(formPanelGO.transform, "CountryInput", "국가(슬러그, 예: newton-empire)",
            new Vector2(165f, 225f), new Vector2(310f, 50f));

        var typeDropGO = UIKit.Dropdown(formPanelGO.transform, "TypeDropdown", new List<string>(),
            new Vector2(-165f, 170f), new Vector2(310f, 50f));
        var skillDropGO = UIKit.Dropdown(formPanelGO.transform, "SkillDropdown", new List<string>(),
            new Vector2(165f, 170f), new Vector2(310f, 50f));

        var promptInputGO = UIKit.Input(formPanelGO.transform, "PromptInput", "문제 지문",
            new Vector2(0f, 105f), new Vector2(640f, 64f));
        promptInputGO.GetComponent<TMP_InputField>().lineType = TMP_InputField.LineType.MultiLineNewline;

        var choice1GO = UIKit.Input(formPanelGO.transform, "Choice1Input", "보기 1",
            new Vector2(-165f, 40f), new Vector2(310f, 50f));
        var choice2GO = UIKit.Input(formPanelGO.transform, "Choice2Input", "보기 2",
            new Vector2(165f, 40f), new Vector2(310f, 50f));
        var choice3GO = UIKit.Input(formPanelGO.transform, "Choice3Input", "보기 3",
            new Vector2(-165f, -15f), new Vector2(310f, 50f));
        var choice4GO = UIKit.Input(formPanelGO.transform, "Choice4Input", "보기 4",
            new Vector2(165f, -15f), new Vector2(310f, 50f));

        var correctIndexDropGO = UIKit.Dropdown(formPanelGO.transform, "CorrectIndexDropdown", new List<string>(),
            new Vector2(-165f, -70f), new Vector2(310f, 50f));
        var acceptedAnswersGO = UIKit.Input(formPanelGO.transform, "AcceptedAnswersInput", "정답(콤마 구분)",
            new Vector2(165f, -70f), new Vector2(310f, 50f));

        var explanationGO = UIKit.Input(formPanelGO.transform, "ExplanationInput", "해설(선택)",
            new Vector2(0f, -140f), new Vector2(640f, 56f));
        explanationGO.GetComponent<TMP_InputField>().lineType = TMP_InputField.LineType.MultiLineNewline;

        var formStatusGO = UIKit.Label(formPanelGO.transform, "FormStatus", "",
            UIKit.TextLevel.Caption, new Vector2(0f, -185f), new Vector2(640f, 26f));

        var formSaveBtnGO   = UIKit.Button(formPanelGO.transform, "SaveBtn",   "저장",   UIKit.BtnKind.Primary, new Vector2(-220f, -235f), new Vector2(200f, 60f));
        var formDeleteBtnGO = UIKit.Button(formPanelGO.transform, "DeleteBtn", "삭제",   UIKit.BtnKind.Danger,  new Vector2(   0f, -235f), new Vector2(200f, 60f));
        var formCancelBtnGO = UIKit.Button(formPanelGO.transform, "CancelBtn", "취소",   UIKit.BtnKind.Neutral, new Vector2( 220f, -235f), new Vector2(200f, 60f));

        var formCmp = formPanelGO.AddComponent<ProblemFormPanel>();
        var formSo  = new SerializedObject(formCmp);
        formSo.FindProperty("purposeDropdown").objectReferenceValue      = purposeDropGO.GetComponent<TMP_Dropdown>();
        formSo.FindProperty("subjectDropdown").objectReferenceValue      = subjectDropGO.GetComponent<TMP_Dropdown>();
        formSo.FindProperty("countryInput").objectReferenceValue         = countryInputGO.GetComponent<TMP_InputField>();
        formSo.FindProperty("skillDropdown").objectReferenceValue        = skillDropGO.GetComponent<TMP_Dropdown>();
        formSo.FindProperty("difficultyDropdown").objectReferenceValue   = difficultyDropGO.GetComponent<TMP_Dropdown>();
        formSo.FindProperty("typeDropdown").objectReferenceValue         = typeDropGO.GetComponent<TMP_Dropdown>();
        formSo.FindProperty("promptInput").objectReferenceValue          = promptInputGO.GetComponent<TMP_InputField>();
        formSo.FindProperty("choice1Input").objectReferenceValue         = choice1GO.GetComponent<TMP_InputField>();
        formSo.FindProperty("choice2Input").objectReferenceValue         = choice2GO.GetComponent<TMP_InputField>();
        formSo.FindProperty("choice3Input").objectReferenceValue         = choice3GO.GetComponent<TMP_InputField>();
        formSo.FindProperty("choice4Input").objectReferenceValue         = choice4GO.GetComponent<TMP_InputField>();
        formSo.FindProperty("correctIndexDropdown").objectReferenceValue = correctIndexDropGO.GetComponent<TMP_Dropdown>();
        formSo.FindProperty("acceptedAnswersInput").objectReferenceValue = acceptedAnswersGO.GetComponent<TMP_InputField>();
        formSo.FindProperty("explanationInput").objectReferenceValue     = explanationGO.GetComponent<TMP_InputField>();
        formSo.FindProperty("idLabel").objectReferenceValue               = idLabelGO.GetComponent<TMP_Text>();
        formSo.FindProperty("statusText").objectReferenceValue           = formStatusGO.GetComponent<TMP_Text>();
        formSo.FindProperty("saveButton").objectReferenceValue           = formSaveBtnGO.GetComponent<Button>();
        formSo.FindProperty("deleteButton").objectReferenceValue         = formDeleteBtnGO.GetComponent<Button>();
        formSo.FindProperty("cancelButton").objectReferenceValue         = formCancelBtnGO.GetComponent<Button>();
        formSo.ApplyModifiedProperties();

        problemTabSo.FindProperty("formPanel").objectReferenceValue = formCmp;
        problemTabSo.ApplyModifiedProperties();

        // ── 공유 상태·닫기 ────────────────────────────────────────────
        var statusAdmin = UIKit.Label(adminPanel.transform, "Status",  "", UIKit.TextLevel.Body, new Vector2(0f, -450f));
        var closeBtnA   = UIKit.Button(adminPanel.transform, "CloseBtn", "닫기  [F1]", UIKit.BtnKind.Neutral, new Vector2(0f, -520f), new Vector2(300f, 70f));

        // ── AdminPanel 컴포넌트 부착·와이어링 ─────────────────────────
        var adminCmp = adminPanel.AddComponent<AdminPanel>();
        var aSo = new SerializedObject(adminCmp);

        var tabButtonsProp = aSo.FindProperty("tabButtons");
        tabButtonsProp.arraySize = 4;
        tabButtonsProp.GetArrayElementAtIndex(0).objectReferenceValue = tab0Btn.GetComponent<Button>();
        tabButtonsProp.GetArrayElementAtIndex(1).objectReferenceValue = tab1Btn.GetComponent<Button>();
        tabButtonsProp.GetArrayElementAtIndex(2).objectReferenceValue = tab2Btn.GetComponent<Button>();
        tabButtonsProp.GetArrayElementAtIndex(3).objectReferenceValue = tab3Btn.GetComponent<Button>();

        var tabPanelsProp = aSo.FindProperty("tabPanels");
        tabPanelsProp.arraySize = 4;
        tabPanelsProp.GetArrayElementAtIndex(0).objectReferenceValue = commandPanel;
        tabPanelsProp.GetArrayElementAtIndex(1).objectReferenceValue = characterPanel;
        tabPanelsProp.GetArrayElementAtIndex(2).objectReferenceValue = playerPanel;
        tabPanelsProp.GetArrayElementAtIndex(3).objectReferenceValue = problemPanel;

        aSo.FindProperty("statusText").objectReferenceValue  = statusAdmin.GetComponent<TMP_Text>();
        aSo.FindProperty("closeButton").objectReferenceValue = closeBtnA.GetComponent<Button>();
        aSo.ApplyModifiedProperties();

        UnityEventTools.AddVoidPersistentListener(tab0Btn.GetComponent<Button>().onClick,  adminCmp.OnTab0);
        UnityEventTools.AddVoidPersistentListener(tab1Btn.GetComponent<Button>().onClick,  adminCmp.OnTab1);
        UnityEventTools.AddVoidPersistentListener(tab2Btn.GetComponent<Button>().onClick,  adminCmp.OnTab2);
        UnityEventTools.AddVoidPersistentListener(tab3Btn.GetComponent<Button>().onClick,  adminCmp.OnTab3);
        UnityEventTools.AddVoidPersistentListener(closeBtnA.GetComponent<Button>().onClick, adminCmp.OnCloseClicked);

        // CommandTab·CharacterTab·PlayerTab·ProblemTab에 owner 주입
        cmdSo.FindProperty("owner").objectReferenceValue    = adminCmp;
        cmdSo.ApplyModifiedProperties();
        charTabSo.FindProperty("owner").objectReferenceValue = adminCmp;
        charTabSo.ApplyModifiedProperties();
        playerTabSo.FindProperty("owner").objectReferenceValue = adminCmp;
        playerTabSo.ApplyModifiedProperties();
        problemTabSo.FindProperty("owner").objectReferenceValue = adminCmp;
        problemTabSo.ApplyModifiedProperties();
        managePanelSo.FindProperty("owner").objectReferenceValue = adminCmp;
        managePanelSo.ApplyModifiedProperties();

        // ── AdminLoginPanel (관리자 비밀 키 입력 패널, 기본 비활성) ──────────
        var adminLoginPanelGO = UIKit.Panel(canvasGO.transform, "AdminLoginPanel", new Vector2(600f, 420f));
        adminLoginPanelGO.SetActive(false);

        UIKit.Label(adminLoginPanelGO.transform, "Title",  "관리자 인증",         UIKit.TextLevel.H1,   new Vector2(0f,  155f));
        UIKit.Label(adminLoginPanelGO.transform, "Desc",   "비밀 키를 입력하세요", UIKit.TextLevel.Body, new Vector2(0f,   90f));
        var keyInput   = UIKit.Input(adminLoginPanelGO.transform,  "KeyInput",   "비밀 키",  new Vector2(0f,   15f), new Vector2(500f, 70f));
        var confirmBtn = UIKit.Button(adminLoginPanelGO.transform, "ConfirmBtn", "확인",
            UIKit.BtnKind.Primary, new Vector2(-130f, -75f), new Vector2(220f, 65f));
        var closeBtnL  = UIKit.Button(adminLoginPanelGO.transform, "CloseBtn",  "닫기",
            UIKit.BtnKind.Neutral, new Vector2( 130f, -75f), new Vector2(220f, 65f));
        var statusL    = UIKit.Label(adminLoginPanelGO.transform,  "Status",    "",          UIKit.TextLevel.Body, new Vector2(0f, -155f));

        // 비밀번호 마스킹
        var keyInputCmp = keyInput.GetComponent<TMP_InputField>();
        keyInputCmp.contentType = TMP_InputField.ContentType.Password;
        keyInputCmp.ForceLabelUpdate();

        var adminLoginCmp = adminLoginPanelGO.AddComponent<AdminLoginPanel>();
        var alSo = new SerializedObject(adminLoginCmp);
        alSo.FindProperty("panel").objectReferenceValue         = adminLoginPanelGO;
        alSo.FindProperty("keyInput").objectReferenceValue      = keyInputCmp;
        alSo.FindProperty("confirmButton").objectReferenceValue = confirmBtn.GetComponent<Button>();
        alSo.FindProperty("closeButton").objectReferenceValue   = closeBtnL.GetComponent<Button>();
        alSo.FindProperty("statusText").objectReferenceValue    = statusL.GetComponent<TMP_Text>();
        alSo.ApplyModifiedProperties();

        // ── RnEPanel (K키, R&E) ─────────────────────────────────────────
        // 허브 구조: 3열 그리드 → 카드 클릭 → 개인 창
        //   [레벨업] → 난이도 선택 → 자원 소모 → 문제(최대 3시도) → EXP
        //   [스킬 연구] → 전체폭 스킬 목록 → 잠금 클릭 → 문제 → 해금
        var skillPanel = UIKit.Panel(canvasGO.transform, "RnEPanel", new Vector2(1000f, 950f));
        skillPanel.SetActive(false);

        // 제목 (상단 좌측)
        {
            var g = new GameObject("Title"); g.transform.SetParent(skillPanel.transform, false);
            var r = g.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0f,1f); r.anchorMax = new Vector2(1f,1f);
            r.offsetMin = new Vector2(14f,-68f); r.offsetMax = new Vector2(-155f,0f);
            var t = g.AddComponent<TextMeshProUGUI>();
            t.text = "R&E"; t.fontSize = 36; t.color = Color.white;
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
            closeBtnKGO.AddComponent<Image>().color = UITheme.BtnNeutral;
            closeBtnKGO.AddComponent<Button>();
            var tg = new GameObject("Text"); tg.transform.SetParent(closeBtnKGO.transform, false);
            var tr = tg.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.sizeDelta = Vector2.zero;
            var t = tg.AddComponent<TextMeshProUGUI>();
            t.text = "닫기"; t.fontSize = 22; t.color = Color.white;
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
        charSvGO.AddComponent<Image>().color = new Color(UITheme.PanelBgDark.r, UITheme.PanelBgDark.g,
                                                          UITheme.PanelBgDark.b, 0.85f);
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
            detailPanelGO.AddComponent<Image>().color = UITheme.PanelBgDarkA;
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
            difficultyPanelGO.AddComponent<Image>().color = new Color(UITheme.PanelBgDark.r,
                UITheme.PanelBgDark.g, UITheme.PanelBgDark.b, 0.75f);
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
            skillListPanelGO.AddComponent<Image>().color = new Color(UITheme.PanelBgDark.r,
                UITheme.PanelBgDark.g, UITheme.PanelBgDark.b, 0.80f);
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
            vl.childControlWidth     = true;  vl.childControlHeight     = false;
            vl.childForceExpandWidth = true;  vl.childForceExpandHeight = false;
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
            skillInfoPanelGO.AddComponent<Image>().color = UITheme.PanelBgDarkA;
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

        // 스탯·설명 텍스트 (중간 45%) — 하단 숙련도 텍스트를 위해 0.35로 조정
        var skillInfoStatsGO = new GameObject("SkillInfoStats");
        skillInfoStatsGO.transform.SetParent(skillInfoPanelGO.transform, false);
        TMP_Text skillInfoStatsTxt;
        {
            var r = skillInfoStatsGO.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0f, 0.35f); r.anchorMax = new Vector2(1f, 0.80f);
            r.offsetMin = new Vector2(14f, 4f); r.offsetMax = new Vector2(-14f, 0f);
            skillInfoStatsTxt = skillInfoStatsGO.AddComponent<TextMeshProUGUI>();
            skillInfoStatsTxt.fontSize = 17;
            skillInfoStatsTxt.color = new Color(0.85f, 0.92f, 1f);
            skillInfoStatsTxt.alignment = TextAlignmentOptions.TopLeft;
            skillInfoStatsTxt.textWrappingMode = TextWrappingModes.Normal;
            if (font != null) skillInfoStatsTxt.font = font;
        }

        // 숙련도 텍스트 (0.28 ~ 0.35) — 스킬 레벨업용
        var skillInfoProfGO = new GameObject("SkillInfoProf");
        skillInfoProfGO.transform.SetParent(skillInfoPanelGO.transform, false);
        TMP_Text skillInfoProfTxt;
        {
            var r = skillInfoProfGO.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0f, 0.28f); r.anchorMax = new Vector2(1f, 0.35f);
            r.offsetMin = new Vector2(14f, 0f); r.offsetMax = new Vector2(-14f, 0f);
            skillInfoProfTxt = skillInfoProfGO.AddComponent<TextMeshProUGUI>();
            skillInfoProfTxt.fontSize = 16;
            skillInfoProfTxt.color = new Color(1f, 0.85f, 0.35f);   // 황금색 — 숙련도 강조
            skillInfoProfTxt.alignment = TextAlignmentOptions.MidlineLeft;
            if (font != null) skillInfoProfTxt.font = font;
        }

        // [연구 시작] 버튼 (하단 좌측 58%)
        var skillResearchStartBtnGO = new GameObject("SkillResearchStartBtn");
        skillResearchStartBtnGO.transform.SetParent(skillInfoPanelGO.transform, false);
        {
            var r = skillResearchStartBtnGO.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0f, 0.02f); r.anchorMax = new Vector2(0.58f, 0.28f);
            r.offsetMin = new Vector2(8f, 2f); r.offsetMax = new Vector2(-4f, -2f);
            skillResearchStartBtnGO.AddComponent<Image>().color = UITheme.BtnSuccess;
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
            skillInfoBackBtnGO.AddComponent<Image>().color = UITheme.BtnNeutral;
            skillInfoBackBtnGO.AddComponent<Button>();
            var tg = new GameObject("Label"); tg.transform.SetParent(skillInfoBackBtnGO.transform, false);
            var tr = tg.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.sizeDelta = Vector2.zero;
            var lt = tg.AddComponent<TextMeshProUGUI>();
            lt.text = "목록으로"; lt.fontSize = 20; lt.color = Color.white;
            lt.alignment = TextAlignmentOptions.Center;
            if (font != null) lt.font = font;
        }

        // [스킬 레벨업] 버튼 (하단 좌측 58%, [연구시작]과 동일 위치 — 해금 스킬에서만 표시)
        var skillLevelUpBtnGO = new GameObject("SkillLevelUpBtn");
        skillLevelUpBtnGO.transform.SetParent(skillInfoPanelGO.transform, false);
        {
            var r = skillLevelUpBtnGO.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0f, 0.02f); r.anchorMax = new Vector2(0.58f, 0.28f);
            r.offsetMin = new Vector2(8f, 2f); r.offsetMax = new Vector2(-4f, -2f);
            skillLevelUpBtnGO.AddComponent<Image>().color = new Color(0.15f, 0.55f, 0.85f, 0.95f);  // 파란색
            skillLevelUpBtnGO.AddComponent<Button>();
            var tg = new GameObject("Label"); tg.transform.SetParent(skillLevelUpBtnGO.transform, false);
            var tr = tg.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.sizeDelta = Vector2.zero;
            var lt = tg.AddComponent<TextMeshProUGUI>();
            lt.text = "레벨업"; lt.fontSize = 20; lt.color = Color.white;
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
            lvUpModeBtnGO.AddComponent<Image>().color = UITheme.BtnSuccess;
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
            skillModeBtnGO.AddComponent<Image>().color = UITheme.BtnPrimary;
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
            problemOverlayGO.AddComponent<Image>().color = new Color(UITheme.PanelBgDark.r,
                UITheme.PanelBgDark.g, UITheme.PanelBgDark.b, 0.97f);
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
                cb.AddComponent<Image>().color = new Color(UITheme.BtnPrimary.r, UITheme.BtnPrimary.g,
                    UITheme.BtnPrimary.b, 0.9f);
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
            answerInputGO = UIKit.Input(fiArea.transform, "AnswerInput", "답 입력...");
            var rAns = (RectTransform)answerInputGO.transform;
            rAns.anchorMin = new Vector2(0f,0.52f); rAns.anchorMax = new Vector2(1f,0.90f);
            rAns.offsetMin = Vector2.zero; rAns.offsetMax = Vector2.zero;
            submitBtnGO = UIKit.Button(fiArea.transform, "SubmitBtn", "제출",
                UIKit.BtnKind.Success, fontSize: UITheme.FontH2);
            var rSub = (RectTransform)submitBtnGO.transform;
            rSub.anchorMin = new Vector2(0f,0.04f); rSub.anchorMax = new Vector2(1f,0.46f);
            rSub.offsetMin = Vector2.zero; rSub.offsetMax = Vector2.zero;
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
            closeProbBtnGO.AddComponent<Image>().color = UITheme.BtnDanger;
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
        srpSo.FindProperty("skillInfoProfText").objectReferenceValue        = skillInfoProfTxt;
        srpSo.FindProperty("skillResearchStartButton").objectReferenceValue = skillResearchStartBtnGO.GetComponent<Button>();
        srpSo.FindProperty("skillInfoBackButton").objectReferenceValue      = skillInfoBackBtnGO.GetComponent<Button>();
        srpSo.FindProperty("skillLevelUpButton").objectReferenceValue       = skillLevelUpBtnGO.GetComponent<Button>();

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

        // ── GachaPanel (도서관 — 상호작용으로만 오픈, 키 단축키 없음) ────────
        var gachaPanel = UIKit.Panel(canvasGO.transform, "GachaPanel", new Vector2(700f, 850f));
        gachaPanel.SetActive(false);

        // 제목
        UIKit.Label(gachaPanel.transform, "Title", "도서관 — 지식 탐구",
            UIKit.TextLevel.H1, new Vector2(0f, 385f));
        UIKit.Divider(gachaPanel.transform, new Vector2(0f, 348f), 660f);

        // 정보 행 (논문 잔액, 천장)
        var paperLbl = UIKit.Label(gachaPanel.transform, "PaperText", "논문: —",
            UIKit.TextLevel.H2, new Vector2(-160f, 295f),
            align: TextAlignmentOptions.MidlineLeft);
        var pityLbl  = UIKit.Label(gachaPanel.transform, "PityText", "천장 0 / 50",
            UIKit.TextLevel.H2, new Vector2(160f, 295f),
            align: TextAlignmentOptions.MidlineRight);

        // 1회 / 10회 버튼
        var singleBtn = UIKit.Button(gachaPanel.transform, "SingleBtn", "1회 탐구",
            UIKit.BtnKind.Primary, new Vector2(-160f, 215f), new Vector2(300f, 70f));
        var tenBtn    = UIKit.Button(gachaPanel.transform, "TenBtn",    "10회 탐구",
            UIKit.BtnKind.Primary, new Vector2( 160f, 215f), new Vector2(300f, 70f));

        // 비용 안내 (버튼 아래)
        var singleCostLbl = UIKit.Label(gachaPanel.transform, "SingleCostText", $"논문 {GachaConfig.CostSingle}",
            UIKit.TextLevel.Caption, new Vector2(-160f, 145f));
        var tenCostLbl    = UIKit.Label(gachaPanel.transform, "TenCostText", $"논문 {GachaConfig.CostTen}",
            UIKit.TextLevel.Caption, new Vector2( 160f, 145f));

        // 결과 영역 (버튼 아래, 클릭 후 표시)
        var resultArea = new GameObject("ResultArea");
        resultArea.transform.SetParent(gachaPanel.transform, false);
        { var rRt = resultArea.AddComponent<RectTransform>();
          rRt.anchorMin = rRt.anchorMax = new Vector2(0.5f, 0.5f);
          rRt.pivot     = new Vector2(0.5f, 0.5f);
          rRt.sizeDelta        = new Vector2(660f, 360f);
          rRt.anchoredPosition = new Vector2(0f, -55f); } // top≈+125, bottom≈-235
        resultArea.SetActive(false);

        var resultLbl = UIKit.Label(resultArea.transform, "ResultText", "",
            UIKit.TextLevel.Body, Vector2.zero, new Vector2(640f, 350f),
            align: TextAlignmentOptions.TopLeft);

        // 상태 메시지
        var statusLbl = UIKit.Label(gachaPanel.transform, "StatusText", "",
            UIKit.TextLevel.Body, new Vector2(0f, -280f));

        // 닫기 버튼
        var closeGachaBtn = UIKit.Button(gachaPanel.transform, "CloseBtn", "닫기",
            UIKit.BtnKind.Neutral, new Vector2(0f, -390f), new Vector2(280f, 65f));

        // GachaPanel 컴포넌트 부착 + 직렬화 필드 주입
        var gachaCmp = gachaPanel.AddComponent<GachaPanel>();
        var gSo = new SerializedObject(gachaCmp);
        gSo.FindProperty("paperText").objectReferenceValue      = paperLbl.GetComponent<TMP_Text>();
        gSo.FindProperty("pityText").objectReferenceValue       = pityLbl.GetComponent<TMP_Text>();
        gSo.FindProperty("singleButton").objectReferenceValue   = singleBtn.GetComponent<Button>();
        gSo.FindProperty("tenButton").objectReferenceValue      = tenBtn.GetComponent<Button>();
        gSo.FindProperty("closeButton").objectReferenceValue    = closeGachaBtn.GetComponent<Button>();
        gSo.FindProperty("singleCostText").objectReferenceValue = singleCostLbl.GetComponent<TMP_Text>();
        gSo.FindProperty("tenCostText").objectReferenceValue    = tenCostLbl.GetComponent<TMP_Text>();
        gSo.FindProperty("resultArea").objectReferenceValue     = resultArea;
        gSo.FindProperty("resultText").objectReferenceValue     = resultLbl.GetComponent<TMP_Text>();
        gSo.FindProperty("statusText").objectReferenceValue     = statusLbl.GetComponent<TMP_Text>();
        gSo.ApplyModifiedProperties();

        UnityEventTools.AddVoidPersistentListener(singleBtn.GetComponent<Button>().onClick,    gachaCmp.OnSingleClicked);
        UnityEventTools.AddVoidPersistentListener(tenBtn.GetComponent<Button>().onClick,       gachaCmp.OnTenClicked);
        UnityEventTools.AddVoidPersistentListener(closeGachaBtn.GetComponent<Button>().onClick, gachaCmp.OnCloseClicked);

        // MetaPanelController 참조 연결
        var ctrlSo = new SerializedObject(controller);
        ctrlSo.FindProperty("collectionPanel").objectReferenceValue = collPanel;
        ctrlSo.FindProperty("inventoryPanel").objectReferenceValue  = invPanel;
        ctrlSo.FindProperty("rnePanel").objectReferenceValue        = skillPanel;
        ctrlSo.FindProperty("adminPanel").objectReferenceValue      = adminPanel;
        ctrlSo.FindProperty("gachaPanel").objectReferenceValue      = gachaPanel;
        ctrlSo.ApplyModifiedProperties();

        // 건물 Interactable 와이어링 (MesoriaHubBuilder가 HubLab · HubLibrary 생성)
        WireBuilding("HubLab",     controller, "OpenLab");
        WireBuilding("HubLibrary", controller, "OpenLibrary");

        // 지도 시스템
        BuildMinimap(canvasGO);
        BuildWorldMapPanel(canvasGO);

        // 씬 저장
        EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene(), SCENE_PATH);
        AssetDatabase.Refresh();

        Debug.Log("[MSRPG] Meta UI 설정 완료! Mesoria 씬에 MetaCanvas가 추가됐습니다.");
    }

    // ── 헬퍼 ──────────────────────────────────────────────────────────────

    /// <summary>
    /// 씬 내 <paramref name="goName"/> GameObject의 Interactable.onInteract 에
    /// <paramref name="target"/>.<paramref name="methodName"/> 을 퍼시스턴트 리스너로 등록한다.
    /// SerializedObject로 직접 기록해 씬 저장 시 누락되지 않도록 한다.
    /// </summary>
    static void WireBuilding(string goName, MonoBehaviour target, string methodName)
    {
        var go = GameObject.Find(goName);
        if (go == null)
        {
            Debug.LogWarning($"[MetaUISetup] '{goName}' GameObject를 찾을 수 없습니다. MSRPG > Setup Mesoria Scene을 먼저 실행하세요.");
            return;
        }
        var ia = go.GetComponent<Interactable>();
        if (ia == null)
        {
            Debug.LogWarning($"[MetaUISetup] '{goName}'에 Interactable 컴포넌트가 없습니다.");
            return;
        }
        var so    = new SerializedObject(ia);
        var calls = so.FindProperty("onInteract")
                      .FindPropertyRelative("m_PersistentCalls")
                      .FindPropertyRelative("m_Calls");
        calls.ClearArray();
        calls.InsertArrayElementAtIndex(0);
        var call = calls.GetArrayElementAtIndex(0);
        call.FindPropertyRelative("m_Target").objectReferenceValue = target;
        call.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue =
            $"{target.GetType().FullName}, {target.GetType().Assembly.GetName().Name}";
        call.FindPropertyRelative("m_MethodName").stringValue = methodName;
        call.FindPropertyRelative("m_Mode").enumValueIndex    = 1; // PersistentListenerMode.Void
        call.FindPropertyRelative("m_CallState").enumValueIndex = 2; // UnityEventCallState.RuntimeOnly
        so.ApplyModifiedProperties();
        Debug.Log($"[MetaUISetup] {goName}.onInteract → {target.GetType().Name}.{methodName} 와이어링 완료");
    }

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

    // ── 지도 시스템 ────────────────────────────────────────────────────────

    static void BuildMinimap(GameObject canvasGO)
    {
        // MinimapContainer — 우상단 고정, 180×180 파치먼트 박스
        var containerGO = new GameObject("MinimapContainer");
        containerGO.transform.SetParent(canvasGO.transform, false);
        var containerRt = containerGO.AddComponent<RectTransform>();
        containerRt.anchorMin        = new Vector2(1f, 1f);
        containerRt.anchorMax        = new Vector2(1f, 1f);
        containerRt.pivot            = new Vector2(1f, 1f);
        containerRt.sizeDelta        = new Vector2(180f, 180f);
        containerRt.anchoredPosition = new Vector2(-20f, -100f); // CurrencyHud(70px) 아래 여백
        containerGO.AddComponent<Image>().color = new Color(0.18f, 0.12f, 0.06f, 1f); // 어두운 테두리 색

        // MinimapBg — 배경, 3px 안쪽 인셋 (테두리 효과)
        var bgGO = new GameObject("MinimapBg");
        bgGO.transform.SetParent(containerGO.transform, false);
        var bgRt = bgGO.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = new Vector2(3f, 3f); bgRt.offsetMax = new Vector2(-3f, -3f);
        bgGO.AddComponent<Image>().color = new Color(0.77f, 0.64f, 0.42f, 1f); // 불투명 파치먼트

        // IconParent — 마커 아이콘 부모, 전체 채움
        var iconParentGO = new GameObject("IconParent");
        iconParentGO.transform.SetParent(containerGO.transform, false);
        var iconParentRt = iconParentGO.AddComponent<RectTransform>();
        iconParentRt.anchorMin = Vector2.zero; iconParentRt.anchorMax = Vector2.one;
        iconParentRt.sizeDelta = Vector2.zero;

        // PlayerDot — 플레이어 위치 표시, 중앙 고정 8×8
        var dotGO = new GameObject("PlayerDot");
        dotGO.transform.SetParent(containerGO.transform, false);
        var dotRt = dotGO.AddComponent<RectTransform>();
        dotRt.anchorMin        = new Vector2(0.5f, 0.5f);
        dotRt.anchorMax        = new Vector2(0.5f, 0.5f);
        dotRt.sizeDelta        = new Vector2(8f, 8f);
        dotRt.anchoredPosition = Vector2.zero;
        dotGO.AddComponent<Image>().color = Color.white;

        // MinimapHud 컴포넌트 부착 및 필드 와이어링
        var minimapHud = containerGO.AddComponent<MinimapHud>();
        var hudSo = new SerializedObject(minimapHud);
        hudSo.FindProperty("iconParent").objectReferenceValue = iconParentGO.GetComponent<RectTransform>();
        hudSo.FindProperty("playerDot").objectReferenceValue  = dotGO.GetComponent<RectTransform>();
        hudSo.FindProperty("mapSize").floatValue              = 160f;
        hudSo.FindProperty("viewRange").floatValue            = 50f;
        hudSo.ApplyModifiedProperties();

        Debug.Log("[MetaUISetup] MinimapContainer 생성 완료");
    }

    static void BuildWorldMapPanel(GameObject canvasGO)
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_SDF_PATH);

        // WorldMapPanel 루트 — 전체 화면 어두운 오버레이
        var panelGO = new GameObject("WorldMapPanel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        var panelRt = panelGO.AddComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero; panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero; panelRt.offsetMax = Vector2.zero;
        panelGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);

        // WorldMapPanel 컴포넌트 먼저 부착 (CloseBtn 와이어링을 위해)
        var worldMap = panelGO.AddComponent<WorldMapPanel>();

        // MapFrame — 750×750 파치먼트 프레임, 중앙
        var frameGO = new GameObject("MapFrame");
        frameGO.transform.SetParent(panelGO.transform, false);
        var frameRt = frameGO.AddComponent<RectTransform>();
        frameRt.anchorMin        = new Vector2(0.5f, 0.5f);
        frameRt.anchorMax        = new Vector2(0.5f, 0.5f);
        frameRt.sizeDelta        = new Vector2(750f, 750f);
        frameRt.anchoredPosition = Vector2.zero;
        frameGO.AddComponent<Image>().color = new Color(0.77f, 0.64f, 0.42f, 1f);

        // MapBg — 위성뷰 지형 텍스처 (FogOverlay 뒤에 렌더)
        var mapBgGO = new GameObject("MapBg");
        mapBgGO.transform.SetParent(frameGO.transform, false);
        var mapBgRt = mapBgGO.AddComponent<RectTransform>();
        mapBgRt.anchorMin = Vector2.zero; mapBgRt.anchorMax = Vector2.one;
        mapBgRt.offsetMin = Vector2.zero; mapBgRt.offsetMax = Vector2.zero;
        mapBgGO.AddComponent<RawImage>().color = Color.white;

        // FogOverlay — 부드러운 원형 안개 알파 마스크 (MapBg 위, 마커 아래)
        var fogOverlayGO = new GameObject("FogOverlay");
        fogOverlayGO.transform.SetParent(frameGO.transform, false);
        var fogOverlayRt = fogOverlayGO.AddComponent<RectTransform>();
        fogOverlayRt.anchorMin = Vector2.zero; fogOverlayRt.anchorMax = Vector2.one;
        fogOverlayRt.offsetMin = Vector2.zero; fogOverlayRt.offsetMax = Vector2.zero;
        var fogOverlayImg = fogOverlayGO.AddComponent<RawImage>();
        fogOverlayImg.color = Color.white;
        fogOverlayGO.GetComponent<UnityEngine.UI.Graphic>().raycastTarget = false;

        // MarkerIconParent — 마커 아이콘 부모, 전체 채움
        var markerParentGO = new GameObject("MarkerIconParent");
        markerParentGO.transform.SetParent(frameGO.transform, false);
        var markerParentRt = markerParentGO.AddComponent<RectTransform>();
        markerParentRt.anchorMin = Vector2.zero; markerParentRt.anchorMax = Vector2.one;
        markerParentRt.offsetMin = Vector2.zero; markerParentRt.offsetMax = Vector2.zero;

        // PlayerDotMap — 플레이어 위치 표시, 10×10 빨간 점
        var playerDotGO = new GameObject("PlayerDotMap");
        playerDotGO.transform.SetParent(frameGO.transform, false);
        var playerDotRt = playerDotGO.AddComponent<RectTransform>();
        playerDotRt.anchorMin        = new Vector2(0.5f, 0.5f);
        playerDotRt.anchorMax        = new Vector2(0.5f, 0.5f);
        playerDotRt.sizeDelta        = new Vector2(10f, 10f);
        playerDotRt.anchoredPosition = Vector2.zero;
        playerDotGO.AddComponent<Image>().color = Color.red;

        // TitleLabel — 맵 제목, MapFrame 상단 중앙
        var titleGO = new GameObject("TitleLabel");
        titleGO.transform.SetParent(frameGO.transform, false);
        var titleRt = titleGO.AddComponent<RectTransform>();
        titleRt.anchorMin        = new Vector2(0.5f, 0.5f);
        titleRt.anchorMax        = new Vector2(0.5f, 0.5f);
        titleRt.sizeDelta        = new Vector2(700f, 50f);
        titleRt.anchoredPosition = new Vector2(0f, 370f); // 750/2 - 5 ≈ 370
        var titleTxt = titleGO.AddComponent<TextMeshProUGUI>();
        titleTxt.text      = "메조리아 지도";
        titleTxt.fontSize  = 28;
        titleTxt.color     = new Color(0.2f, 0.1f, 0.05f, 1f);
        titleTxt.alignment = TextAlignmentOptions.Center;
        if (font != null) titleTxt.font = font;

        // CloseBtn — 우상단 닫기 버튼
        var closeBtnGO = UIKit.Button(panelGO.transform, "CloseBtn", "X",
            UIKit.BtnKind.Danger, new Vector2(-30f, -30f), new Vector2(60f, 60f));
        var closeBtnRt = closeBtnGO.GetComponent<RectTransform>();
        closeBtnRt.anchorMin = new Vector2(1f, 1f);
        closeBtnRt.anchorMax = new Vector2(1f, 1f);
        closeBtnRt.pivot     = new Vector2(1f, 1f);

        // WorldMapPanel 필드 와이어링
        var wmSo = new SerializedObject(worldMap);
        wmSo.FindProperty("mapFrame").objectReferenceValue   = frameGO.GetComponent<RectTransform>();
        wmSo.FindProperty("iconParent").objectReferenceValue = markerParentGO.GetComponent<RectTransform>();
        wmSo.FindProperty("playerDot").objectReferenceValue  = playerDotGO.GetComponent<RectTransform>();
        wmSo.FindProperty("tooltip").objectReferenceValue    = titleGO.GetComponent<TMP_Text>();
        wmSo.FindProperty("mapBg").objectReferenceValue      = mapBgGO.GetComponent<RawImage>();
        wmSo.FindProperty("fogOverlay").objectReferenceValue = fogOverlayImg;
        wmSo.FindProperty("mapFrameSize").floatValue         = 750f;
        wmSo.ApplyModifiedProperties();

        // CloseBtn → worldMap.Close 와이어링
        UnityEventTools.AddVoidPersistentListener(closeBtnGO.GetComponent<Button>().onClick, worldMap.Close);

        // MetaPanelController에 worldMapPanel 참조 와이어링 (M키 감지용)
        var mpc = canvasGO.GetComponent<MetaPanelController>();
        if (mpc != null)
        {
            var mpcSo = new SerializedObject(mpc);
            mpcSo.FindProperty("worldMapPanel").objectReferenceValue = worldMap;
            mpcSo.ApplyModifiedProperties();
        }

        // 에디터에서도 비활성화 (Start()에서도 하지만 에디터 상태 일치)
        panelGO.SetActive(false);

        Debug.Log("[MetaUISetup] WorldMapPanel 생성 완료");
    }

}
