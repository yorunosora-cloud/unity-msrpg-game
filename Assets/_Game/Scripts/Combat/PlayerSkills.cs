using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어 스킬 입력 컴포넌트. Player GameObject에 부착.
/// E/R/T/F/V/G 키 → skills[0..5] 발동, MP 자연 회복 틱, 쿨다운·버프 갱신.
/// 장착/해제 없이 캐릭터가 보유한 스킬 수만큼 키가 활성화된다.
/// </summary>
[RequireComponent(typeof(BoxCharacterBuilder))]
public class PlayerSkills : MonoBehaviour
{
    [Header("MP 자연 회복")]
    [Tooltip("MaxMp 대비 초당 자연 회복 비율 (기본 3%)")]
    [SerializeField] float mpRegenPercent = 0.03f;

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

        float dt = Time.deltaTime;

        // 쿨다운·버프 틱 (UI 열려있어도 계속 감소)
        active.TickCooldowns(dt);
        active.TickBuffs(dt);

        if (UIManager.IsAnyPanelOpen) return;

        // 자연 MP 회복 (초당 mpRegenPercent × MaxMp)
        _regenAccum += dt;
        if (_regenAccum >= 0.5f)                   // 0.5초마다 한 틱
        {
            int regenAmt = UnityEngine.Mathf.Max(1,
                UnityEngine.Mathf.RoundToInt(active.MaxMp * mpRegenPercent * 0.5f));
            active.RecoverMp(regenAmt);
            _regenAccum -= 0.5f;
        }

        // 스킬 키 입력
        var kb = Keyboard.current;
        if (kb == null) return;

        TryCast(kb.eKey.wasPressedThisFrame, 0, active);
        TryCast(kb.rKey.wasPressedThisFrame, 1, active);
        TryCast(kb.tKey.wasPressedThisFrame, 2, active);
        TryCast(kb.fKey.wasPressedThisFrame, 3, active);
        TryCast(kb.vKey.wasPressedThisFrame, 4, active);
        TryCast(kb.gKey.wasPressedThisFrame, 5, active);
    }

    void TryCast(bool pressed, int skillIndex, CombatCharacter active)
    {
        if (!pressed) return;

        var skill = active.CastSkill(skillIndex);
        if (skill == null) return; // MP 부족, 쿨다운 중, 보유하지 않은 인덱스

        SkillExecutor.Execute(skill, active, transform);
        _builder?.PlaySkill(skill.effectKind);       // effectKind별 전용 모션
        SkillRangeVisualizer.Show(skill, transform); // 범위 시각화
    }
}
