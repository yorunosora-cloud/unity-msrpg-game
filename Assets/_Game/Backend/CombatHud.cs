using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 화면 좌하단 플레이어 HP·MP 바 HUD.
/// HP/MP 감소 시 부드럽게 애니메이션, 회복은 즉시 반영.
/// 현재 활성 캐릭터(PlayerRuntime.Active) 기준으로 표시.
/// </summary>
public class CombatHud : MonoBehaviour
{
    [SerializeField] Image    hpFill;
    [SerializeField] Image    mpFill;
    [SerializeField] TMP_Text hpText;
    [SerializeField] TMP_Text mpText;

    const float DrainSpeed = 1.5f;

    float _dispHpFrac = 1f;
    float _targHpFrac = 1f;
    float _dispMpFrac = 1f;
    float _targMpFrac = 1f;

    CombatCharacter _active;
    Party           _party;

    void Start() => TryConnect();

    void Update()
    {
        if (_active == null || _party != PlayerRuntime.Party)
            TryConnect();

        AnimateBars();
    }

    void OnDestroy()
    {
        UnsubscribeActive();
        UnsubscribeParty();
    }

    // ── 연결 ──────────────────────────────────────────────────────────────

    void TryConnect()
    {
        var newParty = PlayerRuntime.Party;
        if (newParty != null && newParty != _party)
        {
            UnsubscribeParty();
            _party = newParty;
            _party.OnActiveChanged += OnActiveCharacterChanged;
        }

        var newActive = PlayerRuntime.Active;
        if (newActive != null && newActive != _active)
        {
            UnsubscribeActive();
            _active = newActive;
            _active.OnChanged += Refresh;

            InitBar(hpFill);
            InitBar(mpFill);

            // 연결 시 실제 값으로 초기화 (애니메이션 없이)
            _dispHpFrac = _targHpFrac = _active.MaxHp > 0 ? (float)_active.Hp / _active.MaxHp : 1f;
            _dispMpFrac = _targMpFrac = _active.MaxMp > 0 ? (float)_active.Mp / _active.MaxMp : 1f;

            Refresh("init");
        }
    }

    void UnsubscribeActive()
    {
        if (_active != null) { _active.OnChanged -= Refresh; _active = null; }
    }

    void UnsubscribeParty()
    {
        if (_party != null) { _party.OnActiveChanged -= OnActiveCharacterChanged; _party = null; }
    }

    void OnActiveCharacterChanged()
    {
        UnsubscribeActive();
        TryConnect();
    }

    // ── 애니메이션 ────────────────────────────────────────────────────────

    void AnimateBars()
    {
        if (_active == null) return;

        bool hpDec = _targHpFrac < _dispHpFrac;
        _dispHpFrac = hpDec
            ? Mathf.MoveTowards(_dispHpFrac, _targHpFrac, DrainSpeed * Time.deltaTime)
            : _targHpFrac;

        bool mpDec = _targMpFrac < _dispMpFrac;
        _dispMpFrac = mpDec
            ? Mathf.MoveTowards(_dispMpFrac, _targMpFrac, DrainSpeed * Time.deltaTime)
            : _targMpFrac;

        SetBarFraction(hpFill, _dispHpFrac,
            Color.Lerp(Color.red, new Color(0.15f, 0.85f, 0.3f), _dispHpFrac));
        SetBarFraction(mpFill, _dispMpFrac, new Color(0.2f, 0.5f, 1f));
    }

    // ── 갱신 (이벤트 수신 시 목표값만 업데이트) ───────────────────────────

    static void InitBar(Image img)
    {
        if (img == null) return;
        img.type           = Image.Type.Simple;
        img.preserveAspect = false;
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    void Refresh(string _)
    {
        if (_active == null) return;

        float hpFrac = Mathf.Clamp01((float)_active.Hp / _active.MaxHp);
        float mpFrac = Mathf.Clamp01((float)_active.Mp / _active.MaxMp);

        // 감소: 목표만 갱신 (AnimateBars가 점진적으로 적용)
        // 회복/교체: 표시값도 즉시 점프
        if (hpFrac > _dispHpFrac) _dispHpFrac = hpFrac;
        if (mpFrac > _dispMpFrac) _dispMpFrac = mpFrac;
        _targHpFrac = hpFrac;
        _targMpFrac = mpFrac;

        if (hpText) hpText.text = $"HP {_active.Hp}/{_active.MaxHp}";
        if (mpText) mpText.text = $"MP {_active.Mp}/{_active.MaxMp}";
    }

    // anchorMax.x를 fraction으로 설정 → 오른쪽에서 왼쪽으로 줄어듦
    static void SetBarFraction(Image img, float fraction, Color color)
    {
        if (img == null) return;
        img.color = color;
        var rt = img.rectTransform;
        rt.anchorMin = new Vector2(0f,       rt.anchorMin.y);
        rt.anchorMax = new Vector2(fraction, rt.anchorMax.y);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
