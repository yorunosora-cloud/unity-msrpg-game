using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// MSRPG 에디터 위젯 팩토리.
/// 스프라이트 없이 단색 Image 사용. 패널에는 ContinentPanel을 자동 부착해 대륙 테마 적용.
/// </summary>
public static class UIKit
{
    const string FONT_PATH = "Assets/_Game/Art/Fonts/malgun SDF.asset";

    static TMP_FontAsset _font;
    public static TMP_FontAsset Font =>
        _font != null ? _font : (_font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH));

    // ══ 패널 ════════════════════════════════════════════════════════════

    /// <summary>중앙 앵커 패널. ContinentPanel이 런타임에 대륙 배경색 적용.</summary>
    public static GameObject Panel(Transform parent, string name, Vector2 size, Vector2 pos = default)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = size;
        rt.anchoredPosition = pos;

        go.AddComponent<Image>().color = UITheme.PanelBgDarkA;
        go.AddComponent<ContinentPanel>();

        return go;
    }

    /// <summary>앵커 스트레치 패널. bgColor 미지정 시 ContinentPanel이 대륙 배경색 적용.</summary>
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

        go.AddComponent<Image>().color = bgColor ?? UITheme.PanelBgDarkA;
        if (bgColor == null) go.AddComponent<ContinentPanel>();

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

        go.AddComponent<Image>().color = BtnColor(kind);
        go.AddComponent<Button>();

        AddLabel(go.transform, label, fontSize, UITheme.TextPrimary);
        return go;
    }

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

        go.AddComponent<Image>().color = BtnColor(kind);
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
        go.AddComponent<Image>().color = UITheme.PanelBgMid;
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
        phText.text      = placeholder;
        phText.fontSize  = UITheme.FontBody + 2;
        phText.color     = new Color(UITheme.TextDisabled.r, UITheme.TextDisabled.g, UITheme.TextDisabled.b, 0.8f);
        phText.fontStyle = FontStyles.Italic;
        phText.alignment = TextAlignmentOptions.MidlineLeft;
        if (Font != null) phText.font = Font;

        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(areaGO.transform, false);
        var txtRT = txtGO.AddComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one; txtRT.sizeDelta = Vector2.zero;
        var tmpText = txtGO.AddComponent<TextMeshProUGUI>();
        tmpText.fontSize  = UITheme.FontBody + 2;
        tmpText.color     = UITheme.TextPrimary;
        tmpText.alignment = TextAlignmentOptions.MidlineLeft;
        if (Font != null) tmpText.font = Font;

        field.textViewport  = areaRT;
        field.textComponent = tmpText;
        field.placeholder   = phText;
        return go;
    }

    // ══ 캐릭터 카드 (§E-2) ══════════════════════════════════════════════

    public static GameObject Card(Transform parent, string name,
        Color continentColor, Vector2 size = default, Vector2 pos = default)
    {
        if (size == default) size = new Vector2(200f, 300f);

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = size;
        rt.anchoredPosition = pos;

        go.AddComponent<Image>().color = continentColor;

        var inner = new GameObject("_CardBG");
        inner.transform.SetParent(go.transform, false);
        var innerRt = inner.AddComponent<RectTransform>();
        innerRt.anchorMin = Vector2.zero;
        innerRt.anchorMax = Vector2.one;
        innerRt.offsetMin = new Vector2(5f, 5f);
        innerRt.offsetMax = new Vector2(-5f, -5f);
        inner.AddComponent<Image>().color = UITheme.PanelBgDarkA;

        return go;
    }

    // ══ 내부 헬퍼 ═══════════════════════════════════════════════════════

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
