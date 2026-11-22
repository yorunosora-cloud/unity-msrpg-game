using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 도감 상세의 "개념 해설" 섹션 (2단 도감, 설계도 18 §3-12 / 18-C).
/// 기본 해설은 항상 보이고, 심화 해설·실제 원리·출처는 개념 문제를 맞히면 열린다.
/// 저장·알림은 호출자가 콜백으로 주입한다(Backend의 MetaSaveService는 PlayFab 의존이라 이 어셈블리에서 직접 부르지 않는다).
/// 레이아웃은 CodexDetailBuilder의 다른 섹션과 같은 규칙(y 오프셋, 위에서 아래로)이다.
/// </summary>
public static class CodexConceptSection
{
    const float PAD       = 16f;
    const float SEC_GAP   = 8f;
    const float LINE_H    = 20f;
    const float CHOICE_H  = 34f;

    // 세션 한정: 캐릭터별 오답 횟수(다음 문제로 순환)와 방금 오답이었는지(피드백 1회 표시)
    static readonly Dictionary<string, int> _attempts     = new();
    static readonly HashSet<string>         _wrongPending = new();

    /// <summary>
    /// 섹션을 parent의 자식으로 만들고 다음 y를 반환한다. 소유하지 않았거나 교육 문구가 없으면 아무것도 만들지 않고 y를 그대로 반환.
    /// </summary>
    /// <param name="onCorrect">정답으로 심화 해설이 열린 직후(저장·로스터 알림용).</param>
    /// <param name="onChanged">정답·오답으로 표시 내용이 바뀌어 상세를 다시 그려야 할 때.</param>
    public static float Build(RectTransform parent, CharacterDef def, OwnedCharacter owned, float y,
                              Action onCorrect = null, Action onChanged = null)
    {
        if (owned == null || !CodexConcept.HasContent(def)) return y;

        var sec = CreateSection(parent, "Concept", y);
        float iy = -PAD;
        iy = AddTitle(sec, "개념 해설", iy);

        if (!string.IsNullOrEmpty(def.basicExplanation))
            iy = AddBlock(sec, def.basicExplanation, UITheme.TextSecondary, iy);

        if (CodexConcept.IsAdvancedOpen(def, owned))
        {
            if (!string.IsNullOrEmpty(def.advancedExplanation))
                iy = AddBlock(sec, def.advancedExplanation, UITheme.TextPrimary, iy);

            if (!string.IsNullOrEmpty(def.realPrinciple))
            {
                string src = string.IsNullOrEmpty(def.sourceRef) ? "" : $"  (출처: {def.sourceRef})";
                iy = AddBlock(sec, $"[실제 원리] {def.realPrinciple}{src}", UITheme.TextSecondary, iy);
            }
        }
        else
        {
            iy = AddLine(sec, "[심화 해설 잠김 — 개념 문제를 풀면 열립니다]", UITheme.TextDisabled, iy, italic: true);

            string key = def.id ?? "";
            _attempts.TryGetValue(key, out int attempt);
            int qi = CodexConcept.PickQuestionIndex(def, attempt);
            var q = def.conceptQuestions[qi];

            if (_wrongPending.Remove(key))
                iy = AddLine(sec, "오답입니다. 다른 문제에 도전해 보세요.", UITheme.BtnDanger, iy, italic: false);

            iy = AddBlock(sec, q.question, UITheme.TextPrimary, iy);

            for (int i = 0; i < q.choices.Length; i++)
            {
                int choice = i;
                iy = AddChoice(sec, $"Choice_{i}", $"{i + 1}. {q.choices[i]}", iy, () =>
                {
                    if (CodexConcept.TryAnswer(def, owned, qi, choice))
                    {
                        _attempts.Remove(key);
                        onCorrect?.Invoke();
                    }
                    else
                    {
                        _attempts[key] = attempt + 1;
                        _wrongPending.Add(key);
                    }
                    onChanged?.Invoke();
                });
            }
        }

        float h = Mathf.Abs(iy) + PAD;
        sec.sizeDelta = new Vector2(sec.sizeDelta.x, h);
        return y - h - SEC_GAP;
    }

    /// <summary>테스트용: 세션 한정 오답 상태 초기화.</summary>
    public static void ResetSessionState() { _attempts.Clear(); _wrongPending.Clear(); }

    // ── 레이아웃 헬퍼 (CodexDetailBuilder와 같은 규칙) ─────────────────────

    static RectTransform CreateSection(RectTransform parent, string name, float yPos)
    {
        var go = new GameObject("Sec_" + name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, yPos);
        rt.sizeDelta = new Vector2(0f, 0f);
        go.AddComponent<Image>().color = UITheme.PanelBgMid;
        return rt;
    }

    static TextMeshProUGUI Text(RectTransform parent, string name, string text, float yPos, float height,
                                int size, Color color, FontStyles style, TextAlignmentOptions align, bool wrap)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, yPos);
        rt.sizeDelta = new Vector2(-PAD * 2f, height);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = color; t.fontStyle = style; t.alignment = align;
        t.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        t.overflowMode = wrap ? TextOverflowModes.Overflow : TextOverflowModes.Ellipsis;
        return t;
    }

    static float AddTitle(RectTransform p, string text, float y)
    {
        Text(p, "Title", text, y, LINE_H + 4f, UITheme.FontH2, UITheme.TextPrimary, FontStyles.Bold, TextAlignmentOptions.Left, false);
        return y - (LINE_H + 4f) - 6f;
    }

    static float AddLine(RectTransform p, string text, Color color, float y, bool italic)
    {
        Text(p, "Line", text, y, LINE_H, UITheme.FontBody, color, italic ? FontStyles.Italic : FontStyles.Normal, TextAlignmentOptions.Left, false);
        return y - LINE_H - 2f;
    }

    static float AddBlock(RectTransform p, string text, Color color, float y)
    {
        int explicitLines = 1;
        foreach (char ch in text) if (ch == '\n') explicitLines++;
        int lines = Mathf.Max(2, Mathf.Max(explicitLines, text.Length / 30 + 1));
        float h = lines * LINE_H + 8f;
        Text(p, "Block", text, y, h, UITheme.FontBody, color, FontStyles.Normal, TextAlignmentOptions.TopLeft, true);
        return y - h - 4f;
    }

    static float AddChoice(RectTransform parent, string name, string label, float yPos, Action onClick)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f); rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, yPos);
        rt.sizeDelta = new Vector2(-PAD * 2f, CHOICE_H);

        var img = go.AddComponent<Image>();
        img.color = UITheme.BtnNeutral;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(() => onClick?.Invoke());

        var tgo = new GameObject("Label", typeof(RectTransform));
        tgo.transform.SetParent(go.transform, false);
        var trt = (RectTransform)tgo.transform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(10f, 0f); trt.offsetMax = new Vector2(-10f, 0f);
        var t = tgo.AddComponent<TextMeshProUGUI>();
        t.text = label; t.fontSize = UITheme.FontBody; t.color = UITheme.TextPrimary;
        t.alignment = TextAlignmentOptions.MidlineLeft; t.raycastTarget = false;

        return yPos - CHOICE_H - 6f;
    }
}
