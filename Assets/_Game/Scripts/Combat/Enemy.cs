using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 MonoBehaviour.
/// 정적 레지스트리(Enemy.All)를 통해 PlayerCombat이 물리 쿼리 없이 탐색한다.
/// </summary>
public class Enemy : MonoBehaviour, ISkillTarget
{
    Transform IDamageable.Transform => transform;
    bool      IDamageable.IsDead    => _unit == null || _unit.IsDead;

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
    [Tooltip("속도 스탯(크로노 시간 지연·도플러 공식 입력). 값은 17번 적 도감 단계에서 채운다")]
    [SerializeField] float spd             = 0f;
    [Tooltip("true면 스턴·넉백·견인 면역")]
    [SerializeField] bool  ccImmune        = false;

    [Header("물리량 속성 (설계도 18, v1 물리만)")]
    [SerializeField] AttributeProfile attributes;

    // ── 공개 접근자 ─────────────────────────────────────────────────────────
    /// <summary>현재 씬에 활성화된 모든 Enemy 목록 (PlayerCombat이 탐색에 사용).</summary>
    public static readonly List<Enemy> All = new();

    public EnemyUnit Unit => _unit;

    /// <summary>적 측 상태 효과(스턴·둔화·도트·받는피해배율 등). 설계도 22 §4-9.</summary>
    public StatusHolder Status { get; } = new StatusHolder();

    // ── 패시브 공식 입력(ISkillTarget) ──
    Vector3 _vel, _lastPos;
    readonly Dictionary<string, float> _marks = new();

    public float Spd => spd;
    public int  StatusStacks(string id)    => Status.Stacks(id);
    public int  StatusMaxStacks(string id) => Status.MaxStacksOf(id);
    public void ConsumeStatus(string id)   => Status.Remove(id);

    public AttributeProfile Attributes => attributes;

    /// <summary>EnemySpawner·테스트가 Start 전에 속성을 주입한다. 17번 적 도감(EnemyDef)이 구현되면 그쪽에서 주입.</summary>
    public void SetAttributes(AttributeProfile a) => attributes = a;

    /// <summary>마지막 처치의 정밀 처치 등급 배율(처치하지 않았으면 1).</summary>
    public float LastKillGrade { get; private set; } = 1f;

    /// <summary>처치 보상량 = max(1, 기본 보상 × 등급). 기본 보상 = max(1, ExpReward/10).</summary>
    public static int RewardAmount(int expReward, float grade)
        => System.Math.Max(1, Mathf.RoundToInt(System.Math.Max(1, expReward / 10) * grade));

    /// <summary>EnemySpawner·테스트가 Start 전에 속도 스탯을 주입한다.</summary>
    public void SetSpd(float v) => spd = v;

    public float ApproachSign(Vector3 from)
    {
        Vector3 to = from - transform.position; to.y = 0f;
        Vector3 v  = _vel; v.y = 0f;
        if (to.sqrMagnitude < 1e-6f || v.sqrMagnitude < 1e-4f) return 0f;
        return Vector3.Dot(v.normalized, to.normalized) >= 0f ? 1f : -1f;
    }

    public bool HasMark(string id) => _marks.ContainsKey(id);
    public void AddMark(string id, float duration) { if (!string.IsNullOrEmpty(id)) _marks[id] = duration; }

    void TickMarks(float dt)
    {
        if (_marks.Count == 0) return;
        foreach (var k in new List<string>(_marks.Keys))
        {
            _marks[k] -= dt;
            if (_marks[k] <= 0f) _marks.Remove(k);
        }
    }

    // ── 내부 상태 ───────────────────────────────────────────────────────────
    EnemyUnit        _unit;
    Transform        _player;
    WorldHealthBar   _hpBar;
    Renderer         _bodyRenderer;
    float            _attackTimer;
    Color            _baseColor;
    float            _flashTimer;

    // ── 표식 상태 ───────────────────────────────────────────────────────────
    Continent? _mark;
    GameObject _markIndicator;

    /// <summary>현재 부여된 속성 표식. null이면 없음.</summary>
    public Continent? Mark => _mark;

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

        // 속도 추정·마크 시간
        float frameDt = Time.deltaTime;
        if (frameDt > 0f) _vel = (transform.position - _lastPos) / frameDt;
        _lastPos = transform.position;
        TickMarks(frameDt);

        // 상태 효과는 플레이어 유무·UI와 무관하게 흐른다 (도트 포함)
        Status.Tick(Time.deltaTime, false, ReceiveDot);
        TickLaunch(Time.deltaTime);
        if (_unit == null || _unit.IsDead) return;

        if (_player == null) return;
        if (UIManager.IsAnyPanelOpen) return;

        float dist = Vector3.Distance(transform.position, _player.position);

        // 피격 플래시 복귀
        if (_flashTimer > 0f)
        {
            _flashTimer -= Time.deltaTime;
            if (_flashTimer <= 0f && _bodyRenderer != null)
                _bodyRenderer.material.color = _baseColor;
        }

        if (Status.IsStunned) return;   // 스턴: 이동·공격 불가

        // 탐지 범위 내 → 추적
        if (dist <= detectRange)
        {
            Vector3 dir = (_player.position - transform.position).normalized;
            dir.y = 0f;

            // 공격 범위 밖이면 이동
            if (dist > attackRange)
            {
                transform.position += dir * (moveSpeed * Status.Mult(StatKind.MoveSpeed) * Time.deltaTime);
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
                    if (active != null && !active.IsDowned && RollHit())
                        active.TakeHit(contactDamage, this);   // 받는 피해 배율·보호막 적용, 패시브(반사·작용반작용)가 공격자를 안다
                }
            }
        }
    }

    // ── 띄우기 (설계도 18 §8-5 #2) ───────────────────────────────────────────

    float _launchHeight, _launchDuration, _launchElapsed;
    Transform _bodyTf;

    /// <summary>현재 띄우기의 최대 높이(진행 중이 아니면 0).</summary>
    public float LaunchHeight => _launchElapsed < _launchDuration ? _launchHeight : 0f;

    /// <summary>현재 체력(Start 전에는 0).</summary>
    public int CurrentHp => _unit?.Hp ?? 0;

    public void Launch(float height, float duration)
    {
        if (ccImmune || height <= 0f || duration <= 0f) return;
        _launchHeight = height; _launchDuration = duration; _launchElapsed = 0f;
    }

    void TickLaunch(float dt)
    {
        if (_launchElapsed >= _launchDuration) return;
        _launchElapsed += dt;
        if (_bodyTf == null && _bodyRenderer != null) _bodyTf = _bodyRenderer.transform;
        if (_bodyTf != null)
        {
            var p = _bodyTf.localPosition;
            p.y = 1f + LaunchCurve.Offset(_launchHeight, _launchDuration, _launchElapsed);
            _bodyTf.localPosition = p;
        }
    }

    // ── 명중 ─────────────────────────────────────────────────────────────────

    /// <summary>테스트용 주사위 주입(0~1 반환). null이면 UnityEngine.Random.</summary>
    public static System.Func<float> HitRoll;

    /// <summary>이 적의 공격이 명중하는지. 명중률 배율(Accuracy)이 1 이상이면 항상 명중.</summary>
    public bool RollHit()
    {
        float acc = Status.Mult(StatKind.Accuracy);
        if (acc >= 1f) return true;
        float roll = HitRoll != null ? HitRoll() : Random.value;
        return roll < acc;
    }

    // ── 표식 ─────────────────────────────────────────────────────────────────

    /// <summary>SkillExecutor가 Mark 스킬 발동 시 호출. 해당 속성 표식을 부여하고 시각 표시.</summary>
    public void SetMark(Continent element)
    {
        _mark = element;

        // 기존 표식 인디케이터 제거 후 재생성
        if (_markIndicator != null)
            Destroy(_markIndicator);

        _markIndicator = CreateMarkIndicator(element);
    }

    GameObject CreateMarkIndicator(Continent element)
    {
        // 머리 위 작은 큐브로 표식 표시 (HP바보다 살짝 위)
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "MarkIndicator";
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 2.8f, 0f);
        go.transform.localScale    = new Vector3(0.25f, 0.25f, 0.25f);

        var rend = go.GetComponent<Renderer>();
        rend.material       = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        rend.material.color = ContinentColors.Of(element);
        return go;
    }

    // ── 상태 효과 · 위치 이동 (ISkillTarget) ────────────────────────────────

    public void ApplyStatus(StatusEffect effect)
    {
        if (effect == null) return;
        if (effect.Kind == StatusKind.Stun && ccImmune) return;
        Status.Apply(effect, ownerActive: true);   // 적 측 효과는 예약 개념 없음
    }

    public void Displace(Vector3 origin, float distance, bool pull)
    {
        if (ccImmune) return;
        Vector3 away = transform.position - origin;
        away.y = 0f;
        float d = away.magnitude;
        if (d < 0.001f) return;
        Vector3 dir = away / d;
        float move = pull ? -Mathf.Min(distance, d) : distance;   // 견인은 중심을 넘지 않는다
        transform.position += dir * move;
    }

    // ── 피격 ─────────────────────────────────────────────────────────────────

    /// <summary>PlayerCombat이 호출. 약점 여부를 계산해 데미지 적용.</summary>
    public void ReceiveHit(Continent attackerElement, int atk)
    {
        if (_unit == null || _unit.IsDead) return;

        bool isWeak = attackerElement == _unit.Weakness;
        int  dmg    = CombatMath.ComputeDamage(atk, _unit.Def, isWeak);
        dmg = Mathf.Max(1, Mathf.RoundToInt(dmg * Status.Mult(StatKind.DamageTaken)));   // 취약/피해감소
        ApplyDamage(dmg, isWeak);
    }

    /// <summary>도트 등 방어력 무시 지속 피해.</summary>
    void ReceiveDot(int amount)
    {
        if (_unit == null || _unit.IsDead || amount <= 0) return;
        ApplyDamage(amount, false);
    }

    void ApplyDamage(int dmg, bool isWeak)
    {
        int hpBefore = _unit.Hp;
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
        {
            // 정밀 처치: 처치 직전 HP와 최종 피해(방어·받는피해배율 적용 후)로 등급 계산 (설계도 18 §3-10)
            LastKillGrade = PrecisionKillGrader.Grade(hpBefore, dmg);
            Die();
        }
    }

    // ── 내부 헬퍼 ───────────────────────────────────────────────────────────

    void Die()
    {
        // 처치 시 해당 과목 연구 자원 지급 (캐릭터 EXP 직접 지급 X — K 패널에서 자원 소모로 레벨업)
        if (MetaState.IsInitialized)
            MetaState.StudyMaterials.Add(_unit.Weakness, RewardAmount(_unit.ExpReward, LastKillGrade));

        _hpBar?.gameObject.SetActive(false);
        Destroy(gameObject);
    }

    void BuildVisual()
    {
        Color col = ContinentColors.Of(weakness);
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
