using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 도감 상세 뷰 섹션 빌드 정적 유틸리티.
/// CollectionPanel 이 ShowDetail() 시 이 클래스에 ScrollContent RectTransform 을 넘기면
/// Header / Lore / Stats / Skills / Synergy / Acquire 섹션을 순서대로 자식으로 추가한다.
/// </summary>
public static class CodexDetailBuilder
{
    // ── 섹션 내부 여백 상수 ───────────────────────────────────────────────
    const float PAD         = 16f;   // 섹션 상단·좌우 패딩
    const float SEC_GAP     = 8f;    // 섹션 사이 간격
    const float LINE_H      = 20f;   // 한 줄 텍스트 높이
    const float HEADER_H    = 120f;  // 헤더 고정 높이
    const float PORTRAIT_W  = 100f;  // 헤더 내 초상화 폭

    // ─────────────────────────────────────────────────────────────────────
    // 공용 진입점
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// container 에 모든 섹션을 자식으로 추가하고 container 의 sizeDelta.y 를 갱신한다.
    /// </summary>
    public static void Build(RectTransform container, CharacterDef def, OwnedCharacter owned)
    {
        bool isOwned = owned != null;
        float y = 0f;

        y = BuildHeader(container, def, owned, isOwned, y);
        y = BuildLore(container, def, isOwned, y);
        y = BuildStats(container, def, owned, isOwned, y);
        y = BuildSkills(container, def, owned, isOwned, y);
        y = BuildSynergy(container, def, isOwned, y);
        y = BuildAcquire(container, def, isOwned, y);

        container.sizeDelta = new Vector2(0, Mathf.Abs(y) + PAD);
    }

    // ─────────────────────────────────────────────────────────────────────
    // 섹션 구현
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>헤더: 초상화 + 이름/배지 영역 (고정 높이 120px)</summary>
    static float BuildHeader(RectTransform parent, CharacterDef def, OwnedCharacter owned, bool isOwned, float y)
    {
        var contCol = ContinentColors.Of(def.continent);
        var rarCol  = def.rarity.DisplayColor();

        // 헤더 배경 (대륙색 틴트)
        var bgColor = new Color(contCol.r * 0.2f, contCol.g * 0.2f, contCol.b * 0.2f, 1f);
        var sec = CreateSection(parent, "Header", y, HEADER_H, bgColor);

        // ── 좌측 초상화 영역 ──────────────────────────────────────────────
        {
            var portGO = new GameObject("Portrait", typeof(RectTransform));
            portGO.transform.SetParent(sec, false);
            var portRt = (RectTransform)portGO.transform;
            portRt.anchorMin        = new Vector2(0f, 0f);
            portRt.anchorMax        = new Vector2(0f, 1f);
            portRt.pivot            = new Vector2(0f, 0.5f);
            portRt.anchoredPosition = new Vector2(PAD, 0f);
            portRt.sizeDelta        = new Vector2(PORTRAIT_W, 0f);

            var portImg = portGO.AddComponent<Image>();
            portImg.color = isOwned ? def.portraitColor : new Color(0.05f, 0.05f, 0.08f, 1f);

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
                qTxt.fontSize  = 48f;
                qTxt.color     = UITheme.TextDisabled;
                qTxt.fontStyle = FontStyles.Bold;
                qTxt.alignment = TextAlignmentOptions.Center;
            }
        }

        // ── 우측 정보 영역 ────────────────────────────────────────────────
        {
            var infoGO = new GameObject("Info", typeof(RectTransform));
            infoGO.transform.SetParent(sec, false);
            var infoRt = (RectTransform)infoGO.transform;
            infoRt.anchorMin        = new Vector2(0f, 0f);
            infoRt.anchorMax        = new Vector2(1f, 1f);
            infoRt.offsetMin        = new Vector2(PAD + PORTRAIT_W + 8f, 8f);
            infoRt.offsetMax        = new Vector2(-8f, -8f);

            float iy = 0f;

            // 이름 (한글)
            MakeTextLine(infoRt, "NameKo", isOwned ? (def.nameKo ?? def.id) : "???",
                UITheme.FontH2, UITheme.TextPrimary, FontStyles.Bold, iy);
            iy -= LINE_H + 4f;

            // 이름 (영문)
            if (isOwned && !string.IsNullOrEmpty(def.nameEn))
            {
                MakeTextLine(infoRt, "NameEn", def.nameEn,
                    UITheme.FontCaption, UITheme.TextSecondary, FontStyles.Normal, iy);
                iy -= LINE_H - 2f;
            }

            // 배지 행 (대륙, 등급, 역할)
            iy -= 4f;
            float bx = 0f;
            bx += MakeBadge(infoRt, "BadgeContinent",
                ContinentLabel(def.continent), contCol, bx, iy) + 4f;

            var rarBadgeCol = rarCol;
            bx += MakeBadge(infoRt, "BadgeRarity",
                def.rarity.ToString(), rarBadgeCol, bx, iy) + 4f;

            if (isOwned)
            {
                MakeBadge(infoRt, "BadgeRole",
                    RoleLabel(def.role), UITheme.BtnNeutral, bx, iy);
            }
        }

        return y - HEADER_H - SEC_GAP;
    }

    /// <summary>로어 섹션</summary>
    static float BuildLore(RectTransform parent, CharacterDef def, bool isOwned, float y)
    {
        string title = isOwned ? "세계관" : "???";

        string body;
        bool italic = false;
        Color textCol;

        if (isOwned)
        {
            body     = !string.IsNullOrEmpty(def.loreKo) ? def.loreKo : def.concept;
            textCol  = UITheme.TextSecondary;
        }
        else
        {
            body     = $"[ 국가: {def.country} | 등급: {def.rarity} ]\n\n—————————— —————— ———————————————————— ——————————————— ——————————————————";
            textCol  = UITheme.TextDisabled;
            italic   = true;
        }

        if (string.IsNullOrEmpty(body))
            body = "(내용 없음)";

        return BuildTextSection(parent, "Lore", title, body, textCol, italic, y);
    }

    /// <summary>스탯 섹션</summary>
    static float BuildStats(RectTransform parent, CharacterDef def, OwnedCharacter owned, bool isOwned, float y)
    {
        var sec = CreateSection(parent, "Stats", y, 0f, UITheme.PanelBgMid);
        float iy = -PAD;

        // 섹션 제목
        iy = AddSectionTitle(sec, "스탯", iy);

        // 레벨 행 (보유만)
        if (isOwned)
        {
            iy = AddBodyLine(sec, $"레벨: {owned.level}", UITheme.TextPrimary, iy, false);
        }

        // 스탯 행
        var s = def.baseStats;
        string[] labels = { "HP", "ATK", "DEF", "SPD", "MP" };
        int[]    vals   = { s.hp, s.atk, s.def, s.spd, s.mp };

        for (int i = 0; i < labels.Length; i++)
        {
            string valStr = isOwned ? vals[i].ToString() : "???";
            Color  col    = isOwned ? UITheme.TextSecondary : UITheme.TextDisabled;
            iy = AddBodyLine(sec, $"{labels[i]}: {valStr}", col, iy, !isOwned);
        }

        float secH = Mathf.Abs(iy) + PAD;
        SetHeight(sec, secH);
        return y - secH - SEC_GAP;
    }

    /// <summary>스킬 섹션</summary>
    static float BuildSkills(RectTransform parent, CharacterDef def, OwnedCharacter owned, bool isOwned, float y)
    {
        var sec = CreateSection(parent, "Skills", y, 0f, UITheme.PanelBgMid);
        float iy = -PAD;

        iy = AddSectionTitle(sec, "스킬", iy);

        if (!isOwned)
        {
            iy = AddBodyLine(sec,
                "[ 스킬 잠김 — 캐릭터를 획득하면 확인할 수 있습니다. ]",
                UITheme.TextDisabled, iy, true);
        }
        else if (def.skills == null || def.skills.Length == 0)
        {
            iy = AddBodyLine(sec, "(등록된 스킬 없음)", UITheme.TextDisabled, iy, false);
        }
        else
        {
            foreach (var skill in def.skills)
            {
                if (skill == null) continue;

                bool unlocked = owned.unlockedSkillIds != null
                    && owned.unlockedSkillIds.Contains(skill.id);
                int  skillLv  = owned.SkillLevel(skill.id);

                // 스킬 이름 + 상태
                string header = $"{(unlocked ? "●" : "○")} {skill.nameKo ?? skill.id}  Lv.{skillLv}";
                iy = AddBodyLine(sec, header, UITheme.TextPrimary, iy, false);

                // 설명 (여러 줄 가능)
                if (!string.IsNullOrEmpty(skill.descKo))
                {
                    iy = AddBodyBlock(sec, skill.descKo, UITheme.TextSecondary, false, iy);
                }

                // MP / 쿨타임
                string meta = $"MP {skill.mpCost}   쿨타임 {skill.cooldown:F1}s";
                iy = AddBodyLine(sec, meta, UITheme.TextDisabled, iy, false);

                iy -= 4f; // 스킬 사이 간격
            }
        }

        float secH = Mathf.Abs(iy) + PAD;
        SetHeight(sec, secH);
        return y - secH - SEC_GAP;
    }

    /// <summary>시너지 섹션</summary>
    static float BuildSynergy(RectTransform parent, CharacterDef def, bool isOwned, float y)
    {
        var sec = CreateSection(parent, "Synergy", y, 0f, UITheme.PanelBgMid);
        float iy = -PAD;

        iy = AddSectionTitle(sec, "시너지", iy);

        if (!isOwned)
        {
            iy = AddBodyLine(sec, "[ 시너지 잠김 ]", UITheme.TextDisabled, iy, true);
        }
        else
        {
            // 시너지 분류
            iy = AddBodyLine(sec, SynergyKindLabel(def.synergyKind), UITheme.TextSecondary, iy, false);

            // 콤보 이름
            if (!string.IsNullOrEmpty(def.synergyComboName))
                iy = AddBodyLine(sec, def.synergyComboName, UITheme.TextPrimary, iy, false);

            // 연계 캐릭터
            if (def.synergyMarkedBy != null && def.synergyMarkedBy.Length > 0)
            {
                string marked = "연계: " + string.Join(", ", def.synergyMarkedBy);
                iy = AddBodyLine(sec, marked, UITheme.TextSecondary, iy, false);
            }
        }

        float secH = Mathf.Abs(iy) + PAD;
        SetHeight(sec, secH);
        return y - secH - SEC_GAP;
    }

    /// <summary>획득 방법 섹션 (보유/미보유 무관 항상 표시)</summary>
    static float BuildAcquire(RectTransform parent, CharacterDef def, bool isOwned, float y)
    {
        var sec = CreateSection(parent, "Acquire", y, 0f, UITheme.PanelBgMid);
        float iy = -PAD;

        iy = AddSectionTitle(sec, "획득 방법", iy);

        bool hasContent = false;

        if (def.gachaObtainable)
        {
            iy = AddBodyLine(sec, "• 가챠 (논문/책)", UITheme.TextSecondary, iy, false);
            hasContent = true;
        }

        if (!string.IsNullOrEmpty(def.acquireCondition))
        {
            iy = AddBodyLine(sec, def.acquireCondition, UITheme.TextSecondary, iy, false);
            hasContent = true;
        }

        if (!hasContent)
            iy = AddBodyLine(sec, "(정보 없음)", UITheme.TextDisabled, iy, false);

        float secH = Mathf.Abs(iy) + PAD;
        SetHeight(sec, secH);
        return y - secH - SEC_GAP;
    }

    // ─────────────────────────────────────────────────────────────────────
    // 단순 텍스트 섹션 헬퍼 (Lore 등에서 사용)
    // ─────────────────────────────────────────────────────────────────────

    static float BuildTextSection(
        RectTransform parent, string name, string title,
        string body, Color bodyColor, bool italic, float y)
    {
        var sec = CreateSection(parent, name, y, 0f, UITheme.PanelBgMid);
        float iy = -PAD;

        iy = AddSectionTitle(sec, title, iy);
        iy = AddBodyBlock(sec, body, bodyColor, italic, iy);

        float secH = Mathf.Abs(iy) + PAD;
        SetHeight(sec, secH);
        return y - secH - SEC_GAP;
    }

    // ─────────────────────────────────────────────────────────────────────
    // 섹션 생성 헬퍼
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 새 섹션 GameObject 를 parent 의 자식으로 생성하고 RectTransform 을 반환한다.
    /// anchorMin=(0,1), anchorMax=(1,1), pivot=(0.5,1) — y 오프셋 방식 배치.
    /// </summary>
    static RectTransform CreateSection(RectTransform parent, string name, float yPos, float height, Color bgColor)
    {
        var go = new GameObject("Sec_" + name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, yPos);
        rt.sizeDelta        = new Vector2(0f, height);

        var img = go.AddComponent<Image>();
        img.color = bgColor;

        return rt;
    }

    static void SetHeight(RectTransform rt, float h)
    {
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, h);
    }

    // ─────────────────────────────────────────────────────────────────────
    // 텍스트 추가 헬퍼 (yPos 반환: 다음 요소가 붙을 y)
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>섹션 제목 텍스트를 추가하고 다음 y 를 반환한다.</summary>
    static float AddSectionTitle(RectTransform parent, string text, float yPos)
    {
        var go = new GameObject("Title", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, yPos);
        rt.sizeDelta        = new Vector2(-PAD * 2f, LINE_H + 4f);

        var t = go.AddComponent<TextMeshProUGUI>();
        t.text      = text;
        t.fontSize  = UITheme.FontH2;
        t.color     = UITheme.TextPrimary;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Left;

        return yPos - (LINE_H + 4f) - 6f;
    }

    /// <summary>한 줄 본문 텍스트를 추가한다 (초과 시 말줄임).</summary>
    static float AddBodyLine(RectTransform parent, string text, Color color, float yPos, bool italic)
    {
        var go = new GameObject("Line", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, yPos);
        rt.sizeDelta        = new Vector2(-PAD * 2f, LINE_H);

        var t = go.AddComponent<TextMeshProUGUI>();
        t.text               = text;
        t.fontSize           = UITheme.FontBody;
        t.color              = color;
        t.fontStyle          = italic ? FontStyles.Italic : FontStyles.Normal;
        t.alignment          = TextAlignmentOptions.Left;
        t.textWrappingMode   = TextWrappingModes.NoWrap;
        t.overflowMode       = TextOverflowModes.Ellipsis;

        return yPos - LINE_H - 2f;
    }

    /// <summary>여러 줄 텍스트 블록을 추가한다 (TextArea 스타일).</summary>
    static float AddBodyBlock(RectTransform parent, string text, Color color, bool italic, float yPos)
    {
        // 줄 수 추정: 명시적 줄바꿈 + 자동 줄바꿈(한글 30자/줄 기준) 중 큰 값
        int explicitLines = CountLines(text);
        int wrappedLines  = text.Length / 30 + 1;
        int lineCount = Mathf.Max(2, Mathf.Max(explicitLines, wrappedLines));
        float blockH = lineCount * LINE_H + 8f;

        var go = new GameObject("Block", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, yPos);
        rt.sizeDelta        = new Vector2(-PAD * 2f, blockH);

        var t = go.AddComponent<TextMeshProUGUI>();
        t.text               = text;
        t.fontSize           = UITheme.FontBody;
        t.color              = color;
        t.fontStyle          = italic ? FontStyles.Italic : FontStyles.Normal;
        t.alignment          = TextAlignmentOptions.TopLeft;
        t.textWrappingMode   = TextWrappingModes.Normal;
        t.overflowMode       = TextOverflowModes.Overflow;

        return yPos - blockH - 4f;
    }

    // ─────────────────────────────────────────────────────────────────────
    // 헤더 전용 헬퍼
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>단일 행 텍스트 (헤더 내 좌측 상단 기준 앵커 배치).</summary>
    static RectTransform MakeTextLine(RectTransform parent, string name, string text,
        float fontSize, Color color, FontStyles style, float yPos)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(0f, yPos);
        rt.sizeDelta        = new Vector2(0f, LINE_H + 4f);

        var t = go.AddComponent<TextMeshProUGUI>();
        t.text      = text;
        t.fontSize  = fontSize;
        t.color     = color;
        t.fontStyle = style;
        t.alignment = TextAlignmentOptions.Left;
        t.overflowMode = TextOverflowModes.Ellipsis;

        return rt;
    }

    /// <summary>배지(색상 배경 + 텍스트)를 헤더 정보 영역에 추가하고 배지 폭을 반환한다.</summary>
    static float MakeBadge(RectTransform parent, string name, string label, Color bgColor, float xPos, float yPos)
    {
        float badgeW = Mathf.Max(36f, label.Length * 9f + 10f);
        const float BADGE_H = 18f;

        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(0f, 1f);
        rt.pivot            = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(xPos, yPos);
        rt.sizeDelta        = new Vector2(badgeW, BADGE_H);

        var img = go.AddComponent<Image>();
        img.color = new Color(bgColor.r * 0.4f, bgColor.g * 0.4f, bgColor.b * 0.4f, 0.85f);

        var txtGO = new GameObject("Label", typeof(RectTransform));
        txtGO.transform.SetParent(go.transform, false);
        var txtRt = (RectTransform)txtGO.transform;
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(3f, 0f);
        txtRt.offsetMax = new Vector2(-3f, 0f);

        var t = txtGO.AddComponent<TextMeshProUGUI>();
        t.text      = label;
        t.fontSize  = UITheme.FontCaption;
        t.color     = Color.Lerp(bgColor, Color.white, 0.5f);
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;

        return badgeW;
    }

    // ─────────────────────────────────────────────────────────────────────
    // 레이블 유틸
    // ─────────────────────────────────────────────────────────────────────

    static string ContinentLabel(Continent c) => c switch
    {
        Continent.Physics   => "물리",
        Continent.Chemistry => "화학",
        Continent.Biology   => "생명",
        Continent.EarthSci  => "지구",
        Continent.Math      => "수학",
        Continent.Info      => "정보",
        _                   => c.ToString(),
    };

    static string RoleLabel(CharacterRole r) => r switch
    {
        CharacterRole.Dealer     => "딜러",
        CharacterRole.Tanker     => "탱커",
        CharacterRole.Supporter  => "서포터",
        CharacterRole.AllRounder => "올라운더",
        _                        => r.ToString(),
    };

    static string SynergyKindLabel(SynergyKind k) => k switch
    {
        SynergyKind.Mark         => "표식 연계",
        SynergyKind.PartyPassive => "파티 패시브",
        SynergyKind.JointUlt     => "합동 필살기",
        _                        => k.ToString(),
    };

    static int CountLines(string text)
    {
        if (string.IsNullOrEmpty(text)) return 1;
        int n = 1;
        foreach (char c in text) if (c == '\n') n++;
        return n;
    }
}
