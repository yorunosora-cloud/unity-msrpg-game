using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어 전투 컴포넌트. Player GameObject에 부착.
/// Q키 → 전방 부채꼴 내 모든 Enemy에 ReceiveHit() 호출.
/// </summary>
public class PlayerCombat : MonoBehaviour
{
    [Header("속성")]
    [SerializeField] Continent playerElement = Continent.Physics; // 인스펙터에서 고정

    [Header("공격 범위")]
    [SerializeField] float attackRange     = 2.2f; // 최대 사거리
    [SerializeField] float attackHalfAngle = 60f;  // 전방 기준 ±각도 (총 부채꼴 각 = 2×)

    [Header("쿨다운")]
    [SerializeField] float attackCooldown = 0.5f;

    BoxCharacterBuilder _builder;
    float               _cooldownTimer;

    void Awake()
    {
        _builder = GetComponent<BoxCharacterBuilder>();
    }

    void Update()
    {
        _cooldownTimer -= Time.deltaTime;

        if (UIManager.IsAnyPanelOpen) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.qKey.wasPressedThisFrame && _cooldownTimer <= 0f)
            DoAttack();
    }

    void DoAttack()
    {
        _cooldownTimer = attackCooldown;

        var stats = PlayerRuntime.Stats;
        if (stats == null) return;

        int atk = stats.Atk;
        Vector3 origin  = transform.position;
        Vector3 forward = transform.forward;

        // 부채꼴 내 Enemy 전체 공격
        foreach (var enemy in Enemy.All.ToArray()) // ToArray: 도중에 제거될 수 있어 복사본 순회
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;

            Vector3 toEnemy = enemy.transform.position - origin;
            toEnemy.y = 0f; // 수직 차이 무시 (수평 거리만 판정)

            float dist  = toEnemy.magnitude;
            if (dist > attackRange) continue;

            float angle = Vector3.Angle(forward, toEnemy);
            if (angle > attackHalfAngle) continue;

            enemy.ReceiveHit(playerElement, atk);
        }

        // 공격 모션 트리거
        _builder?.PlayAttack();
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
