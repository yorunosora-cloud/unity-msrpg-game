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

    /// <summary>공중으로 띄우는 시각 연출(높이 m, 지속 초). 면역이면 무시. 행동불능은 호출자가 스턴으로 부여한다.</summary>
    void Launch(float height, float duration);

    /// <summary>속성 표식 부여.</summary>
    void SetMark(Continent element);

    /// <summary>물리량 속성(질량·속도·온도·전하). 강점 태그가 읽는다. 없으면 전부 0.</summary>
    AttributeProfile Attributes { get; }

    /// <summary>적의 속도 스탯(로렌츠·도플러 공식 입력). 미설정이면 0.</summary>
    float Spd { get; }

    /// <summary>from(플레이어 위치) 기준 접근 중이면 +1, 후퇴 중이면 −1, 정지면 0.</summary>
    float ApproachSign(Vector3 from);

    /// <summary>id별 지속 마크(회전축 등). 지속시간이 지나면 사라진다.</summary>
    bool HasMark(string id);
    void AddMark(string id, float duration);
}
