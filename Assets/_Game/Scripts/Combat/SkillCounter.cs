using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>카운터 시간 감소 방식(설계도 구현계획 23-A).</summary>
public enum CounterDecay { None, ResetAfterIdle, DecayOneEvery }

/// <summary>스킬이 카운터 값을 위력에 반영하는 방식.</summary>
public enum CounterMode { None, Linear, Quadratic }

/// <summary>카운터 정의(CSV skill_counters 1행). 0인 milestoneMaxStacks는 교체 없음.</summary>
[Serializable]
public struct CounterSpec
{
    public string       id;
    public int          maxStacks;
    public int          milestoneMaxStacks;
    public CounterDecay decay;
    public float        decaySeconds;
}

/// <summary>
/// 이름 붙은 정수 카운터 하나(순수). 스킬이 읽고(위력) 소모할 수 있으며 시간·이동으로 쌓이고 줄어든다.
/// 타이머는 소유 캐릭터가 활성일 때만 흐른다(대기 중엔 값 유지, 설계도 22 §4-7 규칙 1).
/// </summary>
public class SkillCounter
{
    readonly CounterSpec _spec;
    bool  _milestone;
    int   _value;
    float _idle;           // 마지막 증가 이후 활성 경과 시간
    float _moveProgress;   // 이동 누적 진행도(초)

    public SkillCounter(CounterSpec spec) { _spec = spec; }

    public string Id    => _spec.id;
    public int    Value => _value;
    public int    Max   => Mathf.Max(1, _milestone && _spec.milestoneMaxStacks > 0 ? _spec.milestoneMaxStacks : _spec.maxStacks);
    public bool   IsFull => _value >= Max;

    /// <summary>마일스톤 상한 적용 여부 변경. 상한이 줄면 값도 잘린다.</summary>
    public void SetMilestone(bool active)
    {
        _milestone = active;
        if (_value > Max) _value = Max;
    }

    public void Add(int n = 1)
    {
        if (n <= 0) return;
        _value = Mathf.Min(Max, _value + n);
        _idle = 0f;
    }

    /// <summary>최대 n개 소모하고 실제 소모한 수를 반환.</summary>
    public int Consume(int n)
    {
        if (n <= 0) return 0;
        int used = Mathf.Min(n, _value);
        _value -= used;
        return used;
    }

    public int ConsumeAll()
    {
        int used = _value;
        _value = 0;
        _idle = 0f;
        return used;
    }

    public void Clear()
    {
        _value = 0; _idle = 0f; _moveProgress = 0f;
    }

    /// <summary>시간 진행. 활성일 때만 흐르고, 감소 방식에 따라 값을 줄인다.</summary>
    public void Tick(float dt, bool isActive)
    {
        if (!isActive) return;
        if (_value == 0) { _idle = 0f; return; }
        if (_spec.decaySeconds <= 0f || _spec.decay == CounterDecay.None) return;

        _idle += dt;
        switch (_spec.decay)
        {
            case CounterDecay.ResetAfterIdle:
                if (_idle >= _spec.decaySeconds) { _value = 0; _idle = 0f; }
                break;
            case CounterDecay.DecayOneEvery:
                while (_idle >= _spec.decaySeconds && _value > 0)
                {
                    _value--;
                    _idle -= _spec.decaySeconds;
                }
                if (_value == 0) _idle = 0f;
                break;
        }
    }

    /// <summary>
    /// 이동 중 1초마다 +1(gainMultiplier배). 멈추면 진행도가 끊긴다. 이번 호출로 늘어난 수를 반환.
    /// </summary>
    public int AccumulateMoving(float dt, bool isMoving, float gainMultiplier = 1f)
    {
        if (!isMoving) { _moveProgress = 0f; return 0; }
        _moveProgress += dt * Mathf.Max(0f, gainMultiplier);
        int whole = Mathf.FloorToInt(_moveProgress + 1e-4f);
        if (whole <= 0) return 0;
        _moveProgress -= whole;
        if (_moveProgress < 0f) _moveProgress = 0f;
        int before = _value;
        Add(whole);
        return _value - before;
    }
}

/// <summary>카운터 값을 위력 배율로 바꾸는 공식(순수).</summary>
public static class CounterFormula
{
    /// <summary>None=base / Linear=base+coeff×n / Quadratic=base+coeff×n². n&lt;0은 0으로 본다.</summary>
    public static float Scale(CounterMode mode, float baseValue, float coeff, int stacks)
    {
        int n = Mathf.Max(0, stacks);
        switch (mode)
        {
            case CounterMode.Linear:    return baseValue + coeff * n;
            case CounterMode.Quadratic: return baseValue + coeff * n * n;
            default:                    return baseValue;
        }
    }
}

/// <summary>최근 N초 안에 사용한 서로 다른 스킬 수(순수). 시각은 호출자가 넘긴다.</summary>
public class DistinctSkillWindow
{
    readonly float _window;
    readonly Dictionary<string, float> _last = new();

    public DistinctSkillWindow(float windowSeconds) { _window = windowSeconds; }

    public void Record(string skillId, float time)
    {
        if (string.IsNullOrEmpty(skillId)) return;
        _last[skillId] = time;
    }

    /// <summary>now 기준 창(now−window ≤ t) 안에 기록된 서로 다른 스킬 수(경계 포함).</summary>
    public int Count(float now)
    {
        int n = 0;
        foreach (var t in _last.Values)
            if (now - t <= _window + 1e-5f) n++;
        return n;
    }

    public void Clear() => _last.Clear();
}
