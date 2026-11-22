using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 패시브의 월드 효과 실행기 (설계도 22 §4-7). Player GameObject에 부착(PlayerSkills가 자동 추가).
/// - 파티 전원의 PassiveFired를 받아 광역 피해·반사·넉백·마크·상태 효과·텔레포트를 실행
/// - 전투 활동(적중·피격)을 Party.Anchor에 기록(영벡터의 "전투 시작 위치")
/// - 활성 캐릭터의 적 오라(질량 정지 관성 등)를 반경 내 적에게 매 프레임 갱신
/// 패시브가 일으킨 피해는 OnHit/OnHitTaken을 다시 일으키지 않는다(HitPipeline을 거치지 않으므로 NotifyHits 대상이 아님).
/// </summary>
public class PassiveHost : MonoBehaviour
{
    const float AuraRefreshSeconds = 0.3f;

    Party _party;
    readonly List<(CombatCharacter m, Action<PassiveFire> fire, Action act)> _subs = new();

    void Update()
    {
        if (PlayerRuntime.Party != _party) Rebind(PlayerRuntime.Party);

        var active = PlayerRuntime.Active;
        if (active != null && !active.IsDowned) ApplyAuras(active);
    }

    void OnDestroy() => Rebind(null);

    void Rebind(Party party)
    {
        foreach (var s in _subs)
        {
            s.m.PassiveFired    -= s.fire;
            s.m.OnCombatActivity -= s.act;
        }
        _subs.Clear();

        _party = party;
        if (party == null) return;

        foreach (var member in party.Members)
        {
            var m = member;
            Action<PassiveFire> fire = f => Execute(m, f);
            Action act = () => _party?.Anchor.NotifyCombat(transform.position);
            m.PassiveFired     += fire;
            m.OnCombatActivity += act;
            _subs.Add((m, fire, act));
        }
    }

    // ── 오라 ──────────────────────────────────────────────────────────────

    void ApplyAuras(CombatCharacter active)
    {
        foreach (var (radius, stat, value) in active.EnemyAuras())
        {
            foreach (var enemy in Enemy.All.ToArray())
            {
                if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;
                Vector3 d = enemy.transform.position - transform.position; d.y = 0f;
                if (d.magnitude > radius) continue;
                enemy.ApplyStatus(new StatusEffect
                {
                    Id = "aura:" + stat, Kind = StatusKind.StatMod, Stat = stat, Value = value,
                    Duration = AuraRefreshSeconds, IsBeneficial = false,
                });
            }
        }
    }

    // ── 효과 실행 ─────────────────────────────────────────────────────────

    void Execute(CombatCharacter owner, PassiveFire fire)
    {
        var e = fire.effect;
        var target = e.target == TargetScope.Attacker ? fire.hit.attacker : fire.hit.target;

        switch (e.type)
        {
            case SkillEffectType.Teleport:
                if (_party != null && _party.Anchor.HasAnchor) TeleportTo(_party.Anchor.StartPosition);
                break;

            case SkillEffectType.Reflect:
                if (Alive(target))
                    target.ReceiveHit(owner.Element, Mathf.Max(1, Mathf.RoundToInt(fire.hit.damage * fire.value)));
                break;

            case SkillEffectType.Damage:
                ExecDamage(owner, fire, target);
                break;

            case SkillEffectType.Knockback:
                if (Alive(target)) target.Displace(transform.position, fire.value, pull: false);
                break;
            case SkillEffectType.Pull:
                if (Alive(target)) target.Displace(transform.position, fire.value, pull: true);
                break;

            case SkillEffectType.Mark:
                if (Alive(target)) target.AddMark(fire.skillId, e.duration);
                break;

            case SkillEffectType.Dot:
                Status(target, fire, StatusKind.Dot, StatKind.Atk, owner.Atk * fire.value);
                break;
            case SkillEffectType.Slow:
                Status(target, fire, StatusKind.StatMod, StatKind.MoveSpeed, fire.value);
                break;
            case SkillEffectType.Stun:
                Status(target, fire, StatusKind.Stun, StatKind.Atk, 0f);
                break;
            case SkillEffectType.StatDebuff:
                Status(target, fire, StatusKind.StatMod, e.stat, fire.value);
                break;
            case SkillEffectType.DamageTakenMult:
                Status(target, fire, StatusKind.StatMod, StatKind.DamageTaken, fire.value);
                break;
        }
    }

    /// <summary>패시브 피해. areaRadius>0이면 적중 지점 중심 반경 안의 다른 적들(적중 대상 제외)에게.</summary>
    void ExecDamage(CombatCharacter owner, PassiveFire fire, ISkillTarget target)
    {
        int atk = Mathf.Max(1, Mathf.RoundToInt(owner.Atk * fire.value));

        if (fire.effect.areaRadius > 0f)
        {
            Vector3 center = fire.hit.hitPoint;
            foreach (var enemy in Enemy.All.ToArray())
            {
                if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.Unit == null || enemy.Unit.IsDead) continue;
                if ((object)enemy == (object)fire.hit.target) continue;
                Vector3 d = enemy.transform.position - center; d.y = 0f;
                if (d.magnitude <= fire.effect.areaRadius) enemy.ReceiveHit(owner.Element, atk);
            }
        }
        else if (Alive(target))
        {
            target.ReceiveHit(owner.Element, atk);
        }
    }

    static void Status(ISkillTarget target, PassiveFire fire, StatusKind kind, StatKind stat, float value)
    {
        if (!Alive(target)) return;
        var e = fire.effect;
        target.ApplyStatus(new StatusEffect
        {
            Id = $"{fire.skillId}:{e.type}:{stat}", Kind = kind, Stat = stat, Value = value,
            Duration = e.duration, IsBeneficial = false, MaxStacks = Mathf.Max(1, e.maxStacks),
        });
    }

    static bool Alive(ISkillTarget t)
    {
        if (t == null) return false;
        if (t is UnityEngine.Object o && o == null) return false;
        return !t.IsDead;
    }

    /// <summary>CharacterController를 잠시 끄고 위치를 옮긴다(CC가 켜져 있으면 위치 대입이 무시될 수 있다).</summary>
    void TeleportTo(Vector3 pos)
    {
        var cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        transform.position = pos;
        if (cc != null) cc.enabled = true;
    }
}
