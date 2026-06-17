using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 도감 그리드 카드 생성 유틸리티 (정적 클래스).
/// 보유 카드(풀 컬러)와 미보유 카드(실루엣) 두 종류를 생성한다.
/// </summary>
public static class CodexCardBuilder
{
    public const float CARD_W = 120f;
    public const float CARD_H = 150f;
    public const float CARD_GAP = 8f;

    /// <summary>
    /// 도감 카드 1장을 생성한다.
    /// </summary>
    /// <param name="parent">부모 RectTransform (그리드 컨테이너)</param>
    /// <param name="def">CharacterDef (null 불가)</param>
    /// <param name="owned">보유 중이면 OwnedCharacter, 미보유이면 null</param>
    /// <param name="onSelected">카드 클릭 콜백</param>
    /// <returns>생성된 카드의 RectTransform</returns>
    public static RectTransform BuildCard(
        RectTransform parent,
        CharacterDef def,
        OwnedCharacter owned,
        Action<CharacterDef, OwnedCharacter> onSelected)
    {
        bool isOwned = owned != null;

        var rarCol  = def.rarity.DisplayColor();
        var contCol = ContinentColors.Of(def.continent);

        // ── 카드 루트 ──────────────────────────────────────────────────────
        var cardGO = new GameObject("Card_" + def.id, typeof(RectTransform));
        cardGO.transform.SetParent(parent, false);
        var cardRt = (RectTransform)cardGO.transform;
        cardRt.sizeDelta = new Vector2(CARD_W, CARD_H);

        // 배경색 — 보유: 등급 틴트, 미보유: 짙은 검정
        var bg = cardGO.AddComponent<Image>();
        if (isOwned)
            bg.color = new Color(rarCol.r * 0.13f, rarCol.g * 0.13f, rarCol.b * 0.13f, 1f);
        else
            bg.color = new Color(0.05f, 0.05f, 0.08f, 1f);

        // 좌측 대륙 컬러 스트립 (보유만 표시, 미보유는 회색)
        AddStrip(cardGO.transform, isOwned ? contCol : new Color(0.2f, 0.2f, 0.2f, 1f));

        // ── 초상화 영역 (상단 2/3) ─────────────────────────────────────────
        {
            var portGO = new GameObject("Portrait", typeof(RectTransform));
            portGO.transform.SetParent(cardGO.transform, false);
            var portRt = (RectTransform)portGO.transform;
            portRt.anchorMin = new Vector2(0f, 0.38f);
            portRt.anchorMax = Vector2.one;
            portRt.offsetMin = new Vector2(6f, 0f);
            portRt.offsetMax = new Vector2(-4f, -4f);

            var portImg = portGO.AddComponent<Image>();
            if (isOwned)
                portImg.color = def.portraitColor;
            else
                portImg.color = new Color(0.1f, 0.1f, 0.1f, 1f);

            // 미보유: "?" 레이블
            if (!isOwned)
            {
                var qGO = new GameObject("Unknown", typeof(RectTransform));
                qGO.transform.SetParent(portGO.transform, false);
                var qRt = (RectTransform)qGO.transform;
                qRt.anchorMin = Vector2.zero;
                qRt.anchorMax = Vector2.one;
                qRt.offsetMin = Vector2.zero;
                qRt.offsetMax = Vector2.zero;
                var qTxt = qGO.AddComponent<TextMeshProUGUI>();
                qTxt.text      = "?";
                qTxt.fontSize  = 36f;
                qTxt.color     = new Color(0.4f, 0.4f, 0.4f, 1f);
                qTxt.fontStyle = FontStyles.Bold;
                qTxt.alignment = TextAlignmentOptions.Center;
            }
        }

        // ── 정보 영역 (하단 38%) ───────────────────────────────────────────
        {
            var infoGO = new GameObject("Info", typeof(RectTransform));
            infoGO.transform.SetParent(cardGO.transform, false);
            var infoRt = (RectTransform)infoGO.transform;
            infoRt.anchorMin = Vector2.zero;
            infoRt.anchorMax = new Vector2(1f, 0.38f);
            infoRt.offsetMin = new Vector2(6f, 2f);
            infoRt.offsetMax = new Vector2(-4f, 0f);

            // 이름
            var nameGO = new GameObject("Name", typeof(RectTransform));
            nameGO.transform.SetParent(infoGO.transform, false);
            var nameRt = (RectTransform)nameGO.transform;
            nameRt.anchorMin = new Vector2(0f, 0.55f);
            nameRt.anchorMax = Vector2.one;
            nameRt.offsetMin = Vector2.zero;
            nameRt.offsetMax = Vector2.zero;
            var nameTxt = nameGO.AddComponent<TextMeshProUGUI>();
            nameTxt.text      = isOwned ? (def.nameKo ?? def.id) : "???";
            nameTxt.fontSize  = 11f;
            nameTxt.color     = isOwned ? UITheme.TextPrimary : UITheme.TextDisabled;
            nameTxt.fontStyle = FontStyles.Bold;
            nameTxt.alignment = TextAlignmentOptions.BottomLeft;
            nameTxt.overflowMode = TextOverflowModes.Ellipsis;

            // 등급
            var rarGO = new GameObject("Rarity", typeof(RectTransform));
            rarGO.transform.SetParent(infoGO.transform, false);
            var rarRt = (RectTransform)rarGO.transform;
            rarRt.anchorMin = Vector2.zero;
            rarRt.anchorMax = new Vector2(1f, 0.55f);
            rarRt.offsetMin = Vector2.zero;
            rarRt.offsetMax = Vector2.zero;
            var rarTxt = rarGO.AddComponent<TextMeshProUGUI>();
            if (isOwned)
            {
                string rarHex = ColorUtility.ToHtmlStringRGB(rarCol);
                string lvStr  = $" Lv.{owned.level}";
                rarTxt.text  = $"<color=#{rarHex}>{def.rarity}</color>{lvStr}";
                rarTxt.color = UITheme.TextSecondary;
            }
            else
            {
                rarTxt.text  = $"<color=#444444>{def.rarity}</color>";
                rarTxt.color = UITheme.TextDisabled;
            }
            rarTxt.fontSize  = 10f;
            rarTxt.alignment = TextAlignmentOptions.TopLeft;
        }

        // ── 클릭 이벤트 ───────────────────────────────────────────────────
        if (onSelected != null)
        {
            var btn = cardGO.AddComponent<Button>();
            btn.targetGraphic = bg;
            var capturedDef   = def;
            var capturedOwned = owned;
            btn.onClick.AddListener(() => onSelected(capturedDef, capturedOwned));
        }

        return cardRt;
    }

    // ── 내부 헬퍼 ──────────────────────────────────────────────────────────

    static void AddStrip(Transform parent, Color color)
    {
        var go = new GameObject("Strip", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = new Vector2(0f, 1f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = new Vector2(5f, 0f);
        go.AddComponent<Image>().color = color;
    }
}
