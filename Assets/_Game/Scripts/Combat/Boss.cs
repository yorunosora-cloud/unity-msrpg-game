using System;
using System.Collections;
using UnityEngine;

public class Boss : MonoBehaviour, IDamageable
{
    public static Boss Active { get; private set; }

    [SerializeField] BossDef _def;

    EnemyUnit     _unit;
    BossWeakPoint _weakPoint;
    bool          _defeated;

    public bool    IsDead     => _unit == null || _unit.IsDead;
    public float   HpFraction => _unit?.HpFraction ?? 0f;
    public BossDef Def        => _def;

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
        Object.Destroy(body.GetComponent<CapsuleCollider>());
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
