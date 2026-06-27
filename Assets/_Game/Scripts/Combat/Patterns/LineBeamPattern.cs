using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "MSRPG/Boss/Patterns/LineBeam", fileName = "LineBeamPattern")]
public class LineBeamPattern : BossPatternModule
{
    public float length   = 14f;
    public float warnTime = 1.0f;
    public int   damage   = 20;

    public override IEnumerator Execute(BossContext ctx)
    {
        if (ctx?.Player == null) yield break;
        Vector3 origin    = ctx.BossTransform.position;
        Vector3 direction = ctx.Player.position - origin;
        direction.y = 0f;
        direction.Normalize();
        Telegraph.SpawnLine(origin, direction, length, warnTime, damage);
        yield return new WaitForSeconds(warnTime + 0.1f);
    }
}
