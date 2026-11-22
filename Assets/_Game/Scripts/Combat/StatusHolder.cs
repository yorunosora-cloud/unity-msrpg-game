using System;
using System.Collections.Generic;

/// <summary>배율 상태 효과가 영향을 주는 능력치.</summary>
public enum StatKind { Atk, Def, MoveSpeed, DamageTaken, CooldownRate, SkillPower, HitDamage, BasicPower }   // SkillPower: 모든 공격 위력 / HitDamage: 다음 타격 배율(소모형) / BasicPower: 기본기 위력   // CooldownRate: 쿨타임 진행 배율(>1이 빠름)

/// <summary>상태 효과의 동작 종류.</summary>
public enum StatusKind { StatMod, Dot, Stun, Shield }

/// <summary>
/// 시간이 있는 상태 효과 1건 (설계도 22 §4-4).
/// Value: StatMod=배율(1=변화 없음), Dot=초당 피해, Shield=흡수량.
/// </summary>
public class StatusEffect
{
    public string     Id;
    public StatusKind Kind;
    public StatKind   Stat;
    public float      Value;
    public float      Duration;            // 남은 시간(초). float.PositiveInfinity 허용
    public bool       IsBeneficial;        // 이로운 효과: 활성일 때만 시간이 흐른다
    public int        MaxStacks = 1;
    public bool       ConsumeOnHit;        // 다음 공격 행동에서 소모(가속도·양자화)

    // ── 런타임 상태 (Apply/Tick이 관리) ──
    public int   Stacks = 1;
    public bool  Pending;                  // 대기 캐릭터에게 걸린 예약 상태
    public float PendingLife;
    internal float DotAccum;
}

/// <summary>
/// 상태 효과 컨테이너 — 순수 C# (설계도 22 §4-4, §4-5-1).
/// 시간 규칙: 이로운 효과는 소유자가 활성일 때만 흐르고(대기 중 일시정지),
/// 대기 캐릭터에게 건 이로운 효과는 예약(Pending)되어 활성이 되는 순간 시작한다.
/// 해로운 효과(디버프·도트·스턴)는 항상 실시간으로 흐른다.
/// </summary>
public class StatusHolder
{
    /// <summary>예약 상태 버프의 실시간 수명(초). 설계도 §4-5-1 튜닝 값.</summary>
    public const float PendingLifetime = 60f;

    const float Epsilon = 1e-4f;   // 프레임 dt 누적 부동소수 오차 허용

    readonly List<StatusEffect> _effects = new();

    public IReadOnlyList<StatusEffect> Effects => _effects;
    public int Count => _effects.Count;

    /// <summary>
    /// 효과를 부여한다. 같은 Id가 있으면 갱신(더 강한 수치·더 긴 시간), 스택형은 스택 +1.
    /// ownerActive=false 이고 이로운 효과이면 예약 상태로 보관한다.
    /// </summary>
    public void Apply(StatusEffect e, bool ownerActive)
    {
        if (e == null) return;

        var existing = _effects.Find(x => x.Id == e.Id && x.Kind == e.Kind && x.Stat == e.Stat);
        if (existing != null)
        {
            if (Stronger(e, existing)) existing.Value = e.Value;
            existing.Duration = Math.Max(existing.Duration, e.Duration);
            if (existing.MaxStacks < e.MaxStacks) existing.MaxStacks = e.MaxStacks;
            if (existing.MaxStacks > 1) existing.Stacks = Math.Min(existing.MaxStacks, existing.Stacks + 1);
            if (existing.Pending)
            {
                if (ownerActive) existing.Pending = false;
                else existing.PendingLife = PendingLifetime;
            }
            return;
        }

        e.Stacks = 1;
        if (e.IsBeneficial && !ownerActive)
        {
            e.Pending     = true;
            e.PendingLife = PendingLifetime;
        }
        else e.Pending = false;
        _effects.Add(e);
    }

    static bool Stronger(StatusEffect a, StatusEffect b)
    {
        if (a.Kind == StatusKind.StatMod)
            return Math.Abs(a.Value - 1f) > Math.Abs(b.Value - 1f);
        return a.Value > b.Value;
    }

    /// <summary>
    /// dt초 진행. isActive=소유자가 현재 활성 캐릭터인지.
    /// dealDot: 도트 피해(정수)를 소유자에게 전달하는 콜백(null 허용).
    /// </summary>
    public void Tick(float dt, bool isActive, Action<int> dealDot)
    {
        for (int i = _effects.Count - 1; i >= 0; i--)
        {
            var e = _effects[i];

            if (e.Pending)
            {
                if (isActive) { e.Pending = false; continue; }   // 활성이 된 틱은 시작만, 시간 소모 없음
                e.PendingLife -= dt;
                if (e.PendingLife <= 0f) _effects.RemoveAt(i);
                continue;
            }

            if (e.IsBeneficial && !isActive) continue;          // 대기 중 이로운 효과는 일시정지

            if (e.Kind == StatusKind.Dot)
            {
                e.DotAccum += e.Value * e.Stacks * dt;
                int whole = (int)(e.DotAccum + Epsilon);   // 부동소수 오차 보정
                if (whole > 0)
                {
                    e.DotAccum -= whole;
                    dealDot?.Invoke(whole);
                    // 콜백(피해→기절→Clear 등)이 목록을 바꿨으면 이 효과는 더 처리하지 않는다
                    if (i >= _effects.Count || !ReferenceEquals(_effects[i], e))
                    {
                        i = Math.Min(i, _effects.Count);
                        continue;
                    }
                }
            }

            e.Duration -= dt;
            if (e.Duration <= Epsilon) _effects.RemoveAt(i);
        }
    }

    /// <summary>소유자가 활성이 되는 순간 호출 — 예약 효과를 즉시 시작한다.</summary>
    public void OnSwitchIn()
    {
        foreach (var e in _effects) e.Pending = false;
    }

    /// <summary>능력치 k에 대한 배율 합성(곱). 예약 상태 효과는 제외. 스택 배율 = 1 + (value−1) × stacks.</summary>
    public float Mult(StatKind k)
    {
        float m = 1f;
        foreach (var e in _effects)
        {
            if (e.Pending || e.Kind != StatusKind.StatMod || e.Stat != k) continue;
            m *= Math.Max(0f, 1f + (e.Value - 1f) * e.Stacks);
        }
        return m;
    }

    public int Stacks(string id)
    {
        var e = _effects.Find(x => x.Id == id);
        return e == null ? 0 : e.Stacks;
    }

    public bool Has(string id) => _effects.Exists(x => x.Id == id);

    public bool IsStunned
    {
        get
        {
            foreach (var e in _effects)
                if (!e.Pending && e.Kind == StatusKind.Stun) return true;
            return false;
        }
    }

    /// <summary>남은 보호막 총량(예약 제외).</summary>
    public float ShieldAmount
    {
        get
        {
            float s = 0f;
            foreach (var e in _effects)
                if (!e.Pending && e.Kind == StatusKind.Shield) s += e.Value;
            return s;
        }
    }

    /// <summary>보호막이 피해를 먼저 흡수한다. 흡수하고 남은 피해를 반환.</summary>
    public int Absorb(int damage)
    {
        int remaining = damage;
        for (int i = 0; i < _effects.Count && remaining > 0; i++)
        {
            var e = _effects[i];
            if (e.Pending || e.Kind != StatusKind.Shield) continue;
            float take = Math.Min(e.Value, remaining);
            e.Value   -= take;
            remaining -= (int)Math.Ceiling(take);
            if (e.Value <= 0.0001f) { _effects.RemoveAt(i); i--; }
        }
        return Math.Max(0, remaining);
    }

    /// <summary>다음 공격 행동에서 소모되는 효과(ConsumeOnHit)를 제거한다. 제거 개수 반환.</summary>
    public int ConsumeHitEffects()
        => _effects.RemoveAll(e => e.ConsumeOnHit && !e.Pending);

    /// <summary>해로운 효과(디버프·도트·스턴) 해제.</summary>
    public void Cleanse() => _effects.RemoveAll(e => !e.IsBeneficial);

    /// <summary>모든 효과 제거(기절·전체 회복·던전 이탈).</summary>
    public void Clear() => _effects.Clear();
}
