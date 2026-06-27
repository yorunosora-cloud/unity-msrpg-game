using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem.UI;

/// <summary>
/// 아틀란티스(수학 대륙) 씬 빌더 — MesoriaSetup 패턴 미러
/// MSRPG > Setup Atlantis Scene 메뉴로 실행.
/// 씬 저장 위치: Assets/_Game/Scenes/Atlantis.unity
/// 플레이어 스폰: 유클리드 평원 섬 위 (-150, -17.5, -85)
/// </summary>
public static class AtlantisSetup
{
    const string FONT_SDF_PATH = "Assets/_Game/Art/Fonts/malgun SDF.asset";
    const string PP_ASSET_PATH = "Assets/_Game/Data/PostProcessProfile.asset";

    [MenuItem("MSRPG/Setup Atlantis Scene")]
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
        sunGO.transform.rotation = Quaternion.Euler(-42f, 25f, 0f);

        // 3. 게임 부트스트랩
        var bootstrap = new GameObject("GameBootstrap");
        bootstrap.AddComponent<GameBootstrap>();

        // 4. 플레이어 — 유클리드 평원 섬 위 스폰 (섬 root(-1000,0,-600), topY=−20)
        var player = new GameObject("Player");
        player.tag = "Player";
        // 다리 입구 바로 안쪽 (Euclid root(-1000,0,-600), 아크시움 방향 북동 모서리)
        // 월드 (-720, -19, -440) = 섬 local (280, -19.5, 160), 다리 끝점 (-700,-19.7,-420) 근처
        player.transform.position = new Vector3(-720f, -19f, -440f);
        var cc = player.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 1f, 0f);
        cc.radius = 0.3f;
        cc.height = 2f;
        player.AddComponent<PlayerController>();
        player.AddComponent<BoxCharacterBuilder>();
        player.AddComponent<PlayerCombat>();
        player.AddComponent<PlayerSkills>();
        player.AddComponent<PartyController>();
        player.AddComponent<PlayerDeath>();
        player.AddComponent<PlayerInteractor>();
        player.AddComponent<FallRecovery>();
        player.AddComponent<SpawnManager>();
        player.AddComponent<FogOfWar>();  // 지도 안개 — 아틀란티스 스케일 설정은 BuildAtlantisMetaCanvas

        // FallRecovery — Atlantis 섬들은 Y=-20 ~ +30 범위이므로 임계값을 충분히 낮게 설정
        // 이 값 이하로 내려가야만 낙사 판정 (다리 위에서 오작동 방지)
        var fr   = player.GetComponent<FallRecovery>();
        var frSO = new SerializedObject(fr);
        frSO.FindProperty("fallThreshold").floatValue = -200f;
        frSO.FindProperty("maxAirTime").floatValue    = 6f;
        frSO.ApplyModifiedProperties();

        // 5. 카메라
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        camGO.AddComponent<Camera>();
        camGO.AddComponent<AudioListener>();
        var followCam = camGO.AddComponent<FollowCamera>();
        var camSO = new SerializedObject(followCam);
        camSO.FindProperty("target").objectReferenceValue = player.transform;
        camSO.ApplyModifiedProperties();

        // 6. EventSystem
        var eventGO = new GameObject("EventSystem");
        eventGO.AddComponent<EventSystem>();
        eventGO.AddComponent<InputSystemUIInputModule>();

        // 7. UI Canvas
        var canvasGO = new GameObject("UI Canvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // 7-a. 계정 패널
        var openBtn = UIKit.Button(canvasGO.transform, "OpenAccountButton", "계정",
            UIKit.BtnKind.Neutral, size: new Vector2(140f, 60f), fontSize: UITheme.FontH2);
        { var r = openBtn.GetComponent<RectTransform>();
          r.anchorMin = r.anchorMax = new Vector2(1f, 1f);
          r.pivot = new Vector2(1f, 1f);
          r.anchoredPosition = new Vector2(-20f, -20f); }

        var panelGO = UIKit.Panel(canvasGO.transform, "AccountPanel", new Vector2(600f, 500f));
        UIKit.Label(panelGO.transform, "Title", "계정 정보", UIKit.TextLevel.H1, new Vector2(0, 180));
        UIKit.Divider(panelGO.transform, new Vector2(0, 135), 520f);
        var userText  = UIKit.Label(panelGO.transform, "UsernameText", "아이디: —",
            UIKit.TextLevel.H2, new Vector2(0, 70), align: TextAlignmentOptions.MidlineLeft);
        var levelText = UIKit.Label(panelGO.transform, "LevelText", "레벨: —",
            UIKit.TextLevel.H2, new Vector2(0, 10), align: TextAlignmentOptions.MidlineLeft);
        var logoutBtn = UIKit.Button(panelGO.transform, "LogoutButton", "로그아웃",
            UIKit.BtnKind.Danger, new Vector2(0, -100), new Vector2(380f, 70f));
        var closeBtn  = UIKit.Button(panelGO.transform, "CloseButton", "✕",
            UIKit.BtnKind.Neutral, size: new Vector2(60f, 60f), fontSize: 24);
        { var r = closeBtn.GetComponent<RectTransform>();
          r.anchorMin = r.anchorMax = new Vector2(1f, 1f);
          r.pivot = new Vector2(1f, 1f);
          r.anchoredPosition = new Vector2(-10f, -10f); }
        panelGO.SetActive(false);

        var ap   = canvasGO.AddComponent<AccountPanel>();
        var apSO = new SerializedObject(ap);
        apSO.FindProperty("panel").objectReferenceValue        = panelGO;
        apSO.FindProperty("usernameText").objectReferenceValue = userText.GetComponent<TextMeshProUGUI>();
        apSO.FindProperty("levelText").objectReferenceValue    = levelText.GetComponent<TextMeshProUGUI>();
        apSO.FindProperty("openButton").objectReferenceValue   = openBtn.GetComponent<Button>();
        apSO.FindProperty("closeButton").objectReferenceValue  = closeBtn.GetComponent<Button>();
        apSO.FindProperty("logoutButton").objectReferenceValue = logoutBtn.GetComponent<Button>();
        apSO.ApplyModifiedProperties();

        // 7-b. 상호작용 프롬프트 라벨
        var promptLabelGO = new GameObject("InteractPromptLabel");
        promptLabelGO.transform.SetParent(canvasGO.transform, false);
        var promptRt = promptLabelGO.AddComponent<RectTransform>();
        promptRt.anchorMin        = new Vector2(0.5f, 0f);
        promptRt.anchorMax        = new Vector2(0.5f, 0f);
        promptRt.pivot            = new Vector2(0.5f, 0f);
        promptRt.sizeDelta        = new Vector2(420f, 55f);
        promptRt.anchoredPosition = new Vector2(0f, 145f);
        var promptTmp = promptLabelGO.AddComponent<TextMeshProUGUI>();
        promptTmp.text      = "E: 상호작용";
        promptTmp.fontSize  = 24;
        promptTmp.color     = Color.white;
        promptTmp.alignment = TextAlignmentOptions.Center;
        promptTmp.fontStyle = FontStyles.Bold;
        if (korFont != null) promptTmp.font = korFont;
        promptLabelGO.SetActive(false);

        var interactor   = player.GetComponent<PlayerInteractor>();
        var interactorSO = new SerializedObject(interactor);
        interactorSO.FindProperty("promptLabel").objectReferenceValue = promptTmp;
        interactorSO.ApplyModifiedProperties();

        // 8. 전투 HUD
        var combatHudRefs = CreateCombatHudPanel(canvasGO.transform, korFont);
        var chud   = canvasGO.AddComponent<CombatHud>();
        var chudSO = new SerializedObject(chud);
        chudSO.FindProperty("hpFill").objectReferenceValue = combatHudRefs.hpFill;
        chudSO.FindProperty("mpFill").objectReferenceValue = combatHudRefs.mpFill;
        chudSO.FindProperty("hpText").objectReferenceValue = combatHudRefs.hpText;
        chudSO.FindProperty("mpText").objectReferenceValue = combatHudRefs.mpText;
        chudSO.ApplyModifiedProperties();

        // 9. 사망 오버레이
        var (deathPanel, countdown) = CreateDeathOverlay(canvasGO.transform, korFont);
        var pd   = player.GetComponent<PlayerDeath>();
        var pdSO = new SerializedObject(pd);
        pdSO.FindProperty("deathOverlay").objectReferenceValue  = deathPanel;
        pdSO.FindProperty("countdownText").objectReferenceValue = countdown;
        pdSO.ApplyModifiedProperties();

        // 10. 아틀란티스 허브 환경 생성
        AtlantisHubBuilder.Build();

        // 11. 파티 HUD
        CreatePartyHud(canvasGO.transform, korFont);

        // 12. 스킬 바 HUD
        CreateSkillBarHud(canvasGO.transform, canvasGO, korFont);

        // 12-a. 아틀란티스 지도 MetaCanvas (미니맵 + 월드맵, M키)
        BuildAtlantisMetaCanvas(player, korFont);

        // 13. 포스트프로세싱 (기존 프로파일 재사용, 없으면 신규 생성)
        VolumeProfile ppProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PP_ASSET_PATH);
        if (ppProfile == null)
        {
            ppProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            System.IO.Directory.CreateDirectory(Application.dataPath + "/_Game/Data");
            AssetDatabase.CreateAsset(ppProfile, PP_ASSET_PATH);

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
        }

        var ppGO     = new GameObject("Post Process Volume");
        var vol      = ppGO.AddComponent<Volume>();
        vol.isGlobal      = true;
        vol.priority      = 1;
        vol.sharedProfile = ppProfile;

        // 14. 씬 저장
        System.IO.Directory.CreateDirectory(Application.dataPath + "/_Game/Scenes");
        const string SCENE_PATH = "Assets/_Game/Scenes/Atlantis.unity";
        EditorSceneManager.SaveScene(scene, SCENE_PATH);

        // 15. EditorBuildSettings에 씬 추가
        AddSceneToBuildSettings(SCENE_PATH);

        AssetDatabase.Refresh();
        Debug.Log("[MSRPG] ✅ Atlantis 씬 생성 완료! 유클리드 평원 스폰 → 가지 다리 → 아크시움 탐험. 세계수 뿌리 아래 다크엘프 구역 확인.");
    }

    static void AddSceneToBuildSettings(string scenePath)
    {
        var scenes = EditorBuildSettings.scenes;
        foreach (var s in scenes)
            if (s.path == scenePath) return; // 이미 있음

        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes)
        {
            new EditorBuildSettingsScene(scenePath, true)
        };
        EditorBuildSettings.scenes = list.ToArray();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 헬퍼 (MesoriaSetup 동일 구조)
    // ─────────────────────────────────────────────────────────────────────────

    static void EnsureTmpDefaultFont(TMP_FontAsset font)
    {
        if (font == null) return;
        var settings = Resources.Load<TMPro.TMP_Settings>("TMP Settings");
        if (settings == null) return;
        var so   = new SerializedObject(settings);
        var prop = so.FindProperty("m_defaultFontAsset");
        if (prop != null && prop.objectReferenceValue == null)
        {
            prop.objectReferenceValue = font;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    struct CombatHudRefs
    {
        public Image    hpFill, mpFill;
        public TMP_Text hpText, mpText;
    }

    static CombatHudRefs CreateCombatHudPanel(Transform canvasParent, TMP_FontAsset font)
    {
        var panelGO = new GameObject("CombatHudPanel");
        panelGO.transform.SetParent(canvasParent, false);
        var rt = panelGO.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot            = Vector2.zero;
        rt.sizeDelta        = new Vector2(340f, 110f);
        rt.anchoredPosition = new Vector2(20f, 20f);
        var bg = panelGO.AddComponent<Image>();
        bg.color = new Color(UITheme.PanelBgDark.r, UITheme.PanelBgDark.g, UITheme.PanelBgDark.b, 0.88f);

        Image    hpFill = CreateBar(panelGO.transform, "HP", new Vector2(16f, 70f), new Color(0.15f, 0.85f, 0.3f));
        Image    mpFill = CreateBar(panelGO.transform, "MP", new Vector2(16f, 36f), new Color(0.2f, 0.5f, 1f));
        TMP_Text hpTxt  = CreateBarLabel(panelGO.transform, "HPText", "HP —/—", new Vector2(16f, 70f), font);
        TMP_Text mpTxt  = CreateBarLabel(panelGO.transform, "MPText", "MP —/—", new Vector2(16f, 36f), font);

        return new CombatHudRefs { hpFill = hpFill, mpFill = mpFill, hpText = hpTxt, mpText = mpTxt };
    }

    static Image CreateBar(Transform parent, string name, Vector2 pos, Color color)
    {
        var borderGO = new GameObject(name + "Border");
        borderGO.transform.SetParent(parent, false);
        var borderRt = borderGO.AddComponent<RectTransform>();
        borderRt.anchorMin = borderRt.anchorMax = Vector2.zero;
        borderRt.pivot            = new Vector2(0f, 0.5f);
        borderRt.sizeDelta        = new Vector2(310f, 24f);
        borderRt.anchoredPosition = new Vector2(pos.x - 1f, pos.y);
        borderGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.65f);

        var bgGO = new GameObject(name + "BG");
        bgGO.transform.SetParent(parent, false);
        var bgRt = bgGO.AddComponent<RectTransform>();
        bgRt.anchorMin = bgRt.anchorMax = Vector2.zero;
        bgRt.pivot            = new Vector2(0f, 0.5f);
        bgRt.sizeDelta        = new Vector2(308f, 22f);
        bgRt.anchoredPosition = pos;
        bgGO.AddComponent<Image>().color = new Color(UITheme.PanelBgDark.r, UITheme.PanelBgDark.g, UITheme.PanelBgDark.b, 0.9f);

        var fillGO  = new GameObject(name + "Fill");
        fillGO.transform.SetParent(bgGO.transform, false);
        var fillRt  = fillGO.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one; fillRt.sizeDelta = Vector2.zero;
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.color = color; fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal; fillImg.fillAmount = 1f;
        return fillImg;
    }

    static TMP_Text CreateBarLabel(Transform parent, string name, string text, Vector2 pos, TMP_FontAsset font)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot            = new Vector2(0f, 0.5f);
        rt.sizeDelta        = new Vector2(308f, 22f);
        rt.anchoredPosition = pos;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = 14; t.color = Color.white;
        t.alignment = TextAlignmentOptions.MidlineLeft;
        if (font != null) t.font = font;
        return t;
    }

    static (GameObject panel, TMP_Text countdown) CreateDeathOverlay(Transform canvasParent, TMP_FontAsset font)
    {
        var panelGO = new GameObject("DeathOverlay");
        panelGO.transform.SetParent(canvasParent, false);
        var rt = panelGO.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        panelGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.88f);

        CreateOverlayLabel(panelGO.transform, "DeathTitle",  "사망",              120, new Vector2(0f,  100f), new Color(0.9f,0.1f,0.1f), font);
        var cdLabel = CreateOverlayLabel(panelGO.transform, "Countdown",         "3",   96, new Vector2(0f,  -20f), Color.white,             font);
        CreateOverlayLabel(panelGO.transform,               "HintText", "잠시 후 부활합니다...", 28, new Vector2(0f, -130f), new Color(1f,1f,1f,0.65f), font);

        panelGO.SetActive(false);
        return (panelGO, cdLabel.GetComponent<TMP_Text>());
    }

    static GameObject CreateOverlayLabel(Transform parent, string name, string text,
        int fontSize, Vector2 pos, Color color, TMP_FontAsset font)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(700f, 160f);
        rt.anchoredPosition = pos;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = fontSize; t.color = color;
        t.alignment = TextAlignmentOptions.Center; t.fontStyle = FontStyles.Bold;
        if (font != null) t.font = font;
        return go;
    }

    static void CreatePartyHud(Transform canvasParent, TMP_FontAsset font)
    {
        const int   SlotCount = 3;
        const float SlotW     = 130f, SlotH = 190f, SlotGap = 6f;

        var rootGO = new GameObject("PartyHudRoot");
        rootGO.transform.SetParent(canvasParent, false);
        var rootRt = rootGO.AddComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(1f, 0f); rootRt.anchorMax = new Vector2(1f, 0f);
        rootRt.pivot     = new Vector2(1f, 0f);
        rootRt.sizeDelta        = new Vector2(SlotW * SlotCount + SlotGap * (SlotCount - 1), SlotH);
        rootRt.anchoredPosition = new Vector2(-16f, 16f);

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
            var slotGO = new GameObject($"Slot_{i + 1}");
            slotGO.transform.SetParent(rootGO.transform, false);
            var slotRt = slotGO.AddComponent<RectTransform>();
            slotRt.anchorMin = slotRt.anchorMax = Vector2.zero; slotRt.pivot = Vector2.zero;
            slotRt.sizeDelta = new Vector2(SlotW, SlotH); slotRt.anchoredPosition = new Vector2(xOff, 0f);
            var bgImg = slotGO.AddComponent<Image>();
            bgImg.color = new Color(UITheme.PanelBgMid.r, UITheme.PanelBgMid.g, UITheme.PanelBgMid.b, 0.93f);
            bgs[i] = bgImg;

            var borderGO = new GameObject("ActiveBorder"); borderGO.transform.SetParent(slotGO.transform, false);
            var borderRt = borderGO.AddComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero; borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = new Vector2(-4f,-4f); borderRt.offsetMax = new Vector2(4f,4f);
            var bImg = borderGO.AddComponent<Image>(); bImg.color = new Color(1f, 0.85f, 0.1f); bImg.enabled = false;
            borders[i] = bImg;

            var downGO = new GameObject("DownedOverlay"); downGO.transform.SetParent(slotGO.transform, false);
            var downRt = downGO.AddComponent<RectTransform>();
            downRt.anchorMin = Vector2.zero; downRt.anchorMax = Vector2.one;
            downRt.offsetMin = downRt.offsetMax = Vector2.zero;
            var downImg = downGO.AddComponent<Image>(); downImg.color = new Color(0f,0f,0f,0.55f); downImg.enabled = false;
            downedOverlays[i] = downImg;

            statusTexts[i] = CreateSlotLabel(slotGO.transform, "StatusText", "대기", 13, font,
                new Vector2(0f,0.86f), new Vector2(1f,1.00f), new Vector2(5f,-2f), true,  TextAlignmentOptions.TopLeft);
            nameTexts[i]   = CreateSlotLabel(slotGO.transform, "NameText",   "—",   17, font,
                new Vector2(0f,0.64f), new Vector2(1f,0.86f), new Vector2(5f, 0f), true,  TextAlignmentOptions.MidlineLeft);
            levelTexts[i]  = CreateSlotLabel(slotGO.transform, "LevelText",  "Lv.1",13, font,
                new Vector2(0f,0.50f), new Vector2(1f,0.64f), new Vector2(5f, 0f), false, TextAlignmentOptions.MidlineLeft);

            (hpFills[i], hpTexts[i]) = CreateStatBar(slotGO.transform, "HP", new Color(0.15f,0.85f,0.3f),
                new Vector2(0f,0.30f), new Vector2(1f,0.48f), font, 5f);
            (mpFills[i], mpTexts[i]) = CreateStatBar(slotGO.transform, "MP", new Color(0.2f,0.5f,1.0f),
                new Vector2(0f,0.10f), new Vector2(1f,0.28f), font, 5f);
        }

        var hud   = canvasParent.gameObject.AddComponent<PartyHud>();
        var hudSO = new SerializedObject(hud);
        SetObjArray(hudSO, "_slotBgs",           bgs);
        SetObjArray(hudSO, "_slotBorders",        borders);
        SetObjArray(hudSO, "_slotDownedOverlays", downedOverlays);
        SetObjArray(hudSO, "_slotStatusTexts",    statusTexts);
        SetObjArray(hudSO, "_slotNameTexts",      nameTexts);
        SetObjArray(hudSO, "_slotLevelTexts",     levelTexts);
        SetObjArray(hudSO, "_slotHpFills",        hpFills);
        SetObjArray(hudSO, "_slotHpTexts",        hpTexts);
        SetObjArray(hudSO, "_slotMpFills",        mpFills);
        SetObjArray(hudSO, "_slotMpTexts",        mpTexts);
        hudSO.ApplyModifiedProperties();
    }

    static void CreateSkillBarHud(Transform canvasParent, GameObject canvasGO, TMP_FontAsset font)
    {
        const int   MaxSlots = 6;
        const float SlotW = 95f, SlotH = 115f, SlotGap = 6f;
        float totalW = SlotW * MaxSlots + SlotGap * (MaxSlots - 1);

        var rootGO = new GameObject("SkillBarRoot");
        rootGO.transform.SetParent(canvasParent, false);
        var rootRt = rootGO.AddComponent<RectTransform>();
        rootRt.anchorMin = rootRt.anchorMax = new Vector2(0.5f, 0f);
        rootRt.pivot            = new Vector2(0.5f, 0f);
        rootRt.sizeDelta        = new Vector2(totalW, SlotH);
        rootRt.anchoredPosition = new Vector2(0f, 16f);

        var slotRoots  = new GameObject[MaxSlots];
        var keyLabels  = new TMP_Text[MaxSlots];
        var nameTexts  = new TMP_Text[MaxSlots];
        var cdOverlays = new Image[MaxSlots];
        var slotBgs    = new Image[MaxSlots];
        string[] keys  = { "U", "I", "O", "H", "J", "K" };

        for (int i = 0; i < MaxSlots; i++)
        {
            float xOff = (SlotW + SlotGap) * i;
            var slotGO = new GameObject($"SkillSlot_{keys[i]}");
            slotGO.transform.SetParent(rootGO.transform, false);
            var slotRt = slotGO.AddComponent<RectTransform>();
            slotRt.anchorMin = slotRt.anchorMax = slotRt.pivot = Vector2.zero;
            slotRt.sizeDelta = new Vector2(SlotW, SlotH); slotRt.anchoredPosition = new Vector2(xOff, 0f);
            slotRoots[i] = slotGO;
            var bg = slotGO.AddComponent<Image>();
            bg.color    = new Color(UITheme.PanelBgDark.r, UITheme.PanelBgDark.g, UITheme.PanelBgDark.b, 0.88f);
            slotBgs[i] = bg;

            var keyGO = new GameObject("KeyLabel"); keyGO.transform.SetParent(slotGO.transform, false);
            var keyRt = keyGO.AddComponent<RectTransform>();
            keyRt.anchorMin = keyRt.anchorMax = new Vector2(0f,1f); keyRt.pivot = new Vector2(0f,1f);
            keyRt.sizeDelta = new Vector2(30f,24f); keyRt.anchoredPosition = new Vector2(6f,-6f);
            var keyT = keyGO.AddComponent<TextMeshProUGUI>();
            keyT.text = keys[i]; keyT.fontSize = 16; keyT.color = new Color(1f,0.9f,0.3f);
            keyT.fontStyle = FontStyles.Bold; keyT.alignment = TextAlignmentOptions.TopLeft;
            if (font != null) keyT.font = font;
            keyLabels[i] = keyT;

            var nameGO = new GameObject("NameText"); nameGO.transform.SetParent(slotGO.transform, false);
            var nameRt = nameGO.AddComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f,0.35f); nameRt.anchorMax = new Vector2(1f,0.75f);
            nameRt.offsetMin = new Vector2(4f,0f); nameRt.offsetMax = new Vector2(-4f,0f);
            var nameT = nameGO.AddComponent<TextMeshProUGUI>();
            nameT.text = "—"; nameT.fontSize = 13; nameT.color = Color.white;
            nameT.alignment = TextAlignmentOptions.Center;
            if (font != null) nameT.font = font;
            nameTexts[i] = nameT;

            var cdGO = new GameObject("CooldownOverlay"); cdGO.transform.SetParent(slotGO.transform, false);
            var cdRt = cdGO.AddComponent<RectTransform>();
            cdRt.anchorMin = Vector2.zero; cdRt.anchorMax = Vector2.one;
            cdRt.offsetMin = cdRt.offsetMax = Vector2.zero;
            var cdImg = cdGO.AddComponent<Image>();
            cdImg.color = new Color(0f,0f,0f,0.65f); cdImg.type = Image.Type.Filled;
            cdImg.fillMethod = Image.FillMethod.Vertical; cdImg.fillOrigin = (int)Image.OriginVertical.Bottom;
            cdImg.fillAmount = 0f;
            cdOverlays[i] = cdImg;
        }

        rootGO.SetActive(false);

        var hud   = canvasGO.AddComponent<SkillBarHud>();
        var hudSO = new SerializedObject(hud);
        SetObjArray(hudSO, "_slotRoots",      slotRoots);
        SetObjArray(hudSO, "_slotKeyLabels",  keyLabels);
        SetObjArray(hudSO, "_slotNameTexts",  nameTexts);
        SetObjArray(hudSO, "_slotCdOverlays", cdOverlays);
        SetObjArray(hudSO, "_slotBgs",        slotBgs);
        hudSO.ApplyModifiedProperties();
    }

    static void SetObjArray(SerializedObject so, string propName, Object[] objs)
    {
        var arr = so.FindProperty(propName);
        if (arr == null) { Debug.LogError($"[AtlantisSetup] 프로퍼티 '{propName}'를 찾을 수 없습니다."); return; }
        arr.arraySize = objs.Length;
        for (int i = 0; i < objs.Length; i++)
            arr.GetArrayElementAtIndex(i).objectReferenceValue = objs[i];
    }

    static TMP_Text CreateSlotLabel(Transform parent, string name, string text,
        int fontSize, TMP_FontAsset font,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offset, bool bold, TextAlignmentOptions align)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = new Vector2( offset.x, 0f); rt.offsetMax = new Vector2(-offset.x, 0f);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = fontSize; t.color = Color.white; t.alignment = align;
        if (bold) t.fontStyle = FontStyles.Bold;
        if (font != null) t.font = font;
        return t;
    }

    static (Image fill, TMP_Text label) CreateStatBar(Transform parent, string name,
        Color fillColor, Vector2 anchorMin, Vector2 anchorMax, TMP_FontAsset font, float padX)
    {
        float bgMaxY = anchorMin.y + (anchorMax.y - anchorMin.y) * 0.55f;

        var borderGO = new GameObject(name + "Border"); borderGO.transform.SetParent(parent, false);
        var borderRt = borderGO.AddComponent<RectTransform>();
        borderRt.anchorMin = anchorMin; borderRt.anchorMax = new Vector2(anchorMax.x, bgMaxY);
        borderRt.offsetMin = new Vector2(padX-1f,-1f); borderRt.offsetMax = new Vector2(-padX+1f,1f);
        borderGO.AddComponent<Image>().color = new Color(1f,1f,1f,0.65f);

        var bgGO = new GameObject(name + "BG"); bgGO.transform.SetParent(parent, false);
        var bgRt = bgGO.AddComponent<RectTransform>();
        bgRt.anchorMin = anchorMin; bgRt.anchorMax = new Vector2(anchorMax.x, bgMaxY);
        bgRt.offsetMin = new Vector2(padX,0f); bgRt.offsetMax = new Vector2(-padX,0f);
        bgGO.AddComponent<Image>().color = new Color(0.08f,0.08f,0.08f,0.85f);

        var fillGO = new GameObject(name + "Fill"); fillGO.transform.SetParent(bgGO.transform, false);
        var fillRt = fillGO.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one; fillRt.sizeDelta = Vector2.zero;
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.color = fillColor; fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal; fillImg.fillAmount = 1f;

        var txtGO = new GameObject(name + "Text"); txtGO.transform.SetParent(parent, false);
        var txtRt = txtGO.AddComponent<RectTransform>();
        txtRt.anchorMin = new Vector2(anchorMin.x, anchorMin.y + (anchorMax.y - anchorMin.y) * 0.55f);
        txtRt.anchorMax = anchorMax; txtRt.offsetMin = new Vector2(padX,0f); txtRt.offsetMax = new Vector2(-padX,0f);
        var t = txtGO.AddComponent<TextMeshProUGUI>();
        t.text = "—/—"; t.fontSize = 11; t.color = new Color(0.9f,0.9f,0.9f);
        t.alignment = TextAlignmentOptions.MidlineLeft;
        if (font != null) t.font = font;

        return (fillImg, t);
    }

    // ── 아틀란티스 지도 MetaCanvas ────────────────────────────────────────────
    // FogOfWar mapHalf=1600 (아크시움 r=250, 유클리드 center=-1000,-600 r=350 포함)
    // viewRange=150  →  미니맵과 안개 반경 일치
    static void BuildAtlantisMetaCanvas(GameObject player, TMP_FontAsset font)
    {
        const float MAP_HALF    = 1600f;
        const float VIEW_RANGE  = 150f;

        // FogOfWar 파라미터 설정
        var fog   = player.GetComponent<FogOfWar>();
        var fogSo = new SerializedObject(fog);
        fogSo.FindProperty("mapHalf").floatValue   = MAP_HALF;
        fogSo.FindProperty("viewRange").floatValue = VIEW_RANGE;
        fogSo.ApplyModifiedProperties();

        // MetaCanvas (sortingOrder=10)
        var canvasGO = new GameObject("MetaCanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        if (canvasGO.GetComponentInChildren<BossHealthBar>() == null)
            BossHealthBar.BuildAndAttach(canvasGO.transform);

        var controller = canvasGO.AddComponent<MetaPanelController>();

        // ── 미니맵 ──────────────────────────────────────────────────────────
        var mmContGO = new GameObject("MinimapContainer");
        mmContGO.transform.SetParent(canvasGO.transform, false);
        var mmContRt = mmContGO.AddComponent<RectTransform>();
        mmContRt.anchorMin        = new Vector2(1f, 1f);
        mmContRt.anchorMax        = new Vector2(1f, 1f);
        mmContRt.pivot            = new Vector2(1f, 1f);
        mmContRt.sizeDelta        = new Vector2(180f, 180f);
        mmContRt.anchoredPosition = new Vector2(-20f, -20f);
        mmContGO.AddComponent<Image>().color = new Color(0.04f, 0.06f, 0.14f, 1f); // 아틀란티스 어두운 테두리

        var mmBgGO = new GameObject("MinimapBg");
        mmBgGO.transform.SetParent(mmContGO.transform, false);
        var mmBgRt = mmBgGO.AddComponent<RectTransform>();
        mmBgRt.anchorMin = Vector2.zero; mmBgRt.anchorMax = Vector2.one;
        mmBgRt.offsetMin = new Vector2(3f, 3f); mmBgRt.offsetMax = new Vector2(-3f, -3f);
        mmBgGO.AddComponent<Image>().color = new Color(0.12f, 0.16f, 0.26f, 1f); // 심해 느낌

        var mmIconParentGO = new GameObject("IconParent");
        mmIconParentGO.transform.SetParent(mmContGO.transform, false);
        var mmIconParentRt = mmIconParentGO.AddComponent<RectTransform>();
        mmIconParentRt.anchorMin = Vector2.zero; mmIconParentRt.anchorMax = Vector2.one;
        mmIconParentRt.sizeDelta = Vector2.zero;

        var mmDotGO = new GameObject("PlayerDot");
        mmDotGO.transform.SetParent(mmContGO.transform, false);
        var mmDotRt = mmDotGO.AddComponent<RectTransform>();
        mmDotRt.anchorMin        = new Vector2(0.5f, 0.5f);
        mmDotRt.anchorMax        = new Vector2(0.5f, 0.5f);
        mmDotRt.sizeDelta        = new Vector2(8f, 8f);
        mmDotRt.anchoredPosition = Vector2.zero;
        mmDotGO.AddComponent<Image>().color = new Color(0.78f, 0.82f, 0.00f); // MathGold

        var minimapHud = mmContGO.AddComponent<MinimapHud>();
        var mmHudSo    = new SerializedObject(minimapHud);
        mmHudSo.FindProperty("iconParent").objectReferenceValue = mmIconParentGO.GetComponent<RectTransform>();
        mmHudSo.FindProperty("playerDot").objectReferenceValue  = mmDotGO.GetComponent<RectTransform>();
        mmHudSo.FindProperty("mapSize").floatValue              = 160f;
        mmHudSo.FindProperty("viewRange").floatValue            = VIEW_RANGE;
        mmHudSo.ApplyModifiedProperties();

        // ── 월드맵 패널 (전체화면, M키) ──────────────────────────────────────
        var panelGO = new GameObject("WorldMapPanel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        var panelRt = panelGO.AddComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero; panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero; panelRt.offsetMax = Vector2.zero;
        panelGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);

        var worldMap = panelGO.AddComponent<WorldMapPanel>();

        var frameGO = new GameObject("MapFrame");
        frameGO.transform.SetParent(panelGO.transform, false);
        var frameRt = frameGO.AddComponent<RectTransform>();
        frameRt.anchorMin        = new Vector2(0.5f, 0.5f);
        frameRt.anchorMax        = new Vector2(0.5f, 0.5f);
        frameRt.sizeDelta        = new Vector2(750f, 750f);
        frameRt.anchoredPosition = Vector2.zero;
        frameGO.AddComponent<Image>().color = new Color(0.06f, 0.08f, 0.18f, 1f); // 아틀란티스 테두리

        var mapBgGO = new GameObject("MapBg");
        mapBgGO.transform.SetParent(frameGO.transform, false);
        var mapBgRt = mapBgGO.AddComponent<RectTransform>();
        mapBgRt.anchorMin = Vector2.zero; mapBgRt.anchorMax = Vector2.one;
        mapBgRt.offsetMin = Vector2.zero; mapBgRt.offsetMax = Vector2.zero;
        mapBgGO.AddComponent<RawImage>().color = Color.white;

        var fogParentGO = new GameObject("FogGridParent");
        fogParentGO.transform.SetParent(frameGO.transform, false);
        var fogParentRt = fogParentGO.AddComponent<RectTransform>();
        fogParentRt.anchorMin = Vector2.zero; fogParentRt.anchorMax = Vector2.one;
        fogParentRt.offsetMin = Vector2.zero; fogParentRt.offsetMax = Vector2.zero;

        var fogGrid   = new Image[8, 8];
        float cellSize = 750f / 8f;
        for (int x = 0; x < 8; x++)
        for (int y = 0; y < 8; y++)
        {
            var cellGO = new GameObject($"FogCell_{x}_{y}");
            cellGO.transform.SetParent(fogParentGO.transform, false);
            var cellRt = cellGO.AddComponent<RectTransform>();
            cellRt.anchorMin        = Vector2.zero;
            cellRt.anchorMax        = Vector2.zero;
            cellRt.sizeDelta        = new Vector2(cellSize, cellSize);
            cellRt.anchoredPosition = new Vector2((x + 0.5f) * cellSize, (y + 0.5f) * cellSize);
            var cellImg = cellGO.AddComponent<Image>();
            cellImg.color = new Color(0f, 0f, 0f, 0.85f);
            fogGrid[x, y] = cellImg;
        }

        var markerParentGO = new GameObject("MarkerIconParent");
        markerParentGO.transform.SetParent(frameGO.transform, false);
        var markerParentRt = markerParentGO.AddComponent<RectTransform>();
        markerParentRt.anchorMin = Vector2.zero; markerParentRt.anchorMax = Vector2.one;
        markerParentRt.offsetMin = Vector2.zero; markerParentRt.offsetMax = Vector2.zero;

        var playerDotGO = new GameObject("PlayerDotMap");
        playerDotGO.transform.SetParent(frameGO.transform, false);
        var playerDotRt = playerDotGO.AddComponent<RectTransform>();
        playerDotRt.anchorMin        = new Vector2(0.5f, 0.5f);
        playerDotRt.anchorMax        = new Vector2(0.5f, 0.5f);
        playerDotRt.sizeDelta        = new Vector2(10f, 10f);
        playerDotRt.anchoredPosition = Vector2.zero;
        playerDotGO.AddComponent<Image>().color = new Color(0.78f, 0.82f, 0.00f); // MathGold

        var titleGO = new GameObject("TitleLabel");
        titleGO.transform.SetParent(frameGO.transform, false);
        var titleRt = titleGO.AddComponent<RectTransform>();
        titleRt.anchorMin        = new Vector2(0.5f, 0.5f);
        titleRt.anchorMax        = new Vector2(0.5f, 0.5f);
        titleRt.sizeDelta        = new Vector2(700f, 50f);
        titleRt.anchoredPosition = new Vector2(0f, 370f);
        var titleTxt = titleGO.AddComponent<TextMeshProUGUI>();
        titleTxt.text      = "아틀란티스 지도";
        titleTxt.fontSize  = 28;
        titleTxt.color     = new Color(0.78f, 0.82f, 0.00f);
        titleTxt.alignment = TextAlignmentOptions.Center;
        if (font != null) titleTxt.font = font;

        var closeBtnGO = UIKit.Button(panelGO.transform, "CloseBtn", "X",
            UIKit.BtnKind.Danger, new Vector2(-30f, -30f), new Vector2(60f, 60f));
        var closeBtnRt = closeBtnGO.GetComponent<RectTransform>();
        closeBtnRt.anchorMin = new Vector2(1f, 1f);
        closeBtnRt.anchorMax = new Vector2(1f, 1f);
        closeBtnRt.pivot     = new Vector2(1f, 1f);

        // WorldMapPanel 와이어링 (isAtlantis=true, mapHalf=1600)
        var wmSo = new SerializedObject(worldMap);
        wmSo.FindProperty("mapFrame").objectReferenceValue   = frameGO.GetComponent<RectTransform>();
        wmSo.FindProperty("iconParent").objectReferenceValue = markerParentGO.GetComponent<RectTransform>();
        wmSo.FindProperty("playerDot").objectReferenceValue  = playerDotGO.GetComponent<RectTransform>();
        wmSo.FindProperty("tooltip").objectReferenceValue    = titleGO.GetComponent<TMP_Text>();
        wmSo.FindProperty("mapBg").objectReferenceValue      = mapBgGO.GetComponent<RawImage>();
        wmSo.FindProperty("mapFrameSize").floatValue         = 750f;
        wmSo.FindProperty("mapHalf").floatValue              = MAP_HALF;
        wmSo.FindProperty("isAtlantis").boolValue            = true;
        wmSo.ApplyModifiedProperties();

        worldMap.FogGrid = fogGrid;

        UnityEventTools.AddVoidPersistentListener(closeBtnGO.GetComponent<Button>().onClick, worldMap.Close);

        // MetaPanelController — worldMapPanel만 연결 (M키 동작)
        var ctrlSo = new SerializedObject(controller);
        ctrlSo.FindProperty("worldMapPanel").objectReferenceValue = worldMap;
        ctrlSo.ApplyModifiedProperties();

        panelGO.SetActive(false);
        Debug.Log("[AtlantisSetup] AtlantisMetaCanvas 생성 완료 (미니맵 + 아틀란티스 지도)");
    }
}
