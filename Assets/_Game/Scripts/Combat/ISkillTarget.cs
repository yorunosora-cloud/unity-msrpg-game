using UnityEngine;

/// <summary>
/// 스킬 효과를 받을 수 있는 적 측 대상(Enemy · Boss). 설계도 22 §4-9.
/// 상태 효과 부여·위치 이동·표식을 통일된 방식으로 받는다.
/// </summary>
public interface ISkillTarget : IDamageable
{
    /// <summary>해로운 상태 효과 부여. 대상이 면역이면 무시한다.</summary>
    void ApplyStatus(StatusEffect effect);

    /// <summary>origin 기준으로 밀어내기(pull=false) 또는 끌어당기기(pull=true). 면역이면 무시.</summary>
    void Displace(Vector3 origin, float distance, bool pull);

    /// <summary>속성 표식 부여.</summary>
    void SetMark(Continent element);
}
