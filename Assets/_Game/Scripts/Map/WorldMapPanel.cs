// Assets/_Game/Scripts/Map/WorldMapPanel.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class WorldMapPanel : MonoBehaviour
{
    [SerializeField] RectTransform mapFrame;
    [SerializeField] RectTransform iconParent;
    [SerializeField] RectTransform playerDot;
    [SerializeField] TMP_Text      tooltip;
    [SerializeField] RawImage      mapBg;
    [SerializeField] float         mapFrameSize = 750f;
    [SerializeField] float         mapHalf      = 250f;
    [SerializeField] bool          isAtlantis   = false;

    public Image[,] FogGrid;

    FogOfWar  _fog;
    Transform _player;
    readonly Dictionary<MapMarker, RectTransform> _icons = new();
    bool _open;
    bool _texGenerated;

    public bool IsOpen => _open;

    void Start()
    {
        _fog    = FindFirstObjectByType<FogOfWar>();
        _player = FindFirstObjectByType<PlayerController>()?.transform;

        if (mapFrame != null)
        {
            var fogGridParent = mapFrame.Find("FogGridParent");
            if (fogGridParent != null)
            {
                FogGrid = new Image[FogOfWar.GRID, FogOfWar.GRID];
                for (int x = 0; x < FogOfWar.GRID; x++)
                for (int y = 0; y < FogOfWar.GRID; y++)
                {
                    var cell = fogGridParent.Find($"FogCell_{x}_{y}");
                    if (cell != null) FogGrid[x, y] = cell.GetComponent<Image>();
                }
            }
        }

        if (!_texGenerated) { GenerateMapTexture(); _texGenerated = true; }
    }

    void Update()
    {
        if (Keyboard.current?.escapeKey.wasPressedThisFrame == true && _open) Close();
        if (_open) Refresh();
    }

    public void Open()
    {
        _open = true;
        gameObject.SetActive(true);
        if (!_texGenerated) { GenerateMapTexture(); _texGenerated = true; }
        Refresh();
    }

    public void Close()
    {
        _open = false;
        gameObject.SetActive(false);
        if (tooltip != null) tooltip.text = "";
    }

    void Refresh()
    {
        if (FogGrid != null && _fog != null)
        {
            for (int x = 0; x < FogOfWar.GRID; x++)
            for (int y = 0; y < FogOfWar.GRID; y++)
            {
                var cell = FogGrid[x, y];
                if (cell != null)
                    cell.gameObject.SetActive(!_fog.IsCellRevealed(x, y));
            }
        }

        var stale = new List<MapMarker>();
        foreach (var kv in _icons)
            if (!MapMarker.All.Contains(kv.Key)) stale.Add(kv.Key);
        foreach (var key in stale)
        {
            if (_icons[key] != null) Destroy(_icons[key].gameObject);
            _icons.Remove(key);
        }

        foreach (var marker in MapMarker.All)
        {
            if (!_icons.TryGetValue(marker, out var rt))
                rt = CreateIcon(marker);
            if (rt == null) continue;

            var wp  = marker.transform.position;
            float x = wp.x / mapHalf * (mapFrameSize * 0.5f);
            float y = wp.z / mapHalf * (mapFrameSize * 0.5f);
            rt.anchoredPosition = new Vector2(x, y);

            var cell   = _fog != null ? _fog.WorldToCell(wp) : new Vector2Int(0, 0);
            bool shown = _fog == null || _fog.IsCellRevealed(cell.x, cell.y);
            rt.gameObject.SetActive(shown);
        }

        if (_player != null && playerDot != null)
        {
            float px = _player.position.x / mapHalf * (mapFrameSize * 0.5f);
            float py = _player.position.z / mapHalf * (mapFrameSize * 0.5f);
            playerDot.anchoredPosition = new Vector2(px, py);
        }
    }

    RectTransform CreateIcon(MapMarker marker)
    {
        var go  = new GameObject(marker.displayName ?? marker.kind.ToString());
        var img = go.AddComponent<Image>();
        img.color = marker.iconColor;
        var btn = go.AddComponent<Button>();
        string dn = marker.displayName ?? marker.kind.ToString();
        btn.onClick.AddListener(() => ShowTooltip(dn));
        var rt  = go.GetComponent<RectTransform>();
        rt.SetParent(iconParent, false);
        rt.sizeDelta = marker.kind == MapMarker.IconKind.Portal
            ? new Vector2(14f, 14f)
            : new Vector2(8f, 8f);
        _icons[marker] = rt;
        return rt;
    }

    void ShowTooltip(string text)
    {
        if (tooltip != null) tooltip.text = text;
    }

    // ── 텍스처 생성 분기 ─────────────────────────────────────────────────────
    void GenerateMapTexture()
    {
        if (isAtlantis) GenerateAtlantisTexture();
        else            GenerateMesoriaTexture();
    }

    // ── 메조리아 위성뷰 텍스처 ───────────────────────────────────────────────
    void GenerateMesoriaTexture()
    {
        if (mapBg == null) return;

        const int RES = 256;
        var tex = new Texture2D(RES, RES, TextureFormat.RGB24, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode   = TextureWrapMode.Clamp;

        var cGround    = new Color(0.46f, 0.38f, 0.26f);
        var cGrass     = new Color(0.28f, 0.48f, 0.16f);
        var cCobble    = new Color(0.56f, 0.51f, 0.40f);
        var cPlaza     = new Color(0.52f, 0.50f, 0.46f);
        var cSea       = new Color(0.18f, 0.45f, 0.72f);
        var cDockWood  = new Color(0.52f, 0.36f, 0.18f);
        var cTileRed   = new Color(0.55f, 0.25f, 0.12f);
        var cGold      = new Color(0.78f, 0.66f, 0.25f);
        var cGoldBrt   = new Color(0.98f, 0.86f, 0.32f);
        var cTSlate    = new Color(0.28f, 0.25f, 0.38f);
        var cTGold     = new Color(0.85f, 0.72f, 0.20f);
        var cWater     = new Color(0.32f, 0.68f, 0.92f);
        var cStDark    = new Color(0.22f, 0.14f, 0.08f);
        var cStWarm    = new Color(0.92f, 0.88f, 0.78f);

        float scale = mapHalf * 2f;

        for (int px = 0; px < RES; px++)
        for (int py = 0; py < RES; py++)
        {
            float wx    = ((float)px / RES - 0.5f) * scale;
            float wz    = ((float)py / RES - 0.5f) * scale;
            float awx   = Mathf.Abs(wx);
            float awz_g = Mathf.Abs(wz - 65f);
            float fd2   = wx*wx + wz*wz;

            var c = cGround;

            if (awx > 140f || wz < -170f) c = cGrass;
            if (wz > 140f) c = cSea;
            if (wz > 137f && wz < 173f && awx < 90f) c = cDockWood;
            if (awx < 13f && wz > -130f && wz < 140f) c = cCobble;
            if (fd2 < 2500f) c = cPlaza;
            if (awx < 22f && awz_g < 25f) c = cPlaza;
            if (wz > -133f && wz < -127f && awx < 80f) c = cStDark;
            if (wz > -133f && wz < -127f && awx < 13f) c = cCobble;
            if (fd2 < 169f) c = cWater;
            if (fd2 <  25f) c = cStWarm;

            if (wx > -97f && wx < -77f && wz > -3f  && wz <  23f) c = cTileRed;
            if (wx > -92f && wx < -82f && wz >  2f  && wz <  18f) c = cGold;
            if (wx >  77f && wx <  97f && wz > -3f  && wz <  23f) c = cTileRed;
            if (wx >  82f && wx <  92f && wz >  2f  && wz <  18f) c = cGoldBrt;
            if (wx > -75f && wx < -55f && wz > -94f && wz < -66f) c = cTileRed;
            if (wx > -71f && wx < -59f && wz > -90f && wz < -70f) c = cGold;

            if (awx < 11f && awz_g < 11f) c = cTSlate;
            if (awx <  6f && awz_g <  6f) c = cTGold;

            if (wx > -56f && wx < -44f)
            {
                if (wz > -79f  && wz < -61f)  c = cTileRed;
                if (wz > -104f && wz < -86f)  c = cTileRed;
                if (wz > -127f && wz < -109f) c = cTileRed;
            }
            if (wx > 44f && wx < 56f)
            {
                if (wz > -79f  && wz < -61f)  c = cTileRed;
                if (wz > -104f && wz < -86f)  c = cTileRed;
                if (wz > -127f && wz < -109f) c = cTileRed;
            }

            tex.SetPixel(px, py, c);
        }

        tex.Apply();
        mapBg.texture = tex;
        mapBg.color   = Color.white;
    }

    // ── 아틀란티스 위성뷰 텍스처 ─────────────────────────────────────────────
    // 아크시움: 월드 (0, 0, 0), 반경 250
    // 유클리드 평원: 월드 (-1000, 0, -600), 반경 350
    // mapHalf = 1600 (설정값과 일치)
    void GenerateAtlantisTexture()
    {
        if (mapBg == null) return;

        const int RES = 256;
        var tex = new Texture2D(RES, RES, TextureFormat.RGB24, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode   = TextureWrapMode.Clamp;

        // AtlantisHubBuilder 팔레트
        var cVoid     = new Color(0.04f, 0.06f, 0.14f);
        var cGrass    = new Color(0.55f, 0.62f, 0.42f);
        var cRock     = new Color(0.38f, 0.34f, 0.28f);
        var cGold     = new Color(0.78f, 0.82f, 0.00f);
        var cLeaf     = new Color(0.15f, 0.48f, 0.05f);
        var cBark     = new Color(0.32f, 0.22f, 0.10f);
        var cEuclid   = new Color(0.90f, 0.92f, 0.88f);
        var cEuclidIn = new Color(0.75f, 0.78f, 0.72f);
        var cBridge   = new Color(0.45f, 0.32f, 0.14f);
        var cPortal   = new Color(0.28f, 0.28f, 0.28f);

        const float AX = 0f,    AZ = 0f,    AR = 250f;   // 아크시움
        const float EX = -1000f, EZ = -600f, ER = 350f;   // 유클리드

        // 아크시움→유클리드 단위 방향 벡터
        float dLen = Mathf.Sqrt((EX-AX)*(EX-AX) + (EZ-AZ)*(EZ-AZ)); // ≈ 1166
        float uX = (EX-AX) / dLen, uZ = (EZ-AZ) / dLen;

        float scale = mapHalf * 2f;

        for (int px = 0; px < RES; px++)
        for (int py = 0; py < RES; py++)
        {
            float wx = ((float)px / RES - 0.5f) * scale;
            float wz = ((float)py / RES - 0.5f) * scale;

            var c = cVoid;

            // 세계수 가지 다리 (아크시움 가장자리 → 유클리드 가장자리)
            {
                float bx = wx - AX, bz = wz - AZ;
                float bt = Mathf.Clamp(bx * uX + bz * uZ, AR, dLen - ER);
                float bdx = bx - uX * bt, bdz = bz - uZ * bt;
                if (bdx*bdx + bdz*bdz < 144f) c = cBridge; // 반경 12
            }

            // 유클리드 평원 섬
            {
                float dx = wx - EX, dz = wz - EZ;
                float d2 = dx*dx + dz*dz;
                if (d2 < ER * ER)
                {
                    c = cRock;
                    float inner = ER - 30f;
                    if (d2 < inner*inner)   c = cEuclid;
                    float core = ER - 80f;
                    if (d2 < core*core)     c = cEuclidIn;
                    // 유클리드 대성당 표시 (섬 중심)
                    if (d2 < 1600f)         c = cPortal;
                    if (d2 < 400f)          c = new Color(0.98f, 0.96f, 0.85f);
                }
            }

            // 아크시움 섬 (최상위 — 다리/유클리드 위에 그림)
            {
                float dx = wx - AX, dz = wz - AZ;
                float d2 = dx*dx + dz*dz;
                if (d2 < AR * AR)
                {
                    c = cRock;
                    float inner = AR - 25f;
                    if (d2 < inner*inner)   c = cGrass;
                    if (d2 < 10000f)        c = cLeaf;   // 세계수 잎 r=100
                    if (d2 < 2500f)         c = cBark;   // 줄기 r=50
                    if (d2 < 625f)          c = cGold;   // 코어 r=25
                    if (d2 < 100f)          c = new Color(0.98f, 1.00f, 0.30f); // 중심 점
                }
            }

            tex.SetPixel(px, py, c);
        }

        tex.Apply();
        mapBg.texture = tex;
        mapBg.color   = Color.white;
    }
}
