using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>23-F — 공격 속성·속성 한정 피해 감소·파티 수정자 조건·반전/해제/속성 무작위 효과.</summary>
public class ElementStatusTests
{
    readonly List<GameObject> _spawned = new();

    [TearDown]
    public void TearDown()
    {
        SkillExecutor.Random01 = null;
        PlayerRuntime.Party = null; PlayerRuntime.Active = null;
        foreach (var g in _spawned) if (g != null) Object.DestroyImmediate(g);
        _spawned.Clear();
    }

    // ── 상태 합성(순수) ───────────────────────────────────────────────────

    static StatusEffect Resist(float v, AttackElement mask = AttackElement.None, bool invert = false, string id = "r")
        => new StatusEffect { Id = id, Kind = StatusKind.StatMod, Stat = StatKind.DamageTaken, Value = v, Duration = 9f, IsBeneficial = true, ElementMask = mask, ElementInvert = invert };

    [Test]
    public void Mult_ElementMask_AppliesOnlyToMatchingAttackElement()
    {
        var h = new StatusHolder();
        h.Apply(Resist(0.5f, AttackElement.Metal), true);
        Assert.AreEqual(0.5f, h.Mult(StatKind.DamageTaken, AttackElement.Metal), 0.0001f);
        Assert.AreEqual(1f, h.Mult(StatKind.DamageTaken, AttackElement.Electric), 0.0001f);
        Assert.AreEqual(1f, h.Mult(StatKind.DamageTaken, AttackElement.None), 0.0001f, "속성을 모르면 속성 한정 효과는 적용되지 않는다");
        Assert.AreEqual(1f, h.Mult(StatKind.DamageTaken), 0.0001f, "기존 Mult(stat)도 같은 규칙");
    }

    [Test]
    public void Mult_InvertedMask_AppliesToEverythingElse_IncludingUnknown()
    {
        var h = new StatusHolder();
        h.Apply(Resist(0.9f, AttackElement.Metal, invert: true), true);
        Assert.AreEqual(1f, h.Mult(StatKind.DamageTaken, AttackElement.Metal), 0.0001f);
        Assert.AreEqual(0.9f, h.Mult(StatKind.DamageTaken, AttackElement.Electric), 0.0001f);
        Assert.AreEqual(0.9f, h.Mult(StatKind.DamageTaken, AttackElement.None), 0.0001f);
    }

    [Test]
    public void Mult_NoMask_AppliesToAll_AndMultiMaskMatchesAnyFlag()
    {
        var h = new StatusHolder();
        h.Apply(Resist(0.8f), true);
        h.Apply(Resist(0.5f, AttackElement.Electric | AttackElement.Light, id: "r2"), true);
        Assert.AreEqual(0.8f, h.Mult(StatKind.DamageTaken, AttackElement.Metal), 0.0001f);
        Assert.AreEqual(0.4f, h.Mult(StatKind.DamageTaken, AttackElement.Light), 0.0001f);
    }

    [Test]
    public void Reverse_StatusAndDispel()
    {
        var h = new StatusHolder();
        h.Apply(new StatusEffect { Id = "rev", Kind = StatusKind.Reverse, Duration = 2f }, true);
        h.Apply(new StatusEffect { Id = "buff", Kind = StatusKind.StatMod, Stat = StatKind.Atk, Value = 1.5f, Duration = 5f, IsBeneficial = true }, true);
        h.Apply(new StatusEffect { Id = "dot", Kind = StatusKind.Dot, Value = 1f, Duration = 5f }, true);
        Assert.IsTrue(h.IsReversed);
        h.Dispel();
        Assert.IsFalse(h.Has("buff"), "이로운 효과 제거");
        Assert.IsTrue(h.Has("dot") && h.Has("rev"), "해로운 효과는 그대로");
        h.Tick(2.1f, false, null);
        Assert.IsFalse(h.IsReversed, "시간이 지나면 풀린다");
    }

    [Test]
    public void ChaseDirection_FlipsWhenReversed()
    {
        Assert.AreEqual(Vector3.forward, Enemy.ChaseDirection(Vector3.forward, false));
        Assert.AreEqual(Vector3.back, Enemy.ChaseDirection(Vector3.forward, true));
    }

    // ── 전투 연결 ─────────────────────────────────────────────────────────

    static CombatCharacter Make(AttackElement el = AttackElement.None, params SkillDef[] skills)
    {
        var def = ScriptableObject.CreateInstance<CharacterDef>();
        def.id = "el" + Random.Range(0, 99999); def.nameKo = "c"; def.rarity = Rarity.N; def.continent = Continent.Physics;
        def.baseStats = new CharacterStats { hp = 1000, atk = 100, def = 10, spd = 5, mp = 100 };
        def.skills = skills; def.attackElement = el;
        var c = new CombatCharacter(def, new OwnedCharacter { id = def.id, level = 1 });
        for (int i = 0; i < skills.Length; i++) c.Unlock(i);
        c.TickStatus(0f, true);
        return c;
    }

    Enemy Spawn(AttackElement el = AttackElement.None, float z = 3f)
    {
        var go = new GameObject("E"); go.transform.position = new Vector3(0, 0, z); _spawned.Add(go);
        var e = go.AddComponent<Enemy>();
        typeof(Enemy).GetField("maxHp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(e, 10000000);
        e.SetAttackElement(el);
        return e;
    }

    Transform Origin() { var o = new GameObject("Caster"); _spawned.Add(o); return o.transform; }

    static SkillDef Skill(params SkillEffectEntry[] effects)
    {
        var s = ScriptableObject.CreateInstance<SkillDef>();
        s.id = "sk"; s.nameKo = "sk"; s.slot = 1; s.mpCost = 0; s.cooldown = 0f; s.effectKind = SkillEffectKind.HealBuff;
        s.shape = SkillShape.Cone; s.range = 8f; s.halfAngle = 60f; s.effects = effects;
        return s;
    }

    [UnityTest]
    public IEnumerator Magnetic_MetalMinus50_OthersMinus10_ViaSkillEffects()
    {
        var s = Skill(
            new SkillEffectEntry { type = SkillEffectType.DamageTakenMult, target = TargetScope.Self, value = 0.5f, duration = 6f, elementMask = AttackElement.Metal },
            new SkillEffectEntry { type = SkillEffectType.DamageTakenMult, target = TargetScope.Self, value = 0.9f, duration = 6f, elementMask = AttackElement.Metal, elementInvert = true });
        var c = Make(AttackElement.None, s);
        var metal = Spawn(AttackElement.Metal); var elec = Spawn(AttackElement.Electric); var plain = Spawn(AttackElement.None); yield return null;

        SkillExecutor.Execute(s, 0, c, Origin());
        int hp = c.Hp; c.TakeHit(100, metal);  Assert.AreEqual(50, hp - c.Hp, "금속 −50%");
        hp = c.Hp;     c.TakeHit(100, elec);   Assert.AreEqual(90, hp - c.Hp, "그 외 −10%");
        hp = c.Hp;     c.TakeHit(100, plain);  Assert.AreEqual(90, hp - c.Hp, "속성 없음도 '그 외'");
    }

    [UnityTest]
    public IEnumerator ElectrostaticShield_ReducesAll35Percent_AndMakesElectricHarmless()
    {
        var s = Skill(
            new SkillEffectEntry { type = SkillEffectType.DamageTakenMult, target = TargetScope.Self, value = 0.65f, duration = 4f },
            new SkillEffectEntry { type = SkillEffectType.DamageTakenMult, target = TargetScope.Self, value = 0f, duration = 4f, elementMask = AttackElement.Electric });
        var c = Make(AttackElement.None, s);
        var elec = Spawn(AttackElement.Electric); var fire = Spawn(AttackElement.Thermal); yield return null;
        SkillExecutor.Execute(s, 0, c, Origin());
        int hp = c.Hp; c.TakeHit(100, elec); Assert.AreEqual(0, hp - c.Hp, "전기 속성 피해 면역");
        hp = c.Hp;     c.TakeHit(100, fire); Assert.AreEqual(65, hp - c.Hp, "그 외는 −35%");
    }

    [Test]
    public void PartyPresence_ElementFilter_BuffsElectricAlliesMore()
    {
        var pas = ScriptableObject.CreateInstance<SkillDef>();
        pas.id = "emforce"; pas.skillType = SkillKind.Passive; pas.slot = 2;
        pas.passive = new PassiveSpec
        {
            modifiers = new[]
            {
                new PassiveModifierSpec { source = ModifierSource.PartyPresence, stat = StatKind.Atk, value = 1.2f, partyWide = true, requireElement = AttackElement.Electric },
                new PassiveModifierSpec { source = ModifierSource.PartyPresence, stat = StatKind.Atk, value = 1.07f, partyWide = true, requireElement = AttackElement.Electric, requireElementInvert = true },
            },
        };
        var support = Make(AttackElement.Electric, pas);
        var electric = Make(AttackElement.Electric);
        var plain = Make(AttackElement.None);
        var party = new Party(new[] { support, electric, plain });

        Assert.AreEqual(100 * 1.2f, electric.Atk, 1f, "전기 속성 아군 +20%");
        Assert.AreEqual(100 * 1.2f, support.Atk, 1f, "마그놀 자신도 전기 속성");
        Assert.AreEqual(100 * 1.07f, plain.Atk, 1f, "그 외 +7%");
    }

    // ── 효과 3종 ──────────────────────────────────────────────────────────

    [UnityTest]
    public IEnumerator ReverseMove_And_Dispel_And_RandomizeElement_Effects()
    {
        var s = Skill(
            new SkillEffectEntry { type = SkillEffectType.ReverseMove, target = TargetScope.EnemyArea, duration = 2f },
            new SkillEffectEntry { type = SkillEffectType.Dispel, target = TargetScope.EnemyArea },
            new SkillEffectEntry { type = SkillEffectType.RandomizeElement, target = TargetScope.EnemyArea });
        var c = Make(AttackElement.None, s);
        var e = Spawn(AttackElement.None); yield return null;
        e.ApplyStatus(new StatusEffect { Id = "buff", Kind = StatusKind.StatMod, Stat = StatKind.Atk, Value = 1.5f, Duration = 5f, IsBeneficial = true });
        SkillExecutor.Random01 = () => 0.3f;     // 4개 속성 중 인덱스 1 = Electric

        SkillExecutor.Execute(s, 0, c, Origin());
        Assert.IsTrue(e.Status.IsReversed, "이동방향 반전");
        Assert.IsFalse(e.Status.Has("buff"), "버프 해제");
        Assert.AreEqual(AttackElement.Electric, ((ISkillTarget)e).AttackElement, "공격 속성이 무작위로 바뀐다");
    }
}
