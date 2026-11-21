using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>설계도 22 P2 — Stage A 순수 코어: StatusHolder · SkillShapes · TargetResolver.</summary>
public class CombatP2CoreTests
{
    // ════════════════════════════════════════════════════════════════════════
    // StatusHolder
    // ════════════════════════════════════════════════════════════════════════

    static StatusEffect Buff(string id, float mult, float dur, StatKind stat = StatKind.Atk, int maxStacks = 1)
        => new StatusEffect { Id = id, Kind = StatusKind.StatMod, Stat = stat, Value = mult,
                              Duration = dur, IsBeneficial = true, MaxStacks = maxStacks };

    static StatusEffect Debuff(string id, float mult, float dur, StatKind stat)
        => new StatusEffect { Id = id, Kind = StatusKind.StatMod, Stat = stat, Value = mult,
                              Duration = dur, IsBeneficial = false };

    [Test]
    public void Status_Mult_NoEffects_IsOne()
    {
        var h = new StatusHolder();
        Assert.AreEqual(1f, h.Mult(StatKind.Atk), 0.0001f);
    }

    [Test]
    public void Status_Buff_Active_AppliesImmediately_AndExpires()
    {
        var h = new StatusHolder();
        h.Apply(Buff("b", 1.5f, 3f), ownerActive: true);
        Assert.AreEqual(1.5f, h.Mult(StatKind.Atk), 0.0001f);
        h.Tick(2f, true, null);
        Assert.AreEqual(1.5f, h.Mult(StatKind.Atk), 0.0001f);
        h.Tick(1.5f, true, null);
        Assert.AreEqual(1f, h.Mult(StatKind.Atk), 0.0001f, "만료 후 배율 복귀");
    }

    [Test]
    public void Status_Mult_DifferentStats_AreIndependent_AndMultiply()
    {
        var h = new StatusHolder();
        h.Apply(Buff("a", 1.5f, 5f, StatKind.Atk), true);
        h.Apply(Buff("a2", 1.2f, 5f, StatKind.Atk), true);
        h.Apply(Buff("d", 2f, 5f, StatKind.Def), true);
        Assert.AreEqual(1.8f, h.Mult(StatKind.Atk), 0.0001f, "다른 id 버프는 공존(곱)");
        Assert.AreEqual(2f, h.Mult(StatKind.Def), 0.0001f);
        Assert.AreEqual(1f, h.Mult(StatKind.MoveSpeed), 0.0001f);
    }

    [Test]
    public void Status_SameId_Refreshes_TakesStrongerValue_AndLongerDuration()
    {
        var h = new StatusHolder();
        h.Apply(Buff("b", 1.3f, 5f), true);
        h.Apply(Buff("b", 1.5f, 2f), true);
        Assert.AreEqual(1.5f, h.Mult(StatKind.Atk), 0.0001f, "더 강한 수치");
        h.Tick(4f, true, null);
        Assert.AreEqual(1.5f, h.Mult(StatKind.Atk), 0.0001f, "더 긴 시간(5초) 유지");
        Assert.AreEqual(1, h.Count, "중복되지 않는다");
    }

    [Test]
    public void Status_Debuff_StrongerMeansFartherBelowOne()
    {
        var h = new StatusHolder();
        h.Apply(Debuff("s", 0.7f, 5f, StatKind.MoveSpeed), false);
        h.Apply(Debuff("s", 0.5f, 5f, StatKind.MoveSpeed), false);
        Assert.AreEqual(0.5f, h.Mult(StatKind.MoveSpeed), 0.0001f);
    }

    [Test]
    public void Status_Stacks_AddUntilCap_AndScaleMult()
    {
        var h = new StatusHolder();
        for (int i = 0; i < 5; i++) h.Apply(Buff("ox", 1.1f, 5f, StatKind.Atk, maxStacks: 3), true);
        Assert.AreEqual(3, h.Stacks("ox"));
        Assert.AreEqual(1.3f, h.Mult(StatKind.Atk), 0.0001f, "1 + (1.1-1) × 3");
    }

    [Test]
    public void Status_BuffOnBench_IsPending_NoEffect_UntilSwitchIn()
    {
        var h = new StatusHolder();
        h.Apply(Buff("b", 1.5f, 3f), ownerActive: false);
        Assert.AreEqual(1f, h.Mult(StatKind.Atk), 0.0001f, "예약 상태에서는 효과 없음");
        h.Tick(2f, false, null);                       // 대기 중에는 시간이 흐르지 않는다
        h.OnSwitchIn();
        Assert.AreEqual(1.5f, h.Mult(StatKind.Atk), 0.0001f);
        h.Tick(2f, true, null);
        Assert.AreEqual(1.5f, h.Mult(StatKind.Atk), 0.0001f, "전환 시점부터 3초 (대기 중 소모 없음)");
        h.Tick(1.5f, true, null);
        Assert.AreEqual(1f, h.Mult(StatKind.Atk), 0.0001f);
    }

    [Test]
    public void Status_PendingBuff_StartsOnFirstActiveTick_WithoutConsumingThatTick()
    {
        var h = new StatusHolder();
        h.Apply(Buff("b", 1.5f, 3f), false);
        h.Tick(1f, true, null);                        // 활성 첫 틱: 시작만, 시간 소모 없음
        h.Tick(2.5f, true, null);
        Assert.AreEqual(1.5f, h.Mult(StatKind.Atk), 0.0001f);
    }

    [Test]
    public void Status_PendingBuff_ExpiresAfter60RealSeconds()
    {
        var h = new StatusHolder();
        h.Apply(Buff("b", 1.5f, 3f), false);
        h.Tick(59f, false, null);
        Assert.AreEqual(1, h.Count);
        h.Tick(2f, false, null);
        Assert.AreEqual(0, h.Count, "예약 60초 후 소멸");
    }

    [Test]
    public void Status_ActiveBuff_PausesWhenBenched_ResumesWhenActive()
    {
        var h = new StatusHolder();
        h.Apply(Buff("b", 1.5f, 3f), true);
        h.Tick(1f, true, null);
        h.Tick(10f, false, null);                      // 대기 중: 일시정지
        Assert.AreEqual(1.5f, h.Mult(StatKind.Atk), 0.0001f);
        h.Tick(1.5f, true, null);
        Assert.AreEqual(1.5f, h.Mult(StatKind.Atk), 0.0001f, "남은 2초 중 1.5초 소모");
        h.Tick(1f, true, null);
        Assert.AreEqual(1f, h.Mult(StatKind.Atk), 0.0001f);
    }

    [Test]
    public void Status_Debuff_ExpiresInRealTime_EvenWhenBenched()
    {
        var h = new StatusHolder();
        h.Apply(Debuff("s", 0.5f, 3f, StatKind.MoveSpeed), false);
        Assert.AreEqual(0.5f, h.Mult(StatKind.MoveSpeed), 0.0001f, "해로운 효과는 예약 없이 즉시 적용");
        h.Tick(3.5f, false, null);
        Assert.AreEqual(1f, h.Mult(StatKind.MoveSpeed), 0.0001f);
    }

    [Test]
    public void Status_Dot_DealsWholeDamagePerSecond_WithFractionAccumulation()
    {
        var h = new StatusHolder();
        h.Apply(new StatusEffect { Id = "burn", Kind = StatusKind.Dot, Value = 10f, Duration = 3f }, false);
        int total = 0;
        for (int i = 0; i < 30; i++) h.Tick(0.1f, false, d => total += d);   // 3초
        Assert.AreEqual(30, total, "10/초 × 3초");
        Assert.AreEqual(0, h.Count, "만료");
    }

    [Test]
    public void Status_Stun_IsStunned_UntilExpiry()
    {
        var h = new StatusHolder();
        h.Apply(new StatusEffect { Id = "stun", Kind = StatusKind.Stun, Duration = 1f }, false);
        Assert.IsTrue(h.IsStunned);
        h.Tick(1.1f, false, null);
        Assert.IsFalse(h.IsStunned);
    }

    [Test]
    public void Status_Shield_AbsorbsDamage_ThenBreaks()
    {
        var h = new StatusHolder();
        h.Apply(new StatusEffect { Id = "sh", Kind = StatusKind.Shield, Value = 30f, Duration = 5f, IsBeneficial = true }, true);
        Assert.AreEqual(0, h.Absorb(20));
        Assert.AreEqual(10f, h.ShieldAmount, 0.0001f);
        Assert.AreEqual(15, h.Absorb(25), "보호막 10 소진 → 15 관통");
        Assert.AreEqual(0f, h.ShieldAmount, 0.0001f);
        Assert.AreEqual(0, h.Count, "소진된 보호막은 제거");
    }

    [Test]
    public void Status_Cleanse_RemovesHarmful_KeepsBeneficial()
    {
        var h = new StatusHolder();
        h.Apply(Buff("b", 1.5f, 5f), true);
        h.Apply(Debuff("s", 0.5f, 5f, StatKind.MoveSpeed), true);
        h.Apply(new StatusEffect { Id = "burn", Kind = StatusKind.Dot, Value = 5f, Duration = 5f }, true);
        h.Cleanse();
        Assert.AreEqual(1, h.Count);
        Assert.AreEqual(1.5f, h.Mult(StatKind.Atk), 0.0001f);
    }

    [Test]
    public void Status_Clear_RemovesEverything()
    {
        var h = new StatusHolder();
        h.Apply(Buff("b", 1.5f, 5f), false);
        h.Apply(Buff("c", 1.5f, 5f), true);
        h.Clear();
        Assert.AreEqual(0, h.Count);
    }

    // ════════════════════════════════════════════════════════════════════════
    // SkillShapes
    // ════════════════════════════════════════════════════════════════════════

    static readonly Vector3 O = Vector3.zero;
    static readonly Vector3 F = Vector3.forward;   // +Z

    [Test]
    public void Shape_Cone_InsideAndOutside()
    {
        Assert.IsTrue (SkillShapes.Contains(SkillShape.Cone, O, F, new Vector3(0, 0, 2), 3f, 60f, 0f, 0f, out _));
        Assert.IsFalse(SkillShapes.Contains(SkillShape.Cone, O, F, new Vector3(0, 0, 4), 3f, 60f, 0f, 0f, out _), "사거리 밖");
        Assert.IsFalse(SkillShapes.Contains(SkillShape.Cone, O, F, new Vector3(2, 0, 0.5f), 3f, 60f, 0f, 0f, out _), "각도 밖");
    }

    [Test]
    public void Shape_Cone_IgnoresVerticalDifference()
    {
        Assert.IsTrue(SkillShapes.Contains(SkillShape.Cone, O, F, new Vector3(0, 5, 2), 3f, 60f, 0f, 0f, out float d));
        Assert.AreEqual(2f, d, 0.0001f);
    }

    [Test]
    public void Shape_Line_UsesLengthAndHalfWidth()
    {
        Assert.IsTrue (SkillShapes.Contains(SkillShape.Line, O, F, new Vector3(0.4f, 0, 5), 8f, 0f, 1f, 0f, out _), "폭 1 → 좌우 0.5");
        Assert.IsFalse(SkillShapes.Contains(SkillShape.Line, O, F, new Vector3(0.6f, 0, 5), 8f, 0f, 1f, 0f, out _), "폭 밖");
        Assert.IsFalse(SkillShapes.Contains(SkillShape.Line, O, F, new Vector3(0, 0, 9), 8f, 0f, 1f, 0f, out _), "길이 밖");
        Assert.IsFalse(SkillShapes.Contains(SkillShape.Line, O, F, new Vector3(0, 0, -1), 8f, 0f, 1f, 0f, out _), "뒤쪽");
    }

    [Test]
    public void Shape_Circle_IsCenteredOnCaster_IgnoresFacing()
    {
        Assert.IsTrue (SkillShapes.Contains(SkillShape.Circle, O, F, new Vector3(0, 0, -2), 0f, 0f, 0f, 3f, out _), "뒤쪽도 포함");
        Assert.IsFalse(SkillShapes.Contains(SkillShape.Circle, O, F, new Vector3(0, 0, 3.5f), 0f, 0f, 0f, 3f, out _));
    }

    // ════════════════════════════════════════════════════════════════════════
    // TargetResolver
    // ════════════════════════════════════════════════════════════════════════

    static CombatCharacter MakeChar(string name)
    {
        var def = ScriptableObject.CreateInstance<CharacterDef>();
        def.id = name; def.nameKo = name; def.rarity = Rarity.N; def.continent = Continent.Physics;
        def.skills = new SkillDef[0];
        def.baseStats = new CharacterStats { hp = 100, atk = 10, def = 1, spd = 1, mp = 50 };
        return new CombatCharacter(def, new OwnedCharacter { id = name, level = 1, exp = 0 });
    }

    static (Party party, CombatCharacter[] m) MakeParty(int n)
    {
        var m = new CombatCharacter[n];
        for (int i = 0; i < n; i++) m[i] = MakeChar("c" + i);
        return (new Party(m), m);
    }

    [Test]
    public void Target_Self_ReturnsCaster()
    {
        var (p, m) = MakeParty(3);
        var r = TargetResolver.Resolve(p, m[0], TargetScope.Self, 0);
        Assert.AreEqual(1, r.Count);
        Assert.AreSame(m[0], r[0]);
    }

    [Test]
    public void Target_EnemyArea_ReturnsNoAllies()
    {
        var (p, m) = MakeParty(3);
        Assert.AreEqual(0, TargetResolver.Resolve(p, m[0], TargetScope.EnemyArea, 0).Count);
    }

    [Test]
    public void Target_Bench_SortsByLowestHpFraction_ExcludesDownedAndSelf()
    {
        var (p, m) = MakeParty(4);
        m[1].Damage(10);                       // 90%
        m[2].Damage(60);                       // 40%
        m[3].Damage(m[3].MaxHp);               // 기절
        var r = TargetResolver.Resolve(p, m[0], TargetScope.Bench, 0);
        Assert.AreEqual(2, r.Count);
        Assert.AreSame(m[2], r[0], "HP 비율 낮은 순");
        Assert.AreSame(m[1], r[1]);
    }

    [Test]
    public void Target_Bench_CapOne_PicksLowest()
    {
        var (p, m) = MakeParty(3);
        m[2].Damage(50);
        var r = TargetResolver.Resolve(p, m[0], TargetScope.Bench, 1);
        Assert.AreEqual(1, r.Count);
        Assert.AreSame(m[2], r[0]);
    }

    [Test]
    public void Target_Bench_CapOne_NoBench_FallsBackToSelf()
    {
        var (p, m) = MakeParty(1);
        var r = TargetResolver.Resolve(p, m[0], TargetScope.Bench, 1);
        Assert.AreEqual(1, r.Count);
        Assert.AreSame(m[0], r[0], "fallbackSelf");
    }

    [Test]
    public void Target_Bench_NoCap_NoBench_IsEmpty()
    {
        var (p, m) = MakeParty(1);
        Assert.AreEqual(0, TargetResolver.Resolve(p, m[0], TargetScope.Bench, 0).Count);
    }

    [Test]
    public void Target_SelfAndBench_CapIncludesSelf()
    {
        var (p, m) = MakeParty(5);
        m[1].Damage(10); m[2].Damage(50); m[3].Damage(30); m[4].Damage(20);
        var r = TargetResolver.Resolve(p, m[0], TargetScope.SelfAndBench, 3);
        Assert.AreEqual(3, r.Count, "자신 포함 최대 3명");
        Assert.AreSame(m[0], r[0], "자신이 항상 포함");
        Assert.AreSame(m[2], r[1], "대기 아군은 HP 비율 낮은 순");
        Assert.AreSame(m[3], r[2]);
    }

    [Test]
    public void Target_SelfAndBench_NoBench_OnlySelf()
    {
        var (p, m) = MakeParty(1);
        var r = TargetResolver.Resolve(p, m[0], TargetScope.SelfAndBench, 4);
        Assert.AreEqual(1, r.Count);
    }

    [Test]
    public void Target_NullParty_Self_AndFallback()
    {
        var c = MakeChar("solo");
        Assert.AreEqual(1, TargetResolver.Resolve(null, c, TargetScope.Self, 0).Count);
        Assert.AreEqual(1, TargetResolver.Resolve(null, c, TargetScope.SelfAndBench, 3).Count);
        Assert.AreEqual(1, TargetResolver.Resolve(null, c, TargetScope.Bench, 1).Count);
        Assert.AreEqual(0, TargetResolver.Resolve(null, c, TargetScope.Bench, 0).Count);
    }
}
