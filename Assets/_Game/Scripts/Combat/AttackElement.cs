using System;
using UnityEngine;

/// <summary>공격 속성(구현계획 23-F). 속성별 피해 감소·면역, 아군 속성 조건 수정자가 읽는다. 플래그라 조합 가능.</summary>
[Flags]
public enum AttackElement
{
    None     = 0,
    Metal    = 1,
    Electric = 2,
    Thermal  = 4,
    Light    = 8,
}

public static class AttackElements
{
    /// <summary>무작위 변경의 후보(None 제외, 정의 순서 고정).</summary>
    public static readonly AttackElement[] Randomizable = { AttackElement.Metal, AttackElement.Electric, AttackElement.Thermal, AttackElement.Light };

    /// <summary>mask가 비어 있으면 모든 속성에 적용, 아니면 attack과 겹칠 때(반전이면 겹치지 않을 때)만 적용.</summary>
    public static bool Matches(AttackElement mask, bool invert, AttackElement attack)
    {
        if (mask == AttackElement.None) return true;
        bool hit = (attack & mask) != 0;
        return invert ? !hit : hit;
    }
}
