using System;
using System.Collections;
using UnityEngine;

public class Boss : MonoBehaviour, ISkillTarget
{
    public static Boss Active { get; private set; }

    [SerializeField] BossDef _def;

    EnemyUnit     _unit;
    BossWeakPoint _weakPoint;
    bool          _defeated;

    public bool    IsDead     => _unit == null || _unit.IsDead;
    public float   HpFraction => _unit?.HpFraction ?? 0f;
    public BossDef Def        => _def;

    /// <summary>보스 상태 효과. 스턴·넉백·견인은 BossDef.ccImmune이면 면역. 설계도 22 §4-9.</summary>
    public StatusHolder Status { get; } = new StatusHolder();

    bool CcImmune => _def != null && _def.ccImmune;

    public event Action OnDefeated;

    Transform IDamageable.Transform => transform;
    bool      IDamageable.IsDead    => IsDead;

    void Awake()
    {
        _weakPoint = GetComponentInChildren<BossWeakPoint>(includeInactive: true);
        BuildVisual();
    }

    void Start()
    {
        if (_def == null) { Debug.LogError("[Boss] BossDef not assigned."); return; }
        _unit = new EnemyUnit(_def.maxHp, _def.def, _def.weakness, expReward: 0);
    }

    void Update()
    {
        if (_unit == null || _unit.IsDead || _defeated) return;
        Status.Tick(Time.deltaTime, false, d => ApplyDamage(d, false));
    }

    public void ApplyStatus(StatusEffect effect)
    {
        if (effect == null) return;
        if (effect.Kind == StatusKind.Stun && CcImmune) return;
        Status.Apply(effect, ownerActive: true);
    }

    public void Displace(Vector3 origin, float distance, bool pull)
    {
        if (CcImmune) return;
        Vector3 away = transform.position - origin; away.y = 0f;
        float d = away.magnitude;
        if (d < 0.001f) return;
        transform.position += (away / d) * (pull ? -Mathf.Min(distance, d) : distance);
    }

    /// <summary>보스는 속성 표식을 받지 않는다(현재 미지원).</summary>
    public void Launch(float height, float duration) { }   // 보스는 CC 면역 — 띄우지 않는다
    public void SetMark(Continent element) { }

    // 패시브 공식 입력: 보스는 속도 스탯·접근 판정·마크를 아직 지원하지 않는다(보스 설계 단계에서 확장)
    public float Spd => 0f;
    public int   StatusStacks(string id)    => 0;     // 보스는 상태 스택 카운터 미지원
    public int   StatusMaxStacks(string id) => 0;
    public void  ConsumeStatus(string id)   { }
    public AttributeProfile Attributes => default;
    public float ApproachSign(Vector3 from) => 0f;
    public bool  HasMark(string id) => false;
    public void  AddMark(string id, float duration) { }
    public void  RemoveMark(string id) { }

    void OnEnable()
    {
        if (Active != null && Active != this)
            Debug.LogWarning($"[Boss] Replacing active boss {Active.name} with {name}.");
        Active = this;
    }

    void OnDisable()
    {
        if (Active == this) Active = null;
    }

    public void ReceiveHit(Continent attackerElement, int atk)
    {
        if (_unit == null || _unit.IsDead || _defeated) return;
        bool isWeak = attackerElement == _unit.Weakness;
        int  dmg    = CombatMath.ComputeDamage(atk, _unit.Def, isWeak);
        dmg = Mathf.Max(1, Mathf.RoundToInt(dmg * Status.Mult(StatKind.DamageTaken)));
        ApplyDamage(dmg, isWeak);
    }

    public void ReceiveWeakHit(int calculatedDamage)
    {
        if (_unit == null || _unit.IsDead || _defeated) return;
        ApplyDamage(calculatedDamage, isCrit: true);
    }

    void ApplyDamage(int dmg, bool isCrit)
    {
        _unit.TakeDamage(dmg);
        DamageNumber.Spawn(transform.position + Vector3.up * 2.5f, dmg, isCrit);
        if (_unit.IsDead && !_defeated)
            StartCoroutine(StartDeath());
    }

    public IEnumerator StartDeath()
    {
        _defeated = true;
        _weakPoint?.SetExposed(0f);
        GetComponent<BossBrain>()?.Cleanup();
        Debug.Log($"[Boss] {_def.defeatLine}");
        yield return new WaitForSeconds(2f);
        FindFirstObjectByType<BossHealthBar>()?.Hide();
        OnDefeated?.Invoke();
    }

    void BuildVisual()
    {
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "BossBody";
        UnityEngine.Object.Destroy(body.GetComponent<CapsuleCollider>());
        body.transform.SetParent(transform, false);
        body.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        body.transform.localScale    = new Vector3(1.2f, 1.5f, 1.2f);

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = Color.white;
        body.GetComponent<Renderer>().material = mat;

        var cc = gameObject.AddComponent<CapsuleCollider>();
        cc.center = new Vector3(0f, 1.5f, 0f);
        cc.radius = 0.6f;
        cc.height = 3f;
    }
}
