using UnityEngine;

/// <summary>
/// 피해 저장→방출 상태(순수, 구현계획 23-H). 시작 후 seconds초 동안 받은 피해를 모았다가 끝나면
/// 저장량 × ratio 를 방출량으로 알린다(탄성 퍼텐셜 에너지).
/// </summary>
public class DamageStore
{
    float _remaining;
    int   _stored;

    public float Ratio  { get; private set; }
    public float Radius { get; private set; }
    public bool  Active => _remaining > 0f;

    public void Begin(float ratio, float seconds, float radius)
    {
        Ratio = ratio; Radius = radius; _remaining = Mathf.Max(0f, seconds); _stored = 0;
    }

    public void Add(int damage)
    {
        if (!Active || damage <= 0) return;
        _stored += damage;
    }

    /// <summary>시간 진행. 끝나는 순간 true와 방출량(0 이상)을 반환한다.</summary>
    public bool Tick(float dt, out int release)
    {
        release = 0;
        if (!Active) return false;
        _remaining -= dt;
        if (_remaining > 0f) return false;
        release = Mathf.RoundToInt(_stored * Ratio);
        _stored = 0; _remaining = 0f;
        return true;
    }

    public void Clear() { _remaining = 0f; _stored = 0; }
}
