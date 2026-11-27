using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>23-G — 피해 기준·방어 무시·상황 비례·급소·확률 분기.</summary>
public class ComputationPathTests
{
    readonly List<GameObject> _spawned = new();

    [TearDown]
    public void TearDown()
    {
        SkillExecutor.Random01 = null;
        PlayerRuntime.Motion = default;
        PlayerRuntime.MaxJumpHeight = 1.5f;
        foreach (var g in _spawned) if (g != null) Object.DestroyImmediate(g);
        _spawned.Clear();
    }

    static SkillDef Skill(params SkillEffectEntry[] effects)
    {
        var s = ScriptableObject.CreateInstance<SkillDef>();
        s.id = "k"; s.nameKo = "k"; s.slot = 1; s.mpCost = 0; s.cooldown = 0f; s.effectKind = SkillEffectKind.Strike;
        s.shape = SkillShape.Cone; s.range = 8f; s.halfAngle = 40f; s.milestoneLevel = 3; s.effects = effects;
        return s;
    }

    static SkillEffectEntry Dmg(float v) => new SkillEffectEntry { type = SkillEffectType.Damage, target = TargetScope.EnemyArea, value = v };

    static CombatCharacter Make(SkillDef s, out OwnedCharacter owned, int spd = 30)
    {
        var def = ScriptableObject.CreateInstance<CharacterDef>();
        def.id = "cp" + Random.Range(0, 99999); def.nameKo = "c"; def.rarity = Rarity.N; def.continent = Continent.Physics;
        def.baseStats = new CharacterStats { hp = 500, atk = 100, def = 10, spd = spd, mp = 100 };
        def.skills = new[] { s };
        owned = new OwnedCharacter { id = def.id, level = 1 };
        var c = new CombatCharacter(def, owned);
        c.Unlock(0); c.TickStatus(0f, true);
        return c;
    }

    Enemy Spawn(float x = 0f, float z = 3f, float spd = 0f, int contact = 6)
    {
        var go = new GameObject("E"); go.transform.position = new Vector3(x, 0, z); _spawned.Add(go);
        var e = go.AddComponent<Enemy>();
        var f = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        typeof(Enemy).GetField("maxHp", f).SetValue(e, 10000000);
        typeof(Enemy).GetField("contactDamage", f).SetValue(e, contact);
        e.SetSpd(spd);
        return e;
    }

    Transform Origin() { var o = new GameObject("Caster"); _spawned.Add(o); return o.transform; }

    int Drop(Enemy e, System.Action act) { int b = e.CurrentHp; act(); return b - e.CurrentHp; }

    // ── 속도 차 비례(상대속도) ────────────────────────────────────────────

    [UnityTest]
    public IEnumerator SpeedDiff_AddsOnePercentPerPoint_UpToCap_AndScalesByMilestone()
    {
        var eff = Dmg(1f); eff.scaleBy = ScaleBy.SpeedDiff; eff.scaleCoeff = 0.01f; eff.scaleCap = 1f; eff.milestoneScaleCap = 1.3f;
        var s = Skill(eff);
        var c = Make(s, out var owned, spd: 30);
        var same = Spawn(0f, 3f, spd: 30); var slow = Spawn(0.3f, 3f, spd: 5); var crawl = Spawn(-0.3f, 3f, spd: -500); yield return null;
        var o = Origin();
        int dSame = same.CurrentHp, dSlow = slow.CurrentHp, dCrawl = crawl.CurrentHp;
        SkillExecutor.Execute(s, 0, c, o);
        dSame -= same.CurrentHp; dSlow -= slow.CurrentHp; dCrawl -= crawl.CurrentHp;
        Assert.Greater(dSlow, dSame, "속도 차 25 → +25%");
        Assert.AreEqual(dSame * 1.25f, dSlow, dSame * 0.12f + 3f);
        Assert.AreEqual(dSame * 2f, dCrawl, dSame * 0.12f + 3f, "상한 +100%");
    }

    // ── 이동 속도·높이 비례(운동에너지·중력 퍼텐셜) ─────────────────────────

    [UnityTest]
    public IEnumerator MoveSpeedRatio_ScalesWithCurrentSpeed()
    {
        var eff = Dmg(1f); eff.scaleBy = ScaleBy.MoveSpeedRatio; eff.scaleCap = 0.5f;
        var s = Skill(eff); var c = Make(s, out _);
        var e = Spawn(); yield return null;
        var o = Origin();
        PlayerRuntime.Motion = new CombatMotionState { speedRatio = 0f };
        int still = Drop(e, () => SkillExecutor.Execute(s, 0, c, o));
        PlayerRuntime.Motion = new CombatMotionState { speedRatio = 1f };
        int fast = Drop(e, () => SkillExecutor.Execute(s, 0, c, o));
        Assert.AreEqual(still * 1.5f, fast, still * 0.1f + 3f, "최대 속도에서 +50%");
    }

    [UnityTest]
    public IEnumerator Height_ScalesWithHeightOverMaxJump_AndClamps()
    {
        var eff = Dmg(1f); eff.scaleBy = ScaleBy.Height; eff.scaleCap = 1f;
        var s = Skill(eff); var c = Make(s, out _);
        var e = Spawn(); yield return null;
        var o = Origin();
        PlayerRuntime.MaxJumpHeight = 2f;
        PlayerRuntime.Motion = new CombatMotionState { height = 0f };
        int ground = Drop(e, () => SkillExecutor.Execute(s, 0, c, o));
        PlayerRuntime.Motion = new CombatMotionState { height = 1f };
        int half = Drop(e, () => SkillExecutor.Execute(s, 0, c, o));
        PlayerRuntime.Motion = new CombatMotionState { height = 9f };
        int top = Drop(e, () => SkillExecutor.Execute(s, 0, c, o));
        Assert.AreEqual(ground * 1.5f, half, ground * 0.1f + 3f, "절반 높이 +50%");
        Assert.AreEqual(ground * 2f, top, ground * 0.1f + 3f, "최대 높이 이상은 +100%에서 멈춘다");
    }

    // ── 피해 기준: 상대 공격력 ────────────────────────────────────────────

    [UnityTest]
    public IEnumerator TargetAtkBasis_UsesTheEnemysAttack_NotTheCasters()
    {
        var eff = Dmg(1f / 3f); eff.damageBasis = DamageBasis.TargetAtk;
        var s = Skill(eff); var c = Make(s, out _);
        var weak = Spawn(-0.3f, 3f, contact: 30); var strong = Spawn(0.3f, 3f, contact: 300); yield return null;
        int w = weak.CurrentHp, st = strong.CurrentHp;
        SkillExecutor.Execute(s, 0, c, Origin());
        w -= weak.CurrentHp; st -= strong.CurrentHp;
        Assert.Greater(st, w * 2f, "공격력이 10배인 적이 훨씬 아프다(상대 공격력 기반)");
    }

    // ── 방어 무시 + 급소 ─────────────────────────────────────────────────

    [UnityTest]
    public IEnumerator IgnoreDefense_DealsTheExactFixedDamage()
    {
        var eff = Dmg(1.5f); eff.ignoreDefense = true;
        var s = Skill(eff); var c = Make(s, out _);
        var e = Spawn(); yield return null;
        Assert.AreEqual(150, Drop(e, () => SkillExecutor.Execute(s, 0, c, Origin())), "공격력 100 × 1.5 = 150, 방어력·약점 무시");
    }

    [UnityTest]
    public IEnumerator Crit_AppliesOnlyWhenTheTargetIsNearTheAimCenter()
    {
        var eff = Dmg(1.5f); eff.ignoreDefense = true; eff.critMult = 2.5f; eff.critHalfAngle = 10f;
        var s = Skill(eff); var c = Make(s, out _);
        var center = Spawn(0f, 3f); var side = Spawn(1.5f, 3f); yield return null;   // 옆: 각도 ≈ 26.6°
        int cb = center.CurrentHp, sb = side.CurrentHp;
        SkillExecutor.Execute(s, 0, c, Origin());
        Assert.AreEqual(375, cb - center.CurrentHp, "정면 급소 ×2.5");
        Assert.AreEqual(150, sb - side.CurrentHp, "급소가 아니면 일반 고정 피해");
    }

    // ── 확률 분기(스트레인지 쿼크) ───────────────────────────────────────

    static SkillDef Strange()
    {
        var a = Dmg(0.5f); a.branchGroup = "g"; a.branchId = "a";
        var b1 = new SkillEffectEntry { type = SkillEffectType.Dot, target = TargetScope.EnemyArea, value = 0.15f, duration = 6f, branchGroup = "g", branchId = "b" };
        var b2 = new SkillEffectEntry { type = SkillEffectType.Slow, target = TargetScope.EnemyArea, value = 0.9f, duration = 6f, branchGroup = "g", branchId = "b" };
        return Skill(a, b1, b2);
    }

    [UnityTest]
    public IEnumerator Branch_LowRoll_PicksFirstBranchOnly()
    {
        var s = Strange(); var c = Make(s, out _);
        var e = Spawn(); yield return null;
        SkillExecutor.Random01 = () => 0.2f;
        int d = Drop(e, () => SkillExecutor.Execute(s, 0, c, Origin()));
        Assert.Greater(d, 0, "분기 a: 직접 피해");
        Assert.IsFalse(e.Status.Has("k:Dot:Atk")); Assert.IsFalse(e.Status.Has("k:Slow:MoveSpeed") || e.Status.Has("k:Slow:MoveSpeed"), "분기 b 효과 없음");
    }

    [UnityTest]
    public IEnumerator Branch_HighRoll_PicksSecondBranch_DotAndSlow_NoDirectHit()
    {
        var s = Strange(); var c = Make(s, out _);
        var e = Spawn(); yield return null;
        SkillExecutor.Random01 = () => 0.8f;
        int d = Drop(e, () => SkillExecutor.Execute(s, 0, c, Origin()));
        Assert.AreEqual(0, d, "직접 피해 없음");
        Assert.IsTrue(e.Status.Has("k:Dot:Atk"), "도트");
        Assert.Less(e.Status.Mult(StatKind.MoveSpeed), 1f, "둔화");
    }

    [UnityTest]
    public IEnumerator Branch_EntriesWithoutAGroup_AlwaysApply()
    {
        var always = Dmg(1f);
        var a = new SkillEffectEntry { type = SkillEffectType.Slow, target = TargetScope.EnemyArea, value = 0.5f, duration = 3f, branchGroup = "g", branchId = "a" };
        var b = new SkillEffectEntry { type = SkillEffectType.Slow, target = TargetScope.EnemyArea, value = 0.8f, duration = 3f, branchGroup = "g", branchId = "b" };
        var s = Skill(always, a, b); var c = Make(s, out _);
        var e = Spawn(); yield return null;
        SkillExecutor.Random01 = () => 0.9f;
        int d = Drop(e, () => SkillExecutor.Execute(s, 0, c, Origin()));
        Assert.Greater(d, 0);
        Assert.AreEqual(0.8f, e.Status.Mult(StatKind.MoveSpeed), 0.001f, "고른 분기(b)만 적용");
    }
}
