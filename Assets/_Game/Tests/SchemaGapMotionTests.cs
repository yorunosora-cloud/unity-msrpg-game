using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>설계도 18 §8-5 스키마 갭 — #1 돌진 / #2 띄우기 / #4 단파·장파 자동 전환.</summary>
public class SchemaGapMotionTests
{
    // ════════════════════════════════════════════════════════════════════════
    // #1 돌진 — 순수 이동 계산
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Dash_MovesExactDistanceOverTime_AndEnds()
    {
        var d = new DashMotion();
        Assert.IsFalse(d.Active);
        d.Begin(Vector3.forward, 3f, 0.15f);
        Assert.IsTrue(d.Active);

        Vector3 total = Vector3.zero;
        for (int i = 0; i < 3; i++) total += d.Step(0.05f);
        Assert.AreEqual(3f, total.z, 0.001f, "0.15초 동안 정확히 3m");
        Assert.AreEqual(0f, total.x, 0.0001f);
        Assert.IsFalse(d.Active, "시간이 끝나면 종료");
        Assert.AreEqual(Vector3.zero, d.Step(0.05f), "종료 후엔 이동 없음");
    }

    [Test]
    public void Dash_LargeDt_ClampsToRemainingDistance()
    {
        var d = new DashMotion();
        d.Begin(Vector3.right, 3f, 0.15f);
        Vector3 step = d.Step(1f);
        Assert.AreEqual(3f, step.x, 0.001f, "한 번에 끝나도 3m를 넘지 않는다");
        Assert.IsFalse(d.Active);
    }

    [Test]
    public void Dash_ZeroDistanceOrTime_DoesNotStart()
    {
        var d = new DashMotion();
        d.Begin(Vector3.forward, 0f, 0.15f);
        Assert.IsFalse(d.Active);
        d.Begin(Vector3.forward, 3f, 0f);
        Assert.IsFalse(d.Active);
    }

    [Test]
    public void Dash_FlattensDirectionToHorizontal()
    {
        var d = new DashMotion();
        d.Begin(new Vector3(0f, 5f, 1f), 2f, 0.1f);
        var step = d.Step(1f);
        Assert.AreEqual(0f, step.y, 0.0001f);
        Assert.AreEqual(2f, step.z, 0.001f);
    }

    [Test]
    public void Dash_NegativeDistance_MovesBackward()
    {
        var d = new DashMotion();
        d.Begin(Vector3.forward, -3f, 0.2f);
        Assert.IsTrue(d.Active);
        var step = d.Step(1f);
        Assert.AreEqual(-3f, step.z, 0.001f, "음수 거리 = 후방 반동");
    }

    // ════════════════════════════════════════════════════════════════════════
    // Q 기본기 마일스톤 (사거리·각도·위력)
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void BasicAttack_AppliesMilestoneRangeAngleAndDamage_AtLevelThree()
    {
        var q = ScriptableObject.CreateInstance<SkillDef>();
        q.id = "q"; q.slot = 0; q.range = 2f; q.halfAngle = 40f; q.damageMultiplier = 1.1f; q.cooldown = 0.7f;
        q.milestoneLevel = 3; q.milestoneRangeBonus = 1.5f; q.milestoneHalfAngleBonus = 10f; q.milestoneDamageBonus = 0.2f;
        var def = ScriptableObject.CreateInstance<CharacterDef>();
        def.id = "bq"; def.nameKo = "c"; def.rarity = Rarity.N;
        def.baseStats = new CharacterStats { hp = 100, atk = 10, def = 1, spd = 1, mp = 10 };
        def.skills = new[] { q };
        var owned = new OwnedCharacter { id = "bq", level = 1 };
        var c = new CombatCharacter(def, owned);
        c.Unlock(0);

        var s1 = BasicAttack.Resolve(c, 9f, 9f, 9f);
        Assert.AreEqual(2f, s1.range, 0.0001f); Assert.AreEqual(1.1f, s1.damageMultiplier, 0.0001f);

        owned.GetSkillProgress("q").level = 3;
        var s3 = BasicAttack.Resolve(c, 9f, 9f, 9f);
        Assert.AreEqual(3.5f, s3.range, 0.0001f);
        Assert.AreEqual(50f, s3.halfAngle, 0.0001f);
        Assert.AreEqual(1.3f, s3.damageMultiplier, 0.0001f);
    }

    [Test]
    public void SkillScaling_MilestoneDamageBonus_AddsToDamageMultiplier()
    {
        var s = ScriptableObject.CreateInstance<SkillDef>();
        s.damageMultiplier = 1.2f; s.milestoneLevel = 3; s.milestoneDamageBonus = 0.3f;
        Assert.AreEqual(1.2f, SkillScaling.Compute(s, 1).damageMultiplier, 0.0001f);
        Assert.AreEqual(1.2f * (1f + 2 * SkillScaling.PowerPerLevel) + 0.3f, SkillScaling.Compute(s, 3).damageMultiplier, 0.0001f);
    }

    // ════════════════════════════════════════════════════════════════════════
    // #2 띄우기 — 곡선
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void LaunchCurve_RisesHoldsAndLands()
    {
        Assert.AreEqual(0f, LaunchCurve.Offset(1.5f, 2f, 0f), 0.0001f);
        Assert.AreEqual(1.5f, LaunchCurve.Offset(1.5f, 2f, 1f), 0.0001f, "중간엔 최대 높이 유지");
        Assert.AreEqual(0f, LaunchCurve.Offset(1.5f, 2f, 2f), 0.0001f);
        Assert.AreEqual(0f, LaunchCurve.Offset(1.5f, 2f, 3f), 0.0001f, "끝난 뒤에는 0");
        float up = LaunchCurve.Offset(1.5f, 2f, 0.1f);
        Assert.That(up, Is.GreaterThan(0f).And.LessThan(1.5f), "상승 중");
        float down = LaunchCurve.Offset(1.5f, 2f, 1.9f);
        Assert.That(down, Is.GreaterThan(0f).And.LessThan(1.5f), "하강 중");
    }

    [Test]
    public void LaunchCurve_InvalidInputs_AreZero()
    {
        Assert.AreEqual(0f, LaunchCurve.Offset(0f, 2f, 1f));
        Assert.AreEqual(0f, LaunchCurve.Offset(1.5f, 0f, 0f));
        Assert.AreEqual(0f, LaunchCurve.Offset(1.5f, 2f, -1f));
    }

    // ════════════════════════════════════════════════════════════════════════
    // 씬 객체 테스트 (적·실행기)
    // ════════════════════════════════════════════════════════════════════════

    readonly List<GameObject> _spawned = new();

    [TearDown]
    public void TearDown()
    {
        foreach (var g in _spawned) if (g != null) Object.DestroyImmediate(g);
        _spawned.Clear();
    }

    Enemy SpawnEnemy(Vector3 pos, int hp = 100000)
    {
        var go = new GameObject("E"); go.transform.position = pos;
        var e = go.AddComponent<Enemy>();
        typeof(Enemy).GetField("maxHp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(e, hp);
        _spawned.Add(go);
        return e;
    }

    static CombatCharacter MakeCaster(SkillDef skill)
    {
        var def = ScriptableObject.CreateInstance<CharacterDef>();
        def.id = "m" + Random.Range(0, 99999); def.nameKo = "c"; def.rarity = Rarity.N; def.continent = Continent.Physics;
        def.baseStats = new CharacterStats { hp = 500, atk = 100, def = 10, spd = 5, mp = 100 };
        def.skills = new[] { skill };
        var c = new CombatCharacter(def, new OwnedCharacter { id = def.id, level = 1 });
        c.Unlock(0); c.TickStatus(0f, true);
        return c;
    }

    [UnityTest]
    public IEnumerator Launch_StunsAndLifts_ImmuneEnemyIsUnaffected()
    {
        var skill = ScriptableObject.CreateInstance<SkillDef>();
        skill.id = "buoy"; skill.shape = SkillShape.Cone; skill.range = 4f; skill.halfAngle = 60f;
        skill.effects = new[] { new SkillEffectEntry { type = SkillEffectType.Launch, target = TargetScope.EnemyArea, value = 1.5f, duration = 2f } };
        var caster = MakeCaster(skill);
        var normal = SpawnEnemy(new Vector3(0, 0, 2));
        var immune = SpawnEnemy(new Vector3(1, 0, 2));
        typeof(Enemy).GetField("ccImmune", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(immune, true);
        yield return null;

        var origin = new GameObject("Caster"); _spawned.Add(origin);
        SkillExecutor.Execute(skill, 0, caster, origin.transform);

        Assert.IsTrue(normal.Status.IsStunned, "띄우면 행동불능");
        Assert.AreEqual(1.5f, normal.LaunchHeight, 0.0001f);
        Assert.IsFalse(immune.Status.IsStunned);
        Assert.AreEqual(0f, immune.LaunchHeight, 0.0001f, "CC 면역은 뜨지 않는다");
    }

    // ════════════════════════════════════════════════════════════════════════
    // #4 단파/장파 자동 전환
    // ════════════════════════════════════════════════════════════════════════

    static SkillDef Radiation()
    {
        var s = ScriptableObject.CreateInstance<SkillDef>();
        s.id = "rad"; s.effectKind = SkillEffectKind.Aoe; s.shape = SkillShape.Cone;
        s.range = 5f; s.halfAngle = 70f; s.damageMultiplier = 1f;
        s.autoSwitchDistance = 3f; s.altEffectKind = SkillEffectKind.Strike;
        s.altRange = 3f; s.altHalfAngle = 40f; s.altDamageMultiplier = 1.6f;
        return s;
    }

    [Test]
    public void AutoSwitch_UsesAltOnlyWhenNearestWithinThreshold()
    {
        var s = Radiation();
        Assert.IsTrue(SkillExecutor.UseAlt(s, 2.9f));
        Assert.IsTrue(SkillExecutor.UseAlt(s, 3f), "경계 포함");
        Assert.IsFalse(SkillExecutor.UseAlt(s, 3.1f));
        Assert.IsFalse(SkillExecutor.UseAlt(s, float.PositiveInfinity), "적이 없으면 장파");
        s.autoSwitchDistance = 0f;
        Assert.IsFalse(SkillExecutor.UseAlt(s, 1f), "자동 전환 설정이 없으면 항상 기본");
    }

    [UnityTest]
    public IEnumerator AutoSwitch_NearEnemy_ShortWave_HitsOnlyNearest_WithBonus()
    {
        var skill = Radiation();
        var caster = MakeCaster(skill);
        var near  = SpawnEnemy(new Vector3(0, 0, 2));
        var far   = SpawnEnemy(new Vector3(0, 0, 4));
        yield return null;
        int nearBefore = near.CurrentHp, farBefore = far.CurrentHp;

        var origin = new GameObject("Caster"); _spawned.Add(origin);
        SkillExecutor.Execute(skill, 0, caster, origin.transform);

        int nearDmg = nearBefore - near.CurrentHp;
        Assert.Greater(nearDmg, 0, "가까운 적이 단파에 맞는다");
        Assert.AreEqual(farBefore, far.CurrentHp, "단파는 단일 대상 — 먼 적은 맞지 않는다");

        // 같은 조건에서 장파 피해와 비교: 가까운 적만 있는 장파(강제 해제)로 기준 피해 측정
        var longSkill = Radiation(); longSkill.autoSwitchDistance = 0f;
        var base1 = SpawnEnemy(new Vector3(5, 0, 2));
        yield return null;
        var origin2 = new GameObject("Caster2"); origin2.transform.position = new Vector3(5, 0, 0); _spawned.Add(origin2);
        int b = base1.CurrentHp;
        SkillExecutor.Execute(longSkill, 0, caster, origin2.transform);
        int longDmg = b - base1.CurrentHp;
        Assert.AreEqual(longDmg * 1.6f, nearDmg, longDmg * 0.05f + 1f, "단파는 ×1.6");
    }

    [UnityTest]
    public IEnumerator AutoSwitch_OnlyFarEnemy_LongWave_Hits()
    {
        var skill = Radiation();
        var caster = MakeCaster(skill);
        var far = SpawnEnemy(new Vector3(0, 0, 4));
        yield return null;
        int before = far.CurrentHp;
        var origin = new GameObject("Caster"); _spawned.Add(origin);
        SkillExecutor.Execute(skill, 0, caster, origin.transform);
        Assert.Less(far.CurrentHp, before, "가까운 적이 없으면 장파가 먼 적을 맞힌다");
    }
}
