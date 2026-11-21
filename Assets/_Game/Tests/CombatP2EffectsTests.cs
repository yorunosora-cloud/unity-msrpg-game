using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>설계도 22 P2 — Stage C: 효과 목록 · 형태 · 아군 효과 · 적/보스 상태 수용.</summary>
public class CombatP2EffectsTests
{
    // ── 헬퍼 ──────────────────────────────────────────────────────────────

    static SkillEffectEntry E(SkillEffectType t, TargetScope target = TargetScope.EnemyArea,
                              float value = 0f, float duration = 0f, StatKind stat = StatKind.Atk,
                              int maxStacks = 0, int cap = 0)
        => new SkillEffectEntry { type = t, target = target, value = value, duration = duration,
                                  stat = stat, maxStacks = maxStacks, targetCap = cap };

    static SkillDef MakeSkill(SkillShape shape, params SkillEffectEntry[] effects)
    {
        var s = ScriptableObject.CreateInstance<SkillDef>();
        s.id = "p2_" + Random.Range(0, 99999); s.nameKo = "t";
        s.shape = shape; s.range = 6f; s.halfAngle = 60f; s.lineWidth = 1f; s.radius = 3f;
        s.mpCost = 0; s.cooldown = 1f;
        s.effects = effects;
        return s;
    }

    static CombatCharacter MakeChar(string name, params SkillDef[] skills)
    {
        var def = ScriptableObject.CreateInstance<CharacterDef>();
        def.id = name; def.nameKo = name; def.rarity = Rarity.N; def.continent = Continent.Physics;
        def.skills = skills;
        def.baseStats = new CharacterStats { hp = 200, atk = 20, def = 5, spd = 1, mp = 100 };
        var c = new CombatCharacter(def, new OwnedCharacter { id = name, level = 1, exp = 0 });
        if (skills.Length > 0) c.Unlock(0);
        return c;
    }

    GameObject _root;
    readonly System.Collections.Generic.List<GameObject> _spawned = new();

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("TestCaster");
        _root.transform.position = Vector3.zero;
        _root.transform.rotation = Quaternion.identity;   // 전방 +Z
        PlayerRuntime.Party = null;
        PlayerRuntime.Active = null;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var g in _spawned) if (g != null) Object.Destroy(g);
        _spawned.Clear();
        if (_root != null) Object.Destroy(_root);
        PlayerRuntime.Party = null;
        PlayerRuntime.Active = null;
    }

    Enemy SpawnEnemy(Vector3 pos)
    {
        var go = new GameObject("E");
        go.transform.position = pos;
        var e = go.AddComponent<Enemy>();
        e.SetWeakness(Continent.Math);            // 시전자(물리)는 약점이 아님 → 1.8배 보정 없음
        _spawned.Add(go);
        return e;
    }

    // ════════════════════════════════════════════════════════════════════════
    // SkillScaling.ScaleEffectValue (순수)
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Scale_Damage_FollowsPowerCurve()
    {
        var e = E(SkillEffectType.Damage, value: 2f);
        Assert.AreEqual(2f, SkillScaling.ScaleEffectValue(e, 1), 0.0001f);
        Assert.AreEqual(2f * (1f + 2 * 0.12f), SkillScaling.ScaleEffectValue(e, 3), 0.0001f);
    }

    [Test]
    public void Scale_StatBuff_ScalesOnlyTheBonusPart()
    {
        var e = E(SkillEffectType.StatBuff, value: 1.5f);
        Assert.AreEqual(1f + 0.5f * (1f + 2 * 0.12f), SkillScaling.ScaleEffectValue(e, 3), 0.0001f);
    }

    [Test]
    public void Scale_Slow_IsUnscaled()
    {
        var e = E(SkillEffectType.Slow, value: 0.5f);
        Assert.AreEqual(0.5f, SkillScaling.ScaleEffectValue(e, 5), 0.0001f);
    }

    // ════════════════════════════════════════════════════════════════════════
    // AllyEffectApplier (순수)
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Ally_Heal_RestoresPercentOfTargetMaxHp()
    {
        var caster = MakeChar("a"); var t = MakeChar("b");
        t.Damage(100);
        int healed = AllyEffectApplier.Apply(E(SkillEffectType.Heal, TargetScope.Bench, 0.25f), 0.25f, "s", caster, t);
        Assert.AreEqual(50, healed);
        Assert.AreEqual(150, t.Hp);
    }

    [Test]
    public void Ally_Heal_SkipsDownedTarget()
    {
        var caster = MakeChar("a"); var t = MakeChar("b");
        t.Damage(t.MaxHp);
        int healed = AllyEffectApplier.Apply(E(SkillEffectType.Heal, TargetScope.Bench, 0.5f), 0.5f, "s", caster, t);
        Assert.AreEqual(0, healed);
        Assert.IsTrue(t.IsDowned);
    }

    [Test]
    public void Ally_StatBuff_OnBench_IsPending_OnSelf_IsActive()
    {
        var caster = MakeChar("a"); var bench = MakeChar("b");
        int baseAtk = caster.Atk;
        var buff = E(SkillEffectType.StatBuff, TargetScope.Self, 2f, 5f, StatKind.Atk);
        AllyEffectApplier.Apply(buff, 2f, "s", caster, caster);
        AllyEffectApplier.Apply(buff, 2f, "s", caster, bench);
        Assert.AreEqual(baseAtk * 2, caster.Atk, "활성 본인은 즉시");
        Assert.AreEqual(baseAtk, bench.Atk, "대기 캐릭터는 예약");
        bench.OnSwitchIn();
        Assert.AreEqual(baseAtk * 2, bench.Atk, "전환 기점에 시작");
    }

    [Test]
    public void Ally_Shield_UsesMaxHpPercent_AndAbsorbs()
    {
        var caster = MakeChar("a");
        AllyEffectApplier.Apply(E(SkillEffectType.Shield, TargetScope.Self, 0.1f, 5f), 0.1f, "s", caster, caster);
        Assert.AreEqual(20f, caster.Status.ShieldAmount, 0.001f, "maxHp 200 × 10%");
    }

    [Test]
    public void Ally_DamageTakenMult_OnSelf_ReducesIncoming()
    {
        var caster = MakeChar("a");
        AllyEffectApplier.Apply(E(SkillEffectType.DamageTakenMult, TargetScope.Self, 0.5f, 5f), 0.5f, "s", caster, caster);
        int hp0 = caster.Hp;
        caster.TakeHit(40);
        Assert.AreEqual(hp0 - 20, caster.Hp);
    }

    [Test]
    public void Ally_RestoreMp_AndCleanse_AreImmediate()
    {
        var caster = MakeChar("a"); var t = MakeChar("b");
        t.UseMp(80);
        t.Status.Apply(new StatusEffect { Id = "burn", Kind = StatusKind.Dot, Value = 1f, Duration = 5f }, false);
        AllyEffectApplier.Apply(E(SkillEffectType.RestoreMp, TargetScope.Bench, 0.5f), 0.5f, "s", caster, t);
        AllyEffectApplier.Apply(E(SkillEffectType.Cleanse, TargetScope.Bench), 0f, "s", caster, t);
        Assert.AreEqual(20 + 50, t.Mp);
        Assert.AreEqual(0, t.Status.Count);
    }

    [Test]
    public void Ally_SelfDamage_NeverKills()
    {
        var caster = MakeChar("a");
        AllyEffectApplier.Apply(E(SkillEffectType.SelfDamage, TargetScope.Self, 5f), 5f, "s", caster, caster);
        Assert.AreEqual(1, caster.Hp, "최소 1 남긴다");
    }

    // ════════════════════════════════════════════════════════════════════════
    // SkillExecutor — 형태 + 효과 목록 (PlayMode, Enemy 사용)
    // ════════════════════════════════════════════════════════════════════════

    [UnityTest]
    public IEnumerator Exec_LineShape_HitsOnlyEnemiesInLine()
    {
        var skill  = MakeSkill(SkillShape.Line, E(SkillEffectType.Damage, value: 1f));
        var caster = MakeChar("a", skill);
        var inLine  = SpawnEnemy(new Vector3(0.3f, 0, 4));
        var offLine = SpawnEnemy(new Vector3(2f, 0, 4));
        yield return null;

        SkillExecutor.Execute(skill, 0, caster, _root.transform);

        Assert.Less(inLine.Unit.Hp, inLine.Unit.MaxHp, "직선 안");
        Assert.AreEqual(offLine.Unit.MaxHp, offLine.Unit.Hp, "직선 밖");
    }

    [UnityTest]
    public IEnumerator Exec_CircleShape_HitsEnemiesBehindCaster()
    {
        var skill  = MakeSkill(SkillShape.Circle, E(SkillEffectType.Damage, value: 1f));
        var caster = MakeChar("a", skill);
        var behind = SpawnEnemy(new Vector3(0, 0, -2));
        var far    = SpawnEnemy(new Vector3(0, 0, 8));
        yield return null;

        SkillExecutor.Execute(skill, 0, caster, _root.transform);

        Assert.Less(behind.Unit.Hp, behind.Unit.MaxHp);
        Assert.AreEqual(far.Unit.MaxHp, far.Unit.Hp);
    }

    [UnityTest]
    public IEnumerator Exec_TargetCap_HitsNearestN()
    {
        var skill  = MakeSkill(SkillShape.Circle, E(SkillEffectType.Damage, value: 1f, cap: 1));
        var caster = MakeChar("a", skill);
        var near = SpawnEnemy(new Vector3(0, 0, 1));
        var far  = SpawnEnemy(new Vector3(0, 0, 2.5f));
        yield return null;

        SkillExecutor.Execute(skill, 0, caster, _root.transform);

        Assert.Less(near.Unit.Hp, near.Unit.MaxHp);
        Assert.AreEqual(far.Unit.MaxHp, far.Unit.Hp, "최대 1명");
    }

    [UnityTest]
    public IEnumerator Exec_Stun_And_Slow_ApplyToEnemy()
    {
        var skill = MakeSkill(SkillShape.Circle,
            E(SkillEffectType.Stun, duration: 2f),
            E(SkillEffectType.Slow, value: 0.4f, duration: 3f));
        var caster = MakeChar("a", skill);
        var enemy = SpawnEnemy(new Vector3(0, 0, 1));
        yield return null;

        SkillExecutor.Execute(skill, 0, caster, _root.transform);

        Assert.IsTrue(enemy.Status.IsStunned);
        Assert.AreEqual(0.4f, enemy.Status.Mult(StatKind.MoveSpeed), 0.0001f);
    }

    [UnityTest]
    public IEnumerator Exec_Dot_DamagesEnemyOverTime()
    {
        var skill = MakeSkill(SkillShape.Circle, E(SkillEffectType.Dot, value: 1f, duration: 4f)); // 초당 Atk×1 = 20
        var caster = MakeChar("a", skill);
        var enemy = SpawnEnemy(new Vector3(0, 0, 1));
        yield return null;
        int hp0 = enemy.Unit.Hp;

        SkillExecutor.Execute(skill, 0, caster, _root.transform);
        yield return new WaitForSeconds(1.2f);

        Assert.Less(enemy.Unit.Hp, hp0, "도트로 체력 감소");
    }

    [UnityTest]
    public IEnumerator Exec_DamageTakenMult_IncreasesNextHit()
    {
        var vuln = MakeSkill(SkillShape.Circle, E(SkillEffectType.DamageTakenMult, value: 2f, duration: 5f));
        var caster = MakeChar("a", vuln);
        var enemy = SpawnEnemy(new Vector3(0, 0, 1));
        yield return null;

        enemy.ReceiveHit(Continent.Physics, 14);
        int firstDmg = enemy.Unit.MaxHp - enemy.Unit.Hp;
        SkillExecutor.Execute(vuln, 0, caster, _root.transform);
        int hpBefore = enemy.Unit.Hp;
        enemy.ReceiveHit(Continent.Physics, 14);
        int secondDmg = hpBefore - enemy.Unit.Hp;

        Assert.AreEqual(firstDmg * 2, secondDmg, "취약: 받는 피해 ×2");
    }

    [UnityTest]
    public IEnumerator Exec_Knockback_And_Pull_MoveEnemy()
    {
        var kb   = MakeSkill(SkillShape.Circle, E(SkillEffectType.Knockback, value: 3f));
        var pull = MakeSkill(SkillShape.Circle, E(SkillEffectType.Pull, value: 2f));
        var caster = MakeChar("a", kb, pull);
        var enemy = SpawnEnemy(new Vector3(0, 0, 2));
        yield return null;

        SkillExecutor.Execute(kb, 0, caster, _root.transform);
        Assert.AreEqual(5f, enemy.transform.position.z, 0.01f, "밀려남(+3)");

        caster.Unlock(1);
        var skillPull = pull;
        skillPull.radius = 10f;
        SkillExecutor.Execute(skillPull, 1, caster, _root.transform);
        Assert.AreEqual(3f, enemy.transform.position.z, 0.01f, "끌려옴(−2)");
    }

    [UnityTest]
    public IEnumerator Exec_MixedScopes_DamageEnemy_BuffSelf_BuffBench()
    {
        var skill = MakeSkill(SkillShape.Circle,
            E(SkillEffectType.Damage, value: 1f),
            E(SkillEffectType.StatBuff, TargetScope.Self, 2f, 5f, StatKind.Atk),
            E(SkillEffectType.StatBuff, TargetScope.Bench, 1.5f, 5f, StatKind.Atk));
        var caster = MakeChar("a", skill);
        var bench1 = MakeChar("b");
        var party = new Party(new[] { caster, bench1 });
        PlayerRuntime.Party = party; PlayerRuntime.Active = caster;
        var enemy = SpawnEnemy(new Vector3(0, 0, 1));
        yield return null;
        int baseAtk = caster.Atk, benchBase = bench1.Atk;

        SkillExecutor.Execute(skill, 0, caster, _root.transform);

        Assert.Less(enemy.Unit.Hp, enemy.Unit.MaxHp, "적 피해");
        Assert.AreEqual(baseAtk * 2, caster.Atk, "자신 버프 즉시");
        Assert.AreEqual(benchBase, bench1.Atk, "대기 아군 버프는 예약");
        party.SwitchTo(1);
        Assert.AreEqual((int)(benchBase * 1.5f), bench1.Atk, "전환 시 적용");
    }

    [UnityTest]
    public IEnumerator Exec_LegacyEffectKind_StillWorks_WhenNoEffects()
    {
        var s = ScriptableObject.CreateInstance<SkillDef>();
        s.id = "legacy"; s.effectKind = SkillEffectKind.Aoe; s.range = 5f; s.halfAngle = 90f;
        s.damageMultiplier = 1f; s.cooldown = 1f;            // effects 비어 있음
        var caster = MakeChar("a", s);
        var enemy = SpawnEnemy(new Vector3(0, 0, 2));
        yield return null;

        SkillExecutor.Execute(s, 0, caster, _root.transform);
        Assert.Less(enemy.Unit.Hp, enemy.Unit.MaxHp);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Enemy / Boss 상태 수용
    // ════════════════════════════════════════════════════════════════════════

    [UnityTest]
    public IEnumerator Enemy_Stunned_DoesNotMove()
    {
        var player = new GameObject("P"); player.tag = "Player";
        player.transform.position = new Vector3(0, 0, 8);
        _spawned.Add(player);
        var enemy = SpawnEnemy(Vector3.zero);
        yield return null; yield return null;

        enemy.ApplyStatus(new StatusEffect { Id = "stun", Kind = StatusKind.Stun, Duration = 5f });
        float z0 = enemy.transform.position.z;
        yield return new WaitForSeconds(0.5f);
        Assert.AreEqual(z0, enemy.transform.position.z, 0.001f, "스턴 중 이동 없음");
    }

    [UnityTest]
    public IEnumerator Enemy_Slowed_MovesSlowerThanNormal()
    {
        var player = new GameObject("P"); player.tag = "Player";
        player.transform.position = new Vector3(0, 0, 8);
        _spawned.Add(player);
        var fast = SpawnEnemy(new Vector3(-3, 0, 0));
        var slow = SpawnEnemy(new Vector3(3, 0, 0));
        yield return null; yield return null;

        slow.ApplyStatus(new StatusEffect { Id = "slow", Kind = StatusKind.StatMod, Stat = StatKind.MoveSpeed,
                                            Value = 0.2f, Duration = 5f });
        yield return new WaitForSeconds(0.6f);

        Assert.Greater(fast.transform.position.z, slow.transform.position.z + 0.2f, "둔화된 쪽이 덜 이동");
    }

    static Boss SpawnBoss(bool ccImmune, System.Collections.Generic.List<GameObject> bag)
    {
        var go = new GameObject("Boss");
        bag.Add(go);
        var boss = go.AddComponent<Boss>();
        var def = ScriptableObject.CreateInstance<BossDef>();
        def.displayName = "t"; def.maxHp = 1000; def.def = 0; def.ccImmune = ccImmune;
        typeof(Boss).GetField("_def", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(boss, def);
        return boss;
    }

    [UnityTest]
    public IEnumerator Boss_CcImmune_IgnoresStun_ButTakesDot()
    {
        var boss = SpawnBoss(true, _spawned);
        yield return null;

        boss.ApplyStatus(new StatusEffect { Id = "stun", Kind = StatusKind.Stun, Duration = 3f });
        Assert.IsFalse(boss.Status.IsStunned, "보스는 스턴 면역(기본)");

        boss.ApplyStatus(new StatusEffect { Id = "dot", Kind = StatusKind.Dot, Value = 50f, Duration = 3f });
        float h0 = boss.HpFraction;
        yield return new WaitForSeconds(1.1f);
        Assert.Less(boss.HpFraction, h0, "도트는 받는다");
    }

    [UnityTest]
    public IEnumerator Boss_NotImmune_CanBeStunned()
    {
        var boss = SpawnBoss(false, _spawned);
        yield return null;
        boss.ApplyStatus(new StatusEffect { Id = "stun", Kind = StatusKind.Stun, Duration = 3f });
        Assert.IsTrue(boss.Status.IsStunned);
    }

    [UnityTest]
    public IEnumerator Boss_Displace_IsIgnored_WhenCcImmune()
    {
        var boss = SpawnBoss(true, _spawned);
        boss.transform.position = new Vector3(0, 0, 3);
        yield return null;
        boss.Displace(Vector3.zero, 2f, pull: false);
        Assert.AreEqual(3f, boss.transform.position.z, 0.001f);
    }
}
