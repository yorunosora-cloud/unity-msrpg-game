using System.Collections;
using UnityEngine;

public abstract class BossPatternModule : ScriptableObject
{
    public abstract IEnumerator Execute(BossContext ctx);
}
