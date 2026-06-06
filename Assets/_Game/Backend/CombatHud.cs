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

    void Start()
    {
        _stats = GameBootstrap.PlayerStats;
        if (_stats == null)
        {
            Debug.LogWarning("[CombatHud] PlayerStats가 없습니다. GameBootstrap 초기화를 확인하세요.");
            return;
        }
        Refresh("init");
        _stats.OnChanged += Refresh;
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
