using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어 스킬 입력 컴포넌트. Player GameObject에 부착.
/// R/T/F/V/G/B 키 → 슬롯 1~6 발동 (설계도 22 §4-1). 기본기(Q)는 PlayerCombat이 처리한다.
/// 파티 전원(대기 포함)의 쿨다운·전환 쿨타임·MP 자연회복 틱, 활성 캐릭터 버프 틱.
/// skillsEnabled = false 면 스킬 키 입력만 차단 (틱은 유지).
/// </summary>
[RequireComponent(typeof(BoxCharacterBuilder))]
public class PlayerSkills : MonoBehaviour
{
    [Header("스킬 활성화")]
    [Tooltip("false 면 R/T/F/V/G/B 스킬 키를 차단. 파일·컴포넌트는 보존 — true로 되돌리면 복구.")]
    [SerializeField] bool skillsEnabled = true;

    [Header("MP 자연 회복")]
    [Tooltip("MaxMp 대비 초당 자연 회복 비율 (기본 3%) — 파티 전원(대기 포함)에 적용")]
    [SerializeField] float mpRegenPercent = 0.03f;

    // 슬롯 1~6에 대응하는 키 (슬롯 0=Q는 PlayerCombat)
    static readonly Key[] SlotKeys = { Key.R, Key.T, Key.F, Key.V, Key.G, Key.B };

    const float RegenTickSeconds = 0.5f;

    BoxCharacterBuilder _builder;
    float               _regenAccum; // 자연 회복 누적 시간

    void Awake()
    {
        _builder = GetComponent<BoxCharacterBuilder>();
    }

    void Update()
    {
        var active = PlayerRuntime.Active;
        if (active == null) return;

        var   party = PlayerRuntime.Party;
        float dt    = Time.deltaTime;

        // 쿨다운 틱: 파티 전원(대기 포함) + 전환 쿨타임. UI가 열려 있어도 계속 감소.
        if (party != null) party.Tick(dt);
        else               active.TickCooldowns(dt);

        // 버프 틱: 활성 캐릭터만 (버프 타이머는 활성일 때만 흐른다)
        active.TickBuffs(dt);

        if (UIManager.IsAnyPanelOpen) return;

        // 자연 MP 회복: 파티 전원(대기 포함, 기절 제외)
        _regenAccum += dt;
        if (_regenAccum >= RegenTickSeconds)
        {
            if (party != null) party.RegenMpTick(mpRegenPercent, RegenTickSeconds);
            else               active.RegenMpTick(mpRegenPercent, RegenTickSeconds);
            _regenAccum -= RegenTickSeconds;
        }

        var kb = Keyboard.current;
        if (kb == null) return;

        if (!skillsEnabled) return;

        for (int i = 0; i < SlotKeys.Length; i++)
            if (kb[SlotKeys[i]].wasPressedThisFrame)
                TryCast(i + 1, active);   // 슬롯 = 키 순서 + 1
    }

    void TryCast(int slot, CombatCharacter active)
    {
        int index = active.IndexForSlot(slot);
        if (index < 0) return;                       // 그 슬롯에 스킬 없음

        var skill = active.CastSkill(index);
        if (skill == null) return;                   // MP 부족, 쿨다운 중, 잠금

        SkillExecutor.Execute(skill, index, active, transform);
        _builder?.PlaySkill(skill.effectKind);       // effectKind별 전용 모션
        SkillRangeVisualizer.Show(skill, transform); // 범위 시각화
    }
}
