using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>23-A 단계 B — 카운터를 CombatCharacter에 연결하고 스킬 효과가 읽고 소모한다.</summary>
public class CounterIntegrationTests
{
    static CounterSpec C(string id, CounterTrigger gain, int max = 5, CounterDecay decay = CounterDecay.None, float sec = 0f,
                         CounterTrigger resetOn = CounterTrigger.None, int msMax = 0, string msSkill = null)
        => new CounterSpec { id = id, maxStacks = max, milestoneMaxStacks = msMax, decay = decay, decaySeconds = sec,
                             gainOn = gain, resetOn = resetOn, milestoneSkillId = msSkill };

    static SkillDef Skill(string id, int slot = 1, int mp = 0)
    {
        var s = ScriptableObject.CreateInstance<SkillDef>();
        s.id = id; s.nameKo = id; s.slot = slot; s.mpCost = mp; s.cooldown = 0f; s.effectKind = SkillEffectKind.Strike;
        return s;
    }

    static CombatCharacter Make(out OwnedCharacter owned, CounterSpec[] counters, params SkillDef[] skills)
    {
        var def = ScriptableObject.CreateInstance<CharacterDef>();
        def.id = "ci" + Random.Range(0, 99999); def.nameKo = "c"; def.rarity = Rarity.N; def.continent = Continent.Physics;
        def.baseStats = new CharacterStats { hp = 500, atk = 100, def = 10, spd = 5, mp = 100 };
        def.skills = skills; def.counters = counters;
        owned = new OwnedCharacter { id = def.id, level = 1 };
        var c = new CombatCharacter(def, owned);
        for (int i = 0; i < skills.Length; i++) c.Unlock(i);
        c.TickStatus(0f, true);
        return c;
    }

    static HitInfo Hit(int action) => new HitInfo { actionId = action, damage = 10 };

    // ── 구성 ──────────────────────────────────────────────────────────────

    [Test]
    public void Counters_AreBuiltFromDefinition_UnknownIsNull()
    {
        var c = Make(out _, new[] { C("a", CounterTrigger.OnHit) });
        Assert.IsNotNull(c.Counter("a"));
        Assert.IsNull(c.Counter("zzz"));
        Assert.AreEqual(0, c.CounterValue("zzz"));
    }

    // ── 증가 이벤트 ───────────────────────────────────────────────────────

    [Test]
    public void OnHit_GainsOncePerAction_NotPerTarget()
    {
        var c = Make(out _, new[] { C("a", CounterTrigger.OnHit) });
        c.NotifyHits(new List<HitInfo> { Hit(1), Hit(1), Hit(1) }, false);
        Assert.AreEqual(1, c.CounterValue("a"));
        c.NotifyHits(new List<HitInfo> { Hit(2) }, true);
        Assert.AreEqual(2, c.CounterValue("a"));
    }

    [Test]
    public void OnBasicAttack_OnlyForBasic_OnAction_ForBoth()
    {
        var c = Make(out _, new[] { C("b", CounterTrigger.OnBasicAttack), C("x", CounterTrigger.OnAction) });
        c.NotifyHits(new List<HitInfo> { Hit(1) }, false);
        Assert.AreEqual((0, 1), (c.CounterValue("b"), c.CounterValue("x")));
        c.NotifyHits(new List<HitInfo> { Hit(2) }, true);
        Assert.AreEqual((1, 2), (c.CounterValue("b"), c.CounterValue("x")));
    }

    [Test]
    public void OnHitTaken_Gains_AndResetOnClearsOtherCounters()
    {
        var c = Make(out _, new[] { C("p", CounterTrigger.OnHitTaken), C("combo", CounterTrigger.OnHit, resetOn: CounterTrigger.OnHitTaken) });
        c.NotifyHits(new List<HitInfo> { Hit(1) }, false); c.NotifyHits(new List<HitInfo> { Hit(2) }, false);
        Assert.AreEqual(2, c.CounterValue("combo"));
        c.TakeHit(10);
        Assert.AreEqual(1, c.CounterValue("p"));
        Assert.AreEqual(0, c.CounterValue("combo"), "피격 시 초기화");
    }

    [Test]
    public void OnSkillCast_GainsWhenCastSucceeds()
    {
        var c = Make(out _, new[] { C("heat", CounterTrigger.OnSkillCast | CounterTrigger.OnHitTaken) }, Skill("s"));
        Assert.IsNotNull(c.CastSkill(0));
        c.TakeHit(5);
        Assert.AreEqual(2, c.CounterValue("heat"), "시전 +1, 피격 +1");
    }

    [Test]
    public void FailedCast_DoesNotGain()
    {
        var c = Make(out _, new[] { C("heat", CounterTrigger.OnSkillCast) }, Skill("s", mp: 9999));
        Assert.IsNull(c.CastSkill(0));
        Assert.AreEqual(0, c.CounterValue("heat"));
    }

    // ── 이동·시간 ─────────────────────────────────────────────────────────

    [Test]
    public void MovingSecond_GainsWhileMoving_ActiveOnly()
    {
        var c = Make(out _, new[] { C("e", CounterTrigger.MovingSecond, 10) });
        var move = new CombatMotionState { isMoving = true };
        c.TickForm(1f, move); c.TickForm(1f, move);
        Assert.AreEqual(2, c.CounterValue("e"));
        c.TickForm(5f, new CombatMotionState { isMoving = false });
        Assert.AreEqual(2, c.CounterValue("e"), "정지하면 진행만 끊기고 값은 유지(감소 없음)");
    }

    [Test]
    public void CounterGainStat_DoublesMovingGain()
    {
        var c = Make(out _, new[] { C("e", CounterTrigger.MovingSecond, 10) });
        c.ApplyStatus(new StatusEffect { Id = "sc", Kind = StatusKind.StatMod, Stat = StatKind.CounterGain, Value = 2f, Duration = 9f, IsBeneficial = true }, true);
        c.TickForm(1f, new CombatMotionState { isMoving = true });
        Assert.AreEqual(2, c.CounterValue("e"));
    }

    [Test]
    public void Decay_FollowsStatusTick_OnlyWhenActive()
    {
        var c = Make(out _, new[] { C("p", CounterTrigger.OnHitTaken, 5, CounterDecay.DecayOneEvery, 3f) });
        c.TakeHit(1); c.TakeHit(1);
        c.TickStatus(3.1f, false);
        Assert.AreEqual(2, c.CounterValue("p"), "대기 중에는 감소하지 않는다");
        c.TickStatus(3.1f, true);
        Assert.AreEqual(1, c.CounterValue("p"));
    }

    // ── 마일스톤·초기화 ──────────────────────────────────────────────────

    [Test]
    public void MilestoneMax_FollowsOwnerSkillLevel()
    {
        var s = Skill("owner"); s.milestoneLevel = 3;
        var c = Make(out var owned, new[] { C("a", CounterTrigger.OnHit, 5, msMax: 7, msSkill: "owner") }, s);
        Assert.AreEqual(5, c.Counter("a").Max);
        owned.GetSkillProgress("owner").level = 3;
        c.TickStatus(0f, true);
        Assert.AreEqual(7, c.Counter("a").Max);
    }

    [Test]
    public void ResetCombatState_AndRestoreFull_ClearCounters()
    {
        var c = Make(out _, new[] { C("a", CounterTrigger.OnHit) });
        c.NotifyHits(new List<HitInfo> { Hit(1) }, false);
        c.ResetCombatState();
        Assert.AreEqual(0, c.CounterValue("a"));
        c.NotifyHits(new List<HitInfo> { Hit(2) }, false);
        c.RestoreFull();
        Assert.AreEqual(0, c.CounterValue("a"));
    }

    // ── 카운터 → 능력치 수정자 ───────────────────────────────────────────

    [Test]
    public void CounterModifier_ScalesStatByStacks()
    {
        var pas = ScriptableObject.CreateInstance<SkillDef>();
        pas.id = "heatpassive"; pas.skillType = SkillKind.Passive; pas.slot = 5;
        pas.passive = new PassiveSpec { modifiers = new[] { new PassiveModifierSpec { source = ModifierSource.Counter, stat = StatKind.SkillPower, value = 0.04f, counterId = "heat" } } };
        var c = Make(out _, new[] { C("heat", CounterTrigger.OnHitTaken) }, pas);
        Assert.AreEqual(1f, c.StatMult(StatKind.SkillPower), 0.0001f);
        c.TakeHit(1); c.TakeHit(1); c.TakeHit(1);
        Assert.AreEqual(1.12f, c.StatMult(StatKind.SkillPower), 0.0001f, "열 3스택 × 4%");
    }

    // ── 최근 사용한 서로 다른 스킬 ───────────────────────────────────────

    [Test]
    public void RecentDistinctSkills_CountsCastsWithinWindow()
    {
        float now = 0f;
        CombatCharacter.Clock = () => now;
        try
        {
            var c = Make(out _, null, Skill("a", 1), Skill("b", 2));
            c.CastSkill(0); now = 1f; c.CastSkill(1); now = 2f; c.CastSkill(0);
            Assert.AreEqual(2, c.RecentDistinctSkills(5f));
            now = 6.5f;
            Assert.AreEqual(1, c.RecentDistinctSkills(5f), "a는 2초에 갱신 → 창 안, b(1초)는 만료");
        }
        finally { CombatCharacter.Clock = null; }
    }

    // ── 스킬 효과가 카운터를 읽고 소모 ───────────────────────────────────

    readonly List<GameObject> _spawned = new();

    [TearDown]
    public void TearDown()
    {
        foreach (var g in _spawned) if (g != null) Object.DestroyImmediate(g);
        _spawned.Clear();
    }

    Enemy SpawnEnemy(float z = 2f)
    {
        var go = new GameObject("E"); go.transform.position = new Vector3(0, 0, z); _spawned.Add(go);
        var e = go.AddComponent<Enemy>();
        typeof(Enemy).GetField("maxHp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(e, 1000000);
        return e;
    }

    static SkillDef Joule()
    {
        var s = Skill("joule", 2);
        s.shape = SkillShape.Cone; s.range = 6f; s.halfAngle = 60f; s.milestoneLevel = 3;
        s.effects = new[] { new SkillEffectEntry { type = SkillEffectType.Damage, target = TargetScope.EnemyArea, value = 1f,
                                                   counterId = "free", counterMode = CounterMode.Quadratic, counterCoeff = 0.1f, milestoneCounterCoeff = 0.12f, counterConsume = true } };
        return s;
    }

    [UnityTest]
    public IEnumerator Executor_QuadraticCounter_FiveStacksIsThreePointFiveTimes_AndConsumes()
    {
        yield return null;
        int d0 = 0, d5 = 0;
        { var skill = Joule(); var c = Make(out _, new[] { C("free", CounterTrigger.MovingSecond) }, skill); var e = SpawnEnemy(); yield return null;
          var o = new GameObject("Caster"); _spawned.Add(o); int b = e.CurrentHp; SkillExecutor.Execute(skill, 0, c, o.transform); d0 = b - e.CurrentHp; }
        { var skill = Joule(); var c = Make(out _, new[] { C("free", CounterTrigger.MovingSecond) }, skill); c.Counter("free").Add(5); var e = SpawnEnemy(); yield return null;
          var o = new GameObject("Caster"); _spawned.Add(o); int b = e.CurrentHp; SkillExecutor.Execute(skill, 0, c, o.transform); d5 = b - e.CurrentHp;
          Assert.AreEqual(0, c.CounterValue("free"), "스택 전부 소모"); }
        Assert.Greater(d0, 0);
        // 방어력 차감 때문에 정확히 3.5배는 아님 — 공식 자체는 CounterFormula 테스트가 검증. 여기서는 3.5배 근처이고 1배보다 훨씬 크다는 것만 본다.
        Assert.That(d5, Is.GreaterThan(d0 * 3.3f).And.LessThan(d0 * 3.8f));
    }

    [UnityTest]
    public IEnumerator Executor_RequireCounterFull_StunsOnlyAtMax()
    {
        var skill = Skill("breakdown", 2);
        skill.shape = SkillShape.Cone; skill.range = 6f; skill.halfAngle = 60f;
        skill.effects = new[]
        {
            new SkillEffectEntry { type = SkillEffectType.Damage, target = TargetScope.EnemyArea, value = 0.7f, counterId = "pol", counterMode = CounterMode.Linear, counterCoeff = 0.25f, counterConsume = true },
            new SkillEffectEntry { type = SkillEffectType.Stun, target = TargetScope.EnemyArea, duration = 2f, requireCounterFull = true, counterId = "pol" },
        };
        var c = Make(out _, new[] { C("pol", CounterTrigger.OnHitTaken) }, skill);
        var e = SpawnEnemy(); yield return null;
        var o = new GameObject("Caster"); _spawned.Add(o);

        c.Counter("pol").Add(4);
        SkillExecutor.Execute(skill, 0, c, o.transform);
        Assert.IsFalse(e.Status.IsStunned, "4스택은 스턴 없음");

        c.Counter("pol").Add(5);
        SkillExecutor.Execute(skill, 0, c, o.transform);
        Assert.IsTrue(e.Status.IsStunned, "5스택(가득)에서만 스턴");
        Assert.AreEqual(0, c.CounterValue("pol"));
    }

    [UnityTest]
    public IEnumerator Executor_RecentDistinctCounter_UsesReservedId()
    {
        var a = Skill("a", 1); var b = Skill("b", 2);
        b.shape = SkillShape.Cone; b.range = 6f; b.halfAngle = 60f;
        b.effects = new[] { new SkillEffectEntry { type = SkillEffectType.Damage, target = TargetScope.EnemyArea, value = 1f, counterId = "@recent", counterMode = CounterMode.Linear, counterCoeff = 0.15f, counterWindow = 5f } };
        var c = Make(out _, null, a, b);
        var e = SpawnEnemy(); yield return null;
        var o = new GameObject("Caster"); _spawned.Add(o);
        c.CastSkill(0); c.CastSkill(1);   // 서로 다른 2종 사용
        int before = e.CurrentHp;
        SkillExecutor.Execute(b, 1, c, o.transform);
        int withTwo = before - e.CurrentHp;
        Assert.Greater(withTwo, 0);
        Assert.AreEqual(2, c.RecentDistinctSkills(5f));
    }

    // ── 적 쪽 상태 스택(교류 → 직류) ──────────────────────────────────────

    static SkillDef DirectCurrent()
    {
        var s = Skill("dc", 3);
        s.shape = SkillShape.Cone; s.range = 6f; s.halfAngle = 60f;
        s.effects = new[]
        {
            new SkillEffectEntry { type = SkillEffectType.Damage, target = TargetScope.EnemyArea, value = 1.8f, counterId = "target:ac", counterMode = CounterMode.Linear, counterCoeff = 0.1f, counterConsume = true },
            new SkillEffectEntry { type = SkillEffectType.Damage, target = TargetScope.EnemyArea, value = 0.8f, counterId = "target:ac", requireCounterFull = true },
        };
        return s;
    }

    static void StackAc(Enemy e, int n)
    {
        for (int i = 0; i < n; i++)
            e.ApplyStatus(new StatusEffect { Id = "ac", Kind = StatusKind.Dot, Value = 0f, Duration = 30f, MaxStacks = 5 });
    }

    IEnumerator DcDamage(int stacks, System.Action<int, bool> result)
    {
        var skill = DirectCurrent();
        var c = Make(out _, null, skill);
        var e = SpawnEnemy(); yield return null;
        StackAc(e, stacks);
        var o = new GameObject("Caster"); _spawned.Add(o);
        int before = e.CurrentHp;
        SkillExecutor.Execute(skill, 0, c, o.transform);
        result(before - e.CurrentHp, e.Status.Has("ac"));
    }

    [UnityTest]
    public IEnumerator TargetCounter_ReadsEnemyStatusStacks_AddsDamage_AndConsumesThem()
    {
        int d0 = 0, d3 = 0; bool has0 = false, has3 = true;
        yield return DcDamage(0, (d, h) => { d0 = d; has0 = h; });
        yield return DcDamage(3, (d, h) => { d3 = d; has3 = h; });
        Assert.Greater(d0, 0);
        Assert.Greater(d3, d0, "AC 3스택이면 더 아프다(+0.3 배율)");
        Assert.IsFalse(has3, "직류가 AC 스택을 소모");
    }

    [UnityTest]
    public IEnumerator TargetCounter_FullStacksTriggerTheExplosionEffect()
    {
        int d4 = 0, d5 = 0;
        yield return DcDamage(4, (d, h) => d4 = d);
        yield return DcDamage(5, (d, h) => d5 = d);
        Assert.Greater(d5 - d4, 50, "가득(5스택)일 때만 추가 폭발 피해");
    }
}
