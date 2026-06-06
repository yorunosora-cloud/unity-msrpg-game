using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public static class MesoriaSetup
{
    const string FONT_SDF_PATH = "Assets/_Game/Art/Fonts/malgun SDF.asset";

    [MenuItem("MSRPG/Setup Mesoria Scene")]
    public static void Run()
    {
        var korFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_SDF_PATH);

        // 1. 새 빈 씬
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 2. 조명
        var sunGO = new GameObject("Directional Light");
        var light  = sunGO.AddComponent<Light>();
        light.type      = LightType.Directional;
        light.intensity = 1f;
        sunGO.transform.rotation = Quaternion.Euler(-50f, 30f, 0f);

        // 3. 지면 + 격자
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(20f, 1f, 20f);
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(0.3f, 0.6f, 0.2f);
        ground.GetComponent<MeshRenderer>().sharedMaterial = mat;
        ground.AddComponent<GridFloor>();

        // 4. 경계 벽
        CreateWall("Wall_N", new Vector3(   0f, 1.5f,  100f), new Vector3(200f, 3f,   1f));
        CreateWall("Wall_S", new Vector3(   0f, 1.5f, -100f), new Vector3(200f, 3f,   1f));
        CreateWall("Wall_E", new Vector3( 100f, 1.5f,    0f), new Vector3(  1f, 3f, 200f));
        CreateWall("Wall_W", new Vector3(-100f, 1.5f,    0f), new Vector3(  1f, 3f, 200f));

        // 5. 게임 부트스트랩
        var bootstrap = new GameObject("GameBootstrap");
        bootstrap.AddComponent<GameBootstrap>();

        // 6. 플레이어
        var player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = Vector3.zero;
        var cc = player.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 1f, 0f);
        cc.radius = 0.3f;
        cc.height = 2f;
        player.AddComponent<PlayerController>();
        player.AddComponent<BoxCharacterBuilder>();
        player.AddComponent<PlayerCombat>();  // Q키 전투

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
        eventGO.AddComponent<StandaloneInputModule>();

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

        // 9-a. 우상단 열기 버튼
        var openBtn = CreateButton(canvasGO.transform, "OpenAccountButton", "계정", korFont,
            anchor: new Vector2(1f, 1f), pivot: new Vector2(1f, 1f),
            pos: new Vector2(-20f, -20f), size: new Vector2(140f, 60f));

        // 9-b. 계정 패널 (기본 비활성)
        var panelGO = CreatePanel(canvasGO.transform, "AccountPanel",
            size: new Vector2(600f, 500f), pos: Vector2.zero,
            bgColor: new Color(0.1f, 0.1f, 0.15f, 0.97f));

        // 제목
        CreateLabel(panelGO.transform, "Title", "계정 정보", 40, new Vector2(0, 180), korFont);

        // 구분선
        CreateHRule(panelGO.transform, new Vector2(0, 135));

        // 정보 텍스트
        var userText  = CreateLabel(panelGO.transform, "UsernameText", "아이디: —",    30, new Vector2(0, 70),  korFont);
        var levelText = CreateLabel(panelGO.transform, "LevelText",    "레벨: —",      30, new Vector2(0, 10),  korFont);

        // 로그아웃 버튼 (빨강)
        var logoutBtn = CreateButton(panelGO.transform, "LogoutButton", "로그아웃", korFont,
            anchor: new Vector2(0.5f, 0.5f), pivot: new Vector2(0.5f, 0.5f),
            pos: new Vector2(0, -100), size: new Vector2(380f, 70f),
            color: new Color(0.8f, 0.2f, 0.2f));

        // 닫기 버튼 (X, 우상단)
        var closeBtn = CreateButton(panelGO.transform, "CloseButton", "X", korFont,
            anchor: new Vector2(1f, 1f), pivot: new Vector2(1f, 1f),
            pos: new Vector2(-10f, -10f), size: new Vector2(60f, 60f),
            color: new Color(0.3f, 0.3f, 0.35f));

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

        // 11. 적 스폰 (과목별 5기, 반경 6~10 범위)
        SpawnEnemies(player.transform);

        // 12. 씬 저장
        System.IO.Directory.CreateDirectory(Application.dataPath + "/_Game/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/_Game/Scenes/Mesoria.unity");
        AssetDatabase.Refresh();

        Debug.Log("[MSRPG] ✅ Mesoria 씬 생성 완료! Q키로 공격, ESC/C/I 패널 토글.");
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────────

    static void CreateWall(string name, Vector3 pos, Vector3 scale)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.position   = pos;
        wall.transform.localScale = scale;
        Object.DestroyImmediate(wall.GetComponent<MeshRenderer>());
    }

    static GameObject CreatePanel(Transform parent, string name,
        Vector2 size, Vector2 pos, Color bgColor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = size;
        rt.anchoredPosition = pos;
        var img = go.AddComponent<Image>();
        img.color = bgColor;
        return go;
    }

    static GameObject CreateLabel(Transform parent, string name, string text,
        int fontSize, Vector2 pos, TMP_FontAsset font)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(540f, 60f);
        rt.anchoredPosition = pos;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text      = text;
        t.fontSize  = fontSize;
        t.color     = Color.white;
        t.alignment = TextAlignmentOptions.MidlineLeft;
        if (font != null) t.font = font;
        return go;
    }

    static void CreateHRule(Transform parent, Vector2 pos)
    {
        var go = new GameObject("Divider");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(520f, 2f);
        rt.anchoredPosition = pos;
        go.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);
    }

    static GameObject CreateButton(Transform parent, string name, string label,
        TMP_FontAsset font, Vector2 anchor, Vector2 pivot,
        Vector2 pos, Vector2 size,
        Color? color = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = anchor;
        rt.anchorMax        = anchor;
        rt.pivot            = pivot;
        rt.sizeDelta        = size;
        rt.anchoredPosition = pos;
        var img = go.AddComponent<Image>();
        img.color = color ?? new Color(0.25f, 0.5f, 1f);
        go.AddComponent<Button>();

        var labelGO = new GameObject("Text");
        labelGO.transform.SetParent(go.transform, false);
        var lrt = labelGO.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.sizeDelta = Vector2.zero;
        var t = labelGO.AddComponent<TextMeshProUGUI>();
        t.text      = label;
        t.fontSize  = 28;
        t.color     = Color.white;
        t.alignment = TextAlignmentOptions.Center;
        if (font != null) t.font = font;

        return go;
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
        bg.color = new Color(0f, 0f, 0f, 0.55f);

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
        bgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

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
