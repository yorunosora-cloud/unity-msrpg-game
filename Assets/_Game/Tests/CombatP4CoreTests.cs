using NUnit.Framework;
using UnityEngine;

/// <summary>설계도 22 P4 — Stage A 순수 코어: PassiveFormula · StackTracker · ModifierState · CombatAnchor · 타입.</summary>
public class CombatP4CoreTests
{
    // ════════════════════════════════════════════════════════════════════════
    // PassiveFormula
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Lorentz_FromDoc_Examples()
    {
        Assert.AreEqual(1f,    PassiveFormula.Lorentz(0f, 2000f), 0.0001f);
        Assert.AreEqual(1.005f, PassiveFormula.Lorentz(200f, 2000f), 0.001f, "v/c=0.1 → γ≈1.005");
        Assert.AreEqual(3.20f,  PassiveFormula.Lorentz(1900f, 2000f), 0.01f, "v/c=0.95 → γ≈3.20");
    }

    [Test]
    public void Lorentz_AtOrAboveC_DoesNotDiverge()
    {
        float g = PassiveFormula.Lorentz(5000f, 2000f);
        Assert.IsFalse(float.IsInfinity(g) || float.IsNaN(g));
        Assert.Greater(g, 10f);
    }

    [Test]
    public void Doppler_ApproachStronger_RecedeWeaker()
    {
        Assert.AreEqual(1f, PassiveFormula.Doppler(0f, 2000f), 0.0001f);
        Assert.AreEqual(Mathf.Sqrt(3f), PassiveFormula.Doppler(1000f, 2000f), 0.001f, "β=+0.5 → √3");
        Assert.AreEqual(1f / Mathf.Sqrt(3f), PassiveFormula.Doppler(-1000f, 2000f), 0.001f, "β=−0.5 → 1/√3");
    }

    [Test]
    public void Doppler_IsClampedTo033And3()
    {
        Assert.AreEqual(3.0f,  PassiveFormula.Doppler(1900f, 2000f), 0.0001f);
        Assert.AreEqual(0.33f, PassiveFormula.Doppler(-1900f, 2000f), 0.0001f);
    }

    [Test]
    public void RangeWork_FivePercentPerUnit_CappedAtFifty()
    {
        Assert.AreEqual(1f,   PassiveFormula.RangeWork(0f), 0.0001f);
        Assert.AreEqual(1.2f, PassiveFormula.RangeWork(4f), 0.0001f);
        Assert.AreEqual(1.5f, PassiveFormula.RangeWork(20f), 0.0001f);
    }

    [Test]
    public void AxisDistance_LinearUpToFive_ThenCapped()
    {
        Assert.AreEqual(1f,    PassiveFormula.AxisDistance(0f, 5f, 0.3f), 0.0001f);
        Assert.AreEqual(1.15f, PassiveFormula.AxisDistance(2.5f, 5f, 0.3f), 0.0001f);
        Assert.AreEqual(1.3f,  PassiveFormula.AxisDistance(7f, 5f, 0.3f), 0.0001f);
        Assert.AreEqual(1.4f,  PassiveFormula.AxisDistance(5f, 5f, 0.4f), 0.0001f, "마일스톤 +40%");
    }

    // ════════════════════════════════════════════════════════════════════════
    // StackSpec.Resolve (마일스톤 교체값)
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void StackSpec_Resolve_AppliesMilestoneReplacements_OnlyWhenActive()
    {
        var s = new StackSpec { maxStacks = 5, perStackValue = 0.03f, milestonePerStackValue = 0.04f, milestoneMaxStacks = 6 };
        Assert.AreEqual(0.03f, s.Resolve(false).perStackValue, 0.0001f);
        Assert.AreEqual(5, s.Resolve(false).maxStacks);
        Assert.AreEqual(0.04f, s.Resolve(true).perStackValue, 0.0001f);
        Assert.AreEqual(6, s.Resolve(true).maxStacks);
    }

    [Test]
    public void StackSpec_Resolve_ZeroMilestone_MeansNoReplacement()
    {
        var s = new StackSpec { maxStacks = 3, perStackValue = 0.1f };
        Assert.AreEqual(0.1f, s.Resolve(true).perStackValue, 0.0001f);
        Assert.AreEqual(3, s.Resolve(true).maxStacks);
    }

    // ════════════════════════════════════════════════════════════════════════
    // StackTracker
    // ════════════════════════════════════════════════════════════════════════

    static StackSpec Spec(int max, float per, StackDecay decay, float secs)
        => new StackSpec { stackId = "s", maxStacks = max, perStackValue = per, decay = decay, decaySeconds = secs };

    [Test]
    public void Stack_AddsUpToMax_AmountScales()
    {
        var t = new StackTracker(Spec(5, 0.08f, StackDecay.ResetAfterIdle, 3f));
        for (int i = 0; i < 9; i++) t.Add();
        Assert.AreEqual(5, t.Stacks);
        Assert.AreEqual(0.4f, t.Amount, 0.0001f);
    }

    [Test]
    public void Stack_ResetAfterIdle_ClearsAllAfterSeconds()
    {
        var t = new StackTracker(Spec(5, 0.08f, StackDecay.ResetAfterIdle, 3f));
        t.Add(); t.Add(); t.Add();
        t.Tick(2.9f, true);
        Assert.AreEqual(3, t.Stacks);
        t.Tick(0.2f, true);
        Assert.AreEqual(0, t.Stacks, "3초 무적중 → 전체 초기화");
    }

    [Test]
    public void Stack_NewHit_RestartsIdleTimer()
    {
        var t = new StackTracker(Spec(5, 0.08f, StackDecay.ResetAfterIdle, 3f));
        t.Add(); t.Tick(2.5f, true);
        t.Add(); t.Tick(2.5f, true);
        Assert.AreEqual(2, t.Stacks, "추가로 타이머가 재시작되어 아직 유지");
    }

    [Test]
    public void Stack_DecayOneEvery_DropsOneStackPerInterval()
    {
        var t = new StackTracker(Spec(5, 0.03f, StackDecay.DecayOneEvery, 3f));
        for (int i = 0; i < 4; i++) t.Add();
        t.Tick(3.1f, true);
        Assert.AreEqual(3, t.Stacks);
        t.Tick(3.1f, true);
        Assert.AreEqual(2, t.Stacks);
    }

    [Test]
    public void Stack_TimersFreeze_WhenNotActive_ButStacksStay()
    {
        var t = new StackTracker(Spec(5, 0.08f, StackDecay.ResetAfterIdle, 3f));
        t.Add(); t.Add();
        t.Tick(100f, false);
        Assert.AreEqual(2, t.Stacks, "대기 중에는 시간이 흐르지 않고 스택 유지");
        t.Tick(2.9f, true);
        Assert.AreEqual(2, t.Stacks);
        t.Tick(0.2f, true);
        Assert.AreEqual(0, t.Stacks);
    }

    [Test]
    public void Stack_ConsumeAtCap_OnlyWhenFull()
    {
        var t = new StackTracker(Spec(4, 0f, StackDecay.ConsumeAtCap, 0f));
        t.Add(); t.Add(); t.Add();
        Assert.IsFalse(t.TryConsume());
        t.Add();
        Assert.IsTrue(t.IsFull);
        Assert.IsTrue(t.TryConsume());
        Assert.AreEqual(0, t.Stacks);
    }

    [Test]
    public void Stack_ConsumeAtCap_DoesNotTimeOut()
    {
        var t = new StackTracker(Spec(4, 0f, StackDecay.ConsumeAtCap, 0f));
        t.Add(); t.Add();
        t.Tick(60f, true);
        Assert.AreEqual(2, t.Stacks);
    }

    [Test]
    public void Stack_DamageRatio_AccumulatesCappedAmount()
    {
        // 운동량: 가한 피해의 10% ÷ 기본 공격력, 최대 +30%, 5초 유지
        var spec = new StackSpec { stackId = "m", damageRatio = 0.1f, cap = 0.3f, decay = StackDecay.ResetAfterIdle, decaySeconds = 5f };
        var t = new StackTracker(spec);
        t.AddDamage(damage: 100, baseAtk: 200);          // 100×0.1/200 = 0.05
        Assert.AreEqual(0.05f, t.Amount, 0.0001f);
        t.AddDamage(1000, 200);                          // 0.5 → 상한 0.3
        Assert.AreEqual(0.3f, t.Amount, 0.0001f);
        t.Tick(5.1f, true);
        Assert.AreEqual(0f, t.Amount, 0.0001f);
    }

    [Test]
    public void Stack_Clear_Resets()
    {
        var t = new StackTracker(Spec(5, 0.08f, StackDecay.ResetAfterIdle, 3f));
        t.Add(); t.Clear();
        Assert.AreEqual(0, t.Stacks);
    }

    // ════════════════════════════════════════════════════════════════════════
    // ModifierState
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Modifier_Constant_ReturnsValue()
    {
        var m = new ModifierState(new PassiveModifierSpec { source = ModifierSource.Constant, stat = StatKind.Def, value = 1.2f });
        Assert.AreEqual(1.2f, m.Multiplier(0f), 0.0001f);
    }

    [Test]
    public void Modifier_LostHpPercent_ReducesDamageTakenWithCap()
    {
        // 수압: 잃은 HP% × 0.5%, 최대 30%
        var m = new ModifierState(new PassiveModifierSpec
            { source = ModifierSource.LostHpPercent, stat = StatKind.DamageTaken, value = 0.005f, cap = 0.3f });
        Assert.AreEqual(1f,   m.Multiplier(0f),   0.0001f);
        Assert.AreEqual(0.8f, m.Multiplier(40f),  0.0001f, "40% 잃음 → −20%");
        Assert.AreEqual(0.7f, m.Multiplier(100f), 0.0001f, "상한 −30%");
    }

    [Test]
    public void Modifier_MovingRamp_RisesWhileMoving_FallsWhenStopped()
    {
        // 등속운동: 이동 중 초당 +3%(최대 +30%), 정지 시 초당 −5%
        var m = new ModifierState(new PassiveModifierSpec
            { source = ModifierSource.MovingRamp, stat = StatKind.MoveSpeed, value = 0.03f, cap = 0.3f, decayPerSecond = 0.05f });
        m.Tick(5f, true);
        Assert.AreEqual(1.15f, m.Multiplier(0f), 0.0001f);
        m.Tick(20f, true);
        Assert.AreEqual(1.3f, m.Multiplier(0f), 0.0001f, "10초 도달 시 상한 +30%");
        m.Tick(2f, false);
        Assert.AreEqual(1.2f, m.Multiplier(0f), 0.0001f, "정지 2초 → −10%");
        m.Tick(100f, false);
        Assert.AreEqual(1f, m.Multiplier(0f), 0.0001f, "0 아래로 내려가지 않음");
    }

    [Test]
    public void Modifier_PartyPresence_ReturnsValue_AndFlagsPartyWide()
    {
        var spec = new PassiveModifierSpec { source = ModifierSource.PartyPresence, stat = StatKind.CooldownRate, value = 1.15f, partyWide = true };
        var m = new ModifierState(spec);
        Assert.AreEqual(1.15f, m.Multiplier(0f), 0.0001f);
        Assert.IsTrue(m.IsPartyWide);
    }

    [Test]
    public void Modifier_Milestone_ReplacesValueAndCap()
    {
        var spec = new PassiveModifierSpec { source = ModifierSource.Constant, stat = StatKind.Atk, value = 1.3f, milestoneValue = 1.4f };
        Assert.AreEqual(1.3f, new ModifierState(spec, false).Multiplier(0f), 0.0001f);
        Assert.AreEqual(1.4f, new ModifierState(spec, true).Multiplier(0f), 0.0001f);
    }

    // ════════════════════════════════════════════════════════════════════════
    // CombatAnchor
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Anchor_FirstCombat_RecordsPosition()
    {
        var a = new CombatAnchor();
        Assert.IsFalse(a.HasAnchor);
        a.NotifyCombat(new Vector3(1, 0, 2));
        Assert.IsTrue(a.HasAnchor);
        Assert.AreEqual(new Vector3(1, 0, 2), a.StartPosition);
    }

    [Test]
    public void Anchor_ContinuousCombat_KeepsOriginalStart()
    {
        var a = new CombatAnchor();
        a.NotifyCombat(new Vector3(1, 0, 2));
        a.Tick(3f);
        a.NotifyCombat(new Vector3(9, 0, 9));
        Assert.AreEqual(new Vector3(1, 0, 2), a.StartPosition, "5초 안에 이어진 전투는 시작 지점 유지");
    }

    [Test]
    public void Anchor_AfterFiveQuietSeconds_NewEngagementMovesStart()
    {
        var a = new CombatAnchor();
        a.NotifyCombat(new Vector3(1, 0, 2));
        a.Tick(5.1f);
        a.NotifyCombat(new Vector3(9, 0, 9));
        Assert.AreEqual(new Vector3(9, 0, 9), a.StartPosition);
    }

    [Test]
    public void Anchor_Clear_ForgetsPosition()
    {
        var a = new CombatAnchor();
        a.NotifyCombat(Vector3.one);
        a.Clear();
        Assert.IsFalse(a.HasAnchor);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 타입 기본값
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Types_HaveNewFields()
    {
        var skill = ScriptableObject.CreateInstance<SkillDef>();
        Assert.AreEqual(PassiveTriggerType.None, skill.passive.trigger);
        var e = new SkillEffectEntry { type = SkillEffectType.Teleport, target = TargetScope.HitTarget, areaRadius = 1f, milestoneValue = 2f, milestoneDuration = 3f };
        Assert.AreEqual(1f, e.areaRadius);
        Assert.IsFalse(new StatusEffect().ConsumeOnHit);
        Assert.AreNotEqual(StatKind.Atk, StatKind.SkillPower);
        Assert.AreNotEqual(StatKind.Atk, StatKind.HitDamage);
        Assert.AreEqual(TargetScope.Attacker, TargetScope.Attacker);
    }

    [Test]
    public void TargetResolver_HitTargetAndAttacker_ReturnNoAllies()
    {
        var def = ScriptableObject.CreateInstance<CharacterDef>();
        def.id = "x"; def.baseStats = new CharacterStats { hp = 10, atk = 1, def = 1, spd = 1, mp = 1 };
        def.skills = new SkillDef[0];
        var c = new CombatCharacter(def, new OwnedCharacter { id = "x", level = 1 });
        var p = new Party(new[] { c });
        Assert.AreEqual(0, TargetResolver.Resolve(p, c, TargetScope.HitTarget, 0).Count);
        Assert.AreEqual(0, TargetResolver.Resolve(p, c, TargetScope.Attacker, 0).Count);
    }
}
