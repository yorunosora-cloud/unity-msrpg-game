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
        // 상태 효과도 Party.Tick이 처리(활성 여부 전달). 파티가 없으면 활성 캐릭터만.
        if (party != null) party.Tick(dt);
        else { active.TickCooldowns(dt); active.TickBuffs(dt); }

        // 활성 캐릭터 폼 틱(자동 복귀 타이머·자동 전환 평가). 대기 캐릭터는 호출하지 않아 일시정지.
        active.TickForm(dt, PlayerRuntime.Motion);

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
        if (!active.CanAct) return;   // 스턴·기절 중에는 스킬·폼 전환 불가 (설계도 §4-13)

        // 폼 전환 키: Z=0, X=1
        if (kb.zKey.wasPressedThisFrame) active.TryFormChange(0, PlayerRuntime.Motion.isMoving);
        if (kb.xKey.wasPressedThisFrame) active.TryFormChange(1, PlayerRuntime.Motion.isMoving);

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
