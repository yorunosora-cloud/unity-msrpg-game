using System;

/// <summary>스킬 종류 (설계도 22 §4-2). Passive/FormChange는 P3~P4에서 사용.</summary>
public enum SkillKind { Active, Passive, FormChange }

/// <summary>조합 가능한 스킬 효과 종류 (설계도 22 §4-3).</summary>
public enum SkillEffectType
{
    Damage,          // value = Atk 배율
    Heal,            // value = 대상 MaxHp 비율
    Shield,          // value = 대상 MaxHp 비율, duration
    StatBuff,        // stat, value = 배율, duration
    StatDebuff,      // stat, value = 배율(<1), duration (적 대상)
    Dot,             // value = 시전자 Atk 대비 초당 비율, duration
    Slow,            // value = 이동속도 배율, duration
    Stun,            // duration
    DamageTakenMult, // value = 받는 피해 배율, duration (적 대상=취약, 아군 대상=피해 감소)
    Knockback,       // value = 거리(m), 적 대상
    Pull,            // value = 거리(m), 적 대상(시전자 방향으로 견인)
    Mark,            // 속성 표식 (03 시너지)
    SelfDamage,      // value = 자신 MaxHp 비율 소모(최소 1 남김)
    RestoreMp,       // value = 대상 MaxMp 비율
    Cleanse,         // 해로운 상태 해제
    Teleport,        // 전투 시작 위치로 이동(영벡터, 패시브)
    Reflect,         // value = 받은 피해의 비율을 공격자에게 반환(패시브 OnHitTaken, target=Attacker)
}

/// <summary>스킬 효과 1건. 한 스킬이 여러 효과(피해 + 둔화 + 아군 버프)를 낸다.</summary>
[Serializable]
public struct SkillEffectEntry
{
    public SkillEffectType type;
    public TargetScope     target;
    public StatKind        stat;        // StatBuff / StatDebuff 전용
    public float           value;
    public float           duration;
    public int             maxStacks;   // 0/1 = 스택 없음
    public int             targetCap;   // "최대 N명"(0=제한 없음). 아군 대상은 자신 포함
    public float           areaRadius;  // >0이면 스킬 형태 대신 적중 지점 중심 이 반경(콤프턴 1.0)
    public float           milestoneValue;     // Lv.3 교체값(0=교체 없음)
    public float           milestoneDuration;
    public string          whenForm;     // OnFormChange 패시브: 새 폼이 이 id일 때만 적용(비면 항상)
    public bool            consumeOnHit; // 버프가 다음 공격 행동에서 소모됨(가속도·양자화)
}
