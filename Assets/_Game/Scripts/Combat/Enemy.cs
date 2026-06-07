using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 MonoBehaviour.
/// 정적 레지스트리(Enemy.All)를 통해 PlayerCombat이 물리 쿼리 없이 탐색한다.
/// </summary>
public class Enemy : MonoBehaviour
{
    // ── 인스펙터 ────────────────────────────────────────────────────────────
    [Header("스탯")]
    [SerializeField] int       maxHp          = 60;
    [SerializeField] int       def            = 4;
    [SerializeField] Continent weakness       = Continent.Physics;
    [SerializeField] int       expReward      = 40;

    [Header("AI")]
    [SerializeField] float moveSpeed       = 2.5f;
    [SerializeField] float detectRange     = 10f;
    [SerializeField] float attackRange     = 1.2f;
    [SerializeField] int   contactDamage   = 6;
    [SerializeField] float attackInterval  = 1.2f;

    // ── 공개 접근자 ─────────────────────────────────────────────────────────
    /// <summary>현재 씬에 활성화된 모든 Enemy 목록 (PlayerCombat이 탐색에 사용).</summary>
    public static readonly List<Enemy> All = new();

    public EnemyUnit Unit => _unit;

    // ── 내부 상태 ───────────────────────────────────────────────────────────
    EnemyUnit        _unit;
    Transform        _player;
    WorldHealthBar   _hpBar;
    Renderer         _bodyRenderer;
    float            _attackTimer;
    Color            _baseColor;
    float            _flashTimer;

    // 과목별 색상 (Continent 순서 일치)
    static readonly Color[] ContinentColors =
    {
        new Color(0.30f, 0.60f, 1.00f),  // Physics   — 파랑
        new Color(0.80f, 0.30f, 0.80f),  // Chemistry — 보라
        new Color(0.20f, 0.80f, 0.30f),  // Biology   — 초록
        new Color(0.90f, 0.70f, 0.20f),  // EarthSci  — 황토
        new Color(0.95f, 0.40f, 0.10f),  // Math      — 주황
        new Color(0.20f, 0.80f, 0.80f),  // Info      — 청록
        new Color(0.60f, 0.60f, 0.60f),  // Mesoria   — 회색
    };

    // ── 라이프사이클 ────────────────────────────────────────────────────────

    void Awake()
    {
        // _unit은 Start에서 생성 — SetWeakness()가 Awake 직후 호출될 수 있어 weakness 설정이 먼저 돼야 함
        BuildVisual();
    }

    void Start()
    {
        _unit = new EnemyUnit(maxHp, def, weakness, expReward);
        _hpBar?.SetFraction(1f);
        EnemySpawner.TryRegister(transform.position, weakness);
    }

    /// <summary>EnemySpawner.RespawnAll()이 런타임 스폰 시 Start 전에 호출해 weakness를 주입한다.</summary>
    public void SetWeakness(Continent w) => weakness = w;

    void OnEnable()
    {
        All.Add(this);
        var playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null) _player = playerGO.transform;
    }

    void OnDisable()
    {
        All.Remove(this);
    }

    void Update()
    {
        if (_unit == null || _unit.IsDead) return;
        if (_player == null) return;

        float dist = Vector3.Distance(transform.position, _player.position);

        // 피격 플래시 복귀
        if (_flashTimer > 0f)
        {
            _flashTimer -= Time.deltaTime;
            if (_flashTimer <= 0f && _bodyRenderer != null)
                _bodyRenderer.material.color = _baseColor;
        }

        // 탐지 범위 내 → 추적
        if (dist <= detectRange)
        {
            Vector3 dir = (_player.position - transform.position).normalized;
            dir.y = 0f;

            // 공격 범위 밖이면 이동
            if (dist > attackRange)
            {
                transform.position += dir * (moveSpeed * Time.deltaTime);
                if (dir != Vector3.zero)
                    transform.rotation = Quaternion.Slerp(transform.rotation,
                        Quaternion.LookRotation(dir), 10f * Time.deltaTime);
            }
            else
            {
                // 공격 범위 내 → 주기 데미지
                _attackTimer += Time.deltaTime;
                if (_attackTimer >= attackInterval)
                {
                    _attackTimer = 0f;
                    var active = PlayerRuntime.Active;
                    if (active != null && !active.IsDowned)
                        active.Damage(contactDamage);
                }
            }
        }
    }

    // ── 피격 ─────────────────────────────────────────────────────────────────

    /// <summary>PlayerCombat이 호출. 약점 여부를 계산해 데미지 적용.</summary>
    public void ReceiveHit(Continent attackerElement, int atk)
    {
        if (_unit == null || _unit.IsDead) return;

        bool isWeak = attackerElement == _unit.Weakness;
        int  dmg    = CombatMath.ComputeDamage(atk, _unit.Def, isWeak);
        _unit.TakeDamage(dmg);

        // 데미지 숫자 스폰
        DamageNumber.Spawn(transform.position + Vector3.up * 2f, dmg, isWeak);

        // HP바 갱신
        _hpBar?.SetFraction(_unit.HpFraction);

        // 피격 플래시 (흰색)
        if (_bodyRenderer != null)
        {
            _bodyRenderer.material.color = Color.white;
            _flashTimer = 0.12f;
        }

        if (_unit.IsDead)
            Die();
    }

    // ── 내부 헬퍼 ───────────────────────────────────────────────────────────

    void Die()
    {
        // 처치 EXP는 활성 캐릭터가 획득 (기절 직후 교체됐을 수 있어 null 가드)
        var active = PlayerRuntime.Active;
        if (active != null)
            active.GainExp(_unit.ExpReward);

        // HP바 숨김
        _hpBar?.gameObject.SetActive(false);

        Destroy(gameObject);
    }

    void BuildVisual()
    {
        Color col = ContinentColors[(int)weakness % ContinentColors.Length];
        _baseColor = col;

        // 캡슐 몸통
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        Destroy(body.GetComponent<CapsuleCollider>());
        body.transform.SetParent(transform, false);
        body.transform.localPosition = new Vector3(0f, 1f, 0f);
        body.transform.localScale    = new Vector3(0.8f, 1f, 0.8f);
        _bodyRenderer = body.GetComponent<Renderer>();
        _bodyRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        _bodyRenderer.material.color = col;

        // 충돌 검사용 캡슐콜라이더는 루트에 부착
        var cc = gameObject.AddComponent<CapsuleCollider>();
        cc.center = new Vector3(0f, 1f, 0f);
        cc.radius = 0.4f;
        cc.height = 2f;

        // 머리 위 HP바
        _hpBar = WorldHealthBar.Create(transform, offset: new Vector3(0f, 2.3f, 0f));
        _hpBar.SetFraction(1f);
    }
}
