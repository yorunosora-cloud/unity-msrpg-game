using UnityEngine;

/// <summary>
/// 스킬 발동 시 범위를 지면 위에 LineRenderer로 표시하고 서서히 페이드아웃한다.
/// Strike/Aoe/Mark → 부채꼴 아웃라인, HealBuff → 원형 아웃라인.
/// </summary>
public static class SkillRangeVisualizer
{
    // effectKind 인덱스 순서 (Strike=0, Aoe=1, HealBuff=2, Mark=3)
    static readonly Color[] KindColors =
    {
        new Color(1.0f, 0.20f, 0.20f, 0.95f),  // Strike   — 빨강
        new Color(1.0f, 0.55f, 0.10f, 0.95f),  // Aoe      — 주황
        new Color(0.20f, 1.0f, 0.30f, 0.95f),  // HealBuff — 초록
        new Color(0.80f, 0.30f, 1.0f, 0.95f),  // Mark     — 보라
    };

    const float ConeFadeDuration   = 0.55f;
    const float CircleFadeDuration = 0.65f;
    const int   ConeSegments       = 24;
    const int   CircleSegments     = 36;
    const float HealRadius         = 2.0f;  // HealBuff 원 반경

    static Material _mat;

    static Material GetMat()
    {
        // Unity가 파괴한 경우(씬 전환 등) 재생성
        if (_mat != null) return _mat;

        var shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        _mat = new Material(shader);
        _mat.hideFlags = HideFlags.HideAndDontSave;
        return _mat;
    }

    /// <summary>스킬 발동 직후 호출. skill과 origin 기준으로 범위 시각화.</summary>
    public static void Show(SkillDef skill, Transform origin)
    {
        if (skill == null || origin == null) return;

        Color c = KindColors[(int)skill.effectKind % KindColors.Length];

        if (skill.effectKind == SkillEffectKind.HealBuff)
            SpawnCircle(origin.position, HealRadius, c, CircleFadeDuration);
        else
            SpawnCone(origin.position, origin.forward, skill.range, skill.halfAngle, c, ConeFadeDuration);
    }

    // ── 부채꼴 ─────────────────────────────────────────────────────────────

    static void SpawnCone(Vector3 pos, Vector3 forward, float range,
                           float halfAngle, Color color, float duration)
    {
        // 포인트 구조: [origin, arc_0 ... arc_N, origin] 로 닫힌 폴리라인
        int   total  = ConeSegments + 3; // origin + arc(N+1개) + 닫음
        var   pts    = new Vector3[total];
        float yFloor = pos.y + 0.06f;

        pts[0] = new Vector3(pos.x, yFloor, pos.z);

        for (int i = 0; i <= ConeSegments; i++)
        {
            float angle = Mathf.Lerp(-halfAngle, halfAngle, (float)i / ConeSegments);
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * new Vector3(forward.x, 0f, forward.z).normalized;
            pts[i + 1]  = new Vector3(pos.x + dir.x * range, yFloor, pos.z + dir.z * range);
        }

        pts[total - 1] = pts[0]; // 닫기

        Spawn(pts, 0.05f, color, duration);
    }

    // ── 원형 ───────────────────────────────────────────────────────────────

    static void SpawnCircle(Vector3 pos, float radius, Color color, float duration)
    {
        var   pts    = new Vector3[CircleSegments + 1];
        float yFloor = pos.y + 0.06f;

        for (int i = 0; i <= CircleSegments; i++)
        {
            float angle = (float)i / CircleSegments * Mathf.PI * 2f;
            pts[i] = new Vector3(
                pos.x + Mathf.Sin(angle) * radius,
                yFloor,
                pos.z + Mathf.Cos(angle) * radius);
        }

        Spawn(pts, 0.07f, color, duration);
    }

    // ── 공통 LineRenderer 스폰 ─────────────────────────────────────────────

    static void Spawn(Vector3[] points, float width, Color color, float duration)
    {
        var go = new GameObject("SkillRange");
        var lr = go.AddComponent<LineRenderer>();
        lr.material      = GetMat();
        lr.startWidth    = width;
        lr.endWidth      = width;
        lr.positionCount = points.Length;
        lr.SetPositions(points);
        lr.useWorldSpace = true;
        lr.startColor    = color;
        lr.endColor      = color;
        lr.numCornerVertices = 2;

        var fade = go.AddComponent<SkillRangeFade>();
        fade.Init(color, duration);
    }
}

/// <summary>
/// LineRenderer 알파를 duration 동안 서서히 0으로 줄이고 자동 소멸.
/// SkillRangeVisualizer와 같은 파일에 배치 — 외부에서 직접 사용하지 않는다.
/// </summary>
public class SkillRangeFade : MonoBehaviour
{
    LineRenderer _lr;
    Color        _color;
    float        _duration;
    float        _elapsed;

    public void Init(Color color, float duration)
    {
        _lr       = GetComponent<LineRenderer>();
        _color    = color;
        _duration = duration;
    }

    void Update()
    {
        if (_lr == null) { Destroy(gameObject); return; }

        _elapsed += Time.deltaTime;
        float alpha = Mathf.Lerp(_color.a, 0f, _elapsed / _duration);
        var   c     = new Color(_color.r, _color.g, _color.b, alpha);
        _lr.startColor = c;
        _lr.endColor   = c;

        if (_elapsed >= _duration)
            Destroy(gameObject);
    }
}
