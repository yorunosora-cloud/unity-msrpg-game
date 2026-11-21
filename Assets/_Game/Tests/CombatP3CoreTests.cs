using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>설계도 22 P3 — Stage A 순수 코어: FormMath · FormChangeResolver · FormAutoEvaluator.</summary>
public class CombatP3CoreTests
{
    // ════════════════════════════════════════════════════════════════════════
    // FormMath.CarryOver — HP/MP 비율 이월
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Carry_Full_StaysFull()
        => Assert.AreEqual(1700, FormMath.CarryOver(2975, 2975, 1700));

    [Test]
    public void Carry_Ratio_MassToInertia_FromDoc()
        => Assert.AreEqual(23, FormMath.CarryOver(40, 2975, 1700), "문서 예시: 40/2975 → 약 23");

    [Test]
    public void Carry_Alive_NeverRoundsToZero()
        => Assert.AreEqual(1, FormMath.CarryOver(1, 100000, 1000));

    [Test]
    public void Carry_Dead_StaysZero()
        => Assert.AreEqual(0, FormMath.CarryOver(0, 2975, 1700));

    [Test]
    public void Carry_SameMax_KeepsValue()
        => Assert.AreEqual(1234, FormMath.CarryOver(1234, 2125, 2125));

    // ════════════════════════════════════════════════════════════════════════
    // FormChangeResolver
    // ════════════════════════════════════════════════════════════════════════

    static FormContext Ctx(float sinceHit = 99f, bool moving = false)
        => new FormContext { secondsSinceHitTaken = sinceHit, isMoving = moving };

    // 루미에르/세미라: Toggle
    static FormChangeSpec Toggle() => new FormChangeSpec
        { keyIndex = 0, mode = FormChangeMode.Toggle, sequence = new[] { "Particle", "Wave" } };

    [Test]
    public void Toggle_SwapsBothWays()
    {
        Assert.AreEqual("Wave",     FormChangeResolver.Resolve(Toggle(), "Particle", "Particle", Ctx(), out _));
        Assert.AreEqual("Particle", FormChangeResolver.Resolve(Toggle(), "Wave",     "Particle", Ctx(), out _));
    }

    [Test]
    public void Toggle_UnknownCurrent_ReturnsNull()
        => Assert.IsNull(FormChangeResolver.Resolve(Toggle(), "Other", "Particle", Ctx(), out _));

    // 수소: Sequential
    static FormChangeSpec Neutron() => new FormChangeSpec
    {
        keyIndex = 0, mode = FormChangeMode.Sequential,
        sequence = new[] { "Protium", "Deuterium", "Tritium" },
        autoRevertForms = new[] { "Tritium" }, revertAfterSeconds = 12.3f, revertToForm = "Protium",
    };

    [Test]
    public void Sequential_AdvancesOneStep_AndStopsAtLast()
    {
        Assert.AreEqual("Deuterium", FormChangeResolver.Resolve(Neutron(), "Protium",   "Protium", Ctx(), out bool r1));
        Assert.IsFalse(r1);
        Assert.AreEqual("Tritium",   FormChangeResolver.Resolve(Neutron(), "Deuterium", "Protium", Ctx(), out _));
        Assert.IsNull(FormChangeResolver.Resolve(Neutron(), "Tritium", "Protium", Ctx(), out _), "마지막 폼에서는 사용 불가");
    }

    // 산소: Hub (Z=해리 → 원자 산소, X=결합 → 오존), 재입력 복귀
    static FormChangeSpec Dissociate() => new FormChangeSpec
    {
        keyIndex = 0, mode = FormChangeMode.Hub, fromForms = new[] { "O2" }, toForm = "O",
        revertAfterSeconds = 8f, revertToForm = "O2", pressAgainReverts = true,
    };

    [Test]
    public void Hub_FromHub_GoesToTarget()
        => Assert.AreEqual("O", FormChangeResolver.Resolve(Dissociate(), "O2", "O2", Ctx(), out bool rev) );

    [Test]
    public void Hub_PressAgain_FromAnyNonHubForm_RevertsToHub()
    {
        Assert.AreEqual("O2", FormChangeResolver.Resolve(Dissociate(), "O",  "O2", Ctx(), out bool r1));
        Assert.IsTrue(r1, "재입력 복귀는 isRevert");
        Assert.AreEqual("O2", FormChangeResolver.Resolve(Dissociate(), "O3", "O2", Ctx(), out bool r2), "오존에서 Z를 눌러도 복귀");
        Assert.IsTrue(r2);
    }

    [Test]
    public void Hub_NoPressAgain_FromOtherForm_ReturnsNull()
    {
        var spec = Dissociate(); spec.pressAgainReverts = false;
        Assert.IsNull(FormChangeResolver.Resolve(spec, "O", "O2", Ctx(), out _));
    }

    // 탄소: StateBased
    static FormChangeSpec Allotrope() => new FormChangeSpec
    {
        keyIndex = 0, mode = FormChangeMode.StateBased, fromForms = new[] { "Graphite" },
        pressAgainReverts = true, revertToForm = "Graphite",
        stateRules = new[]
        {
            new FormStateRule { cond = FormStateCond.RecentlyHit, param = 3f, toForm = "Diamond",   priority = 3 },
            new FormStateRule { cond = FormStateCond.Moving,                  toForm = "Graphene",  priority = 2 },
            new FormStateRule { cond = FormStateCond.Always,                  toForm = "Fullerene", priority = 1 },
        },
    };

    [Test]
    public void State_RecentlyHit_BeatsMoving()
        => Assert.AreEqual("Diamond", FormChangeResolver.Resolve(Allotrope(), "Graphite", "Graphite", Ctx(sinceHit: 1f, moving: true), out _));

    [Test]
    public void State_Moving_WhenNotRecentlyHit()
        => Assert.AreEqual("Graphene", FormChangeResolver.Resolve(Allotrope(), "Graphite", "Graphite", Ctx(sinceHit: 5f, moving: true), out _));

    [Test]
    public void State_Idle_FallsBackToAlways()
        => Assert.AreEqual("Fullerene", FormChangeResolver.Resolve(Allotrope(), "Graphite", "Graphite", Ctx(sinceHit: 5f, moving: false), out _));

    [Test]
    public void State_FromNonDefault_PressAgainRevertsToGraphite()
    {
        Assert.AreEqual("Graphite", FormChangeResolver.Resolve(Allotrope(), "Diamond", "Graphite", Ctx(), out bool rev));
        Assert.IsTrue(rev);
    }

    [Test]
    public void NullSpec_ReturnsNull()
        => Assert.IsNull(FormChangeResolver.Resolve(null, "A", "A", Ctx(), out _));

    // ════════════════════════════════════════════════════════════════════════
    // FormAutoEvaluator
    // ════════════════════════════════════════════════════════════════════════

    static CombatMotionState M(float ratio = 0f, bool moving = false, bool air = false, float vy = 0f)
        => new CombatMotionState { speedRatio = ratio, isMoving = moving, isAirborne = air, verticalSpeed = vy };

    // 에네르기아: 속도 교차(히스테리시스) + 점프 덮어쓰기
    static FormAutoRule[] Energia() => new[]
    {
        new FormAutoRule { cond = FormAutoCond.SpeedRatioAtLeast, param = 0.7f, toForm = "Kinetic",   priority = 1 },
        new FormAutoRule { cond = FormAutoCond.SpeedRatioBelow,   param = 0.4f, toForm = "Potential", priority = 1 },
        new FormAutoRule { cond = FormAutoCond.JumpApex,          param = 1.5f, toForm = "Potential", priority = 2 },
        new FormAutoRule { cond = FormAutoCond.JumpFalling,       param = 1.5f, toForm = "Kinetic",   priority = 3 },
    };

    [Test]
    public void Energia_FastMovement_SwitchesToKinetic()
        => Assert.AreEqual("Kinetic", new FormAutoEvaluator(Energia()).Evaluate(0.3f, M(0.8f, true), "Potential"));

    [Test]
    public void Energia_MiddleBand_KeepsCurrentForm_Hysteresis()
    {
        var ev = new FormAutoEvaluator(Energia());
        Assert.IsNull(ev.Evaluate(0.3f, M(0.5f, true), "Kinetic"));
        Assert.IsNull(ev.Evaluate(0.3f, M(0.5f, true), "Potential"));
    }

    [Test]
    public void Energia_Slow_SwitchesToPotential()
        => Assert.AreEqual("Potential", new FormAutoEvaluator(Energia()).Evaluate(0.3f, M(0.2f, false), "Kinetic"));

    [Test]
    public void Energia_JumpApex_OverridesFastSpeed()
        => Assert.AreEqual("Potential", new FormAutoEvaluator(Energia()).Evaluate(0.3f, M(0.9f, true, air: true, vy: 0.5f), "Kinetic"));

    [Test]
    public void Energia_JumpFalling_OverridesEverything()
        => Assert.AreEqual("Kinetic", new FormAutoEvaluator(Energia()).Evaluate(0.3f, M(0.1f, false, air: true, vy: -3f), "Potential"));

    [Test]
    public void Energia_HigherPriorityMatchEqualToCurrent_BlocksLowerRules()
    {
        // 정점(우선 2→Potential)에서 이미 Potential이면 속도 규칙(→Kinetic)이 끼어들면 안 된다
        var ev = new FormAutoEvaluator(Energia());
        Assert.IsNull(ev.Evaluate(0.3f, M(0.9f, true, air: true, vy: 0.2f), "Potential"));
    }

    [Test]
    public void MinHold_BlocksSwitchRightAfterAChange()
    {
        var ev = new FormAutoEvaluator(Energia());
        ev.NotifyFormChanged();
        Assert.IsNull(ev.Evaluate(0.1f, M(0.9f, true), "Potential"), "전환 직후 0.25초는 유지");
        Assert.AreEqual("Kinetic", ev.Evaluate(0.2f, M(0.9f, true), "Potential"));
    }

    // 마시오&이네르셔: 정지/이동 지속
    static FormAutoRule[] Inertia() => new[]
    {
        new FormAutoRule { cond = FormAutoCond.IdleFor,   param = 2f, toForm = "Mass",    priority = 1 },
        new FormAutoRule { cond = FormAutoCond.MovingFor, param = 2f, toForm = "Inertia", priority = 1 },
    };

    [Test]
    public void Inertia_IdleFor2Seconds_SwitchesToMass()
    {
        var ev = new FormAutoEvaluator(Inertia());
        Assert.IsNull(ev.Evaluate(1f, M(0f, false), "Inertia"));
        Assert.AreEqual("Mass", ev.Evaluate(1.1f, M(0f, false), "Inertia"));
    }

    [Test]
    public void Inertia_MovingInterruptsIdleCounter()
    {
        var ev = new FormAutoEvaluator(Inertia());
        ev.Evaluate(1.5f, M(0f, false), "Inertia");
        ev.Evaluate(0.1f, M(0.5f, true), "Inertia");      // 이동 → 정지 카운터 0
        Assert.IsNull(ev.Evaluate(1f, M(0f, false), "Inertia"), "다시 정지 1초뿐");
    }

    [Test]
    public void Inertia_MovingFor2Seconds_SwitchesToInertia()
    {
        var ev = new FormAutoEvaluator(Inertia());
        ev.Evaluate(1f, M(0.5f, true), "Mass");
        Assert.AreEqual("Inertia", ev.Evaluate(1.1f, M(0.5f, true), "Mass"));
    }

    [Test]
    public void OnSwitchIn_ResetsDurationCounters()
    {
        var ev = new FormAutoEvaluator(Inertia());
        ev.Evaluate(1.5f, M(0f, false), "Inertia");
        ev.OnSwitchIn();
        Assert.IsNull(ev.Evaluate(1f, M(0f, false), "Inertia"), "입장 시 카운터 0부터");
    }

    // 테르미아: 타격 대상 temp 연속 3회
    sealed class FakeAttrs : IEnemyAttributes
    {
        readonly Dictionary<string, float> _d = new();
        public FakeAttrs(float temp) { _d["temp"] = temp; }
        public bool TryGet(string key, out float v) => _d.TryGetValue(key, out v);
    }

    static FormAutoRule[] Termia() => new[]
    {
        new FormAutoRule { cond = FormAutoCond.HitTargetStat, statKey = "temp", statCompare = FormCompare.AtLeast,
                           statThreshold = 50f, consecutive = 3, toForm = "Hot",  priority = 1 },
        new FormAutoRule { cond = FormAutoCond.HitTargetStat, statKey = "temp", statCompare = FormCompare.Below,
                           statThreshold = 50f, consecutive = 3, toForm = "Cold", priority = 1 },
    };

    [Test]
    public void Termia_ThreeHotHits_SwitchesToHot()
    {
        var ev = new FormAutoEvaluator(Termia());
        ev.RegisterHit(new FakeAttrs(80)); ev.RegisterHit(new FakeAttrs(80));
        Assert.IsNull(ev.Evaluate(0.3f, M(), "Cold"), "2회는 부족");
        ev.RegisterHit(new FakeAttrs(80));
        Assert.AreEqual("Hot", ev.Evaluate(0.3f, M(), "Cold"));
    }

    [Test]
    public void Termia_OppositeHit_BreaksTheStreak()
    {
        var ev = new FormAutoEvaluator(Termia());
        ev.RegisterHit(new FakeAttrs(80)); ev.RegisterHit(new FakeAttrs(80));
        ev.RegisterHit(new FakeAttrs(10));                 // 반대 성질 → 뜨거운 연속 끊김
        ev.RegisterHit(new FakeAttrs(80));
        Assert.IsNull(ev.Evaluate(0.3f, M(), "Cold"));
    }

    [Test]
    public void Termia_FiringResetsStreaks()
    {
        var ev = new FormAutoEvaluator(Termia());
        for (int i = 0; i < 3; i++) ev.RegisterHit(new FakeAttrs(80));
        Assert.AreEqual("Hot", ev.Evaluate(0.3f, M(), "Cold"));
        Assert.IsNull(ev.Evaluate(0.3f, M(), "Cold"), "발동 후 카운터 초기화 — 다시 3회 필요");
    }

    [Test]
    public void Termia_MissingStat_ResetsStreak()
    {
        var ev = new FormAutoEvaluator(Termia());
        ev.RegisterHit(new FakeAttrs(80)); ev.RegisterHit(new FakeAttrs(80));
        ev.RegisterHit(null);                               // 속성 없는 적
        ev.RegisterHit(new FakeAttrs(80));
        Assert.IsNull(ev.Evaluate(0.3f, M(), "Cold"));
    }

    [Test]
    public void OnSwitchIn_ResetsHitStreaks()
    {
        var ev = new FormAutoEvaluator(Termia());
        ev.RegisterHit(new FakeAttrs(80)); ev.RegisterHit(new FakeAttrs(80));
        ev.OnSwitchIn();
        ev.RegisterHit(new FakeAttrs(80));
        Assert.IsNull(ev.Evaluate(0.3f, M(), "Cold"));
    }

    // ════════════════════════════════════════════════════════════════════════
    // 데이터 필드 (CharacterDef / SkillDef)
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Defs_HaveFormFields_WithSafeDefaults()
    {
        var def = ScriptableObject.CreateInstance<CharacterDef>();
        Assert.IsTrue(def.forms == null || def.forms.Length == 0);
        Assert.IsTrue(def.autoRules == null || def.autoRules.Length == 0);
        var skill = ScriptableObject.CreateInstance<SkillDef>();
        Assert.IsNull(skill.formChange);
    }
}
