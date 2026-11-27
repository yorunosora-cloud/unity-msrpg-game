using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어 전투 컴포넌트. Player GameObject에 부착.
/// Q키 → 전방 부채꼴 내 모든 Enemy에 ReceiveHit() 호출.
/// 속성과 공격력은 PlayerRuntime.Active(현재 활성 캐릭터)에서 가져온다.
/// </summary>
public class PlayerCombat : MonoBehaviour
{
    [Header("공격 범위")]
    [SerializeField] float attackRange     = 2.2f; // 최대 사거리
    [SerializeField] float attackHalfAngle = 60f;  // 전방 기준 ±각도 (총 부채꼴 각 = 2×)

    [Header("쿨다운")]
    [SerializeField] float attackCooldown = 0.5f;

    [Header("평타 MP 회복")]
    [Tooltip("평타 1회당 회복되는 MP 고정값")]
    [SerializeField] int mpRecoverPerHit = 8;

    BoxCharacterBuilder _builder;
    float               _cooldownTimer;

    void Awake()
    {
        _builder = GetComponent<BoxCharacterBuilder>();
    }

    void Update()
    {
        _cooldownTimer = TickAttackCooldown(_cooldownTimer, Time.deltaTime, PlayerRuntime.Active);

        if (UIManager.IsAnyPanelOpen) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.qKey.wasPressedThisFrame && _cooldownTimer <= 0f)
            DoAttack();
    }

    /// <summary>기본기 쿨타임을 dt초 진행한다. 캐릭터의 쿨타임 진행 배율(CooldownRate)을 반영(없으면 1배).</summary>
    public static float TickAttackCooldown(float remaining, float dt, CombatCharacter c)
        => remaining - dt * (c != null ? c.StatMult(StatKind.CooldownRate) : 1f);

    void DoAttack()
    {
        var active = PlayerRuntime.Active;
        // 기절·스턴 중엔 공격 불가. 못 하는 공격이 쿨타임을 소모하지 않게(스턴이 풀리면 바로 공격 가능)
        if (active == null || !active.CanAct) return;

        // 기본기(슬롯 0) 수치: 슬롯 0 스킬이 있으면 그 값, 없으면 인스펙터 기본값
        var spec = BasicAttack.Resolve(active, attackRange, attackHalfAngle, attackCooldown);
        _cooldownTimer = spec.cooldown;

        // 기본기가 효과 목록을 가지면(출혈·카운터 소모 등) 효과 실행기로 처리한다(설계도 구현계획 23-C)
        int qIndex = active.IndexForSlot(SkillSlots.Basic);
        var qSkill = qIndex >= 0 ? active.SkillAt(qIndex) : null;
        if (qSkill != null && qSkill.effects != null && qSkill.effects.Length > 0)
        {
            SkillExecutor.ExecuteBasic(qSkill, qIndex, active, transform);
            _builder?.PlayAttack();
            active.RecoverMp(mpRecoverPerHit);
            return;
        }

        int       atk     = Mathf.RoundToInt(active.Atk * spec.damageMultiplier);
        float     hitMult = active.HitMultiplier(true);          // 공격 행동당 1회(소모형 보너스 소모)
        int       actionId = CombatCharacter.NewActionId();
        var       hits    = new List<HitInfo>();
        Continent element = active.Element;
        Vector3   origin  = transform.position;
        Vector3   forward = transform.forward;

        // 부채꼴 내 Enemy 전체 공격
        foreach (var enemy in Enemy.All.ToArray()) // ToArray: 도중에 제거될 수 있어 복사본 순회
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;

            Vector3 toEnemy = enemy.transform.position - origin;
            toEnemy.y = 0f; // 수직 차이 무시 (수평 거리만 판정)

            float dist  = toEnemy.magnitude;
            if (dist > spec.range) continue;

            float angle = Vector3.Angle(forward, toEnemy);
            if (angle > spec.halfAngle) continue;

            HitPipeline.Apply(enemy, atk, active, hitMult, spec.range, origin, "basic", true, actionId, hits, spec.strengthTags);
        }

        // Boss 본체 타격
        var boss = Boss.Active;
        if (boss != null && !boss.IsDead)
        {
            Vector3 toBoss = boss.transform.position - origin;
            toBoss.y = 0f;
            float bossDist  = toBoss.magnitude;
            float bossAngle = Vector3.Angle(forward, toBoss);
            if (bossDist <= spec.range && bossAngle <= spec.halfAngle)
                HitPipeline.Apply(boss, atk, active, hitMult, spec.range, origin, "basic", true, actionId, hits, spec.strengthTags);
        }

        // 노출된 약점 코어 타격
        foreach (var wp in BossWeakPoint.ExposedInstances.ToArray())
        {
            if (wp == null || ((IDamageable)wp).IsDead) continue;
            Vector3 toWp = wp.transform.position - origin;
            toWp.y = 0f;
            float wpDist  = toWp.magnitude;
            float wpAngle = Vector3.Angle(forward, toWp);
            if (wpDist <= spec.range && wpAngle <= spec.halfAngle)
                wp.ReceiveHit(element, Mathf.RoundToInt(atk * hitMult));
        }

        // 적중 알림: OnHit 패시브(스택·효과)
        if (hits.Count > 0) active.NotifyHits(hits, true);

        // 공격 모션 트리거
        _builder?.PlayAttack();

        // 평타 MP 회복 (swing 자체로 회복 — 공격 성공 여부 무관)
        active.RecoverMp(mpRecoverPerHit);
    }

#if UNITY_EDITOR
    // 에디터에서 부채꼴 범위 시각화
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.35f);
        Vector3 left  = Quaternion.Euler(0, -attackHalfAngle, 0) * transform.forward * attackRange;
        Vector3 right = Quaternion.Euler(0,  attackHalfAngle, 0) * transform.forward * attackRange;
        Gizmos.DrawLine(transform.position, transform.position + left);
        Gizmos.DrawLine(transform.position, transform.position + right);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
#endif
}
