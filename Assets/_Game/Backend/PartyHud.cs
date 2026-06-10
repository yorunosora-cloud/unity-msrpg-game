using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 화면 우하단 파티 슬롯 HUD (최대 3칸).
/// HP/MP 바: CombatHud와 동일하게 Image.Type.Simple + anchorMax.x 방식으로 너비 제어.
/// 감소는 MoveTowards로 부드럽게, 회복/부활은 즉시.
/// 슬롯 배경색: 상태 기반 (기본/활성/기절). 속성 색 미사용.
/// </summary>
public class PartyHud : MonoBehaviour
{
    // ── MesoriaSetup이 SerializedObject로 주입 ────────────────────────────
    [SerializeField] Image[]    _slotBgs;
    [SerializeField] Image[]    _slotBorders;
    [SerializeField] Image[]    _slotDownedOverlays;
    [SerializeField] TMP_Text[] _slotStatusTexts;
    [SerializeField] TMP_Text[] _slotNameTexts;
    [SerializeField] TMP_Text[] _slotLevelTexts;
    [SerializeField] Image[]    _slotHpFills;
    [SerializeField] TMP_Text[] _slotHpTexts;
    [SerializeField] Image[]    _slotMpFills;
    [SerializeField] TMP_Text[] _slotMpTexts;

    // ── 슬롯 색상 (상태 기반, UITheme §B-3) ─────────────────────────────
    static readonly Color SlotNormalColor = new Color(
        UITheme.PanelBgMid.r, UITheme.PanelBgMid.g, UITheme.PanelBgMid.b, 0.93f);
    static readonly Color SlotActiveColor = new Color(
        UITheme.PanelBgMid.r + 0.04f, UITheme.PanelBgMid.g + 0.04f,
        UITheme.PanelBgMid.b + 0.10f, 0.95f);
    static readonly Color SlotDownedColor = new Color(0.25f, 0.07f, 0.07f, 0.93f); // 기절=빨강 유지

    static readonly Color ActiveBorderColor = new Color(1.00f, 0.85f, 0.10f);
    static readonly Color DownedTextColor   = new Color(1.00f, 0.30f, 0.30f);
    static readonly Color ActiveTextColor   = new Color(1.00f, 0.95f, 0.20f);
    static readonly Color ReadyTextColor    = new Color(0.75f, 0.85f, 0.75f);

    // ── HP/MP 바 애니메이션 ───────────────────────────────────────────────
    const float DrainSpeed = 1.5f;

    float[] _dispHpFracs;
    float[] _targHpFracs;
    float[] _dispMpFracs;
    float[] _targMpFracs;

    bool _barsInitialized;

    Party             _party;
    CombatCharacter[] _subscribedMembers;

    int SlotCount => _slotBgs != null ? _slotBgs.Length : 0;

    // ── 라이프사이클 ──────────────────────────────────────────────────────

    void Start() => TryConnect();

    void Update()
    {
        if (_party != PlayerRuntime.Party) TryConnect();
        AnimateBars();
    }

    void OnDestroy() => UnsubscribeParty();

    // ── 연결 ──────────────────────────────────────────────────────────────

    void TryConnect()
    {
        var newParty = PlayerRuntime.Party;
        if (newParty == null || newParty == _party) return;

        UnsubscribeParty();
        _party = newParty;
        _party.OnActiveChanged += RefreshAll;
        _party.OnPartyChanged  += RefreshAll;

        int n = _party.Members.Count;
        _dispHpFracs = new float[n];
        _targHpFracs = new float[n];
        _dispMpFracs = new float[n];
        _targMpFracs = new float[n];

        for (int i = 0; i < n; i++)
        {
            var m = _party.Members[i];
            float hp = m.MaxHp > 0 ? (float)m.Hp / m.MaxHp : 1f;
            float mp = m.MaxMp > 0 ? (float)m.Mp / m.MaxMp : 1f;
            _dispHpFracs[i] = _targHpFracs[i] = hp;
            _dispMpFracs[i] = _targMpFracs[i] = mp;
        }

        // CombatHud와 동일: Simple 타입 + anchorMax.x 방식으로 초기화
        InitSlotBars();

        _subscribedMembers = new CombatCharacter[n];
        for (int i = 0; i < n; i++)
        {
            _subscribedMembers[i] = _party.Members[i];
            int captured = i;
            _party.Members[i].OnChanged += _ => RefreshSlot(captured);
        }

        RefreshAll();
    }

    void UnsubscribeParty()
    {
        if (_party == null) return;
        _party.OnActiveChanged -= RefreshAll;
        _party.OnPartyChanged  -= RefreshAll;
        _party = null;
        _subscribedMembers = null;
    }

    // ── 바 초기화 (CombatHud.InitBar와 동일 방식) ─────────────────────────

    void InitSlotBars()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            var hp = _slotHpFills != null && i < _slotHpFills.Length ? _slotHpFills[i] : null;
            var mp = _slotMpFills != null && i < _slotMpFills.Length ? _slotMpFills[i] : null;
            InitFill(hp);
            InitFill(mp);
            SetBarBg(hp, new Color(0.22f, 0.04f, 0.04f, 1f));
            SetBarBg(mp, new Color(0.04f, 0.06f, 0.25f, 1f));
            AddBorderToBar(hp);
            AddBorderToBar(mp);
        }
        _barsInitialized = true;
    }

    static void SetBarBg(Image fill, Color bg)
    {
        if (fill == null) return;
        var bgImg = fill.transform.parent?.GetComponent<Image>();
        if (bgImg != null) bgImg.color = bg;
    }

    static void AddBorderToBar(Image fill)
    {
        if (fill == null) return;
        var bg = fill.transform.parent?.GetComponent<Image>();
        if (bg == null) return;
        var outline = bg.GetComponent<Outline>() ?? bg.gameObject.AddComponent<Outline>();
        outline.effectColor    = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(2.5f, 2.5f);
    }

    static void InitFill(Image img)
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

    // ── HP/MP 바 애니메이션 (CombatHud.SetBarFraction과 동일 방식) ─────────

    void AnimateBars()
    {
        if (!_barsInitialized || _dispHpFracs == null || _party == null) return;

        var members = _party.Members;
        for (int i = 0; i < _dispHpFracs.Length && i < SlotCount; i++)
        {
            bool isDowned = i < members.Count && members[i].IsDowned;

            // 감소: 부드럽게. 회복/부활: 즉시.
            bool hpDec = _targHpFracs[i] < _dispHpFracs[i];
            _dispHpFracs[i] = hpDec
                ? Mathf.MoveTowards(_dispHpFracs[i], _targHpFracs[i], DrainSpeed * Time.deltaTime)
                : _targHpFracs[i];

            bool mpDec = _targMpFracs[i] < _dispMpFracs[i];
            _dispMpFracs[i] = mpDec
                ? Mathf.MoveTowards(_dispMpFracs[i], _targMpFracs[i], DrainSpeed * Time.deltaTime)
                : _targMpFracs[i];

            Color hpColor = isDowned
                ? new Color(0.35f, 0.35f, 0.35f)
                : Color.Lerp(Color.red, new Color(0.15f, 0.85f, 0.3f), _dispHpFracs[i]);
            Color mpColor = isDowned ? new Color(0.20f, 0.25f, 0.45f) : new Color(0.3f, 0.6f, 1.0f);

            SetBarFraction(_slotHpFills[i], _dispHpFracs[i], hpColor);
            SetBarFraction(_slotMpFills[i], _dispMpFracs[i], mpColor);
        }
    }

    // CombatHud.SetBarFraction과 완전히 동일
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

    // ── 슬롯 갱신 ─────────────────────────────────────────────────────────

    void RefreshAll()
    {
        for (int i = 0; i < SlotCount; i++) RefreshSlot(i);
    }

    void RefreshSlot(int i)
    {
        if (_slotBgs == null || i >= SlotCount) return;

        var members = _party?.Members;
        if (members == null || i >= members.Count)
        {
            if (_slotBgs[i]) _slotBgs[i].gameObject.SetActive(false);
            return;
        }

        var  member   = members[i];
        bool isActive = (_party.ActiveIndex == i);
        bool isDowned = member.IsDowned;

        // 배경색: 활성 슬롯은 대륙 색을 약하게 틴트 → 배경만으로도 어떤 슬롯인지 구별
        if (_slotBgs[i])
        {
            Color bgColor;
            if (isDowned)
                bgColor = SlotDownedColor;
            else if (isActive)
            {
                var cc = ContinentColors.Of(member.Element);
                bgColor = new Color(0.12f + cc.r * 0.18f, 0.14f + cc.g * 0.14f, 0.25f + cc.b * 0.15f, 0.95f);
            }
            else
                bgColor = SlotNormalColor;
            _slotBgs[i].color = bgColor;
            _slotBgs[i].gameObject.SetActive(true);
        }

        // 활성 테두리: 대륙 색을 HSV 최대 밝기(V=1)로 부스트 → 어두운 배경에서 선명하게 구별
        if (_slotBorders[i])
        {
            _slotBorders[i].enabled = isActive;
            if (isActive)
            {
                Color.RGBToHSV(ContinentColors.Of(member.Element), out float h, out float s, out float v);
                _slotBorders[i].color = Color.HSVToRGB(h, Mathf.Max(s, 0.85f), 1f);
            }
            else
                _slotBorders[i].color = ActiveBorderColor;
        }

        // 기절 오버레이
        if (_slotDownedOverlays[i])
            _slotDownedOverlays[i].enabled = isDowned;

        // 상태 텍스트
        if (_slotStatusTexts[i])
        {
            if (isDowned)      { _slotStatusTexts[i].text = "기절";        _slotStatusTexts[i].color = DownedTextColor; }
            else if (isActive) { _slotStatusTexts[i].text = "▶ 플레이 중"; _slotStatusTexts[i].color = ActiveTextColor; }
            else               { _slotStatusTexts[i].text = "대기";         _slotStatusTexts[i].color = ReadyTextColor;  }
        }

        // 이름
        if (_slotNameTexts[i])
        {
            string name = member.DisplayName ?? "?";
            _slotNameTexts[i].text  = name.Length > 5 ? name.Substring(0, 5) : name;
            _slotNameTexts[i].color = isDowned ? new Color(0.55f, 0.55f, 0.55f) : Color.white;
        }

        // 레벨
        if (_slotLevelTexts[i])
            _slotLevelTexts[i].text = $"Lv.{member.Level}";

        // HP 목표값 + 수치 텍스트 (바 시각은 AnimateBars 처리)
        if (_targHpFracs != null && i < _targHpFracs.Length)
        {
            float hpFrac = member.MaxHp > 0 ? Mathf.Clamp01((float)member.Hp / member.MaxHp) : 0f;
            _targHpFracs[i] = hpFrac;
            if (hpFrac > _dispHpFracs[i]) _dispHpFracs[i] = hpFrac; // 회복: 즉시
        }
        if (_slotHpTexts[i])
            _slotHpTexts[i].text = $"HP {member.Hp}/{member.MaxHp}";

        // MP 목표값 + 수치 텍스트
        if (_targMpFracs != null && i < _targMpFracs.Length)
        {
            float mpFrac = member.MaxMp > 0 ? Mathf.Clamp01((float)member.Mp / member.MaxMp) : 0f;
            _targMpFracs[i] = mpFrac;
            if (mpFrac > _dispMpFracs[i]) _dispMpFracs[i] = mpFrac; // 회복: 즉시
        }
        if (_slotMpTexts[i])
            _slotMpTexts[i].text = $"MP {member.Mp}/{member.MaxMp}";
    }
}
