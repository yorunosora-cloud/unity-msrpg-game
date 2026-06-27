using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Telegraph : MonoBehaviour
{
    public static readonly List<Telegraph> All = new();

    float   _warnTime;
    int     _damage;
    bool    _isLine;
    Vector3 _halfExtents;
    float   _radius;
    Vector3 _lineOrigin;
    Vector3 _lineDirection;

    Renderer _rend;
    static readonly Color BaseColor  = new(1f, 0.15f, 0.1f, 0.45f);
    static readonly Color FlashColor = new(1f, 0.15f, 0.1f, 0.95f);

    void OnEnable()  => All.Add(this);
    void OnDisable() => All.Remove(this);

    public static Telegraph SpawnLine(Vector3 origin, Vector3 direction,
                                      float length, float warnTime, int damage)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Telegraph_Line";
        Object.Destroy(go.GetComponent<Collider>());

        go.transform.position   = origin + direction.normalized * (length * 0.5f);
        go.transform.rotation   = Quaternion.LookRotation(direction);
        go.transform.localScale = new Vector3(0.8f, 0.08f, length);

        var t = go.AddComponent<Telegraph>();
        t._warnTime      = warnTime;
        t._damage        = damage;
        t._isLine        = true;
        t._halfExtents   = new Vector3(0.4f, 0.5f, length * 0.5f);
        t._lineOrigin    = origin;
        t._lineDirection = direction;
        t.SetupMaterial();
        t.StartCoroutine(t.Run());
        return t;
    }

    public static Telegraph SpawnCircle(Vector3 center, float radius,
                                        float warnTime, int damage)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "Telegraph_Circle";
        Object.Destroy(go.GetComponent<Collider>());

        go.transform.position   = new Vector3(center.x, 0.05f, center.z);
        go.transform.localScale = new Vector3(radius * 2f, 0.04f, radius * 2f);

        var t = go.AddComponent<Telegraph>();
        t._warnTime = warnTime;
        t._damage   = damage;
        t._isLine   = false;
        t._radius   = radius;
        t.SetupMaterial();
        t.StartCoroutine(t.Run());
        return t;
    }

    void SetupMaterial()
    {
        _rend = GetComponent<Renderer>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = BaseColor;
        _rend.material = mat;
        transform.position = new Vector3(transform.position.x, 0.05f, transform.position.z);
    }

    IEnumerator Run()
    {
        yield return new WaitForSeconds(_warnTime * 0.8f);
        _rend.material.color = FlashColor;
        yield return new WaitForSeconds(_warnTime * 0.2f);
        DealDamage();
        Destroy(gameObject);
    }

    void DealDamage()
    {
        Collider[] hits;
        if (_isLine)
        {
            Vector3 center = _lineOrigin + _lineDirection.normalized * _halfExtents.z;
            hits = Physics.OverlapBox(center, _halfExtents,
                       Quaternion.LookRotation(_lineDirection));
        }
        else
        {
            hits = Physics.OverlapSphere(transform.position, _radius);
        }

        foreach (var col in hits)
        {
            if (!col.CompareTag("Player")) continue;
            var active = PlayerRuntime.Active;
            if (active != null && !active.IsDowned)
                active.Damage(_damage);
        }
    }
}
