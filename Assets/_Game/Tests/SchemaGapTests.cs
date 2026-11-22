using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>설계도 18 §8-5 스키마 갭 해소 — G1 마일스톤 쿨타임·Q 쿨타임 배율 / G2 명중률 / G3 둔화 면역.</summary>
public class SchemaGapTests
{
    static SkillDef Skill(string id, float cd = 1f, int slot = 1)
    {
        var s = ScriptableObject.CreateInstance<SkillDef>();
        s.id = id; s.nameKo = id; s.slot = slot; s.cooldown = cd; s.mpCost = 0; s.effectKind = SkillEffectKind.Strike;
        return s;
    }

    static CombatCharacter Make(out OwnedCharacter owned, params SkillDef[] skills)
    {
        var def = ScriptableObject.CreateInstance<CharacterDef>();
        def.id = "g" + Random.Range(0, 99999); def.nameKo = "c"; def.rarity = Rarity.N; def.continent = Continent.Physics;
        def.baseStats = new CharacterStats { hp = 500, atk = 40, def = 10, spd = 5, mp = 100 };
        def.skills = skills;
        owned = new OwnedCharacter { id = def.id, level = 1 };
        var c = new CombatCharacter(def, owned);
        for (int i = 0; i < skills.Length; i++) c.Unlock(i);
        c.TickStatus(0f, true);
        return c;
    }

    // ════════════════════════════════════════════════════════════════════════
    // G1 — 마일스톤 쿨타임
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void EffectiveCooldown_AppliesMilestoneBonusFromMilestoneLevel_NeverNegative()
    {
        var s = Skill("a", cd: 0.6f); s.milestoneCooldownBonus = -0.2f;
        Assert.AreEqual(0.6f, SkillScaling.EffectiveCooldown(s, 1), 0.0001f);
        Assert.AreEqual(0.6f, SkillScaling.EffectiveCooldown(s, 2), 0.0001f);
        Assert.AreEqual(0.4f, SkillScaling.EffectiveCooldown(s, 3), 0.0001f, "Lv.3 이상: 0.6 → 0.4");
        Assert.AreEqual(0.4f, SkillScaling.EffectiveCooldown(s, 5), 0.0001f);
        s.milestoneCooldownBonus = -5f;
        Assert.AreEqual(0f, SkillScaling.EffectiveCooldown(s, 3), 0.0001f, "음수로 내려가지 않는다");
    }

    [Test]
    public void EffectiveCooldown_NoBonus_IsPlainCooldown()
        => Assert.AreEqual(2f, SkillScaling.EffectiveCooldown(Skill("a", 2f), 5), 0.0001f);

    [Test]
    public void CastSkill_UsesMilestoneCooldown_AtLevelThree()
    {
        var s = Skill("a", cd: 1f); s.milestoneCooldownBonus = -0.4f;
        var c = Make(out var owned, s);
        c.CastSkill(0);
        Assert.AreEqual(1f, c.CooldownRemaining(0), 0.0001f);

        c.TickCooldowns(5f);
        owned.GetSkillProgress("a").level = 3;
        c.CastSkill(0);
        Assert.AreEqual(0.6f, c.CooldownRemaining(0), 0.0001f, "Lv.3 → 쿨타임 1.0 − 0.4");
    }

    [Test]
    public void BasicAttack_UsesMilestoneCooldown()
    {
        var basic = Skill("basic", cd: 0.6f, slot: 0); basic.milestoneCooldownBonus = -0.2f;
        var c = Make(out var owned, basic);
        Assert.AreEqual(0.6f, BasicAttack.Resolve(c, 2.2f, 60f, 0.5f).cooldown, 0.0001f);
        owned.GetSkillProgress("basic").level = 3;
        Assert.AreEqual(0.4f, BasicAttack.Resolve(c, 2.2f, 60f, 0.5f).cooldown, 0.0001f);
    }

    // ════════════════════════════════════════════════════════════════════════
    // G1b — 기본기(Q) 쿨타임에 CooldownRate 적용
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void AttackCooldownTick_ScalesByCooldownRate()
    {
        var c = Make(out _);
        Assert.AreEqual(0.5f, PlayerCombat.TickAttackCooldown(1f, 0.5f, c), 0.0001f, "배율 없음 = 그대로");
        c.Status.Apply(new StatusEffect { Id = "cr", Kind = StatusKind.StatMod, Stat = StatKind.CooldownRate, Value = 2f, Duration = 99f, IsBeneficial = true }, true);
        Assert.AreEqual(0f, PlayerCombat.TickAttackCooldown(1f, 0.5f, c), 0.0001f, "2배 빠르게 → 0.5초에 1초분");
        Assert.AreEqual(0.5f, PlayerCombat.TickAttackCooldown(0.5f, 0.1f, null), 0.4f, "캐릭터가 없어도 안전");
    }

    // ════════════════════════════════════════════════════════════════════════
    // G2 — 명중률
    // ════════════════════════════════════════════════════════════════════════

    GameObject _go;

    [TearDown]
    public void TearDown()
    {
        Enemy.HitRoll = null;
        if (_go != null) Object.DestroyImmediate(_go);
    }

    Enemy SpawnEnemy()
    {
        _go = new GameObject("E");
        return _go.AddComponent<Enemy>();
    }

    [Test]
    public void Accuracy_NoDebuff_AlwaysHits()
    {
        var e = SpawnEnemy();
        Enemy.HitRoll = () => 0.999f;
        Assert.IsTrue(e.RollHit());
        Enemy.HitRoll = () => 1f;
        Assert.IsTrue(e.RollHit(), "명중률 100%는 어떤 주사위 값에서도 명중");
    }

    [Test]
    public void Accuracy_Debuff_ReducesHitChance()
    {
        var e = SpawnEnemy();
        e.ApplyStatus(new StatusEffect { Id = "acc", Kind = StatusKind.StatMod, Stat = StatKind.Accuracy, Value = 0.8f, Duration = 5f });
        Enemy.HitRoll = () => 0.79f;
        Assert.IsTrue(e.RollHit(), "79% < 80%");
        Enemy.HitRoll = () => 0.81f;
        Assert.IsFalse(e.RollHit(), "81% > 80% → 빗나감");
    }

    [Test]
    public void Accuracy_ZeroMultiplier_AlwaysMisses()
    {
        var e = SpawnEnemy();
        e.ApplyStatus(new StatusEffect { Id = "acc", Kind = StatusKind.StatMod, Stat = StatKind.Accuracy, Value = 0f, Duration = 5f });
        Enemy.HitRoll = () => 0f;
        Assert.IsFalse(e.RollHit());
    }

    [UnityTest]
    public IEnumerator Accuracy_IsAValidDebuffStat_ForSkillEffects()
    {
        var skill = ScriptableObject.CreateInstance<SkillDef>();
        skill.id = "refract"; skill.shape = SkillShape.Circle; skill.radius = 3f;
        skill.effects = new[] { new SkillEffectEntry { type = SkillEffectType.StatDebuff, target = TargetScope.EnemyArea, stat = StatKind.Accuracy, value = 0.8f, duration = 1f } };
        var c = Make(out _, skill);
        var e = SpawnEnemy();
        _go.transform.position = new Vector3(0, 0, 1);
        yield return null;   // Start에서 EnemyUnit 생성 → 생존 상태가 된다
        var caster = new GameObject("Caster"); caster.transform.position = Vector3.zero;
        try { SkillExecutor.Execute(skill, 0, c, caster.transform); }
        finally { Object.DestroyImmediate(caster); }
        Assert.AreEqual(0.8f, e.Status.Mult(StatKind.Accuracy), 0.0001f);
    }

    // ════════════════════════════════════════════════════════════════════════
    // G3 — 둔화 면역
    // ════════════════════════════════════════════════════════════════════════

    static SkillDef ImmunityPassive(string formReq = null)
    {
        var s = ScriptableObject.CreateInstance<SkillDef>();
        s.id = "immune"; s.skillType = SkillKind.Passive; s.slot = 5; s.formReq = formReq;
        s.passive = new PassiveSpec
        {
            modifiers = new[]
            {
                new PassiveModifierSpec { source = ModifierSource.Constant, stat = StatKind.MoveSpeed, value = 1.1f, immuneToSlow = true },
            },
        };
        return s;
    }

    static StatusEffect Slow(float v) => new StatusEffect { Id = "slow" + v, Kind = StatusKind.StatMod, Stat = StatKind.MoveSpeed, Value = v, Duration = 5f };

    [Test]
    public void SlowImmunity_IgnoresSlows_ButKeepsOwnSpeedBonus()
    {
        var c = Make(out _, ImmunityPassive());
        Assert.IsTrue(c.IsSlowImmune);
        c.ApplyStatus(Slow(0.5f), true);
        Assert.AreEqual(1.1f, c.MoveSpeedMultiplier, 0.0001f, "둔화는 무시, 패시브 +10%는 유지");
    }

    [Test]
    public void SlowImmunity_StillAllowsBeneficialSpeedBuffs()
    {
        var c = Make(out _, ImmunityPassive());
        c.ApplyStatus(new StatusEffect { Id = "haste", Kind = StatusKind.StatMod, Stat = StatKind.MoveSpeed, Value = 1.5f, Duration = 5f, IsBeneficial = true }, true);
        Assert.AreEqual(1.5f * 1.1f, c.MoveSpeedMultiplier, 0.0001f);
    }

    [Test]
    public void NoImmunity_SlowApplies()
    {
        var c = Make(out _);
        Assert.IsFalse(c.IsSlowImmune);
        c.ApplyStatus(Slow(0.5f), true);
        Assert.AreEqual(0.5f, c.MoveSpeedMultiplier, 0.0001f);
    }

    [Test]
    public void SlowImmunity_DoesNotBlockStun_AndOnlyHarmfulMoveSpeedIsIgnored()
    {
        var c = Make(out _, ImmunityPassive());
        c.ApplyStatus(new StatusEffect { Id = "stun", Kind = StatusKind.Stun, Duration = 2f }, true);
        Assert.IsTrue(c.Status.IsStunned, "면역은 둔화만 — 스턴은 별개");
        c.ApplyStatus(new StatusEffect { Id = "atkdown", Kind = StatusKind.StatMod, Stat = StatKind.Atk, Value = 0.5f, Duration = 5f }, true);
        Assert.AreEqual(0.5f, c.Status.Mult(StatKind.Atk), 0.0001f, "공격력 디버프는 그대로 적용");
    }

    [Test]
    public void SlowImmunity_IsFormGated()
    {
        var def = ScriptableObject.CreateInstance<CharacterDef>();
        def.id = "fg"; def.nameKo = "c"; def.rarity = Rarity.N;
        def.baseStats = new CharacterStats { hp = 100, atk = 10, def = 1, spd = 1, mp = 10 };
        def.forms = new[]
        {
            new CharacterForm { formId = "Particle", isDefault = true, baseStats = def.baseStats },
            new CharacterForm { formId = "Wave", baseStats = def.baseStats },
        };
        def.skills = new[] { ImmunityPassive("Particle") };
        var c = new CombatCharacter(def, new OwnedCharacter { id = "fg", level = 1 });
        c.Unlock(0); c.TickStatus(0f, true);
        Assert.IsTrue(c.IsSlowImmune);
        c.ChangeForm("Wave", FormChangeCause.Manual);
        Assert.IsFalse(c.IsSlowImmune, "입자성에서만 면역");
    }
}
