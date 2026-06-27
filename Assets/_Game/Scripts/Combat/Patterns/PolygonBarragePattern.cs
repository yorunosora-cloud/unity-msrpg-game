using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "MSRPG/Boss/Patterns/PolygonBarrage", fileName = "PolygonBarragePattern")]
public class PolygonBarragePattern : BossPatternModule
{
    public int   projectileCount = 6;
    public float speed           = 6f;
    public int   damage          = 15;
    public float lifetime        = 4f;
    public float delayBetween    = 0.05f;

    public override IEnumerator Execute(BossContext ctx)
    {
        if (ctx?.BossTransform == null || projectileCount <= 0) yield break;
        Vector3 origin = ctx.BossTransform.position + Vector3.up;
        for (int i = 0; i < projectileCount; i++)
        {
            float angle = i * (360f / projectileCount);
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            BossProjectile.Spawn(origin, dir, speed, damage, lifetime);
            if (delayBetween > 0f)
                yield return new WaitForSeconds(delayBetween);
        }
    }
}
