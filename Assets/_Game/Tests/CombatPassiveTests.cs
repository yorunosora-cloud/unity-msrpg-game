using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>설계도 22 P4 — Stage B: PassiveRunner·CombatCharacter 통합 (캐릭터 문서의 대표 패시브를 코드로 구성해 검증).</summary>
public class CombatPassiveTests
{
    // ── 헬퍼 ──────────────────────────────────────────────────────────────

    static SkillEffectEntry E(SkillEffectType t, TargetScope target = TargetScope.Self, float value = 0f, float dur = 0f,
                              StatKind stat = StatKind.Atk, string whenForm = null, bool consume = false, float area = 0f)
        => new SkillEffectEntry { type = t, target = target, value = value, duration = dur, stat = stat,
                                  whenForm = whenForm, consumeOnHit = consume, areaRadius = area };

    static SkillDef Pas(string id, PassiveSpec spec, string formReq = null)
    {
        var s = ScriptableObject.CreateInstance<SkillDef>();
        s.id = id; s.nameKo = id; s.skillType = SkillKind.Passive; s.slot = 1; s.passive = spec; s.formReq = formReq;
        return s;
    }

    static SkillDef Plain(string id = "plain", float cd = 10f)
    {
        var s = ScriptableObject.CreateInstance<SkillDef>();
        s.id = id; s.nameKo = id; s.slot = 1; s.cooldown = cd; s.effectKind = SkillEffectKind.Strike;
        return s;
    }

    static CharacterForm F(string id, bool isDefault = false, int def = 10, int atk = 40)
        => new CharacterForm { formId = id, nameKo = id, isDefault = isDefault, tint = Color.white,
                               baseStats = new CharacterStats { hp = 500, atk = atk, def = def, spd = 5, mp = 100 } };

    static CombatCharacter Make(CharacterForm[] forms, out OwnedCharacter owned, params SkillDef[] skills)
    {
        var def = ScriptableObject.CreateInstance<CharacterDef>();
        def.id = "cp" + Random.Range(0, 99999); def.nameKo = "c"; def.rarity = Rarity.N; def.continent = Continent.Physics;
        def.baseStats = new CharacterStats { hp = 500, atk = 40, def = 10, spd = 5, mp = 100 };
        def.forms = forms; def.skills = skills;
        owned = new OwnedCharacter { id = def.id, level = 1, exp = 0 };
        var c = new CombatCharacter(def, owned);
        for (int i = 0; i < skills.Length; i++) c.Unlock(i);
        c.TickStatus(0f, true);                                   // 활성 캐릭터로 표시
        return c;
    }

    static CombatCharacter Make(params SkillDef[] skills) => Make(null, out _, skills);

    static List<HitInfo> Hits(int n, int action, bool proc = false, int dmg = 10)
    {
        var l = new List<HitInfo>();
        for (int i = 0; i < n; i++)
            l.Add(new HitInfo { actionId = action, damage = dmg, isProc = proc, hitPoint = new Vector3(i, 0, 0), sourceSkillId = "s" });
        return l;
    }

    static StackSpec Stack(int max, float per, StackDecay decay, float secs, StatKind stat)
        => new StackSpec { stackId = "k", maxStacks = max, perStackValue = per, decay = decay, decaySeconds = secs, stat = stat };

    // ════════════════════════════════════════════════════════════════════════
    // 슬롯·기본
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void PassiveSkills_AreNotCastableViaSlot()
    {
        var c = Make(Pas("p", new PassiveSpec()), Plain());
        Assert.AreEqual(1, c.IndexForSlot(1), "슬롯 1은 액티브 스킬만 잡힌다(패시브 제외)");
    }

    // ════════════════════════════════════════════════════════════════════════
    // 경로 A — OnHit 스택
    // ════════════════════════════════════════════════════════════════════════

    static SkillDef Impulse() => Pas("impulse", new PassiveSpec   // 충격량
    {
        trigger = PassiveTriggerType.OnHit, hitCount = HitCountMode.PerAction,
        stack = Stack(5, 0.08f, StackDecay.ResetAfterIdle, 3f, StatKind.SkillPower),
    });

    [Test]
    public void OnHit_PerAction_CountsOncePerActionNotPerTarget()
    {
        var c = Make(Impulse());
        c.NotifyHits(Hits(3, action: 1), isBasic: false);          // 범위기가 3명을 맞혀도 1회
        Assert.AreEqual(1, c.PassiveStacks("impulse"));
        Assert.AreEqual(1.08f, c.HitMultiplier(false), 0.0001f);
    }

    [Test]
    public void OnHit_PerAction_TwoActionsInOneCall_AddsTwo()
    {
        var c = Make(Impulse());
        var l = Hits(2, 1); l.AddRange(Hits(1, 2));
        c.NotifyHits(l, false);
        Assert.AreEqual(2, c.PassiveStacks("impulse"));
    }

    [Test]
    public void OnHit_ProcDamage_DoesNotTrigger()
    {
        var c = Make(Impulse());
        c.NotifyHits(Hits(3, 1, proc: true), false);
        Assert.AreEqual(0, c.PassiveStacks("impulse"));
    }

    [Test]
    public void OnHit_Stacks_CapAndResetAfterIdle()
    {
        var c = Make(Impulse());
        for (int a = 1; a <= 8; a++) c.NotifyHits(Hits(1, a), false);
        Assert.AreEqual(5, c.PassiveStacks("impulse"));
        Assert.AreEqual(1.4f, c.StatMult(StatKind.SkillPower), 0.0001f, "+8%×5 = +40%");
        c.TickStatus(3.1f, true);
        Assert.AreEqual(0, c.PassiveStacks("impulse"));
    }

    [Test]
    public void OnHit_PerTarget_CountsEachTarget()
    {
        var c = Make(Pas("pt", new PassiveSpec
        {
            trigger = PassiveTriggerType.OnHit, hitCount = HitCountMode.PerTarget,
            stack = Stack(10, 0.01f, StackDecay.ResetAfterIdle, 3f, StatKind.SkillPower),
        }));
        c.NotifyHits(Hits(3, 1), false);
        Assert.AreEqual(3, c.PassiveStacks("pt"));
    }

    [Test]
    public void Stacks_PersistWhileBenched_TimerFrozen()
    {
        var c = Make(Impulse());
        c.NotifyHits(Hits(1, 1), false);
        c.TickStatus(100f, false);                                   // 대기
        Assert.AreEqual(1, c.PassiveStacks("impulse"));
        c.TickStatus(2.9f, true);
        Assert.AreEqual(1, c.PassiveStacks("impulse"));
    }

    [Test]
    public void Carbon_Catenation_FourStacksGiveSixteenPercent()
    {
        var c = Make(Pas("cat", new PassiveSpec
        {
            trigger = PassiveTriggerType.OnHit, hitCount = HitCountMode.PerAction,
            stack = Stack(4, 0.04f, StackDecay.ResetAfterIdle, 3f, StatKind.SkillPower),
        }));
        for (int a = 1; a <= 6; a++) c.NotifyHits(Hits(1, a), false);
        Assert.AreEqual(1.16f, c.HitMultiplier(false), 0.0001f);
    }

    [Test]
    public void Quantization_ConsumedOnNextAction_AtFourStacks()
    {
        var c = Make(Pas("quant", new PassiveSpec
        {
            trigger = PassiveTriggerType.OnHit, hitCount = HitCountMode.PerAction, value = 2.5f,
            stack = Stack(4, 0f, StackDecay.ConsumeAtCap, 0f, StatKind.HitDamage),
        }));
        for (int a = 1; a <= 3; a++) c.NotifyHits(Hits(1, a), false);
        Assert.AreEqual(1f, c.HitMultiplier(false), 0.0001f, "3스택까지는 보너스 없음");
        c.NotifyHits(Hits(1, 4), false);
        Assert.AreEqual(2.5f, c.HitMultiplier(false), 0.0001f, "4스택 → 다음 타격 ×2.5");
        Assert.AreEqual(1f, c.HitMultiplier(false), 0.0001f, "소모됨");
        Assert.AreEqual(0, c.PassiveStacks("quant"));
    }

    [Test]
    public void Momentum_DamageRatio_AddsAttackFractionCapped()
    {
        var spec = new PassiveSpec
        {
            trigger = PassiveTriggerType.OnHit, hitCount = HitCountMode.PerAction,
            stack = new StackSpec { stackId = "mo", damageRatio = 0.1f, cap = 0.3f, decay = StackDecay.ResetAfterIdle, decaySeconds = 5f, stat = StatKind.Atk },
        };
        var c = Make(Pas("momentum", spec));
        int baseAtk = c.Atk;                                         // 40
        c.NotifyHits(Hits(1, 1, dmg: 100), false);                   // 100×0.1/40 = 0.25
        Assert.AreEqual(1.25f, c.StatMult(StatKind.Atk), 0.0001f);
        c.NotifyHits(Hits(1, 2, dmg: 100), false);                   // 상한 0.3
        Assert.AreEqual(1.3f, c.StatMult(StatKind.Atk), 0.0001f);
        Assert.AreEqual(baseAtk * 1.3f, c.Atk, 1f);   // 부동소수 절단 허용
    }

    // ── 효과 발행(월드 효과는 PassiveFire로) ──

    [Test]
    public void OnHit_WorldEffect_EmitsFirePerTarget()
    {
        var c = Make(Pas("compton", new PassiveSpec
        {
            trigger = PassiveTriggerType.OnHit, hitCount = HitCountMode.PerTarget,
            effects = new[] { E(SkillEffectType.Damage, TargetScope.HitTarget, 0.5f, area: 1f) },
        }));
        var fires = new List<PassiveFire>();
        c.PassiveFired += f => fires.Add(f);
        c.NotifyHits(Hits(3, 1), false);
        Assert.AreEqual(3, fires.Count);
        Assert.AreEqual(SkillEffectType.Damage, fires[0].effect.type);
        Assert.AreEqual(1f, fires[0].effect.areaRadius);
        Assert.AreEqual("compton", fires[0].skillId);
        Assert.AreEqual(new Vector3(2, 0, 0), fires[2].hit.hitPoint);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 경로 A — OnHitTaken
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Counter_NextHitBoosted_ConsumedAfterOneAction()
    {
        var c = Make(Pas("counter", new PassiveSpec   // 가속도: 피격 후 1초 내 다음 타격 +60%
        {
            trigger = PassiveTriggerType.OnHitTaken,
            effects = new[] { E(SkillEffectType.StatBuff, TargetScope.Self, 1.6f, 1f, StatKind.HitDamage, consume: true) },
        }));
        c.TakeHit(10);
        Assert.AreEqual(1.6f, c.HitMultiplier(false), 0.0001f);
        Assert.AreEqual(1f, c.HitMultiplier(false), 0.0001f, "한 번 쓰면 소모");
    }

    [Test]
    public void Counter_ExpiresAfterOneSecond()
    {
        var c = Make(Pas("counter", new PassiveSpec
        {
            trigger = PassiveTriggerType.OnHitTaken,
            effects = new[] { E(SkillEffectType.StatBuff, TargetScope.Self, 1.6f, 1f, StatKind.HitDamage, consume: true) },
        }));
        c.TakeHit(10);
        c.TickStatus(1.1f, true);
        Assert.AreEqual(1f, c.HitMultiplier(false), 0.0001f);
    }

    [Test]
    public void Dielectric_StacksReduceDamageTaken_DecayOnePerThreeSeconds()
    {
        var c = Make(Pas("diel", new PassiveSpec   // 부도체: 스택당 −3%, 최대 5, 3초 무피격 시 −1
        {
            trigger = PassiveTriggerType.OnHitTaken,
            stack = Stack(5, -0.03f, StackDecay.DecayOneEvery, 3f, StatKind.DamageTaken),
        }));
        int hp0 = c.Hp;
        c.TakeHit(100);                                              // 스택 0일 때 100
        c.TakeHit(100);                                              // 스택 1 → 97
        Assert.AreEqual(hp0 - 197, c.Hp);
        Assert.AreEqual(2, c.PassiveStacks("diel"));
        c.TickStatus(3.1f, true);
        Assert.AreEqual(1, c.PassiveStacks("diel"));
    }

    [Test]
    public void Reflect_EmitsFire_WithDamageTakenAndAttacker()
    {
        var c = Make(Pas("reflect", new PassiveSpec
        {
            trigger = PassiveTriggerType.OnHitTaken,
            effects = new[] { E(SkillEffectType.Reflect, TargetScope.Attacker, 0.2f) },
        }));
        PassiveFire fire = default; int n = 0;
        c.PassiveFired += f => { fire = f; n++; };
        c.TakeHit(100, null);
        Assert.AreEqual(1, n);
        Assert.AreEqual(SkillEffectType.Reflect, fire.effect.type);
        Assert.AreEqual(100, fire.hit.damage);
        Assert.AreEqual(PassiveTriggerType.OnHitTaken, fire.trigger);
    }

    [Test]
    public void OnHitTaken_Knockback_EmitsFire()
    {
        var c = Make(Pas("reaction", new PassiveSpec
        {
            trigger = PassiveTriggerType.OnHitTaken,
            effects = new[] { E(SkillEffectType.Knockback, TargetScope.Attacker, 2f) },
        }));
        var fires = new List<PassiveFire>();
        c.PassiveFired += f => fires.Add(f);
        c.TakeHit(10);
        Assert.AreEqual(1, fires.Count);
        Assert.AreEqual(2f, fires[0].value, 0.0001f);
    }

    // ════════════════════════════════════════════════════════════════════════
    // HpBelowPercent (영벡터)
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void HpBelow_FiresOnDownwardCrossing_WithInternalCooldown_AndCleanses()
    {
        var c = Make(Pas("zero", new PassiveSpec
        {
            trigger = PassiveTriggerType.HpBelowPercent, value = 0.4f, internalCooldown = 60f,
            effects = new[] { E(SkillEffectType.Teleport, TargetScope.Self), E(SkillEffectType.Cleanse, TargetScope.Self) },
        }));
        var fires = new List<PassiveFire>();
        c.PassiveFired += f => fires.Add(f);
        c.Status.Apply(new StatusEffect { Id = "burn", Kind = StatusKind.Dot, Value = 1f, Duration = 99f }, true);

        c.Damage(250);                                               // 250/500 = 50% — 아직 아님
        Assert.AreEqual(0, fires.Count);
        c.Damage(60);                                                // 190/500 = 38% → 교차
        Assert.AreEqual(1, fires.Count);
        Assert.AreEqual(SkillEffectType.Teleport, fires[0].effect.type);
        Assert.AreEqual(0, c.Status.Count, "디버프 해제(Cleanse)");

        c.Heal(c.MaxHp);
        c.Damage(400);                                               // 다시 교차하지만 내부 쿨타임
        Assert.AreEqual(1, fires.Count);
        c.Heal(c.MaxHp);
        c.TickStatus(60.1f, true);
        c.Damage(400);
        Assert.AreEqual(2, fires.Count, "쿨타임 후 재발동");
    }

    [Test]
    public void HpBelow_DoesNotFireWhenKilled()
    {
        var c = Make(Pas("zero", new PassiveSpec
        {
            trigger = PassiveTriggerType.HpBelowPercent, value = 0.4f,
            effects = new[] { E(SkillEffectType.Teleport, TargetScope.Self) },
        }));
        int n = 0; c.PassiveFired += _ => n++;
        c.Damage(c.MaxHp);
        Assert.AreEqual(0, n);
    }

    // ════════════════════════════════════════════════════════════════════════
    // OnFormChange / SelfDebuffed
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void OnFormChange_DirectionalBuffs_Energia()
    {
        var c = Make(new[] { F("Kinetic", true), F("Potential") }, out _, Pas("conserve", new PassiveSpec
        {
            trigger = PassiveTriggerType.OnFormChange,
            effects = new[]
            {
                E(SkillEffectType.StatBuff, TargetScope.Self, 0.8f, 2f, StatKind.DamageTaken, whenForm: "Potential"),
                E(SkillEffectType.StatBuff, TargetScope.Self, 1.2f, 2f, StatKind.Atk, whenForm: "Kinetic"),
            },
        }));
        int baseAtk = c.Atk;
        c.ChangeForm("Potential", FormChangeCause.Auto);
        Assert.AreEqual(0.8f, c.StatMult(StatKind.DamageTaken), 0.0001f, "운동→위치: 받는 피해 −20%");
        Assert.AreEqual(baseAtk, c.Atk);
        c.ChangeForm("Kinetic", FormChangeCause.Auto);
        Assert.AreEqual((int)(baseAtk * 1.2f), c.Atk, "위치→운동: 공격력 +20%");
    }

    [Test]
    public void OnFormChange_BasicPowerBuff_Hydrogen()
    {
        var c = Make(new[] { F("A", true), F("B") }, out _, Pas("isotope", new PassiveSpec
        {
            trigger = PassiveTriggerType.OnFormChange,
            effects = new[] { E(SkillEffectType.StatBuff, TargetScope.Self, 1.2f, 3f, StatKind.BasicPower) },
        }));
        c.ChangeForm("B", FormChangeCause.Manual);
        Assert.AreEqual(1f,   c.HitMultiplier(false), 0.0001f, "스킬에는 영향 없음");
        Assert.AreEqual(1.2f, c.HitMultiplier(true),  0.0001f, "기본기만 +20%");
    }

    [Test]
    public void SelfDebuffed_TriggersOnlyForHarmfulStatus()
    {
        var c = Make(Pas("adapt", new PassiveSpec
        {
            trigger = PassiveTriggerType.SelfDebuffed,
            effects = new[] { E(SkillEffectType.StatBuff, TargetScope.Self, 1.5f, 5f, StatKind.Atk) },
        }));
        int baseAtk = c.Atk;
        c.ApplyStatus(new StatusEffect { Id = "b", Kind = StatusKind.StatMod, Stat = StatKind.Def, Value = 1.2f, Duration = 5f, IsBeneficial = true }, true);
        Assert.AreEqual(baseAtk, c.Atk);
        c.ApplyStatus(new StatusEffect { Id = "d", Kind = StatusKind.Dot, Value = 1f, Duration = 5f }, true);
        Assert.AreEqual((int)(baseAtk * 1.5f), c.Atk);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 경로 B — 상시 수정자
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Pressure_LostHpPercent_ReducesDamageTaken_OnlyWhenActive()
    {
        var c = Make(Pas("pressure", new PassiveSpec
        {
            modifiers = new[] { new PassiveModifierSpec { source = ModifierSource.LostHpPercent, stat = StatKind.DamageTaken, value = 0.005f, cap = 0.3f } },
        }));
        c.Damage(200);                                               // 40% 잃음
        Assert.AreEqual(0.8f, c.StatMult(StatKind.DamageTaken), 0.0001f);
        c.TickStatus(0f, false);                                     // 대기 → 활성일 때만 계산
        Assert.AreEqual(1f, c.StatMult(StatKind.DamageTaken), 0.0001f);
    }

    [Test]
    public void Velocity_MovingRamp_RaisesMoveSpeed_WhileTickedAsActive()
    {
        var c = Make(Pas("uniform", new PassiveSpec
        {
            modifiers = new[] { new PassiveModifierSpec { source = ModifierSource.MovingRamp, stat = StatKind.MoveSpeed, value = 0.03f, cap = 0.3f, decayPerSecond = 0.05f } },
        }));
        c.TickForm(5f, new CombatMotionState { isMoving = true });
        Assert.AreEqual(1.15f, c.MoveSpeedMultiplier, 0.0001f);
        c.TickForm(2f, new CombatMotionState { isMoving = false });
        Assert.AreEqual(1.05f, c.MoveSpeedMultiplier, 0.0001f);
    }

    [Test]
    public void FormGatedPassive_OnlyWorksInItsForm()
    {
        var c = Make(new[] { F("Mass", true), F("Inertia") }, out _, Pas("static", new PassiveSpec
        {
            modifiers = new[] { new PassiveModifierSpec { source = ModifierSource.Constant, stat = StatKind.Def, value = 1.2f } },
        }, formReq: "Mass"));
        Assert.AreEqual(12, c.Def);
        c.ChangeForm("Inertia", FormChangeCause.Auto);
        Assert.AreEqual(10, c.Def);
    }

    [Test]
    public void EnemyAura_ListedOnlyWhileActive_AndNotAppliedToSelf()
    {
        var c = Make(Pas("aura", new PassiveSpec
        {
            modifiers = new[] { new PassiveModifierSpec { source = ModifierSource.Constant, stat = StatKind.MoveSpeed, value = 0.85f, enemyAuraRadius = 6f } },
        }));
        var auras = new List<(float radius, StatKind stat, float value)>(c.EnemyAuras());
        Assert.AreEqual(1, auras.Count);
        Assert.AreEqual(6f, auras[0].radius);
        Assert.AreEqual(1f, c.StatMult(StatKind.MoveSpeed), 0.0001f, "오라는 본인에게 적용되지 않는다");
        c.TickStatus(0f, false);
        Assert.AreEqual(0, new List<(float, StatKind, float)>(c.EnemyAuras()).Count);
    }

    [Test]
    public void PartyPresence_CooldownRate_AppliesToBenchToo_NoDuplicateStacking()
    {
        var spec = new PassiveSpec
        {
            modifiers = new[] { new PassiveModifierSpec { source = ModifierSource.PartyPresence, stat = StatKind.CooldownRate, value = 1.15f, partyWide = true } },
        };
        var chronoA = Make(Pas("chrono_s4", spec));
        var chronoB = Make(Pas("chrono_s4", spec));                 // 같은 id 두 명
        var other   = Make(Plain("x", 10f));
        var party = new Party(new[] { other, chronoA, chronoB });
        other.StartCooldown(0, 10f);
        party.Tick(2f);
        Assert.AreEqual(10f - 2f * 1.15f, other.CooldownRemaining(0), 0.001f, "−15% 한 번만(중복 적용 없음), 대기 캐릭터도 해당");
    }

    // ════════════════════════════════════════════════════════════════════════
    // 마일스톤 · 초기화
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Milestone_Level3_ReplacesPerStackValue()
    {
        var spec = new PassiveSpec
        {
            trigger = PassiveTriggerType.OnHitTaken,
            stack = new StackSpec { stackId = "d", maxStacks = 5, perStackValue = -0.03f, milestonePerStackValue = -0.04f,
                                    decay = StackDecay.DecayOneEvery, decaySeconds = 3f, stat = StatKind.DamageTaken },
        };
        var skill = Pas("diel", spec);
        var c = Make(null, out var owned, skill);
        owned.GetSkillProgress(skill.id).level = 3;
        c.TakeHit(10);
        Assert.AreEqual(0.96f, c.StatMult(StatKind.DamageTaken), 0.0001f, "Lv.3: 스택당 −4%");
    }

    [Test]
    public void Downed_And_ResetCombatState_ClearPassiveStacks()
    {
        var c = Make(Impulse());
        c.NotifyHits(Hits(1, 1), false);
        c.ResetCombatState();
        Assert.AreEqual(0, c.PassiveStacks("impulse"));
        c.NotifyHits(Hits(1, 2), false);
        c.Damage(c.MaxHp);
        Assert.AreEqual(0, c.PassiveStacks("impulse"));
    }

    [Test]
    public void LockedPassive_DoesNothing()
    {
        var def = ScriptableObject.CreateInstance<CharacterDef>();
        def.id = "lk"; def.baseStats = new CharacterStats { hp = 100, atk = 10, def = 1, spd = 1, mp = 10 };
        def.skills = new[] { Plain("first"), Impulse() };            // 인덱스 1은 해금 안 함
        var c = new CombatCharacter(def, new OwnedCharacter { id = "lk", level = 1 });
        c.TickStatus(0f, true);
        c.NotifyHits(Hits(1, 1), false);
        Assert.AreEqual(0, c.PassiveStacks("impulse"));
    }
}
