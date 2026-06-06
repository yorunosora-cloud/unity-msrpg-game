using UnityEngine;
using TMPro;

/// <summary>
/// 데미지 발생·레벨업 시 월드 스페이스에 떠오르며 페이드아웃하는 텍스트.
/// DamageNumber.Spawn / SpawnLevelUp 으로 스폰한다.
/// </summary>
public class DamageNumber : MonoBehaviour
{
    const float Duration   = 0.75f;
    const float RiseSpeed  = 1.8f;

    static TMP_FontAsset _cachedFont;

    TextMeshPro _tmp;
    float       _elapsed;
    Color       _baseColor;

    // ── 폰트 취득 (씬의 TextMeshProUGUI에서 캐시) ──────────────────────────
    static TMP_FontAsset AcquireFont()
    {
        if (_cachedFont != null) return _cachedFont;

        // 1) TMP 전역 설정 시도
        _cachedFont = TMP_Settings.defaultFontAsset;
        if (_cachedFont != null) return _cachedFont;

        // 2) 씬의 모든 TextMeshProUGUI 중 폰트가 있는 것을 찾아 빌림
        foreach (var t in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
        {
            if (t.font != null) { _cachedFont = t.font; return _cachedFont; }
        }

        return null; // 폰트 없으면 null (TMP 기본 폰트로 폴백됨)
    }

    static GameObject BuildTextObject(string name, string text, int fontSize,
                                       Color color, FontStyles style, float scale)
    {
        var go = new GameObject(name);
        go.transform.localScale = Vector3.one * scale;
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.font      = AcquireFont();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = color;
        tmp.fontStyle = style;

        var dn = go.AddComponent<DamageNumber>();
        dn._tmp       = tmp;
        dn._baseColor = color;
        return go;
    }

    /// <summary>데미지 숫자를 월드 스페이스에 스폰합니다.</summary>
    public static void Spawn(Vector3 worldPos, int amount, bool isCrit)
    {
        var go = BuildTextObject(
            "DmgNum",
            isCrit ? $"★{amount}" : amount.ToString(),
            isCrit ? 36 : 28,
            isCrit ? new Color(1f, 0.9f, 0.1f) : Color.white,
            isCrit ? FontStyles.Bold : FontStyles.Normal,
            isCrit ? 0.018f : 0.013f);
        go.transform.position = worldPos + Random.insideUnitSphere * 0.3f;
    }

    /// <summary>레벨업 알림을 플레이어 위치 위에 스폰합니다.</summary>
    public static void SpawnLevelUp(Vector3 playerPos, int newLevel)
    {
        var go = BuildTextObject(
            "LevelUpNotice",
            $"LEVEL UP!  Lv.{newLevel}",
            42,
            new Color(1f, 0.85f, 0f),  // 황금색
            FontStyles.Bold,
            0.025f);
        go.transform.position = playerPos + Vector3.up * 3f;
    }

    void Update()
    {
        _elapsed += Time.deltaTime;
        float t = _elapsed / Duration;

        // 위로 떠오름
        transform.position += Vector3.up * (RiseSpeed * Time.deltaTime);

        // 페이드아웃
        Color c = _baseColor;
        c.a = Mathf.Lerp(1f, 0f, t);
        _tmp.color = c;

        // 카메라 빌보드
        var cam = Camera.main;
        if (cam != null)
            transform.LookAt(transform.position + cam.transform.rotation * Vector3.forward,
                             cam.transform.rotation * Vector3.up);

        if (_elapsed >= Duration)
            Destroy(gameObject);
    }
}
