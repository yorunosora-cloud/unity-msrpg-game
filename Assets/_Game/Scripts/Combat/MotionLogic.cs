using UnityEngine;

/// <summary>
/// 돌진 이동 계산(순수). 플레이어 컨트롤러가 매 프레임 Step(dt)의 변위만큼 이동한다.
/// 설계도 18 §8-5 #1: 포르테 탄성력 — 전방 N m를 T초에 이동.
/// </summary>
public struct DashMotion
{
    const float Epsilon = 1e-4f;   // 부동소수 오차로 종료가 한 프레임 늦어지지 않게
    Vector3 _velocity;
    float   _remainingTime;

    public bool Active => _remainingTime > Epsilon;

    /// <summary>돌진 시작. 방향은 수평으로 눌러 정규화한다. 거리·시간이 0 이하면 시작하지 않는다.</summary>
    public void Begin(Vector3 direction, float distance, float time)
    {
        _remainingTime = 0f;
        if (distance < 0f) { direction = -direction; distance = -distance; }   // 음수 거리 = 후방(반동)
        direction.y = 0f;
        if (distance <= 0f || time <= 0f || direction.sqrMagnitude < 1e-6f) return;
        _velocity      = direction.normalized * (distance / time);
        _remainingTime = time;
    }

    /// <summary>dt초 진행하고 이번 프레임의 변위를 반환. 남은 시간을 넘기면 남은 만큼만 이동한다.</summary>
    public Vector3 Step(float dt)
    {
        if (!Active || dt <= 0f) return Vector3.zero;
        float t = Mathf.Min(dt, _remainingTime);
        _remainingTime -= t;
        return _velocity * t;
    }

    public void Cancel() => _remainingTime = 0f;
}

/// <summary>
/// 띄우기 높이 곡선(순수). 올라가는 구간·유지·내려오는 구간으로 이루어진다.
/// 설계도 18 §8-5 #2: 포르테 부력 — 스턴 동안 적을 띄우는 시각 연출.
/// </summary>
public static class LaunchCurve
{
    /// <summary>오르내림 구간의 최대 길이(초). 지속시간이 짧으면 25%로 줄인다.</summary>
    public const float RampSeconds = 0.25f;

    public static float Offset(float height, float duration, float elapsed)
    {
        if (height <= 0f || duration <= 0f || elapsed <= 0f || elapsed >= duration) return 0f;
        float ramp = Mathf.Min(RampSeconds, duration * 0.25f);
        if (elapsed < ramp)             return height * Mathf.SmoothStep(0f, 1f, elapsed / ramp);
        if (elapsed > duration - ramp)  return height * Mathf.SmoothStep(0f, 1f, (duration - elapsed) / ramp);
        return height;
    }
}
