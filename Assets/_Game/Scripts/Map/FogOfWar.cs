using UnityEngine;

/// <summary>
/// 플레이어를 중심으로 한 원형 안개 해제 시스템.
/// 8×8 격자 대신 RES×RES 부동소수 버퍼를 사용해 부드러운 그라디언트 원을 만든다.
/// 한번 해제된 영역은 영구 유지(누적 max).
/// </summary>
public class FogOfWar : MonoBehaviour
{
    public const int RES = 128;   // 해제 버퍼 해상도 (RES×RES)

    [SerializeField] float mapHalf    = 250f;
    [SerializeField] float viewRange  = 50f;
    [SerializeField, Range(0f, 1f)] float featherFrac = 0.35f;  // 가장자리 페이드 비율

    readonly float[] _reveal = new float[RES * RES];  // 0=완전 안개, 1=완전 해제

    /// <summary>버퍼가 바뀔 때마다 증가 → WorldMapPanel이 텍스처 재생성 throttle에 사용</summary>
    public int Version { get; private set; }

    public int     Resolution   => RES;
    public float[] RevealBuffer => _reveal;

    /// <summary>월드 좌표에서의 해제도(0..1) — 마커 가시성 판정용</summary>
    public float SampleReveal(Vector3 worldPos)
    {
        float u  = (worldPos.x + mapHalf) / (mapHalf * 2f);
        float v  = (worldPos.z + mapHalf) / (mapHalf * 2f);
        int   tx = Mathf.Clamp(Mathf.FloorToInt(u * RES), 0, RES - 1);
        int   ty = Mathf.Clamp(Mathf.FloorToInt(v * RES), 0, RES - 1);
        return _reveal[ty * RES + tx];
    }

    void Update()
    {
        RevealCircle(transform.position);
    }

    void RevealCircle(Vector3 pos)
    {
        float worldPerTexel = mapHalf * 2f / RES;
        float fx = (pos.x + mapHalf) / (mapHalf * 2f) * RES;
        float fy = (pos.z + mapHalf) / (mapHalf * 2f) * RES;

        float radTex   = viewRange / worldPerTexel;
        float innerTex = radTex * (1f - featherFrac);

        int x0 = Mathf.Clamp(Mathf.FloorToInt(fx - radTex), 0, RES - 1);
        int x1 = Mathf.Clamp(Mathf.CeilToInt (fx + radTex), 0, RES - 1);
        int y0 = Mathf.Clamp(Mathf.FloorToInt(fy - radTex), 0, RES - 1);
        int y1 = Mathf.Clamp(Mathf.CeilToInt (fy + radTex), 0, RES - 1);

        bool changed = false;
        for (int ty = y0; ty <= y1; ty++)
        for (int tx = x0; tx <= x1; tx++)
        {
            float dx = tx + 0.5f - fx;
            float dy = ty + 0.5f - fy;
            float d  = Mathf.Sqrt(dx * dx + dy * dy);

            if (d >= radTex) continue;   // 범위 밖 건너뜀

            float target;
            if (d <= innerTex)
                target = 1f;
            else
            {
                float t = (d - innerTex) / (radTex - innerTex);
                target = 1f - t * t * (3f - 2f * t);  // smoothstep 감쇠
            }

            int idx = ty * RES + tx;
            if (target > _reveal[idx]) { _reveal[idx] = target; changed = true; }
        }
        if (changed) Version++;
    }
}
