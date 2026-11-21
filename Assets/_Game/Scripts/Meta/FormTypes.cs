using System;
using UnityEngine;

// 폼 시스템 데이터 타입 (설계도 22 §4-6, §4-6-1).

/// <summary>캐릭터 폼 1개. 폼별 스탯·role·gender·색을 가진다.</summary>
[Serializable]
public class CharacterForm
{
    public string         formId;     // 예: "Protium"
    public string         nameKo;
    public CharacterRole  role;
    public Gender         gender;
    public CharacterStats baseStats;  // 레벨 1 기준. hp를 폼 공통으로 맞출지는 데이터에서 결정
    public Color          tint = Color.white;
    public bool           isDefault;  // 기본 폼(기절·초기화 시 복귀)
}

/// <summary>폼이 바뀐 원인.</summary>
public enum FormChangeCause { Manual, Auto, Revert }

public enum FormChangeMode { Toggle, Sequential, Hub, StateBased }
public enum FormStateCond  { Always, RecentlyHit, Moving, Idle }
public enum FormCompare    { AtLeast, Below }

[Serializable]
public struct FormStateRule
{
    public FormStateCond cond;
    public float         param;      // RecentlyHit: 초
    public string        toForm;
    public int           priority;   // 높을수록 우선
}

/// <summary>
/// 수동 폼 전환 규칙 (SkillDef.skillType == FormChange 스킬에 부착).
/// Toggle: sequence 정확히 2개 폼을 번갈아. Sequential: sequence 순서대로 한 단계씩(마지막에서는 사용 불가).
/// Hub: fromForms(허브)에서 toForm으로. StateBased: 누른 순간 상태(stateRules 우선순위)로 결정.
/// </summary>
[Serializable]
public class FormChangeSpec
{
    public int              keyIndex;            // 0=Z, 1=X
    public FormChangeMode   mode;
    public string[]         fromForms;           // 이 규칙이 쓰일 수 있는 폼(Hub/StateBased: 허브·기본 폼)
    public string           toForm;              // Hub 목적 폼
    public string[]         sequence;            // Toggle(2개)/Sequential
    public FormStateRule[]  stateRules;          // StateBased
    public float            revertAfterSeconds;  // 0 = 자동 복귀 없음
    public string           revertToForm;        // 자동·재입력 복귀 대상(비면 기본 폼)
    public string[]         autoRevertForms;     // 이 폼에 진입하면 복귀 타이머 시작(비면 toForm)
    public bool             pressAgainReverts;   // 허브가 아닌 폼에서 재입력 → 복귀(mp 무소모, 쿨타임만)
}

public enum FormAutoCond
{
    SpeedRatioAtLeast, SpeedRatioBelow,   // 수평속도 / 최대 속도
    JumpApex, JumpFalling,                // 공중에서 |vY| < param / vY < -param
    IdleFor, MovingFor,                   // 정지·이동 지속 param초
    HitTargetStat,                        // 타격한 적의 속성이 조건을 연속 N회 만족
}

/// <summary>자동 폼 전환 규칙 (CharacterDef.autoRules[]).</summary>
[Serializable]
public struct FormAutoRule
{
    public FormAutoCond cond;
    public float        param;
    public string       statKey;         // HitTargetStat: 적 속성 키("temp")
    public FormCompare  statCompare;
    public float        statThreshold;
    public int          consecutive;     // HitTargetStat: 연속 횟수
    public string       toForm;
    public int          priority;        // 높을수록 우선
}
