using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "MSRPG/Boss/Patterns/CircleZone", fileName = "CircleZonePattern")]
public class CircleZonePattern : BossPatternModule
{
    public float radius   = 3.5f;
    public float warnTime = 1.2f;
    public int   damage   = 25;

    public override IEnumerator Execute(BossContext ctx)
    {
        if (ctx?.Player == null) yield break;
        Vector3 center = ctx.Player.position;
        center.y = 0f;
        Telegraph.SpawnCircle(center, radius, warnTime, damage);
        yield return new WaitForSeconds(warnTime + 0.1f);
    }
}
