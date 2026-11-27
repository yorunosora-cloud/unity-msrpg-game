using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>23-H — 부활(전투당 1회)과 피해 저장→방출.</summary>
public class SurvivalTests
{
    readonly List<GameObject> _spawned = new();

    [TearDown]
    public void TearDown()
    {
        PlayerRuntime.Party = null; PlayerRuntime.Active = null;
        foreach (var g in _spawned) if (g != null) Object.DestroyImmediate(g);
        _spawned.Clear();
    }

    static SkillDef RevivePassive(bool unlocked = true)
    {
        var s = ScriptableObject.CreateInstance<SkillDef>();
        s.id = "bottom"; s.nameKo = "바텀쿼크"; s.skillType = SkillKind.Passive; s.slot = 5; s.startUnlocked = unlocked;
        s.passive = new PassiveSpec
        {
            trigger = PassiveTriggerType.OnLethal, value = 0.3f,
            effects = new[] { new SkillEffectEntry { type = SkillEffectType.DamageTakenMult, target = TargetScope.Self, value = 0f, duration = 3f } },
        };
        return s;
    }

    static CombatCharacter Make(out OwnedCharacter owned, bool unlock = true, params SkillDef[] skills)
    {
        var def = ScriptableObject.CreateInstance<CharacterDef>();
        def.id = "sv" + Random.Range(0, 99999); def.nameKo = "c"; def.rarity = Rarity.N; def.continent = Continent.Physics;
        def.baseStats = new CharacterStats { hp = 1000, atk = 100, def = 10, spd = 5, mp = 100 };
        def.skills = skills;
        owned = new OwnedCharacter { id = def.id, level = 1 };
        var c = new CombatCharacter(def, owned);
        if (unlock) for (int i = 0; i < skills.Length; i++) c.Unlock(i);
        c.TickStatus(0f, true);
        return c;
    }

    // ── 부활 ──────────────────────────────────────────────────────────────

    [Test]
    public void LethalHit_RevivesOnce_WithHpRatio_AndTemporaryInvulnerability()
    {
        var c = Make(out _, true, RevivePassive());
        int revived = 0; c.Revived += () => revived++;

        c.TakeHit(5000);
        Assert.IsFalse(c.IsDowned, "치명 피해에도 쓰러지지 않는다");
        Assert.AreEqual(300, c.Hp, "최대 HP의 30%로 부활");
        Assert.AreEqual(1, revived);

        c.TakeHit(5000);
        Assert.AreEqual(300, c.Hp, "부활 직후 무적(받는 피해 0배)");

        c.TickStatus(3.1f, true);
        c.TakeHit(5000);
        Assert.IsTrue(c.IsDowned, "전투당 1회뿐이라 두 번째 치명 피해에는 쓰러진다");
        Assert.AreEqual(1, revived);
    }

    [Test]
    public void ResetCombatState_ReArmsTheRevive()
    {
        var c = Make(out _, true, RevivePassive());
        c.TakeHit(5000); c.TickStatus(3.1f, true);
        c.ResetCombatState();
        c.TakeHit(5000);
        Assert.IsFalse(c.IsDowned, "전투 구역 이탈 후 새 전투에서 다시 부활 가능");
    }

    [Test]
    public void RestoreFull_ReArmsTheRevive_AfterBeingDowned()
    {
        var c = Make(out _, true, RevivePassive());
        c.TakeHit(5000); c.TickStatus(3.1f, true); c.TakeHit(5000);
        Assert.IsTrue(c.IsDowned);
        c.RestoreFull();
        c.TakeHit(5000);
        Assert.IsFalse(c.IsDowned);
    }

    [Test]
    public void LockedPassive_Or_NoPassive_DoesNotRevive()
    {
        var basic = ScriptableObject.CreateInstance<SkillDef>(); basic.id = "q"; basic.slot = 0; basic.startUnlocked = true;   // 시작 해금 스킬이 있으면 레거시 "첫 스킬 기본 해금" 규칙이 꺼진다
        var locked = Make(out _, false, basic, RevivePassive(unlocked: false));
        locked.TakeHit(5000);
        Assert.IsTrue(locked.IsDowned, "해금 전에는 부활하지 않는다");
        var none = Make(out _, true);
        none.TakeHit(5000);
        Assert.IsTrue(none.IsDowned);
    }

    [Test]
    public void NonLethalHit_DoesNotConsumeTheRevive()
    {
        var c = Make(out _, true, RevivePassive());
        c.TakeHit(400);
        Assert.AreEqual(600, c.Hp);
        c.TakeHit(5000);
        Assert.IsFalse(c.IsDowned, "아직 부활을 쓰지 않았으므로 치명 피해에서 부활");
    }

    [Test]
    public void DirectDamage_Dot_AlsoTriggersTheRevive()
    {
        var c = Make(out _, true, RevivePassive());
        c.Damage(5000);
        Assert.IsFalse(c.IsDowned);
        Assert.AreEqual(300, c.Hp);
    }

    // ── 피해 저장 → 방출(순수) ───────────────────────────────────────────

    [Test]
    public void DamageStore_AccumulatesThenReleasesRatioWhenTheWindowEnds()
    {
        var s = new DamageStore();
        s.Begin(1.5f, 3f, 3f);
        s.Add(100); s.Add(60);
        Assert.IsFalse(s.Tick(2.9f, out _));
        Assert.IsTrue(s.Tick(0.2f, out int release));
        Assert.AreEqual(240, release, "160 × 1.5");
        Assert.IsFalse(s.Active);
        Assert.IsFalse(s.Tick(5f, out _), "방출 뒤에는 다시 방출하지 않는다");
    }

    [Test]
    public void DamageStore_IgnoresDamageWhenInactive_AndEmptyStoreReleasesNothing()
    {
        var s = new DamageStore();
        s.Add(500);                              // 시작 전 — 무시
        s.Begin(1.5f, 1f, 3f);
        Assert.IsTrue(s.Tick(1.1f, out int release));
        Assert.AreEqual(0, release);
    }

    // ── 피해 저장 → 방출(캐릭터 통합) ───────────────────────────────────

    static SkillDef ElasticSkill()
    {
        var s = ScriptableObject.CreateInstance<SkillDef>();
        s.id = "elastic"; s.nameKo = "탄성"; s.slot = 2; s.mpCost = 0; s.cooldown = 0f; s.milestoneLevel = 3; s.effectKind = SkillEffectKind.HealBuff;
        s.effects = new[] { new SkillEffectEntry { type = SkillEffectType.StoreDamage, target = TargetScope.Self, value = 1.5f, milestoneValue = 1.8f, duration = 3f, areaRadius = 3f } };
        return s;
    }

    [Test]
    public void StoreDamageEffect_StoresTakenDamage_AndReleasesViaEvent()
    {
        var sk = ElasticSkill();
        var c = Make(out _, true, sk);
        var released = new List<(int dmg, float radius)>();
        c.StoredDamageReleased += (d, r) => released.Add((d, r));

        SkillExecutor.Execute(sk, 0, c, new GameObject("o").transform);
        c.TakeHit(100); c.TakeHit(100);
        Assert.AreEqual(800, c.Hp, "피해는 정상 적용된다(저장은 별도)");
        c.TickStatus(3.1f, true);
        Assert.AreEqual(1, released.Count);
        Assert.AreEqual((300, 3f), released[0], "저장 200 × 150%, 반경 3");
    }

    [Test]
    public void StoreDamageEffect_MilestoneRatio_AtLevelThree()
    {
        var sk = ElasticSkill();
        var c = Make(out var owned, true, sk);
        owned.GetSkillProgress("elastic").level = 3;
        var released = new List<int>();
        c.StoredDamageReleased += (d, r) => released.Add(d);
        SkillExecutor.Execute(sk, 0, c, new GameObject("o").transform);
        c.TakeHit(100);
        c.TickStatus(3.1f, true);
        Assert.AreEqual(180, released[0], "Lv.3: 150% → 180%");
    }

    // ── 월드 방출(PassiveHost) ───────────────────────────────────────────

    [UnityTest]
    public IEnumerator PassiveHost_ReleasesStoredDamageToEnemiesInRadius()
    {
        var sk = ElasticSkill();
        var c = Make(out _, true, sk);
        PlayerRuntime.Active = c; PlayerRuntime.Party = new Party(new[] { c });
        var player = new GameObject("TestPlayer"); _spawned.Add(player);
        player.AddComponent<PassiveHost>();

        var f = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        Enemy Spawn(float z) { var go = new GameObject("E"); go.transform.position = new Vector3(0, 0, z); _spawned.Add(go); var e = go.AddComponent<Enemy>(); typeof(Enemy).GetField("maxHp", f).SetValue(e, 1000000); return e; }
        var near = Spawn(2f); var far = Spawn(9f);
        yield return null; yield return null;

        SkillExecutor.Execute(sk, 0, c, player.transform);
        c.TakeHit(100); c.TakeHit(100);
        int nb = near.CurrentHp, fb = far.CurrentHp;
        c.TickStatus(3.1f, true);
        Assert.AreEqual(300, nb - near.CurrentHp, "반경 안의 적에게 방출 피해(방어력 무시)");
        Assert.AreEqual(0, fb - far.CurrentHp, "반경 밖은 영향 없음");
    }
}
