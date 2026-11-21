using System;
using UnityEngine;

/// <summary>플레이어 이동·점프 상태 스냅샷 (폼 자동 규칙·등속운동 패시브 공용). PlayerController가 갱신.</summary>
public struct CombatMotionState
{
    public float speedRatio;      // 수평속도 / 최대 속도(runSpeed)
    public bool  isMoving;
    public bool  isAirborne;
    public float verticalSpeed;
}

/// <summary>적 속성 조회 (P5의 AttributeProfile이 구현). HitTargetStat 조건용.</summary>
public interface IEnemyAttributes
{
    bool TryGet(string key, out float value);
}

/// <summary>수동 폼 전환 판정 입력.</summary>
public struct FormContext
{
    public float secondsSinceHitTaken;
    public bool  isMoving;
}

/// <summary>HP·MP 비율 이월 (설계도 22 §4-6-1 ②).</summary>
public static class FormMath
{
    /// <summary>새 현재값 = 새 최대 × (이전 현재 / 이전 최대). 살아 있으면 최소 1, 새 최대 초과 불가.</summary>
    public static int CarryOver(int oldCurrent, int oldMax, int newMax)
    {
        if (oldCurrent <= 0 || newMax <= 0) return 0;
        if (oldMax <= 0) return newMax;
        float ratio = Mathf.Clamp01((float)oldCurrent / oldMax);
        int v = Mathf.RoundToInt(newMax * ratio);
        return Mathf.Clamp(Mathf.Max(1, v), 1, newMax);
    }
}

/// <summary>수동 폼 전환 규칙 해석 (순수 정적).</summary>
public static class FormChangeResolver
{
    /// <summary>
    /// 현재 폼에서 이 전환 키를 눌렀을 때 도달할 폼. 전환 불가면 null.
    /// isRevert = 재입력으로 기본 폼에 복귀하는 경우(mp 무소모).
    /// </summary>
    public static string Resolve(FormChangeSpec spec, string current, string defaultForm,
                                 FormContext ctx, out bool isRevert)
    {
        isRevert = false;
        if (spec == null) return null;

        switch (spec.mode)
        {
            case FormChangeMode.Toggle:
            {
                if (spec.sequence == null || spec.sequence.Length != 2) return null;
                int i = Array.IndexOf(spec.sequence, current);
                return i < 0 ? null : spec.sequence[1 - i];
            }
            case FormChangeMode.Sequential:
            {
                if (spec.sequence == null) return null;
                int i = Array.IndexOf(spec.sequence, current);
                if (i < 0 || i >= spec.sequence.Length - 1) return null;
                return spec.sequence[i + 1];
            }
            case FormChangeMode.Hub:
                if (InFrom(spec, current, defaultForm)) return spec.toForm;
                return TryRevert(spec, current, defaultForm, out isRevert);

            case FormChangeMode.StateBased:
                if (InFrom(spec, current, defaultForm))
                    return PickByState(spec, current, ctx);
                return TryRevert(spec, current, defaultForm, out isRevert);
        }
        return null;
    }

    static bool InFrom(FormChangeSpec spec, string current, string defaultForm)
    {
        if (spec.fromForms == null || spec.fromForms.Length == 0) return current == defaultForm;
        return Array.IndexOf(spec.fromForms, current) >= 0;
    }

    static string TryRevert(FormChangeSpec spec, string current, string defaultForm, out bool isRevert)
    {
        isRevert = false;
        if (!spec.pressAgainReverts) return null;
        string target = string.IsNullOrEmpty(spec.revertToForm) ? defaultForm : spec.revertToForm;
        if (string.IsNullOrEmpty(target) || target == current) return null;
        isRevert = true;
        return target;
    }

    static string PickByState(FormChangeSpec spec, string current, FormContext ctx)
    {
        if (spec.stateRules == null) return null;
        string best = null; int bestPri = int.MinValue;
        foreach (var r in spec.stateRules)
        {
            bool match = r.cond switch
            {
                FormStateCond.Always      => true,
                FormStateCond.RecentlyHit => ctx.secondsSinceHitTaken <= r.param,
                FormStateCond.Moving      => ctx.isMoving,
                FormStateCond.Idle        => !ctx.isMoving,
                _ => false,
            };
            if (match && r.priority > bestPri) { best = r.toForm; bestPri = r.priority; }
        }
        return best == current ? null : best;
    }
}

/// <summary>
/// 자동 폼 전환 평가기 (순수). 활성 캐릭터에서만 Evaluate를 호출하고 입장 시 OnSwitchIn으로 카운터를 초기화한다.
/// 가장 높은 우선순위로 맞는 규칙이 이긴다(현재 폼과 같아도 낮은 규칙을 막는다 → 히스테리시스·점프 덮어쓰기).
/// </summary>
public class FormAutoEvaluator
{
    /// <summary>폼이 바뀐 뒤 최소 유지 시간(초, 잠정). 깜빡임 방지.</summary>
    public const float MinHoldSeconds = 0.25f;

    readonly FormAutoRule[] _rules;
    readonly int[]          _streak;
    float _idleTime, _movingTime;
    float _sinceChange = float.MaxValue;

    public FormAutoEvaluator(FormAutoRule[] rules)
    {
        _rules  = rules ?? new FormAutoRule[0];
        _streak = new int[_rules.Length];
    }

    public bool HasRules => _rules.Length > 0;

    /// <summary>활성이 되는 순간: 지속시간·연속 타격 카운터를 0으로.</summary>
    public void OnSwitchIn()
    {
        _idleTime = _movingTime = 0f;
        Array.Clear(_streak, 0, _streak.Length);
    }

    /// <summary>폼이 바뀌었음을 알린다(최소 유지 시간 시작).</summary>
    public void NotifyFormChanged() => _sinceChange = 0f;

    /// <summary>타격 1회 등록(공격 행동이 적을 맞힐 때마다). attrs==null이면 연속이 끊긴다.</summary>
    public void RegisterHit(IEnemyAttributes attrs)
    {
        for (int i = 0; i < _rules.Length; i++)
        {
            var r = _rules[i];
            if (r.cond != FormAutoCond.HitTargetStat) continue;
            if (attrs != null && attrs.TryGet(r.statKey, out float v) && Compare(v, r))
                _streak[i]++;
            else
                _streak[i] = 0;
        }
    }

    static bool Compare(float v, FormAutoRule r)
        => r.statCompare == FormCompare.AtLeast ? v >= r.statThreshold : v < r.statThreshold;

    /// <summary>dt초 진행하며 전환할 폼을 반환(없으면 null).</summary>
    public string Evaluate(float dt, CombatMotionState m, string currentForm)
    {
        if (m.isMoving) { _movingTime += dt; _idleTime = 0f; }
        else            { _idleTime   += dt; _movingTime = 0f; }
        if (_sinceChange < float.MaxValue) _sinceChange += dt;

        string best = null; int bestPri = int.MinValue;
        for (int i = 0; i < _rules.Length; i++)
        {
            var r = _rules[i];
            if (!Matches(i, r, m)) continue;
            if (r.priority > bestPri) { best = r.toForm; bestPri = r.priority; }
        }

        if (best == null || best == currentForm) return null;
        if (_sinceChange < MinHoldSeconds) return null;

        Array.Clear(_streak, 0, _streak.Length);   // 발동 후 연속 타격 카운터 초기화
        return best;
    }

    bool Matches(int i, FormAutoRule r, CombatMotionState m)
    {
        switch (r.cond)
        {
            case FormAutoCond.SpeedRatioAtLeast: return m.speedRatio >= r.param;
            case FormAutoCond.SpeedRatioBelow:   return m.speedRatio <  r.param;
            case FormAutoCond.JumpApex:          return m.isAirborne && Mathf.Abs(m.verticalSpeed) < r.param;
            case FormAutoCond.JumpFalling:       return m.isAirborne && m.verticalSpeed < -r.param;
            case FormAutoCond.IdleFor:           return _idleTime   >= r.param;
            case FormAutoCond.MovingFor:         return _movingTime >= r.param;
            case FormAutoCond.HitTargetStat:     return _streak[i] >= Mathf.Max(1, r.consecutive);
        }
        return false;
    }
}
