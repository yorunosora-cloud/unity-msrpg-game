using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem.UI;

public static class MesoriaSetup
{
    const string FONT_SDF_PATH = "Assets/_Game/Art/Fonts/malgun SDF.asset";

    [MenuItem("MSRPG/Setup Mesoria Scene")]
    public static void Run()
    {
        var korFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_SDF_PATH);
        EnsureTmpDefaultFont(korFont);

        // 1. 새 빈 씬
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 2. 조명
        var sunGO = new GameObject("Directional Light");
        var light  = sunGO.AddComponent<Light>();
        light.type      = LightType.Directional;
        light.intensity = 1f;
        sunGO.transform.rotation = Quaternion.Euler(-50f, 30f, 0f);

        // 3·4. 지면 + 경계 벽 → MesoriaHubBuilder.Build() 에서 일괄 생성

        // 5. 게임 부트스트랩
        var bootstrap = new GameObject("GameBootstrap");
        bootstrap.AddComponent<GameBootstrap>();

        // 6. 플레이어
        var player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = new Vector3(0f, 0f, -100f); // 남쪽 성문 밖 — 허브 스폰 지점
        var cc = player.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 1f, 0f);
        cc.radius = 0.3f;
        cc.height = 2f;
        player.AddComponent<PlayerController>();
        player.AddComponent<BoxCharacterBuilder>();
        player.AddComponent<PlayerCombat>();      // Q키 평타
        player.AddComponent<PlayerSkills>();      // U/I/O/H/J/K 스킬 입력 + MP 자연회복
        player.AddComponent<PartyController>();   // 캐릭터 교체 (1/2/3키)
        player.AddComponent<PlayerDeath>();       // 사망/부활 — 씬 완성 후 와이어링
        player.AddComponent<PlayerInteractor>();  // 월드 상호작용 (E키, 도서관 등)

        // 7. 카메라
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        camGO.AddComponent<Camera>();
        camGO.AddComponent<AudioListener>();
        var followCam = camGO.AddComponent<FollowCamera>();
        var camSO = new SerializedObject(followCam);
        camSO.FindProperty("target").objectReferenceValue = player.transform;
        camSO.ApplyModifiedProperties();

        // 8. EventSystem (Login 씬 언로드 후 필요)
        var eventGO = new GameObject("EventSystem");
        eventGO.AddComponent<EventSystem>();
        eventGO.AddComponent<InputSystemUIInputModule>();

        // 9. 계정 패널 UI
        var canvasGO = new GameObject("UI Canvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // 9-a. 우상단 열기 버튼 (우상단 앵커)
        var openBtn = UIKit.Button(canvasGO.transform, "OpenAccountButton", "계정",
            UIKit.BtnKind.Neutral, size: new Vector2(140f, 60f), fontSize: UITheme.FontH2);
        { var r = openBtn.GetComponent<RectTransform>();
          r.anchorMin = r.anchorMax = new Vector2(1f, 1f);
          r.pivot = new Vector2(1f, 1f);
          r.anchoredPosition = new Vector2(-20f, -20f); }

        // 9-b. 계정 패널 (기본 비활성)
        var panelGO = UIKit.Panel(canvasGO.transform, "AccountPanel", new Vector2(600f, 500f));

        // 제목
        UIKit.Label(panelGO.transform, "Title", "계정 정보",
            UIKit.TextLevel.H1, new Vector2(0, 180));

        // 구분선
        UIKit.Divider(panelGO.transform, new Vector2(0, 135), 520f);

        // 정보 텍스트
        var userText  = UIKit.Label(panelGO.transform, "UsernameText", "아이디: —",
            UIKit.TextLevel.H2, new Vector2(0, 70),
            align: TextAlignmentOptions.MidlineLeft);
        var levelText = UIKit.Label(panelGO.transform, "LevelText",    "레벨: —",
            UIKit.TextLevel.H2, new Vector2(0, 10),
            align: TextAlignmentOptions.MidlineLeft);

        // 로그아웃 버튼 (위험색)
        var logoutBtn = UIKit.Button(panelGO.transform, "LogoutButton", "로그아웃",
            UIKit.BtnKind.Danger, new Vector2(0, -100), new Vector2(380f, 70f));

        // 닫기 버튼 (X, 우상단 앵커)
        var closeBtn = UIKit.Button(panelGO.transform, "CloseButton", "✕",
            UIKit.BtnKind.Neutral, size: new Vector2(60f, 60f), fontSize: 24);
        { var r = closeBtn.GetComponent<RectTransform>();
          r.anchorMin = r.anchorMax = new Vector2(1f, 1f);
          r.pivot = new Vector2(1f, 1f);
          r.anchoredPosition = new Vector2(-10f, -10f); }

        panelGO.SetActive(false);

        // 9-c. AccountPanel 컴포넌트 연결
        var ap   = canvasGO.AddComponent<AccountPanel>();
        var apSO = new SerializedObject(ap);
        apSO.FindProperty("panel").objectReferenceValue        = panelGO;
        apSO.FindProperty("usernameText").objectReferenceValue = userText.GetComponent<TextMeshProUGUI>();
        apSO.FindProperty("levelText").objectReferenceValue    = levelText.GetComponent<TextMeshProUGUI>();
        apSO.FindProperty("openButton").objectReferenceValue   = openBtn.GetComponent<Button>();
        apSO.FindProperty("closeButton").objectReferenceValue  = closeBtn.GetComponent<Button>();
        apSO.FindProperty("logoutButton").objectReferenceValue = logoutBtn.GetComponent<Button>();
        apSO.ApplyModifiedProperties();

        // 9-d. 상호작용 프롬프트 라벨 (하단 중앙, 기본 숨김) + PlayerInteractor 와이어링
        var promptLabelGO = new GameObject("InteractPromptLabel");
        promptLabelGO.transform.SetParent(canvasGO.transform, false);
        var promptRt = promptLabelGO.AddComponent<RectTransform>();
        promptRt.anchorMin        = new Vector2(0.5f, 0f);
        promptRt.anchorMax        = new Vector2(0.5f, 0f);
        promptRt.pivot            = new Vector2(0.5f, 0f);
        promptRt.sizeDelta        = new Vector2(420f, 55f);
        promptRt.anchoredPosition = new Vector2(0f, 145f); // 스킬바(슬롯 115h + 16pad) 위
        var promptTmpTxt = promptLabelGO.AddComponent<TextMeshProUGUI>();
        promptTmpTxt.text      = "E: 상호작용";
        promptTmpTxt.fontSize  = 24;
        promptTmpTxt.color     = Color.white;
        promptTmpTxt.alignment = TextAlignmentOptions.Center;
        promptTmpTxt.fontStyle = FontStyles.Bold;
        if (korFont != null) promptTmpTxt.font = korFont;
        promptLabelGO.SetActive(false);

        var interactor   = player.GetComponent<PlayerInteractor>();
        var interactorSO = new SerializedObject(interactor);
        interactorSO.FindProperty("promptLabel").objectReferenceValue = promptTmpTxt;
        interactorSO.ApplyModifiedProperties();

        // 10. 전투 HUD — 좌하단 플레이어 HP/MP 바
        var combatHudGO = CreateCombatHudPanel(canvasGO.transform, korFont);
        canvasGO.AddComponent<CombatHud>();
        // SerializedObject 방식으로 fill/text 연결
        var chud    = canvasGO.GetComponent<CombatHud>();
        var chudSO  = new SerializedObject(chud);
        chudSO.FindProperty("hpFill").objectReferenceValue = combatHudGO.hpFill;
        chudSO.FindProperty("mpFill").objectReferenceValue = combatHudGO.mpFill;
        chudSO.FindProperty("hpText").objectReferenceValue = combatHudGO.hpText;
        chudSO.FindProperty("mpText").objectReferenceValue = combatHudGO.mpText;
        chudSO.ApplyModifiedProperties();

        // 11. 사망 오버레이 + PlayerDeath 와이어링
        var (deathPanel, countdown) = CreateDeathOverlay(canvasGO.transform, korFont);
        var pd   = player.GetComponent<PlayerDeath>();
        var pdSO = new SerializedObject(pd);
        pdSO.FindProperty("deathOverlay").objectReferenceValue  = deathPanel;
        pdSO.FindProperty("countdownText").objectReferenceValue = countdown;
        pdSO.ApplyModifiedProperties();

        // 12. 허브 맵 환경 생성 (지면·바다·부두·포탈·건물·광장·탑·조명·경계 벽 일체)
        MesoriaHubBuilder.Build();

        // 13. 파티 HUD (우하단, 3칸)
        CreatePartyHud(canvasGO.transform, korFont);

        // 14. 스킬 바 HUD (하단 중앙, 최대 6칸)
        CreateSkillBarHud(canvasGO.transform, canvasGO, korFont);

        // 14-a. 포스트프로세싱 Global Volume
        var ppProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        System.IO.Directory.CreateDirectory(Application.dataPath + "/_Game/Data");
        const string ppAssetPath = "Assets/_Game/Data/PostProcessProfile.asset";
        if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(ppAssetPath) != null)
            AssetDatabase.DeleteAsset(ppAssetPath);
        AssetDatabase.CreateAsset(ppProfile, ppAssetPath);

        var bloom      = ppProfile.Add<Bloom>();
        bloom.active   = true;
        bloom.intensity.Override(0.4f);
        bloom.threshold.Override(0.9f);
        bloom.scatter.Override(0.7f);

        var colorAdj    = ppProfile.Add<ColorAdjustments>();
        colorAdj.active = true;
        colorAdj.contrast.Override(10f);
        colorAdj.colorFilter.Override(new Color(0.96f, 0.97f, 1.0f));

        var vignette    = ppProfile.Add<Vignette>();
        vignette.active = true;
        vignette.intensity.Override(0.25f);
        vignette.smoothness.Override(0.40f);

        EditorUtility.SetDirty(ppProfile);
        AssetDatabase.SaveAssets();

        var ppGO          = new GameObject("Post Process Volume");
        var vol           = ppGO.AddComponent<Volume>();
        vol.isGlobal      = true;
        vol.priority      = 1;
        vol.sharedProfile = ppProfile;

        // 15. 씬 저장
        System.IO.Directory.CreateDirectory(Application.dataPath + "/_Game/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/_Game/Scenes/Mesoria.unity");
        AssetDatabase.Refresh();

        Debug.Log("[MSRPG] ✅ Mesoria 씬 생성 완료! Q=평타, U/I/O/H/J/K=스킬, 1/2/3=파티 교체, E=건물 상호작용(연구소·도서관·길드), C=도감, Tab=인벤토리.");
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// TMP 기본 폰트가 미설정일 때 malgun을 지정해
    /// AddComponent&lt;TextMeshProUGUI&gt;() 시 "LiberationSans not found" 경고를 방지한다.
    /// </summary>
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

    // ── 전투 HUD 헬퍼 ──────────────────────────────────────────────────────

    struct CombatHudRefs
    {
        public Image    hpFill, mpFill;
        public TMP_Text hpText, mpText;
    }

    static CombatHudRefs CreateCombatHudPanel(Transform canvasParent, TMP_FontAsset font)
    {
        // 좌하단 앵커 패널
        var panelGO = new GameObject("CombatHudPanel");
        panelGO.transform.SetParent(canvasParent, false);
        var rt = panelGO.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 0f);
        rt.anchorMax        = new Vector2(0f, 0f);
        rt.pivot            = new Vector2(0f, 0f);
        rt.sizeDelta        = new Vector2(340f, 110f);
        rt.anchoredPosition = new Vector2(20f, 20f);
        var bg = panelGO.AddComponent<Image>();
        bg.color = new Color(UITheme.PanelBgDark.r, UITheme.PanelBgDark.g,
                             UITheme.PanelBgDark.b, 0.88f);

        Image    hpFill = CreateBar(panelGO.transform, "HP", new Vector2(16f, 70f),
                              new Color(0.15f, 0.85f, 0.3f));
        Image    mpFill = CreateBar(panelGO.transform, "MP", new Vector2(16f, 36f),
                              new Color(0.2f, 0.5f, 1f));
        TMP_Text hpTxt  = CreateBarLabel(panelGO.transform, "HPText", "HP —/—",
                              new Vector2(16f, 70f), font);
        TMP_Text mpTxt  = CreateBarLabel(panelGO.transform, "MPText", "MP —/—",
                              new Vector2(16f, 36f), font);

        return new CombatHudRefs { hpFill = hpFill, mpFill = mpFill,
                                   hpText = hpTxt,  mpText = mpTxt };
    }

    static Image CreateBar(Transform parent, string name, Vector2 pos, Color color)
    {
        // 흰색 테두리 (BG보다 먼저 생성 → BG 뒤에 렌더링)
        var borderGO = new GameObject(name + "Border");
        borderGO.transform.SetParent(parent, false);
        var borderRt = borderGO.AddComponent<RectTransform>();
        borderRt.anchorMin        = new Vector2(0f, 0f);
        borderRt.anchorMax        = new Vector2(0f, 0f);
        borderRt.pivot            = new Vector2(0f, 0.5f);
        borderRt.sizeDelta        = new Vector2(310f, 24f);
        borderRt.anchoredPosition = new Vector2(pos.x - 1f, pos.y);
        borderGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.65f);

        // 배경
        var bgGO = new GameObject(name + "BG");
        bgGO.transform.SetParent(parent, false);
        var bgRt = bgGO.AddComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 0f);
        bgRt.anchorMax = new Vector2(0f, 0f);
        bgRt.pivot     = new Vector2(0f, 0.5f);
        bgRt.sizeDelta        = new Vector2(308f, 22f);
        bgRt.anchoredPosition = pos;
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(UITheme.PanelBgDark.r, UITheme.PanelBgDark.g,
                                UITheme.PanelBgDark.b, 0.9f);

        // 채움
        var fillGO = new GameObject(name + "Fill");
        fillGO.transform.SetParent(bgGO.transform, false);
        var fillRt = fillGO.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.sizeDelta = Vector2.zero;
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.color      = color;
        fillImg.type       = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 1f;

        return fillImg;
    }

    static TMP_Text CreateBarLabel(Transform parent, string name, string text,
        Vector2 pos, TMP_FontAsset font)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 0f);
        rt.anchorMax        = new Vector2(0f, 0f);
        rt.pivot            = new Vector2(0f, 0.5f);
        rt.sizeDelta        = new Vector2(308f, 22f);
        rt.anchoredPosition = pos;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text      = text;
        t.fontSize  = 14;
        t.color     = Color.white;
        t.alignment = TextAlignmentOptions.MidlineLeft;
        if (font != null) t.font = font;
        return t;
    }

    // ── 사망 오버레이 ──────────────────────────────────────────────────────

    static (GameObject panel, TMP_Text countdown) CreateDeathOverlay(Transform canvasParent, TMP_FontAsset font)
    {
        // 전체화면 반투명 검정 패널 (Canvas 마지막 자식 → 최상단 렌더링)
        var panelGO = new GameObject("DeathOverlay");
        panelGO.transform.SetParent(canvasParent, false);
        var rt = panelGO.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        panelGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.88f);

        // "사망" 큰 빨간 텍스트
        CreateOverlayLabel(panelGO.transform, "DeathTitle", "사망",
            120, new Vector2(0f, 100f), new Color(0.9f, 0.1f, 0.1f), font);

        // 카운트다운 숫자 (PlayerDeath가 매 프레임 갱신)
        var cdLabel = CreateOverlayLabel(panelGO.transform, "Countdown", "3",
            96, new Vector2(0f, -20f), Color.white, font);

        // 안내 문구
        CreateOverlayLabel(panelGO.transform, "HintText", "잠시 후 부활합니다...",
            28, new Vector2(0f, -130f), new Color(1f, 1f, 1f, 0.65f), font);

        panelGO.SetActive(false);
        return (panelGO, cdLabel.GetComponent<TMP_Text>());
    }

    static GameObject CreateOverlayLabel(Transform parent, string name, string text,
        int fontSize, Vector2 pos, Color color, TMP_FontAsset font)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(700f, 160f);
        rt.anchoredPosition = pos;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text      = text;
        t.fontSize  = fontSize;
        t.color     = color;
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = FontStyles.Bold;
        if (font != null) t.font = font;
        return go;
    }

    // ── 파티 HUD ──────────────────────────────────────────────────────────

    static void CreatePartyHud(Transform canvasParent, TMP_FontAsset font)
    {
        const int   SlotCount = 3;
        const float SlotW     = 130f;
        const float SlotH     = 190f;
        const float SlotGap   = 6f;
        const float PadRight  = 16f;
        const float PadBottom = 16f;

        // 파티 HUD 루트 (우하단)
        var rootGO = new GameObject("PartyHudRoot");
        rootGO.transform.SetParent(canvasParent, false);
        var rootRt = rootGO.AddComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(1f, 0f);
        rootRt.anchorMax = new Vector2(1f, 0f);
        rootRt.pivot     = new Vector2(1f, 0f);
        float totalW = SlotW * SlotCount + SlotGap * (SlotCount - 1);
        rootRt.sizeDelta        = new Vector2(totalW, SlotH);
        rootRt.anchoredPosition = new Vector2(-PadRight, PadBottom);

        // 슬롯 ref 배열
        var bgs           = new Image[SlotCount];
        var borders       = new Image[SlotCount];
        var downedOverlays= new Image[SlotCount];
        var statusTexts   = new TMP_Text[SlotCount];
        var nameTexts     = new TMP_Text[SlotCount];
        var levelTexts    = new TMP_Text[SlotCount];
        var hpFills       = new Image[SlotCount];
        var hpTexts       = new TMP_Text[SlotCount];
        var mpFills       = new Image[SlotCount];
        var mpTexts       = new TMP_Text[SlotCount];

        for (int i = 0; i < SlotCount; i++)
        {
            float xOff = (SlotW + SlotGap) * i;

            // ── 슬롯 배경 ──
            var slotGO = new GameObject($"Slot_{i + 1}");
            slotGO.transform.SetParent(rootGO.transform, false);
            var slotRt = slotGO.AddComponent<RectTransform>();
            slotRt.anchorMin        = new Vector2(0f, 0f);
            slotRt.anchorMax        = new Vector2(0f, 0f);
            slotRt.pivot            = new Vector2(0f, 0f);
            slotRt.sizeDelta        = new Vector2(SlotW, SlotH);
            slotRt.anchoredPosition = new Vector2(xOff, 0f);
            var bgImg = slotGO.AddComponent<Image>();
            bgImg.color = new Color(UITheme.PanelBgMid.r, UITheme.PanelBgMid.g,
                                    UITheme.PanelBgMid.b, 0.93f);
            bgs[i] = bgImg;

            // ── 활성 금색 테두리 ──
            var borderGO = new GameObject("ActiveBorder");
            borderGO.transform.SetParent(slotGO.transform, false);
            var borderRt = borderGO.AddComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = new Vector2(-4f, -4f);
            borderRt.offsetMax = new Vector2( 4f,  4f);
            var borderImg = borderGO.AddComponent<Image>();
            borderImg.color   = new Color(1f, 0.85f, 0.1f); // 금색
            borderImg.enabled = false;
            borders[i] = borderImg;

            // ── 기절 반투명 오버레이 ──
            var downGO = new GameObject("DownedOverlay");
            downGO.transform.SetParent(slotGO.transform, false);
            var downRt = downGO.AddComponent<RectTransform>();
            downRt.anchorMin = Vector2.zero;
            downRt.anchorMax = Vector2.one;
            downRt.offsetMin = Vector2.zero;
            downRt.offsetMax = Vector2.zero;
            var downImg = downGO.AddComponent<Image>();
            downImg.color   = new Color(0f, 0f, 0f, 0.55f);
            downImg.enabled = false;
            downedOverlays[i] = downImg;

            // ── 행 1: 상태 텍스트 (최상단, 슬롯 높이 14%) ──
            statusTexts[i] = CreateSlotLabel(slotGO.transform, "StatusText",
                "대기", 13, font,
                anchorMin: new Vector2(0f, 0.86f), anchorMax: new Vector2(1f, 1.00f),
                offset: new Vector2(5f, -2f), bold: true, align: TextAlignmentOptions.TopLeft);

            // ── 행 2: 캐릭터 이름 (슬롯 높이 22%) ──
            nameTexts[i] = CreateSlotLabel(slotGO.transform, "NameText",
                "—", 17, font,
                anchorMin: new Vector2(0f, 0.64f), anchorMax: new Vector2(1f, 0.86f),
                offset: new Vector2(5f, 0f), bold: true, align: TextAlignmentOptions.MidlineLeft);

            // ── 행 3: 레벨 (슬롯 높이 14%) ──
            levelTexts[i] = CreateSlotLabel(slotGO.transform, "LevelText",
                "Lv.1", 13, font,
                anchorMin: new Vector2(0f, 0.50f), anchorMax: new Vector2(1f, 0.64f),
                offset: new Vector2(5f, 0f), bold: false, align: TextAlignmentOptions.MidlineLeft);

            // ── 행 4: HP 바 + 수치 (슬롯 높이 18%) ──
            (hpFills[i], hpTexts[i]) = CreateStatBar(slotGO.transform, "HP",
                new Color(0.15f, 0.85f, 0.3f),
                anchorMin: new Vector2(0f, 0.30f), anchorMax: new Vector2(1f, 0.48f),
                font, padX: 5f);

            // ── 행 5: MP 바 + 수치 (슬롯 높이 18%) ──
            (mpFills[i], mpTexts[i]) = CreateStatBar(slotGO.transform, "MP",
                new Color(0.2f, 0.5f, 1.0f),
                anchorMin: new Vector2(0f, 0.10f), anchorMax: new Vector2(1f, 0.28f),
                font, padX: 5f);
        }

        // PartyHud 컴포넌트 추가 + SerializedObject로 배열 주입 (씬에 직렬화)
        var hud   = canvasParent.gameObject.AddComponent<PartyHud>();
        var hudSO = new SerializedObject(hud);
        SetObjArray(hudSO, "_slotBgs",            bgs);
        SetObjArray(hudSO, "_slotBorders",         borders);
        SetObjArray(hudSO, "_slotDownedOverlays",  downedOverlays);
        SetObjArray(hudSO, "_slotStatusTexts",     statusTexts);
        SetObjArray(hudSO, "_slotNameTexts",       nameTexts);
        SetObjArray(hudSO, "_slotLevelTexts",      levelTexts);
        SetObjArray(hudSO, "_slotHpFills",         hpFills);
        SetObjArray(hudSO, "_slotHpTexts",         hpTexts);
        SetObjArray(hudSO, "_slotMpFills",         mpFills);
        SetObjArray(hudSO, "_slotMpTexts",         mpTexts);
        hudSO.ApplyModifiedProperties();
    }

    /// <summary>SerializedObject의 UnityObject 배열 프로퍼티를 일괄 설정.</summary>
    static void SetObjArray(SerializedObject so, string propName, Object[] objs)
    {
        var arr = so.FindProperty(propName);
        if (arr == null) { Debug.LogError($"[MesoriaSetup] 프로퍼티 '{propName}'를 찾을 수 없습니다."); return; }
        arr.arraySize = objs.Length;
        for (int i = 0; i < objs.Length; i++)
            arr.GetArrayElementAtIndex(i).objectReferenceValue = objs[i];
    }

    /// <summary>슬롯 내 텍스트 레이블 생성 헬퍼.</summary>
    static TMP_Text CreateSlotLabel(Transform parent, string name, string text,
        int fontSize, TMP_FontAsset font,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offset, bool bold, TextAlignmentOptions align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = new Vector2( offset.x, 0f);
        rt.offsetMax = new Vector2(-offset.x, 0f);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text      = text;
        t.fontSize  = fontSize;
        t.color     = Color.white;
        t.alignment = align;
        if (bold) t.fontStyle = FontStyles.Bold;
        if (font != null) t.font = font;
        return t;
    }

    /// <summary>슬롯 내 스탯 바 + 수치 텍스트 생성 헬퍼. (fill, text) 반환.</summary>
    static (Image fill, TMP_Text label) CreateStatBar(Transform parent, string name,
        Color fillColor, Vector2 anchorMin, Vector2 anchorMax,
        TMP_FontAsset font, float padX)
    {
        // 흰색 테두리 (BG보다 먼저 생성 → BG 뒤에 렌더링)
        float bgMaxY = anchorMin.y + (anchorMax.y - anchorMin.y) * 0.55f;
        var borderGO = new GameObject(name + "Border");
        borderGO.transform.SetParent(parent, false);
        var borderRt = borderGO.AddComponent<RectTransform>();
        borderRt.anchorMin = anchorMin;
        borderRt.anchorMax = new Vector2(anchorMax.x, bgMaxY);
        borderRt.offsetMin = new Vector2(padX - 1f, -1f);
        borderRt.offsetMax = new Vector2(-padX + 1f, 1f);
        borderGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.65f);

        // 바 배경
        var bgGO = new GameObject(name + "BG");
        bgGO.transform.SetParent(parent, false);
        var bgRt = bgGO.AddComponent<RectTransform>();
        bgRt.anchorMin = anchorMin;
        bgRt.anchorMax = new Vector2(anchorMax.x, bgMaxY);
        bgRt.offsetMin = new Vector2(padX, 0f);
        bgRt.offsetMax = new Vector2(-padX, 0f);
        bgGO.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.85f);

        // 채움
        var fillGO = new GameObject(name + "Fill");
        fillGO.transform.SetParent(bgGO.transform, false);
        var fillRt = fillGO.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.color      = fillColor;
        fillImg.type       = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 1f;

        // 수치 텍스트 (바 아래쪽)
        var txtGO = new GameObject(name + "Text");
        txtGO.transform.SetParent(parent, false);
        var txtRt = txtGO.AddComponent<RectTransform>();
        txtRt.anchorMin = new Vector2(anchorMin.x, anchorMin.y + (anchorMax.y - anchorMin.y) * 0.55f);
        txtRt.anchorMax = anchorMax;
        txtRt.offsetMin = new Vector2(padX, 0f);
        txtRt.offsetMax = new Vector2(-padX, 0f);
        var t = txtGO.AddComponent<TextMeshProUGUI>();
        t.text      = "—/—";
        t.fontSize  = 11;
        t.color     = new Color(0.9f, 0.9f, 0.9f);
        t.alignment = TextAlignmentOptions.MidlineLeft;
        if (font != null) t.font = font;

        return (fillImg, t);
    }

    // ── 스킬 바 HUD ───────────────────────────────────────────────────────

    static void CreateSkillBarHud(Transform canvasParent, GameObject canvasGO, TMP_FontAsset font)
    {
        const int   MaxSlots = 6;
        const float SlotW    = 95f;
        const float SlotH    = 115f;
        const float SlotGap  = 6f;
        const float TotalW   = SlotW * MaxSlots + SlotGap * (MaxSlots - 1);

        // 루트 패널 (하단 중앙)
        var rootGO = new GameObject("SkillBarRoot");
        rootGO.transform.SetParent(canvasParent, false);
        var rootRt = rootGO.AddComponent<RectTransform>();
        rootRt.anchorMin        = new Vector2(0.5f, 0f);
        rootRt.anchorMax        = new Vector2(0.5f, 0f);
        rootRt.pivot            = new Vector2(0.5f, 0f);
        rootRt.sizeDelta        = new Vector2(TotalW, SlotH);
        rootRt.anchoredPosition = new Vector2(0f, 16f);

        var slotRoots     = new GameObject[MaxSlots];
        var keyLabels     = new TMP_Text[MaxSlots];
        var nameTexts     = new TMP_Text[MaxSlots];
        var cdOverlays    = new Image[MaxSlots];
        var slotBgs       = new Image[MaxSlots];

        string[] keys = { "U", "I", "O", "H", "J", "K" };

        for (int i = 0; i < MaxSlots; i++)
        {
            float xOff = (SlotW + SlotGap) * i;

            // ── 슬롯 루트 ──
            var slotGO = new GameObject($"SkillSlot_{keys[i]}");
            slotGO.transform.SetParent(rootGO.transform, false);
            var slotRt = slotGO.AddComponent<RectTransform>();
            slotRt.anchorMin        = Vector2.zero;
            slotRt.anchorMax        = Vector2.zero;
            slotRt.pivot            = Vector2.zero;
            slotRt.sizeDelta        = new Vector2(SlotW, SlotH);
            slotRt.anchoredPosition = new Vector2(xOff, 0f);
            slotRoots[i] = slotGO;

            // ── 배경 ──
            var bg = slotGO.AddComponent<Image>();
            bg.color    = new Color(UITheme.PanelBgDark.r, UITheme.PanelBgDark.g,
                                    UITheme.PanelBgDark.b, 0.88f);
            slotBgs[i] = bg;

            // ── 키 라벨 (좌상단) ──
            var keyGO = new GameObject("KeyLabel");
            keyGO.transform.SetParent(slotGO.transform, false);
            var keyRt = keyGO.AddComponent<RectTransform>();
            keyRt.anchorMin        = new Vector2(0f, 1f);
            keyRt.anchorMax        = new Vector2(0f, 1f);
            keyRt.pivot            = new Vector2(0f, 1f);
            keyRt.sizeDelta        = new Vector2(30f, 24f);
            keyRt.anchoredPosition = new Vector2(6f, -6f);
            var keyT = keyGO.AddComponent<TextMeshProUGUI>();
            keyT.text      = keys[i];
            keyT.fontSize  = 16;
            keyT.color     = new Color(1f, 0.9f, 0.3f); // 금색
            keyT.fontStyle = FontStyles.Bold;
            keyT.alignment = TextAlignmentOptions.TopLeft;
            if (font != null) keyT.font = font;
            keyLabels[i] = keyT;

            // ── 스킬 이름 (중앙) ──
            var nameGO = new GameObject("NameText");
            nameGO.transform.SetParent(slotGO.transform, false);
            var nameRt = nameGO.AddComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 0.35f);
            nameRt.anchorMax = new Vector2(1f, 0.75f);
            nameRt.offsetMin = new Vector2(4f, 0f);
            nameRt.offsetMax = new Vector2(-4f, 0f);
            var nameT = nameGO.AddComponent<TextMeshProUGUI>();
            nameT.text      = "—";
            nameT.fontSize  = 13;
            nameT.color     = Color.white;
            nameT.alignment = TextAlignmentOptions.Center;
            if (font != null) nameT.font = font;
            nameTexts[i] = nameT;

            // ── 쿨다운 오버레이 (어두운 Fill — Vertical, Bottom→Top) ──
            var cdGO = new GameObject("CooldownOverlay");
            cdGO.transform.SetParent(slotGO.transform, false);
            var cdRt = cdGO.AddComponent<RectTransform>();
            cdRt.anchorMin = Vector2.zero;
            cdRt.anchorMax = Vector2.one;
            cdRt.offsetMin = Vector2.zero;
            cdRt.offsetMax = Vector2.zero;
            var cdImg = cdGO.AddComponent<Image>();
            cdImg.color      = new Color(0f, 0f, 0f, 0.65f);
            cdImg.type       = Image.Type.Filled;
            cdImg.fillMethod = Image.FillMethod.Vertical;
            cdImg.fillOrigin = (int)Image.OriginVertical.Bottom;
            cdImg.fillAmount = 0f; // 초기엔 쿨다운 없음
            cdOverlays[i] = cdImg;
        }

        // 스킬 비활성화 중 — skillsEnabled 플래그가 false인 동안 UI도 숨김 (파일·컴포넌트는 보존)
        rootGO.SetActive(false);

        // SkillBarHud 컴포넌트 + 직렬화
        var hud   = canvasGO.AddComponent<SkillBarHud>();
        var hudSO = new SerializedObject(hud);
        SetObjArray(hudSO, "_slotRoots",      slotRoots);
        SetObjArray(hudSO, "_slotKeyLabels",  keyLabels);
        SetObjArray(hudSO, "_slotNameTexts",  nameTexts);
        SetObjArray(hudSO, "_slotCdOverlays", cdOverlays);
        SetObjArray(hudSO, "_slotBgs",        slotBgs);
        hudSO.ApplyModifiedProperties();
    }

    // ── 적 스폰 ────────────────────────────────────────────────────────────

    static void SpawnEnemies(Transform playerTransform)
    {
        // 과목별 1기씩 + Physics 1기 추가 (약점 크리티컬 확인용)
        // Physics 약점 × 2 (플레이어 속성=Physics → 크리 vs 비크리 비교 가능)
        var specs = new (Continent weakness, float radius, float angle)[]
        {
            (Continent.Physics,   7f,   0f),   // 정면 — 크리티컬 확인용
            (Continent.Chemistry, 8f,  60f),
            (Continent.Biology,   9f, 130f),
            (Continent.EarthSci,  7f, 200f),
            (Continent.Math,      8f, 280f),
        };

        foreach (var (weakness, radius, angleDeg) in specs)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            var pos = playerTransform.position
                    + new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * radius;

            var go = new GameObject($"Enemy_{weakness}");
            go.transform.position = pos;
            var enemy = go.AddComponent<Enemy>();

            // 인스펙터 기본값은 Enemy.cs 직렬화 기본값(maxHp=60, def=4, expReward=40)과 동일
            // weakness 필드를 SerializedObject로 설정
            var so = new SerializedObject(enemy);
            so.FindProperty("weakness").enumValueIndex = (int)weakness;
            so.ApplyModifiedProperties();
        }
    }
}
