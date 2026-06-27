using NUnit.Framework;
using UnityEngine;

public class BossTests
{
    [Test]
    public void PhaseDef_DefaultPatternInterval_IsThree()
    {
        var phase = new PhaseDef();
        Assert.AreEqual(3f, phase.patternInterval, 0.001f);
    }

    [Test]
    public void PhaseDef_DefaultWeakPointExposeDuration_IsFive()
    {
        var phase = new PhaseDef();
        Assert.AreEqual(5f, phase.weakPointExposeDuration, 0.001f);
    }

    [Test]
    public void BossDef_DefaultMaxHp_Is1500()
    {
        var def = ScriptableObject.CreateInstance<BossDef>();
        Assert.AreEqual(1500, def.maxHp);
        Object.DestroyImmediate(def);
    }

    [Test]
    public void BossDef_DefaultDef_Is20()
    {
        var def = ScriptableObject.CreateInstance<BossDef>();
        Assert.AreEqual(20, def.def);
        Object.DestroyImmediate(def);
    }

    [Test]
    public void BossContext_StoresPassedValues()
    {
        var ctx = new BossContext(null, null, null);
        Assert.IsNull(ctx.Boss);
        Assert.IsNull(ctx.BossTransform);
        Assert.IsNull(ctx.Player);
    }

    [Test]
    public void BossWeakPoint_ExposedInstances_StartsEmpty()
    {
        Assert.AreEqual(0, BossWeakPoint.ExposedInstances.Count);
    }
}
