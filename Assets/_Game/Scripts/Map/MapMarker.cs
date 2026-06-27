using System.Collections.Generic;
using UnityEngine;

public class MapMarker : MonoBehaviour
{
    public enum IconKind { Building, Portal, Harbor, Gate }

    [SerializeField] public IconKind kind;
    [SerializeField] public string   displayName;
    [SerializeField] public Color    iconColor    = Color.white;
    [SerializeField] public float    footprintW   = 0f;  // 월드 단위 너비 (0 = 점 마커)
    [SerializeField] public float    footprintD   = 0f;  // 월드 단위 깊이

    public static readonly List<MapMarker> All = new List<MapMarker>();

    void OnEnable()  { if (!All.Contains(this)) All.Add(this); }
    void OnDisable() { All.Remove(this); }
}
