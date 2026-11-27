using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>23-B 단계 A — 이름 붙은 표식 부여·소멸·난이도 단축.</summary>
public class SynergyMarkTests
{
    readonly List<GameObject> _spawned = new();

    [TearDown]
    public void TearDown()
    {
        SynergyTuning.Reset();
        foreach (var g in _spawned) if (g != null) Object.DestroyImmediate(g);
        _spawned.Clear();
    }

    static SkillDef MarkSkill(string markId, float duration, float msDuration = 0f)
    {
        var s = ScriptableObject.CreateInstance<SkillDef>();
        s.id = "marker"; s.nameKo = "m"; s.slot = 1; s.mpCost = 0; s.cooldown = 0f; s.effectKind = SkillEffectKind.Mark;
        s.shape = SkillShape.Cone; s.range = 6f; s.halfAngle = 60f; s.milestoneLevel = 3;
        s.effects = new[]
        {
            new SkillEffectEntry { type = SkillEffectType.Damage, target = TargetScope.EnemyArea, value = 1f },
            new SkillEffectEntry { type = SkillEffectType.Mark, target = TargetScope.EnemyArea, markId = markId, duration = duration, milestoneDuration = msDuration },
        };
        return s;
    }

    static CombatCharacter Make(SkillDef skill, out OwnedCharacter owned)
    {
        var def = ScriptableObject.CreateInstance<CharacterDef>();
        def.id = "sm" + Random.Range(0, 99999); def.nameKo = "c"; def.rarity = Rarity.N; def.continent = Continent.Physics;
        def.baseStats = new CharacterStats { hp = 500, atk = 100, def = 10, spd = 5, mp = 100 };
        def.skills = new[] { skill };
        owned = new OwnedCharacter { id = def.id, level = 1 };
        var c = new CombatCharacter(def, owned);
        c.Unlock(0); c.TickStatus(0f, true);
        return c;
    }

    Enemy Spawn(float z = 2f)
    {
        var go = new GameObject("E"); go.transform.position = new Vector3(0, 0, z); _spawned.Add(go);
        var e = go.AddComponent<Enemy>();
        typeof(Enemy).GetField("maxHp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(e, 1000000);
        return e;
    }

    Transform Origin() { var o = new GameObject("Caster"); _spawned.Add(o); return o.transform; }

    [UnityTest]
    public IEnumerator MarkEffect_WithMarkId_AddsANamedMarkWithDuration()
    {
        var s = MarkSkill("vektor_disp", 8f);
        var c = Make(s, out _);
        var e = Spawn(); yield return null;
        SkillExecutor.Execute(s, 0, c, Origin());
        Assert.IsTrue(e.HasMark("vektor_disp"));
        Assert.AreEqual(8f, e.MarkRemaining("vektor_disp"), 0.01f);
    }

    [UnityTest]
    public IEnumerator MarkEffect_UsesMilestoneDuration_AtLevelThree()
    {
        var s = MarkSkill("impulsa_j", 6f, 8f);
        var c = Make(s, out var owned);
        owned.GetSkillProgress("marker").level = 3;
        var e = Spawn(); yield return null;
        SkillExecutor.Execute(s, 0, c, Origin());
        Assert.AreEqual(8f, e.MarkRemaining("impulsa_j"), 0.01f);
    }

    [UnityTest]
    public IEnumerator MarkDuration_IsScaledByTheDifficultyMultiplier()
    {
        SynergyTuning.MarkDurationMult = 0.6f;
        var s = MarkSkill("m", 8f);
        var c = Make(s, out _);
        var e = Spawn(); yield return null;
        SkillExecutor.Execute(s, 0, c, Origin());
        Assert.AreEqual(4.8f, e.MarkRemaining("m"), 0.01f, "보스전 0.6배 → 4.8초");
    }

    [UnityTest]
    public IEnumerator MarkWithoutId_KeepsTheElementMarkBehaviour_AndAddsNoNamedMark()
    {
        var s = MarkSkill(null, 8f);
        var c = Make(s, out _);
        var e = Spawn(); yield return null;
        SkillExecutor.Execute(s, 0, c, Origin());
        Assert.IsFalse(e.HasMark(""), "이름 없는 표식은 사전에 들어가지 않는다");
    }

    [UnityTest]
    public IEnumerator RemoveMark_ClearsOnlyThatMark()
    {
        var e = Spawn(); yield return null;
        e.AddMark("a", 5f); e.AddMark("b", 5f);
        ((ISkillTarget)e).RemoveMark("a");
        Assert.IsFalse(e.HasMark("a")); Assert.IsTrue(e.HasMark("b"));
        Assert.AreEqual(0f, e.MarkRemaining("a"));
    }

    [Test]
    public void SynergyTuning_ResetRestoresDefault()
    {
        SynergyTuning.MarkDurationMult = 0.4f;
        SynergyTuning.Reset();
        Assert.AreEqual(1f, SynergyTuning.MarkDurationMult);
    }
}
