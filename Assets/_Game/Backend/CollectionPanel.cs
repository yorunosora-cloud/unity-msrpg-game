using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>보유 캐릭터 컬렉션(도감) 패널. 대륙·레어리티 색상 행 목록.</summary>
public class CollectionPanel : MonoBehaviour
{
    [SerializeField] RectTransform contentRoot;

    CharacterDatabase _db;

    void OnEnable()
    {
        _db = Resources.Load<CharacterDatabase>("CharacterDatabase");
        Refresh();
        if (MetaState.IsInitialized)
            MetaState.Roster.OnChanged += Refresh;
    }

    void OnDisable()
    {
        if (MetaState.IsInitialized)
            MetaState.Roster.OnChanged -= Refresh;
        UIManager.Close();
    }

    void Refresh()
    {
        if (contentRoot == null || !MetaState.IsInitialized) return;

        while (contentRoot.childCount > 0)
            Object.DestroyImmediate(contentRoot.GetChild(0).gameObject);

        var owned = MetaState.Roster.Owned;

        if (owned.Count == 0)
        {
            BuildEmpty(contentRoot, "보유 캐릭터가 없습니다.\n가챠를 뽑아 캐릭터를 획득해보세요!");
            contentRoot.sizeDelta = new Vector2(0, 80f);
            return;
        }

        const float ROW_H = 68f;
        const float GAP   = 3f;
        const float PAD   = 4f;
        float y = -PAD;

        foreach (var oc in owned)
        {
            BuildCharRow(contentRoot, oc, _db?.ById(oc.id), y);
            y -= ROW_H + GAP;
        }
        contentRoot.sizeDelta = new Vector2(0, Mathf.Abs(y) + PAD);
    }

    public void OnCloseClicked() => gameObject.SetActive(false);

    // ── 행 생성 ──────────────────────────────────────────────────────────

    static void BuildEmpty(RectTransform parent, string msg)
    {
        var go = new GameObject("Empty", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        SetRowRect((RectTransform)go.transform, 0f, 80f);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text      = msg;
        t.fontSize  = UITheme.FontBody;
        t.color     = UITheme.TextSecondary;
        t.alignment = TextAlignmentOptions.Center;
    }

    static void BuildCharRow(RectTransform parent, OwnedCharacter oc, CharacterDef def, float yPos)
    {
        const float ROW_H = 68f;

        var rarity  = def?.rarity    ?? Rarity.N;
        var contId  = def?.continent ?? Continent.Mesoria;
        var rarCol  = rarity.DisplayColor();
        var contCol = ContinentColors.Of(contId);

        var row = new GameObject("Row_" + oc.id, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        SetRowRect((RectTransform)row.transform, yPos, ROW_H);

        // 배경: 레어리티 색의 매우 어두운 틴트
        row.AddComponent<Image>().color = new Color(rarCol.r * 0.13f, rarCol.g * 0.13f, rarCol.b * 0.13f, 1f);

        // 좌측 대륙 컬러 스트립
        AddStrip(row.transform, contCol);

        // 캐릭터 이름 (좌)
        var nameGO = new GameObject("Name", typeof(RectTransform));
        nameGO.transform.SetParent(row.transform, false);
        var nRt = (RectTransform)nameGO.transform;
        nRt.anchorMin = new Vector2(0f, 0f); nRt.anchorMax = new Vector2(0.52f, 1f);
        nRt.offsetMin = new Vector2(16f, 0f); nRt.offsetMax = Vector2.zero;
        var nTxt = nameGO.AddComponent<TextMeshProUGUI>();
        nTxt.text      = def?.nameKo ?? oc.id;
        nTxt.fontSize  = UITheme.FontH2;
        nTxt.color     = UITheme.TextPrimary;
        nTxt.fontStyle = FontStyles.Bold;
        nTxt.alignment = TextAlignmentOptions.MidlineLeft;

        // 등급·레벨·스킬 (우)
        var infoGO = new GameObject("Info", typeof(RectTransform));
        infoGO.transform.SetParent(row.transform, false);
        var iRt = (RectTransform)infoGO.transform;
        iRt.anchorMin = new Vector2(0.52f, 0f); iRt.anchorMax = Vector2.one;
        iRt.offsetMin = Vector2.zero;            iRt.offsetMax = new Vector2(-10f, 0f);
        var iTxt = infoGO.AddComponent<TextMeshProUGUI>();

        int sUnlocked = (oc.unlockedSkillIds == null || oc.unlockedSkillIds.Count == 0)
            ? 1 : oc.unlockedSkillIds.Count;
        int sTotal = def?.skills?.Length ?? 1;
        sUnlocked = System.Math.Min(sUnlocked, sTotal);

        string dup    = oc.dupes > 0 ? $"  <color=#888>+{oc.dupes}</color>" : "";
        string rarHex = ColorUtility.ToHtmlStringRGB(rarCol);
        string dots   = SkillDots(sUnlocked, sTotal);

        iTxt.text      = $"<b><color=#{rarHex}>{rarity}</color></b>  Lv.{oc.level}{dup}\n<size=10>{dots}</size>";
        iTxt.fontSize  = UITheme.FontBody;
        iTxt.color     = UITheme.TextSecondary;
        iTxt.alignment = TextAlignmentOptions.MidlineRight;
    }

    // ── 공용 헬퍼 ────────────────────────────────────────────────────────

    static void SetRowRect(RectTransform rt, float yPos, float height)
    {
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, yPos);
        rt.sizeDelta        = new Vector2(0f, height);
    }

    static void AddStrip(Transform parent, Color color)
    {
        var go = new GameObject("Strip", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = new Vector2(0f, 1f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = new Vector2(6f, 0f);
        go.AddComponent<Image>().color = color;
    }

    static string SkillDots(int unlocked, int total)
    {
        var sb = new System.Text.StringBuilder("스킬 ");
        for (int i = 0; i < total; i++)
            sb.Append(i < unlocked ? "●" : "○");
        return sb.ToString();
    }
}
