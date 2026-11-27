using System;

// 패시브 데이터 타입 (설계도 22 §4-7). 3경로: A 이벤트 / B 상시 수정자 / C 타격 공식.

public enum PassiveTriggerType
{
    None,           // 상시(경로 B) 또는 항시 계산형
    OnHit,          // 내가 적중시킬 때(경로 A)
    OnHitTaken,     // 내가 피격될 때(경로 A)
    OnFormChange,   // 폼이 바뀐 직후(경로 A)
    HpBelowPercent, // HP가 임계 아래로 내려가는 순간(경로 A, 에지 트리거)
    SelfDebuffed,   // 자신이 디버프에 걸렸을 때(경로 A)
    SelfMoving,     // 본인이 계속 이동 중일 때(경로 B)
    TargetMoving,   // 대상이 움직일 때(경로 C 공식)
    PartyPresence,  // 파티에 포함되어 있는 동안(경로 B, 활성·대기 무관)
    OnLethal,       // 치명 피해를 받는 순간(전투당 1회 부활, value=부활 시 HP 비율)
}

public enum HitCountMode { PerAction, PerTarget }                          // 공격 행동당 1회 / 적중 대상마다 1회
public enum StackDecay   { None, ResetAfterIdle, DecayOneEvery, ConsumeAtCap }
public enum PassiveFormulaId { None, Lorentz, Doppler, RangeWork, AxisDistance }
public enum ModifierSource   { Constant, LostHpPercent, MovingRamp, PartyPresence, Counter }   // Counter: 1 + value × 카운터 값

/// <summary>스택 규칙. 스택당 수치·최대·감소 방식을 가진다. 0인 milestone 값은 교체 없음.</summary>
[Serializable]
public struct StackSpec
{
    public string     stackId;
    public int        maxStacks;               // 0이면 스택 없음(damageRatio 모드 제외)
    public float      perStackValue;           // 스택당 수치(능력치 증분 등)
    public StackDecay decay;
    public float      decaySeconds;            // ResetAfterIdle=무적중 초기화 시간 / DecayOneEvery=1스택 감소 간격
    public StatKind   stat;                    // 이 스택이 올리는 능력치(SkillPower·DamageTaken·Atk …)
    public float      damageRatio;             // >0이면 피해 비례 모드: 증가량 = 가한 피해 × ratio ÷ 기본 공격력
    public float      cap;                     // 피해 비례 모드의 누적 상한
    public float      milestonePerStackValue;  // Lv.3 교체값
    public int        milestoneMaxStacks;

    /// <summary>마일스톤이 활성이면 교체값을 적용한 사본을 반환한다.</summary>
    public StackSpec Resolve(bool milestoneActive)
    {
        var s = this;
        if (!milestoneActive) return s;
        if (milestonePerStackValue != 0f) s.perStackValue = milestonePerStackValue;   // 0=교체 없음(음수 허용)
        if (milestoneMaxStacks > 0)      s.maxStacks     = milestoneMaxStacks;
        return s;
    }
}

/// <summary>상시 수정자(경로 B) 1건. 능력치 배율을 상태·공식으로 계속 계산한다.</summary>
[Serializable]
public struct PassiveModifierSpec
{
    public ModifierSource source;
    public string         counterId;        // source=Counter: 읽을 카운터 id
    public StatKind       stat;
    public float          value;            // Constant/PartyPresence=배율 / LostHpPercent=잃은 HP% 1당 감소 비율 / MovingRamp=초당 증분
    public float          cap;              // 상한(LostHpPercent 감소 상한, MovingRamp 증분 상한)
    public float          decayPerSecond;   // MovingRamp: 정지 시 초당 감소
    public bool           partyWide;        // PartyPresence: 파티 전원(대기 포함)에 적용
    public float          enemyAuraRadius;  // >0이면 적 오라: 활성인 동안 이 반경 안의 모든 적에게 적용
    public bool           immuneToSlow;     // true면 활성인 동안 이동속도 감소(둔화) 디버프를 받지 않는다
    public float          milestoneValue;   // Lv.3 교체값
    public float          milestoneCap;
}

/// <summary>패시브 정의. SkillDef.skillType == Passive 일 때 사용.</summary>
[Serializable]
public struct PassiveSpec
{
    public PassiveTriggerType trigger;
    public float            value;             // HpBelowPercent 임계(0.4) 또는 공식 파라미터(c=2000)
    public HitCountMode     hitCount;          // OnHit 전용
    public float            internalCooldown;  // 재발동 쿨타임(영벡터 60초)
    public StackSpec        stack;             // 스택형이면 maxStacks>0 또는 damageRatio>0
    public SkillEffectEntry[] effects;         // 경로 A 발동 효과
    public PassiveModifierSpec[] modifiers;    // 경로 B
    public PassiveFormulaId formula;           // 경로 C
    public float            milestoneValue;    // Lv.3 교체값(회전축 최대 보너스 +30%→+40% 등)
}
