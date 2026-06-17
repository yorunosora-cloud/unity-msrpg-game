using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 화면 하단 중앙 스킬 바 HUD.
/// 활성 캐릭터가 보유한 스킬을 E/R/T/F/V/G 키 라벨과 함께 표시한다.
/// 캐릭터 교체 시 슬롯 내용·개수가 갱신된다.
/// </summary>
public class SkillBarHud : MonoBehaviour
{
    // ── 6슬롯 참조 배열 (MesoriaSetup이 직렬화로 주입) ─────────────────────

    [SerializeField] GameObject[]  _slotRoots;        // 슬롯 루트 (enable/disable)
    [SerializeField] TMP_Text[]    _slotKeyLabels;   // "E" / "R" / … / "G"
    [SerializeField] TMP_Text[]    _slotNameTexts;   // skill.nameKo
    [SerializeField] Image[]       _slotCdOverlays;  // 쿨다운 오버레이 (fillAmount 1→0)
    [SerializeField] Image[]       _slotBgs;         // MP 부족 시 어둡게
    [SerializeField] GameObject[]  _slotLockOverlays; // 잠긴 스킬 자물쇠 오버레이

    static readonly string[] KeyLabels = { "U", "I", "O", "H", "J", "K" };

    const float DimAlpha   = 0.4f; // MP 부족·쿨다운 시 배경 투명도
    const float NormalAlpha = 0.85f;

    CombatCharacter _active;
    Party           _party;

    void Start()  => TryConnect();
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
            _active.OnChanged += OnChanged;
            RebuildSlots();
        }
    }

    void UnsubscribeActive()
    {
        if (_active != null) { _active.OnChanged -= OnChanged; _active = null; }
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

    // ── 갱신 ──────────────────────────────────────────────────────────────

    /// <summary>캐릭터 교체 또는 스킬 수 변경 시 슬롯 내용 전면 재구성.</summary>
    void RebuildSlots()
    {
        if (_slotRoots == null) return;

        int count = _active?.SkillCount ?? 0;

        for (int i = 0; i < _slotRoots.Length; i++)
        {
            if (_slotRoots[i] == null) continue;

            bool hasSlot = i < count;
            _slotRoots[i].SetActive(hasSlot);

            if (!hasSlot) continue;

            var skill = _active.SkillAt(i);

            if (_slotKeyLabels  != null && i < _slotKeyLabels.Length  && _slotKeyLabels[i]  != null)
                _slotKeyLabels[i].text = KeyLabels[i];

            if (_slotNameTexts  != null && i < _slotNameTexts.Length  && _slotNameTexts[i]  != null)
                _slotNameTexts[i].text = skill?.nameKo ?? "—";

            // 초기 CD 상태
            UpdateSlotVisual(i, skill);
        }
    }

    void OnChanged(string reason)
    {
        if (_active == null) return;
        if (reason == "cooldown" || reason == "mp" || reason == "restore" || reason == "levelup" || reason == "unlock")
            RefreshVisuals();
    }

    void RefreshVisuals()
    {
        if (_active == null || _slotRoots == null) return;

        for (int i = 0; i < Mathf.Min(_slotRoots.Length, _active.SkillCount); i++)
        {
            if (_slotRoots[i] == null || !_slotRoots[i].activeSelf) continue;
            UpdateSlotVisual(i, _active.SkillAt(i));
        }
    }

    void UpdateSlotVisual(int i, SkillDef skill)
    {
        if (skill == null) return;

        bool locked     = !_active.IsUnlocked(i);
        bool onCooldown = !_active.IsReady(i);
        bool mpOk       = _active.Mp >= skill.mpCost;
        bool available  = !locked && mpOk && !onCooldown && !_active.IsDowned;

        // 자물쇠 오버레이
        if (_slotLockOverlays != null && i < _slotLockOverlays.Length && _slotLockOverlays[i] != null)
            _slotLockOverlays[i].SetActive(locked);

        // 배경 투명도로 "사용 불가" 표시
        if (_slotBgs != null && i < _slotBgs.Length && _slotBgs[i] != null)
        {
            var c = _slotBgs[i].color;
            c.a = available ? NormalAlpha : DimAlpha;
            _slotBgs[i].color = c;
        }

        // 잠긴 슬롯은 쿨다운 오버레이 숨김
        if (_slotCdOverlays != null && i < _slotCdOverlays.Length && _slotCdOverlays[i] != null)
        {
            if (locked)
            {
                _slotCdOverlays[i].fillAmount = 0f;
            }
            else
            {
                float fraction = (skill.cooldown > 0f)
                    ? Mathf.Clamp01(_active.CooldownRemaining(i) / skill.cooldown)
                    : 0f;
                _slotCdOverlays[i].fillAmount = fraction;
            }
        }
    }

    // Update: 매 프레임 활성 슬롯 갱신 (쿨다운 오버레이 부드럽게 줄어듦) ────

    void Update()
    {
        if (_active == null || _party != PlayerRuntime.Party)
            TryConnect();

        if (_active == null || _slotRoots == null) return;

        // 슬롯 수가 적어 CD 오버레이가 남아 있을 경우도 포함해 전부 갱신
        int count = Mathf.Min(_slotRoots.Length, _active.SkillCount);
        for (int i = 0; i < count; i++)
        {
            if (_slotRoots[i] != null && _slotRoots[i].activeSelf)
                UpdateSlotVisual(i, _active.SkillAt(i));
        }
    }
}
