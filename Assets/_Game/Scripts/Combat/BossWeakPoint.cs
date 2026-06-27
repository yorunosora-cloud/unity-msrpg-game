using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossWeakPoint : MonoBehaviour, IDamageable
{
    public static readonly List<BossWeakPoint> ExposedInstances = new();

    [SerializeField] float _weakMultiplier = 3f;

    Boss     _boss;
    bool     _exposed;
    Renderer _coreRend;
    Coroutine _exposeRoutine;

    public bool Exposed => _exposed;

    Transform IDamageable.Transform => transform;
    bool      IDamageable.IsDead    => _boss == null || _boss.IsDead;

    void Awake()
    {
        _boss = GetComponentInParent<Boss>();
        BuildCore();
    }

    public void SetExposed(float duration)
    {
        if (_exposeRoutine != null) StopCoroutine(_exposeRoutine);
        if (duration <= 0f) { CloseWeakPoint(); return; }
        _exposeRoutine = StartCoroutine(ExposeRoutine(duration));
    }

    IEnumerator ExposeRoutine(float duration)
    {
        OpenWeakPoint();
        yield return new WaitForSeconds(duration);
        CloseWeakPoint();
    }

    void OpenWeakPoint()
    {
        _exposed = true;
        ExposedInstances.Add(this);
        if (_coreRend) _coreRend.enabled = true;
    }

    void CloseWeakPoint()
    {
        _exposed = false;
        ExposedInstances.Remove(this);
        if (_coreRend) _coreRend.enabled = false;
        _exposeRoutine = null;
    }

    void OnDisable()
    {
        ExposedInstances.Remove(this);
        _exposed = false;
    }

    public void ReceiveHit(Continent attackerElement, int atk)
    {
        if (!_exposed || _boss == null || _boss.IsDead) return;
        int bonusDmg = Mathf.RoundToInt(atk * _weakMultiplier);
        _boss.ReceiveWeakHit(bonusDmg);
        DamageNumber.Spawn(transform.position + Vector3.up, bonusDmg, isCrit: true);
    }

    void BuildCore()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "WeakPointCore";
        Object.Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale    = Vector3.one * 0.4f;

        _coreRend = go.GetComponent<Renderer>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(1f, 0.85f, 0f);
        _coreRend.material = mat;
        _coreRend.enabled  = false;
    }
}
