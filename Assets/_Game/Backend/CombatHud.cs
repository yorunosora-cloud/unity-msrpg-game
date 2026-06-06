using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 화면 좌하단 플레이어 HP·MP 바 HUD.
/// GameBootstrap.PlayerStats.OnChanged를 구독해 자동 갱신.
/// MetaUISetup 에디터 메뉴가 자동으로 배치합니다.
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
        // GameBootstrap.Awake가 CombatHud.Start보다 늦을 경우 매 프레임 재시도
        if (_stats == null) TryConnect();
    }

    void TryConnect()
    {
        var s = GameBootstrap.PlayerStats;
        if (s == null || s == _stats) return;
        _stats = s;

        // fillAmount가 작동하려면 반드시 Filled 타입이어야 함
        // 씬 직렬화 불일치를 방어해 런타임에서 강제 설정
        SetFilled(hpFill);
        SetFilled(mpFill);

        _stats.OnChanged += Refresh;
        Refresh("init");
    }

    static void SetFilled(Image img)
    {
        if (img == null) return;
        img.type       = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
    }

    void OnDestroy()
    {
        if (_stats != null)
            _stats.OnChanged -= Refresh;
    }

    void Refresh(string _)
    {
        if (_stats == null) return;
        float hpFrac = (float)_stats.Hp / _stats.MaxHp;
        float mpFrac = (float)_stats.Mp / _stats.MaxMp;

        if (hpFill) hpFill.fillAmount = hpFrac;
        if (mpFill) mpFill.fillAmount = mpFrac;

        // 색상: 잔량에 따라 초록→빨강 (HP), 파랑 고정 (MP)
        if (hpFill) hpFill.color = Color.Lerp(Color.red, new Color(0.15f, 0.85f, 0.3f), hpFrac);

        if (hpText) hpText.text = $"HP {_stats.Hp}/{_stats.MaxHp}";
        if (mpText) mpText.text = $"MP {_stats.Mp}/{_stats.MaxMp}";
    }
}
