using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// MSRPG 에디터 위젯 팩토리.
/// UITheme 토큰 + 둥근 9-slice 스프라이트를 사용해 모든 패널·버튼·레이블을 생성.
/// MesoriaSetup / MetaUISetup 에서 개별 헬퍼 대신 이 클래스를 공용으로 사용.
/// </summary>
public static class UIKit
{
    // 스프라이트 경로
    const string SP_PANEL  = "Assets/_Game/Art/UI/round_panel.png";
    const string SP_BUTTON = "Assets/_Game/Art/UI/round_button.png";
    const string SP_CARD   = "Assets/_Game/Art/UI/round_card.png";
    const string FONT_PATH = "Assets/_Game/Art/Fonts/malgun SDF.asset";

    // ── 폰트 캐시 ────────────────────────────────────────────────────────

    static TMP_FontAsset _font;
    public static TMP_FontAsset Font =>
        _font != null ? _font : (_font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH));

    // ── 스프라이트 캐시 ───────────────────────────────────────────────────

    static Sprite _spPanel, _spButton, _spCard;

    static Sprite SpPanel  => _spPanel  != null ? _spPanel  : (_spPanel  = Load(SP_PANEL));
    static Sprite SpButton => _spButton != null ? _spButton : (_spButton = Load(SP_BUTTON));
    static Sprite SpCard   => _spCard   != null ? _spCard   : (_spCard   = Load(SP_CARD));

    static Sprite Load(string path)
    {
        var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sp == null)
            Debug.LogWarning($"[UIKit] 스프라이트 없음: {path}. MSRPG > Generate UI Sprites 먼저 실행.");
        return sp;
    }

    // ══ 패널 ════════════════════════════════════════════════════════════

    /// <summary>
    /// 화면 중앙 둥근 패널. 배경 PanelBgDarkA + Border 테두리 1겹.
    /// </summary>
    public static GameObject Panel(Transform parent, string name, Vector2 size, Vector2 pos = default)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = size;
        rt.anchoredPosition = pos;

        // 테두리 이미지 (바깥 레이어)
        var borderImg = go.AddComponent<Image>();
        borderImg.sprite = SpPanel;
        borderImg.type   = Image.Type.Sliced;
        borderImg.color  = UITheme.Border;

        // 내부 배경 (inset 2px)
        var inner = new GameObject("_BG");
        inner.transform.SetParent(go.transform, false);
        var innerRt = inner.AddComponent<RectTransform>();
        innerRt.anchorMin = Vector2.zero;
        innerRt.anchorMax = Vector2.one;
        innerRt.offsetMin = new Vector2(2f, 2f);
        innerRt.offsetMax = new Vector2(-2f, -2f);
        var innerImg = inner.AddComponent<Image>();
        innerImg.sprite = SpPanel;
        innerImg.type   = Image.Type.Sliced;
        innerImg.color  = UITheme.PanelBgDarkA;

        return go;
    }

    /// <summary>
    /// 앵커 스트레치 방식 패널 (anchorMin/Max 지정). 배경 + 테두리 포함.
    /// </summary>
    public static GameObject PanelStretched(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin = default, Vector2 offsetMax = default,
        Color? bgColor = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        rt.sizeDelta = Vector2.zero;

        var borderImg = go.AddComponent<Image>();
        borderImg.sprite = SpPanel;
        borderImg.type   = Image.Type.Sliced;
        borderImg.color  = UITheme.Border;

        var inner = new GameObject("_BG");
        inner.transform.SetParent(go.transform, false);
        var innerRt = inner.AddComponent<RectTransform>();
        innerRt.anchorMin = Vector2.zero;
        innerRt.anchorMax = Vector2.one;
        innerRt.offsetMin = new Vector2(2f, 2f);
        innerRt.offsetMax = new Vector2(-2f, -2f);
        var innerImg = inner.AddComponent<Image>();
        innerImg.sprite = SpPanel;
        innerImg.type   = Image.Type.Sliced;
        innerImg.color  = bgColor ?? UITheme.PanelBgDarkA;

        return go;
    }

    // ══ 버튼 ════════════════════════════════════════════════════════════

    public enum BtnKind { Primary, Danger, Neutral, Success }

    static Color BtnColor(BtnKind k) => k switch
    {
        BtnKind.Danger  => UITheme.BtnDanger,
        BtnKind.Neutral => UITheme.BtnNeutral,
        BtnKind.Success => UITheme.BtnSuccess,
        _               => UITheme.BtnPrimary,
    };

    /// <summary>둥근 버튼. size 지정 없으면 기본(580×70).</summary>
    public static GameObject Button(Transform parent, string name, string label,
        BtnKind kind = BtnKind.Primary,
        Vector2 pos = default, Vector2 size = default,
        int fontSize = 0)
    {
        if (size == default) size = new Vector2(580f, 70f);
        if (fontSize == 0) fontSize = UITheme.FontH2;

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = size;
        rt.anchoredPosition = pos;

        var img = go.AddComponent<Image>();
        img.sprite = SpButton;
        img.type   = Image.Type.Sliced;
        img.color  = BtnColor(kind);
        go.AddComponent<Button>();

        AddLabel(go.transform, label, fontSize, UITheme.TextPrimary);
        return go;
    }

    /// <summary>앵커 기반 버튼.</summary>
    public static GameObject ButtonAnchored(Transform parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin = default, Vector2 offsetMax = default,
        BtnKind kind = BtnKind.Primary, int fontSize = 0)
    {
        if (fontSize == 0) fontSize = UITheme.FontH2;

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        rt.sizeDelta = Vector2.zero;

        var img = go.AddComponent<Image>();
        img.sprite = SpButton;
        img.type   = Image.Type.Sliced;
        img.color  = BtnColor(kind);
        go.AddComponent<Button>();

        AddLabel(go.transform, label, fontSize, UITheme.TextPrimary);
        return go;
    }

    // ══ 레이블 ══════════════════════════════════════════════════════════

    public enum TextLevel { Display, H1, H2, Body, Caption, Stat }

    static (int size, bool bold, Color color) TextStyle(TextLevel lv) => lv switch
    {
        TextLevel.Display => (UITheme.FontDisplay, true,  UITheme.TextPrimary),
        TextLevel.H1      => (UITheme.FontH1,      true,  UITheme.TextPrimary),
        TextLevel.H2      => (UITheme.FontH2,       true,  UITheme.TextPrimary),
        TextLevel.Body    => (UITheme.FontBody,     false, UITheme.TextSecondary),
        TextLevel.Caption => (UITheme.FontCaption,  false, UITheme.TextDisabled),
        TextLevel.Stat    => (UITheme.FontStat,     true,  UITheme.TextPrimary),
        _                 => (UITheme.FontBody,     false, UITheme.TextPrimary),
    };

    /// <summary>중앙 앵커 레이블 (sizeDelta 기반).</summary>
    public static GameObject Label(Transform parent, string name, string text,
        TextLevel level = TextLevel.Body,
        Vector2 pos = default, Vector2 size = default,
        TextAlignmentOptions align = TextAlignmentOptions.Center,
        Color? colorOverride = null)
    {
        if (size == default) size = new Vector2(650f, 60f);
        var (fs, bold, col) = TextStyle(level);

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = size;
        rt.anchoredPosition = pos;

        var t = go.AddComponent<TextMeshProUGUI>();
        t.text      = text;
        t.fontSize  = fs;
        t.color     = colorOverride ?? col;
        t.alignment = align;
        if (bold) t.fontStyle = FontStyles.Bold;
        if (Font != null) t.font = Font;
        return go;
    }

    /// <summary>앵커 스트레치 레이블.</summary>
    public static GameObject LabelAnchored(Transform parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin = default, Vector2 offsetMax = default,
        TextLevel level = TextLevel.Body,
        TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft,
        Color? colorOverride = null)
    {
        var (fs, bold, col) = TextStyle(level);

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        rt.sizeDelta = Vector2.zero;

        var t = go.AddComponent<TextMeshProUGUI>();
        t.text      = text;
        t.fontSize  = fs;
        t.color     = colorOverride ?? col;
        t.alignment = align;
        if (bold) t.fontStyle = FontStyles.Bold;
        if (Font != null) t.font = Font;
        return go;
    }

    // ══ 구분선 ══════════════════════════════════════════════════════════

    /// <summary>수평 구분선 (1px, Border 색).</summary>
    public static void Divider(Transform parent, Vector2 pos, float width = 520f)
    {
        var go = new GameObject("Divider");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(width, 1f);
        rt.anchoredPosition = pos;
        go.AddComponent<Image>().color = new Color(
            UITheme.Border.r, UITheme.Border.g, UITheme.Border.b, 0.6f);
    }

    // ══ 인풋 필드 ════════════════════════════════════════════════════════

    /// <summary>TMP 인풋 필드 (중앙 앵커).</summary>
    public static GameObject Input(Transform parent, string name, string placeholder,
        Vector2 pos = default, Vector2 size = default)
    {
        if (size == default) size = new Vector2(400f, 65f);

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = size;
        rt.anchoredPosition = pos;
        var bg = go.AddComponent<Image>();
        bg.sprite = SpButton;
        bg.type   = Image.Type.Sliced;
        bg.color  = UITheme.PanelBgMid;
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
        phText.text = placeholder; phText.fontSize = UITheme.FontBody + 2;
        phText.color = new Color(UITheme.TextDisabled.r, UITheme.TextDisabled.g, UITheme.TextDisabled.b, 0.8f);
        phText.fontStyle = FontStyles.Italic;
        phText.alignment = TextAlignmentOptions.MidlineLeft;
        if (Font != null) phText.font = Font;

        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(areaGO.transform, false);
        var txtRT = txtGO.AddComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one; txtRT.sizeDelta = Vector2.zero;
        var tmpText = txtGO.AddComponent<TextMeshProUGUI>();
        tmpText.fontSize = UITheme.FontBody + 2;
        tmpText.color    = UITheme.TextPrimary;
        tmpText.alignment = TextAlignmentOptions.MidlineLeft;
        if (Font != null) tmpText.font = Font;

        field.textViewport  = areaRT;
        field.textComponent = tmpText;
        field.placeholder   = phText;
        return go;
    }

    // ══ 캐릭터 카드 (§E-2) ══════════════════════════════════════════════

    /// <summary>
    /// 2:3 세로형 캐릭터 카드 컨테이너.
    /// 대륙색 외곽 프레임 + 내부 배경. 실제 일러스트·텍스트는 호출자가 자식으로 추가.
    /// </summary>
    public static GameObject Card(Transform parent, string name,
        Color continentColor, Vector2 size = default, Vector2 pos = default)
    {
        if (size == default) size = new Vector2(200f, 300f); // 2:3

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = size;
        rt.anchoredPosition = pos;

        // 대륙색 외곽 프레임
        var borderImg = go.AddComponent<Image>();
        borderImg.sprite = SpCard;
        borderImg.type   = Image.Type.Sliced;
        borderImg.color  = continentColor;

        // 어두운 내부 배경 (5px inset)
        var inner = new GameObject("_CardBG");
        inner.transform.SetParent(go.transform, false);
        var innerRt = inner.AddComponent<RectTransform>();
        innerRt.anchorMin = Vector2.zero;
        innerRt.anchorMax = Vector2.one;
        innerRt.offsetMin = new Vector2(5f, 5f);
        innerRt.offsetMax = new Vector2(-5f, -5f);
        var innerImg = inner.AddComponent<Image>();
        innerImg.sprite = SpCard;
        innerImg.type   = Image.Type.Sliced;
        innerImg.color  = UITheme.PanelBgDarkA;

        return go;
    }

    // ══ 내부 헬퍼 ═══════════════════════════════════════════════════════

    /// <summary>전체 영역을 채우는 텍스트 레이블을 자식으로 추가.</summary>
    static void AddLabel(Transform parent, string text, int fontSize, Color color,
        TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(8f, 0f); rt.offsetMax = new Vector2(-8f, 0f);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text      = text;
        t.fontSize  = fontSize;
        t.color     = color;
        t.alignment = align;
        if (Font != null) t.font = Font;
    }
}
