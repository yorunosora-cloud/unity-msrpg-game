using UnityEngine;

public class BossProjectile : MonoBehaviour
{
    Vector3 _dir;
    float   _speed;
    int     _damage;
    float   _lifetime;
    float   _elapsed;

    public static BossProjectile Spawn(Vector3 pos, Vector3 dir,
                                       float speed, int damage, float lifetime)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "BossProjectile";
        go.transform.position   = pos;
        go.transform.localScale = Vector3.one * 0.35f;

        var col = go.GetComponent<SphereCollider>();
        col.isTrigger = true;

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(1f, 0.85f, 0.1f);
        go.GetComponent<Renderer>().material = mat;

        var p = go.AddComponent<BossProjectile>();
        p._dir      = dir.normalized;
        p._speed    = speed;
        p._damage   = damage;
        p._lifetime = lifetime;
        return p;
    }

    void Update()
    {
        transform.position += _dir * (_speed * Time.deltaTime);
        _elapsed += Time.deltaTime;
        if (_elapsed >= _lifetime)
            Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        var active = PlayerRuntime.Active;
        if (active != null && !active.IsDowned)
            active.Damage(_damage);
        Destroy(gameObject);
    }
}
