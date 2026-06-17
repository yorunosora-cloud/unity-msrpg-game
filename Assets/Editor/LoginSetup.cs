// ⚠️ PlayFab SDK 임포트 후에 이 메뉴를 실행하세요.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor.Events;
using TMPro;
using UnityEngine.InputSystem.UI;

/// <summary>
/// MSRPG > Setup Login Scene 메뉴 — 한글 폰트 설정 + Login.unity 씬을 자동으로 생성합니다.
/// </summary>
public static class LoginSetup
{
    const string FONT_TTF_PATH = "Assets/_Game/Art/Fonts/malgun.ttf";
    const string FONT_SDF_PATH = "Assets/_Game/Art/Fonts/malgun SDF.asset";

    // ── 한글 폰트 에셋 생성/로드 ─────────────────────────────────────────────

    static TMP_FontAsset EnsureKoreanFont()
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_SDF_PATH);
        if (existing != null) return existing;

        AssetDatabase.ImportAsset(FONT_TTF_PATH, ImportAssetOptions.ForceUpdate);
        var font = AssetDatabase.LoadAssetAtPath<Font>(FONT_TTF_PATH);
        if (font == null)
        {
            Debug.LogError("[LoginSetup] malgun.ttf 임포트 실패.");
            return null;
        }

        var fontAsset = TMP_FontAsset.CreateFontAsset(font);
        fontAsset.name = "malgun SDF";

        // 메인 에셋 파일 생성
        AssetDatabase.CreateAsset(fontAsset, FONT_SDF_PATH);

        // Material과 Atlas Texture를 서브 에셋으로 함께 저장 (없으면 런타임에 참조 깨짐)
        if (fontAsset.material != null)
        {
            fontAsset.material.name = "malgun SDF Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }
        if (fontAsset.atlasTextures != null)
        {
            for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
            {
                var tex = fontAsset.atlasTextures[i];
                if (tex == null) continue;
                tex.name = $"malgun SDF Atlas {i}";
                AssetDatabase.AddObjectToAsset(tex, fontAsset);
            }
        }

        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        Debug.Log("[LoginSetup] 맑은 고딕 TMP 폰트 에셋 생성 완료");

        // Refresh 후 참조가 무효화되므로 디스크에서 다시 로드
        AssetDatabase.Refresh();
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_SDF_PATH);
    }

    // ── 씬 생성 ──────────────────────────────────────────────────────────────

    [MenuItem("MSRPG/Setup Login Scene")]
    public static void Run()
    {
        var korFont = EnsureKoreanFont();

        // 1. 새 빈 씬
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 2. EventSystem
        var eventGO = new GameObject("EventSystem");
        eventGO.AddComponent<EventSystem>();
        eventGO.AddComponent<InputSystemUIInputModule>();

        // 3. PlayFabManager
        var pfmGO = new GameObject("PlayFabManager");
        pfmGO.AddComponent<PlayFabManager>();

        // 4. 카메라
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags      = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.08f, 0.12f);
        camGO.AddComponent<AudioListener>();
        camGO.transform.position = new Vector3(0f, 0f, -10f);

        // 5. Canvas
        var canvasGO = new GameObject("Canvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // 6. LoginPanel
        var loginPanel     = CreatePanel(canvasGO.transform, "LoginPanel");
        CreateTMPLabel(loginPanel.transform,   "Title",             "MSRPG 로그인",               48, new Vector2(0,  300), korFont);
        var loginIdInput   = CreateTMPInput(loginPanel.transform,   "IdOrEmailInput",              "아이디 또는 이메일",   new Vector2(0,  160), korFont);
        var loginPwdInput  = CreateTMPInput(loginPanel.transform,   "PasswordInput",               "비밀번호",             new Vector2(0,   60), korFont, password: true);
        var loginBtn       = CreateTMPButton(loginPanel.transform,  "LoginButton",                 "로그인",               new Vector2(0,  -60), korFont);
        var switchToRegBtn = CreateTMPButton(loginPanel.transform,  "SwitchToRegisterBtn",         "계정이 없으신가요? 회원가입", new Vector2(0, -160), korFont, fontSize: 24, bgAlpha: 0f);

        // 7. RegisterPanel (비활성)
        var registerPanel    = CreatePanel(canvasGO.transform, "RegisterPanel");
        registerPanel.SetActive(false);
        CreateTMPLabel(registerPanel.transform,    "Title",          "MSRPG 회원가입",              48, new Vector2(0,  340), korFont);
        var regEmailInput    = CreateTMPInput(registerPanel.transform, "EmailInput",                "이메일 (Gmail 가능)", new Vector2(0,  200), korFont);
        var regUsernameInput = CreateTMPInput(registerPanel.transform, "UsernameInput",             "아이디",              new Vector2(0,   90), korFont);
        var regPwdInput      = CreateTMPInput(registerPanel.transform, "PasswordInput",             "비밀번호",            new Vector2(0,  -20), korFont, password: true);
        var registerBtn      = CreateTMPButton(registerPanel.transform,"RegisterButton",            "회원가입",            new Vector2(0, -130), korFont);
        var switchToLoginBtn = CreateTMPButton(registerPanel.transform,"SwitchToLoginBtn",          "이미 계정이 있으신가요? 로그인", new Vector2(0, -230), korFont, fontSize: 24, bgAlpha: 0f);

        // 8. 상태 텍스트
        var statusGO   = CreateTMPLabel(canvasGO.transform, "StatusText", "", 28, new Vector2(0, -550), korFont);
        var statusText = statusGO.GetComponent<TextMeshProUGUI>();
        statusText.color = Color.red;

        // 9. AuthUI 컴포넌트 + 참조 연결
        var authUI = canvasGO.AddComponent<AuthUI>();
        var so     = new SerializedObject(authUI);
        so.FindProperty("loginPanel").objectReferenceValue             = loginPanel;
        so.FindProperty("registerPanel").objectReferenceValue          = registerPanel;
        so.FindProperty("loginIdOrEmailInput").objectReferenceValue    = loginIdInput.GetComponent<TMP_InputField>();
        so.FindProperty("loginPasswordInput").objectReferenceValue     = loginPwdInput.GetComponent<TMP_InputField>();
        so.FindProperty("loginButton").objectReferenceValue            = loginBtn.GetComponent<Button>();
        so.FindProperty("registerEmailInput").objectReferenceValue     = regEmailInput.GetComponent<TMP_InputField>();
        so.FindProperty("registerUsernameInput").objectReferenceValue  = regUsernameInput.GetComponent<TMP_InputField>();
        so.FindProperty("registerPasswordInput").objectReferenceValue  = regPwdInput.GetComponent<TMP_InputField>();
        so.FindProperty("registerButton").objectReferenceValue         = registerBtn.GetComponent<Button>();
        so.FindProperty("statusText").objectReferenceValue             = statusText;
        so.ApplyModifiedProperties();

        // 10. 버튼 onClick 연결
        UnityEventTools.AddVoidPersistentListener(loginBtn.GetComponent<Button>().onClick,         authUI.OnLoginClicked);
        UnityEventTools.AddVoidPersistentListener(switchToRegBtn.GetComponent<Button>().onClick,   authUI.ShowRegister);
        UnityEventTools.AddVoidPersistentListener(registerBtn.GetComponent<Button>().onClick,      authUI.OnRegisterClicked);
        UnityEventTools.AddVoidPersistentListener(switchToLoginBtn.GetComponent<Button>().onClick, authUI.ShowLogin);

        // 11. 씬 저장
        System.IO.Directory.CreateDirectory(Application.dataPath + "/_Game/Scenes");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(scene, "Assets/_Game/Scenes/Login.unity");
        AssetDatabase.Refresh();

        Debug.Log("[MSRPG] ✅ Login 씬 생성 완료! ▶ Play로 테스트하세요.");
    }

    // ── UI 생성 헬퍼 ──────────────────────────────────────────────────────────

    static GameObject CreatePanel(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(700, 900);
        rt.anchoredPosition = Vector2.zero;
        return go;
    }

    static GameObject CreateTMPLabel(Transform parent, string name, string text, int fontSize,
                                      Vector2 pos, TMP_FontAsset font)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(650, 80);
        rt.anchoredPosition = pos;
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text      = text;
        t.fontSize  = fontSize;
        t.color     = Color.white;
        t.alignment = TextAlignmentOptions.Center;
        if (font != null) t.font = font;
        return go;
    }

    static GameObject CreateTMPInput(Transform parent, string name, string placeholder,
                                      Vector2 pos, TMP_FontAsset font, bool password = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(600, 70);
        rt.anchoredPosition = pos;
        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.25f);
        var field = go.AddComponent<TMP_InputField>();

        // Text Area
        var areaGO = new GameObject("Text Area");
        areaGO.transform.SetParent(go.transform, false);
        var areaRT = areaGO.AddComponent<RectTransform>();
        areaRT.anchorMin = Vector2.zero;
        areaRT.anchorMax = Vector2.one;
        areaRT.offsetMin = new Vector2(15, 5);
        areaRT.offsetMax = new Vector2(-15, -5);
        areaGO.AddComponent<RectMask2D>();

        // Placeholder
        var phGO = new GameObject("Placeholder");
        phGO.transform.SetParent(areaGO.transform, false);
        var phRT = phGO.AddComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero;
        phRT.anchorMax = Vector2.one;
        phRT.sizeDelta = Vector2.zero;
        var phText       = phGO.AddComponent<TextMeshProUGUI>();
        phText.text      = placeholder;
        phText.fontSize  = 28;
        phText.color     = new Color(1, 1, 1, 0.4f);
        phText.fontStyle = FontStyles.Italic;
        phText.alignment = TextAlignmentOptions.MidlineLeft;
        if (font != null) phText.font = font;

        // Input Text
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(areaGO.transform, false);
        var textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;
        var tmpText      = textGO.AddComponent<TextMeshProUGUI>();
        tmpText.fontSize  = 28;
        tmpText.color     = Color.white;
        tmpText.alignment = TextAlignmentOptions.MidlineLeft;
        if (font != null) tmpText.font = font;

        field.textViewport     = areaRT;
        field.textComponent    = tmpText;
        field.placeholder      = phText;
        field.onFocusSelectAll = false;
        if (password) field.contentType = TMP_InputField.ContentType.Password;

        return go;
    }

    static GameObject CreateTMPButton(Transform parent, string name, string label, Vector2 pos,
                                       TMP_FontAsset font, int fontSize = 32, float bgAlpha = 1f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(480, 75);
        rt.anchoredPosition = pos;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.25f, 0.5f, 1f, bgAlpha);
        go.AddComponent<Button>();

        var labelGO = new GameObject("Text");
        labelGO.transform.SetParent(go.transform, false);
        var labelRT = labelGO.AddComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.sizeDelta = Vector2.zero;
        var t = labelGO.AddComponent<TextMeshProUGUI>();
        t.text      = label;
        t.fontSize  = fontSize;
        t.color     = bgAlpha > 0 ? Color.white : new Color(0.6f, 0.8f, 1f);
        t.alignment = TextAlignmentOptions.Center;
        if (font != null) t.font = font;

        return go;
    }
}
