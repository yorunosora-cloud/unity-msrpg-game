using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>결정 인벤토리 패널. 대륙별 컬러 행 목록.</summary>
public class InventoryPanel : MonoBehaviour
{
    [SerializeField] RectTransform contentRoot;

    void OnEnable()
    {
        Refresh();
        if (MetaState.IsInitialized)
            MetaState.Crystals.OnChanged += Refresh;
    }

    void OnDisable()
    {
        if (MetaState.IsInitialized)
            MetaState.Crystals.OnChanged -= Refresh;
        UIManager.Close();
    }

    void Refresh()
    {
        if (contentRoot == null || !MetaState.IsInitialized) return;

        while (contentRoot.childCount > 0)
            Object.DestroyImmediate(contentRoot.GetChild(0).gameObject);

        var kinds  = (CrystalKind[])System.Enum.GetValues(typeof(CrystalKind));
        const float ROW_H = 62f;
        const float GAP   = 3f;
        const float PAD   = 4f;
        float y   = -PAD;
        bool  any = false;

        foreach (var kind in kinds)
        {
            int amt = MetaState.Crystals.Get(kind);
            if (amt <= 0) continue;
            BuildCrystalRow(contentRoot, kind, amt, y);
            y -= ROW_H + GAP;
            any = true;
        }

        if (!any)
        {
            BuildEmpty(contentRoot, "보유 결정이 없습니다.");
            contentRoot.sizeDelta = new Vector2(0, 80f);
        }
        else
        {
            contentRoot.sizeDelta = new Vector2(0, Mathf.Abs(y) + PAD);
        }
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

    static void BuildCrystalRow(RectTransform parent, CrystalKind kind, int amount, float yPos)
    {
        const float ROW_H  = 62f;
        var contCol = ContinentColors.Of(KindToContinent(kind));

        var row = new GameObject("Row_" + kind, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        SetRowRect((RectTransform)row.transform, yPos, ROW_H);

        row.AddComponent<Image>().color =
            new Color(contCol.r * 0.11f, contCol.g * 0.11f, contCol.b * 0.11f, 1f);

        AddStrip(row.transform, contCol);

        // 이름 (좌)
        var nameGO = new GameObject("Name", typeof(RectTransform));
        nameGO.transform.SetParent(row.transform, false);
        var nRt = (RectTransform)nameGO.transform;
        nRt.anchorMin = new Vector2(0f, 0f); nRt.anchorMax = new Vector2(0.72f, 1f);
        nRt.offsetMin = new Vector2(16f, 0f); nRt.offsetMax = Vector2.zero;
        var nTxt = nameGO.AddComponent<TextMeshProUGUI>();
        string labelHex = ColorUtility.ToHtmlStringRGB(contCol);
        nTxt.text      = $"<size=11><color=#{labelHex}>{CrystalCatalog.ContinentLabel(kind)}</color></size>  {CrystalCatalog.DisplayName(kind)}";
        nTxt.fontSize  = UITheme.FontBody;
        nTxt.color     = UITheme.TextPrimary;
        nTxt.alignment = TextAlignmentOptions.MidlineLeft;

        // 수량 (우)
        var amtGO = new GameObject("Amount", typeof(RectTransform));
        amtGO.transform.SetParent(row.transform, false);
        var aRt = (RectTransform)amtGO.transform;
        aRt.anchorMin = new Vector2(0.72f, 0f); aRt.anchorMax = Vector2.one;
        aRt.offsetMin = Vector2.zero;            aRt.offsetMax = new Vector2(-12f, 0f);
        var aTxt = amtGO.AddComponent<TextMeshProUGUI>();
        aTxt.text      = $"×{amount:N0}";
        aTxt.fontSize  = UITheme.FontStat;
        aTxt.color     = contCol;
        aTxt.fontStyle = FontStyles.Bold;
        aTxt.alignment = TextAlignmentOptions.MidlineRight;
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

    static Continent KindToContinent(CrystalKind kind) => kind switch
    {
        CrystalKind.PrimeForce      => Continent.Physics,
        CrystalKind.ElementaCrystal => Continent.Chemistry,
        CrystalKind.LifeCode        => Continent.Biology,
        CrystalKind.MemoryOfStar    => Continent.EarthSci,
        CrystalKind.BoneOfTheEarth  => Continent.EarthSci,
        CrystalKind.OceanPrime      => Continent.EarthSci,
        CrystalKind.TornadoCore     => Continent.EarthSci,
        CrystalKind.Axioma          => Continent.Math,
        CrystalKind.PrimeData       => Continent.Info,
        _                           => Continent.Mesoria,
    };
}
