using System;

/// <summary>
/// 물리량 속성(질량·속도·온도·전하). 설계도 18 §3-6. v1은 물리 4종만 — 값 0 = 속성 없음.
/// 적(Enemy)이 보유하고, 강점 태그(TagRule)가 이 값을 읽어 피해 배율을 만든다.
/// </summary>
[Serializable]
public struct AttributeProfile
{
    public int mass, velocity, temp, charge;
}
