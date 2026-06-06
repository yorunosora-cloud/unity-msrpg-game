using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 화면 좌하단 플레이어 HP·MP 바 HUD.
/// fillAmount 대신 RectTransform.anchorMax.x를 직접 조정해 크기를 제어한다.
/// 오른쪽에서 왼쪽 방향으로 줄어든다 (anchorMin.x = 0 고정, anchorMax.x = fraction).
/// </summary>
public class CombatHud : MonoBehaviour
{
    [SerializeField] Image    hpFill;
    [SerializeField] Image    mpFill;
    [SerializeField] TMP_Text hpText;
    [SerializeField] TMP_Text mpText;

    PlayerStats _stats;

    void Start() => TryConnect();

    void Update()
    {
        if (_stats == null) TryConnect();
    }

    void TryConnect()
    {
        var s = GameBootstrap.PlayerStats;
        if (s == null || s == _stats) return;
        _stats = s;

        // Simple 타입으로 설정 — 크기는 RectTransform 앵커로 제어
        InitBar(hpFill);
        InitBar(mpFill);

        _stats.OnChanged += Refresh;
        Refresh("init");
    }

    void OnDestroy()
    {
        if (_stats != null)
            _stats.OnChanged -= Refresh;
    }

    static void InitBar(Image img)
    {
        if (img == null) return;
        img.type = Image.Type.Simple;
        img.preserveAspect = false;
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;        // 왼쪽 하단 기준
        rt.anchorMax = Vector2.one;         // 초기 100%
        rt.offsetMin = Vector2.zero;        // 배경에 딱 맞춤
        rt.offsetMax = Vector2.zero;
    }

    void Refresh(string _)
    {
        if (_stats == null) return;
        float hpFrac = Mathf.Clamp01((float)_stats.Hp / _stats.MaxHp);
        float mpFrac = Mathf.Clamp01((float)_stats.Mp / _stats.MaxMp);

        SetBarFraction(hpFill, hpFrac, Color.Lerp(Color.red, new Color(0.15f, 0.85f, 0.3f), hpFrac));
        SetBarFraction(mpFill, mpFrac, new Color(0.2f, 0.5f, 1f));

        if (hpText) hpText.text = $"HP {_stats.Hp}/{_stats.MaxHp}";
        if (mpText) mpText.text = $"MP {_stats.Mp}/{_stats.MaxMp}";
    }

    // anchorMax.x를 fraction으로 설정 → 오른쪽에서 왼쪽으로 줄어듦
    static void SetBarFraction(Image img, float fraction, Color color)
    {
        if (img == null) return;
        img.color = color;
        var rt = img.rectTransform;
        rt.anchorMin = new Vector2(0f,        rt.anchorMin.y);
        rt.anchorMax = new Vector2(fraction,  rt.anchorMax.y);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
