using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "MSRPG/Boss/Patterns/ParallelLasers", fileName = "ParallelLasersPattern")]
public class ParallelLasersPattern : BossPatternModule
{
    public float separation = 3.0f;
    public float length     = 12f;
    public float warnTime   = 0.8f;
    public int   damage     = 18;
    public float stagger    = 0.25f;

    public override IEnumerator Execute(BossContext ctx)
    {
        if (ctx?.Player == null) yield break;
        Vector3 origin   = ctx.BossTransform.position;
        Vector3 toPlayer = ctx.Player.position - origin;
        toPlayer.y = 0f;
        toPlayer.Normalize();
        Vector3 perp = Vector3.Cross(toPlayer, Vector3.up).normalized;

        Telegraph.SpawnLine(origin + perp * (separation * 0.5f), toPlayer, length, warnTime, damage);
        yield return new WaitForSeconds(stagger);
        Telegraph.SpawnLine(origin - perp * (separation * 0.5f), toPlayer, length, warnTime - stagger, damage);
        yield return new WaitForSeconds(warnTime - stagger + 0.1f);
    }
}
