using System;
using UnityEngine;

/// <summary>공격 적중·피격 1건의 정보 (설계도 22 §4-7 규칙 3).</summary>
public struct HitInfo
{
    public ISkillTarget target;       // 적중한 대상(OnHit)
    public ISkillTarget attacker;     // 공격자(OnHitTaken)
    public int          damage;
    public string       sourceSkillId;
    public bool         isBasic;
    public Vector3      hitPoint;
    public int          actionId;     // 같은 공격 행동의 적중은 같은 id
    public bool         isProc;       // 패시브·도트가 일으킨 피해(OnHit/OnHitTaken 트리거에서 제외)
}

/// <summary>패시브 공식 모음 — 경로 C (순수 정적).</summary>
public static class PassiveFormula
{
    /// <summary>로렌츠 인자 γ = 1/√(1−(v/c)²). v/c는 0.9999로 제한해 발산을 막는다.</summary>
    public static float Lorentz(float v, float c)
    {
        if (c <= 0f) return 1f;
        float b = Mathf.Clamp(Mathf.Abs(v) / c, 0f, 0.9999f);
        return 1f / Mathf.Sqrt(1f - b * b);
    }

    /// <summary>도플러 인자 D = √((1+β)/(1−β)), β = vRel/c (접근 +, 후퇴 −). [0.33, 3.0]으로 클램프.</summary>
    public static float Doppler(float vRel, float c)
    {
        if (c <= 0f) return 1f;
        float b = Mathf.Clamp(vRel / c, -0.9999f, 0.9999f);
        return Mathf.Clamp(Mathf.Sqrt((1f + b) / (1f - b)), 0.33f, 3.0f);
    }

    /// <summary>일(W=Fd): 사거리 1.0당 +5%, 최대 +50% → 배율.</summary>
    public static float RangeWork(float range, float perUnit = 0.05f, float cap = 0.5f)
        => 1f + Mathf.Min(cap, Mathf.Max(0f, range) * perUnit);

    /// <summary>회전축: 거리 0→+0%, full 이상→+maxBonus (선형) → 배율.</summary>
    public static float AxisDistance(float distance, float full, float maxBonus)
        => 1f + maxBonus * Mathf.Clamp01(full > 0f ? distance / full : 0f);
}

/// <summary>
/// 패시브 스택 추적기 (순수). 스택 수·누적량·감소 규칙을 가진다.
/// 타이머는 활성일 때만 흐르고 대기 중에는 스택을 유지한다(설계도 §4-7 규칙 1).
/// </summary>
public class StackTracker
{
    readonly StackSpec _spec;
    int   _stacks;
    float _amount;     // 피해 비례 모드 누적량
    float _idle;       // 마지막 증가 이후 활성 경과 시간

    public StackTracker(StackSpec spec) { _spec = spec; }

    int Max => Mathf.Max(1, _spec.maxStacks);

    public int  Stacks => _stacks;
    public bool IsFull => _stacks >= Max;

    /// <summary>능력치에 더할 총량: 피해 비례 모드는 누적량, 아니면 스택 × 스택당 수치.</summary>
    public float Amount => _spec.damageRatio > 0f ? _amount : _stacks * _spec.perStackValue;

    public void Add(int n = 1)
    {
        _stacks = Mathf.Min(Max, _stacks + n);
        _idle = 0f;
    }

    /// <summary>피해 비례 모드: 가한 피해 × ratio ÷ 기본 공격력을 누적(상한 cap).</summary>
    public void AddDamage(int damage, int baseAtk)
    {
        if (_spec.damageRatio <= 0f || baseAtk <= 0) return;
        float add = damage * _spec.damageRatio / baseAtk;
        _amount = _spec.cap > 0f ? Mathf.Min(_spec.cap, _amount + add) : _amount + add;
        _idle = 0f;
    }

    /// <summary>시간 진행. 활성일 때만 흐르고(대기 시 정지), 감소 방식에 따라 스택을 줄인다.</summary>
    public void Tick(float dt, bool isActive)
    {
        if (!isActive) return;
        if (_stacks == 0 && _amount <= 0f) { _idle = 0f; return; }
        if (_spec.decaySeconds <= 0f) return;

        _idle += dt;
        switch (_spec.decay)
        {
            case StackDecay.ResetAfterIdle:
                if (_idle >= _spec.decaySeconds) Clear();
                break;
            case StackDecay.DecayOneEvery:
                while (_idle >= _spec.decaySeconds && _stacks > 0)
                {
                    _stacks--;
                    _idle -= _spec.decaySeconds;
                }
                if (_stacks == 0) _idle = 0f;
                break;
        }
    }

    /// <summary>ConsumeAtCap: 가득 찼으면 비우고 true(다음 타격에서 소모).</summary>
    public bool TryConsume()
    {
        if (!IsFull) return false;
        Clear();
        return true;
    }

    public void Clear() { _stacks = 0; _amount = 0f; _idle = 0f; }
}

/// <summary>상시 수정자 1건의 상태·계산 (순수). 경로 B.</summary>
public class ModifierState
{
    readonly PassiveModifierSpec _spec;
    readonly float _value, _cap;
    float _ramp;

    public ModifierState(PassiveModifierSpec spec, bool milestoneActive = false)
    {
        _spec  = spec;
        _value = milestoneActive && spec.milestoneValue != 0f ? spec.milestoneValue : spec.value;   // 0=교체 없음(음수 허용)
        _cap   = milestoneActive && spec.milestoneCap   > 0f ? spec.milestoneCap   : spec.cap;
    }

    public StatKind Stat => _spec.stat;
    public bool IsPartyWide => _spec.partyWide || _spec.source == ModifierSource.PartyPresence;
    public float EnemyAuraRadius => _spec.enemyAuraRadius;
    public bool  ImmuneToSlow    => _spec.immuneToSlow;

    /// <summary>MovingRamp 전용: 이동 중 초당 증가, 정지 시 초당 감소.</summary>
    public void Tick(float dt, bool isMoving)
    {
        if (_spec.source != ModifierSource.MovingRamp) return;
        if (isMoving) _ramp = Mathf.Min(_cap > 0f ? _cap : float.MaxValue, _ramp + _value * dt);
        else          _ramp = Mathf.Max(0f, _ramp - _spec.decayPerSecond * dt);
    }

    /// <summary>이 수정자가 능력치에 곱할 배율. lostHpPercent = 잃은 HP 0~100.</summary>
    public string CounterId => _spec.counterId;

    public float Multiplier(float lostHpPercent, int counterValue = 0)
    {
        switch (_spec.source)
        {
            case ModifierSource.Counter:
                return Mathf.Max(0f, 1f + _value * counterValue);
            case ModifierSource.LostHpPercent:
            {
                float red = Mathf.Max(0f, lostHpPercent) * _value;
                if (_cap > 0f) red = Mathf.Min(_cap, red);
                return Mathf.Max(0f, 1f - red);
            }
            case ModifierSource.MovingRamp:
                return 1f + _ramp;
            default:   // Constant, PartyPresence
                return _value;
        }
    }
}

/// <summary>전투 시작 위치 기록 (영벡터용). 5초 이상 전투가 끊긴 뒤 시작된 교전의 첫 위치를 기억한다.</summary>
public class CombatAnchor
{
    public const float QuietSeconds = 5f;

    float _quiet = QuietSeconds;
    public Vector3 StartPosition { get; private set; }
    public bool    HasAnchor     { get; private set; }

    /// <summary>적중·피격이 발생할 때 호출(pos=그 순간 플레이어 위치).</summary>
    public void NotifyCombat(Vector3 pos)
    {
        if (!HasAnchor || _quiet >= QuietSeconds)
        {
            StartPosition = pos;
            HasAnchor     = true;
        }
        _quiet = 0f;
    }

    public void Tick(float dt) { _quiet += dt; }

    public void Clear() { HasAnchor = false; _quiet = QuietSeconds; }
}
