using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>액티브 스킬 효과의 마일스톤 교체값(milestoneValue·milestoneDuration)이 전투에서 적용된다.</summary>
public class MilestoneEffectTests
{
    readonly List<GameObject> _spawned = new();

    [TearDown]
    public void TearDown()
    {
        foreach (var g in _spawned) if (g != null) Object.DestroyImmediate(g);
        _spawned.Clear();
    }

    static SkillDef Skill(params SkillEffectEntry[] effects)
    {
        var s = ScriptableObject.CreateInstance<SkillDef>();
        s.id = "ms"; s.nameKo = "ms"; s.slot = 1; s.mpCost = 0; s.cooldown = 0f; s.effectKind = SkillEffectKind.Strike;
        s.shape = SkillShape.Cone; s.range = 8f; s.halfAngle = 60f; s.milestoneLevel = 3; s.effects = effects;
        return s;
    }

    static CombatCharacter Make(SkillDef s, int level)
    {
        var def = ScriptableObject.CreateInstance<CharacterDef>();
        def.id = "me" + Random.Range(0, 99999); def.nameKo = "c"; def.rarity = Rarity.N; def.continent = Continent.Physics;
        def.baseStats = new CharacterStats { hp = 500, atk = 100, def = 10, spd = 5, mp = 100 };
        def.skills = new[] { s };
        var owned = new OwnedCharacter { id = def.id, level = 1 };
        var c = new CombatCharacter(def, owned);
        c.Unlock(0);
        owned.GetSkillProgress("ms").level = level;
        c.TickStatus(0f, true);
        return c;
    }

    Enemy Spawn()
    {
        var go = new GameObject("E"); go.transform.position = new Vector3(0, 0, 3); _spawned.Add(go);
        var e = go.AddComponent<Enemy>();
        typeof(Enemy).GetField("maxHp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(e, 10000000);
        return e;
    }

    Transform Origin() { var o = new GameObject("Caster"); _spawned.Add(o); return o.transform; }

    int DamageAtLevel(int level, float value, float msValue, out Enemy e)
    {
        var s = Skill(new SkillEffectEntry { type = SkillEffectType.Damage, target = TargetScope.EnemyArea, value = value, milestoneValue = msValue });
        var c = Make(s, level);
        e = Spawn();
        return 0;
    }

    [UnityTest]
    public IEnumerator DamageValue_IsReplacedAtTheMilestoneLevel()
    {
        var s = Skill(new SkillEffectEntry { type = SkillEffectType.Damage, target = TargetScope.EnemyArea, value = 1f, milestoneValue = 2f });
        var lv1 = Make(s, 1); var lv3 = Make(s, 3);
        var e = Spawn(); yield return null;
        var o = Origin();
        int b = e.CurrentHp; SkillExecutor.Execute(s, 0, lv1, o); int d1 = b - e.CurrentHp;
        b = e.CurrentHp; SkillExecutor.Execute(s, 0, lv3, o); int d3 = b - e.CurrentHp;
        Assert.AreEqual(d1 * 2f, d3, d1 * 0.1f + 3f, "Lv.3에서 값이 1.0 → 2.0으로 교체(레벨 곡선이 한 번 더 곱해지지 않는다)");
    }

    [UnityTest]
    public IEnumerator DamageValue_KeepsGrowingAfterTheMilestoneLevel()
    {
        var s = Skill(new SkillEffectEntry { type = SkillEffectType.Damage, target = TargetScope.EnemyArea, value = 1f, milestoneValue = 2f });
        var lv3 = Make(s, 3); var lv5 = Make(s, 5);
        var e = Spawn(); yield return null;
        var o = Origin();
        int b = e.CurrentHp; SkillExecutor.Execute(s, 0, lv3, o); int d3 = b - e.CurrentHp;
        b = e.CurrentHp; SkillExecutor.Execute(s, 0, lv5, o); int d5 = b - e.CurrentHp;
        Assert.Greater(d5, d3, "마일스톤 이후 레벨도 위력이 계속 오른다(+12%/레벨)");
        Assert.AreEqual(d3 * 1.24f, d5, d3 * 0.1f + 3f);
    }

    [UnityTest]
    public IEnumerator DurationAndValue_AreReplaced_ForStatusEffects()
    {
        var s = Skill(
            new SkillEffectEntry { type = SkillEffectType.Stun, target = TargetScope.EnemyArea, duration = 2f, milestoneDuration = 3f },
            new SkillEffectEntry { type = SkillEffectType.Slow, target = TargetScope.EnemyArea, value = 0.8f, duration = 4f, milestoneValue = 0.6f, milestoneDuration = 6f });
        var below = Make(s, 2); var at = Make(s, 3);
        var e1 = Spawn(); var e2 = Spawn(); yield return null;
        SkillExecutor.Execute(s, 0, below, Origin());
        SkillExecutor.Execute(s, 0, at, Origin());

        float stun2 = -1f, stunBoth = -1f;
        foreach (var st in e1.Status.Effects) if (st.Kind == StatusKind.Stun) stun2 = st.Duration;
        Assert.Greater(stun2, 0f);
        // 두 번째 시전(마일스톤)이 더 긴 값으로 갱신하므로 현재 지속시간은 3초에 가깝다
        Assert.AreEqual(3f, stun2, 0.05f, "Lv.3 기절 2초 → 3초");
        foreach (var st in e1.Status.Effects) if (st.Kind == StatusKind.StatMod && st.Stat == StatKind.MoveSpeed) stunBoth = st.Value;
        Assert.AreEqual(0.6f, stunBoth, 0.001f, "둔화 0.8 → 0.6(더 강한 값으로 갱신)");
        yield return null;
    }

    [UnityTest]
    public IEnumerator BelowTheMilestoneLevel_UsesTheBaseValues()
    {
        var s = Skill(new SkillEffectEntry { type = SkillEffectType.Stun, target = TargetScope.EnemyArea, duration = 2f, milestoneDuration = 3f });
        var c = Make(s, 2);
        var e = Spawn(); yield return null;
        SkillExecutor.Execute(s, 0, c, Origin());
        foreach (var st in e.Status.Effects) if (st.Kind == StatusKind.Stun) Assert.AreEqual(2f, st.Duration, 0.05f);
    }
}
