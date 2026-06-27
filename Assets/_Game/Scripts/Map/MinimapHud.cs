// Assets/_Game/Scripts/Map/MinimapHud.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimapHud : MonoBehaviour
{
    [SerializeField] RectTransform iconParent;
    [SerializeField] RectTransform playerDot;
    [SerializeField] float         mapSize   = 160f;
    [SerializeField] float         viewRange = 50f;  // 로컬 뷰 반경 (월드 단위)

    FogOfWar _fog;
    Transform _player;
    readonly Dictionary<MapMarker, RectTransform> _icons = new();

    void Start()
    {
        _fog    = FindFirstObjectByType<FogOfWar>();
        _player = FindFirstObjectByType<PlayerController>()?.transform;
    }

    void Update()
    {
        var stale = new List<MapMarker>();
        foreach (var kv in _icons)
            if (!MapMarker.All.Contains(kv.Key)) stale.Add(kv.Key);
        foreach (var key in stale)
        {
            if (_icons[key] != null) Destroy(_icons[key].gameObject);
            _icons.Remove(key);
        }

        if (_player != null && playerDot != null)
            playerDot.anchoredPosition = Vector2.zero;

        float originX = _player != null ? _player.position.x : 0f;
        float originZ = _player != null ? _player.position.z : 0f;
        float rangesSq = viewRange * viewRange;

        foreach (var marker in MapMarker.All)
        {
            if (!_icons.TryGetValue(marker, out var rt))
                rt = CreateIcon(marker);

            if (rt == null) continue;

            var wp = marker.transform.position;
            float dx = wp.x - originX;
            float dz = wp.z - originZ;

            bool inRange = (dx * dx + dz * dz) <= rangesSq;
            if (!inRange) { rt.gameObject.SetActive(false); continue; }

            float x = dx / viewRange * (mapSize * 0.5f);
            float y = dz / viewRange * (mapSize * 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.gameObject.SetActive(true);
        }
    }

    RectTransform CreateIcon(MapMarker marker)
    {
        var go  = new GameObject(marker.displayName ?? marker.kind.ToString());
        var img = go.AddComponent<Image>();
        img.color = marker.iconColor;
        var rt  = go.GetComponent<RectTransform>();
        rt.SetParent(iconParent, false);
        rt.sizeDelta = new Vector2(6f, 6f);
        _icons[marker] = rt;
        return rt;
    }
}
