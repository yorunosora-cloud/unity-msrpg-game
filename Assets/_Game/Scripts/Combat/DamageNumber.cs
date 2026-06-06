using UnityEngine;
using TMPro;

/// <summary>
/// 데미지 발생 시 월드 스페이스에 떠오르며 페이드아웃하는 텍스트.
/// DamageNumber.Spawn(pos, amount, isCrit)으로 스폰한다.
/// </summary>
[RequireComponent(typeof(TextMeshPro))]
public class DamageNumber : MonoBehaviour
{
    const float Duration   = 0.75f;
    const float RiseSpeed  = 1.8f;

    TextMeshPro _tmp;
    float       _elapsed;
    Color       _baseColor;

    /// <summary>데미지 숫자를 월드 스페이스에 스폰합니다.</summary>
    public static void Spawn(Vector3 worldPos, int amount, bool isCrit)
    {
        var go  = new GameObject("DmgNum");
        go.transform.position = worldPos + Random.insideUnitSphere * 0.3f;
        go.transform.localScale = Vector3.one * (isCrit ? 0.018f : 0.013f);

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text      = isCrit ? $"★{amount}" : amount.ToString();
        tmp.fontSize  = isCrit ? 36 : 28;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = isCrit ? new Color(1f, 0.9f, 0.1f) : Color.white;
        tmp.fontStyle = isCrit ? FontStyles.Bold : FontStyles.Normal;

        var dn = go.AddComponent<DamageNumber>();
        dn._tmp       = tmp;
        dn._baseColor = tmp.color;
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
