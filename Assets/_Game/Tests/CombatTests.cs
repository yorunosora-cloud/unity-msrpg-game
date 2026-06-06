using NUnit.Framework;
using UnityEngine;

/// <summary>전투 로직 EditMode 테스트 (CombatMath, EnemyUnit).</summary>
public class CombatTests
{
    // ════════════════════════════════════════════════════════════════════════
    // CombatMath.ComputeDamage
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void ComputeDamage_NormalHit_ReturnsAtkMinusDef()
    {
        int result = CombatMath.ComputeDamage(atk: 15, def: 4, isWeakness: false);
        Assert.AreEqual(11, result);
    }

    [Test]
    public void ComputeDamage_WhenDefExceedsAtk_ReturnsMinimumOne()
    {
        int result = CombatMath.ComputeDamage(atk: 5, def: 20, isWeakness: false);
        Assert.AreEqual(1, result);
    }

    [Test]
    public void ComputeDamage_WeaknessHit_AppliesCritMultiplier()
    {
        // (15-4) * 1.8 = 19.8 → RoundToInt = 20
        int result = CombatMath.ComputeDamage(atk: 15, def: 4, isWeakness: true);
        Assert.AreEqual(20, result);
    }

    [Test]
    public void ComputeDamage_WeaknessHitWhenDefExceedsAtk_ReturnsAtLeastOne()
    {
        // base = max(1, 0) = 1, * 1.8 → 2 (roundToInt)
        int result = CombatMath.ComputeDamage(atk: 3, def: 100, isWeakness: true);
        Assert.GreaterOrEqual(result, 1);
    }

    [Test]
    public void ComputeDamage_WeaknessDamage_IsHigherThanNormal()
    {
        int normal = CombatMath.ComputeDamage(atk: 15, def: 4, isWeakness: false);
        int crit   = CombatMath.ComputeDamage(atk: 15, def: 4, isWeakness: true);
        Assert.Greater(crit, normal);
    }

    // ════════════════════════════════════════════════════════════════════════
    // EnemyUnit
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void EnemyUnit_StartsAtFullHp()
    {
        var unit = new EnemyUnit(maxHp: 60, def: 4, weakness: Continent.Physics, expReward: 40);
        Assert.AreEqual(60, unit.Hp);
        Assert.AreEqual(60, unit.MaxHp);
    }

    [Test]
    public void EnemyUnit_TakeDamage_ReducesHp()
    {
        var unit = new EnemyUnit(maxHp: 60, def: 4, weakness: Continent.Physics, expReward: 40);
        int dealt = unit.TakeDamage(11);
        Assert.AreEqual(49, unit.Hp);
        Assert.AreEqual(11, dealt);
    }

    [Test]
    public void EnemyUnit_TakeDamage_ClampsToZero()
    {
        var unit = new EnemyUnit(maxHp: 60, def: 4, weakness: Continent.Physics, expReward: 40);
        int dealt = unit.TakeDamage(999);
        Assert.AreEqual(0, unit.Hp);
        Assert.AreEqual(60, dealt); // 실제 깎인 양 = 60 (max hp)
    }

    [Test]
    public void EnemyUnit_IsDead_TrueWhenHpIsZero()
    {
        var unit = new EnemyUnit(maxHp: 60, def: 4, weakness: Continent.Physics, expReward: 40);
        Assert.IsFalse(unit.IsDead);
        unit.TakeDamage(999);
        Assert.IsTrue(unit.IsDead);
    }

    [Test]
    public void EnemyUnit_HpFraction_IsOneBefore_AndZeroAfterLethal()
    {
        var unit = new EnemyUnit(maxHp: 60, def: 4, weakness: Continent.Physics, expReward: 40);
        Assert.AreEqual(1f, unit.HpFraction, 0.001f);
        unit.TakeDamage(30);
        Assert.AreEqual(0.5f, unit.HpFraction, 0.001f);
        unit.TakeDamage(999);
        Assert.AreEqual(0f, unit.HpFraction, 0.001f);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 약점 매칭
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void WeaknessCheck_SameContinentIsWeakness()
    {
        var unit = new EnemyUnit(maxHp: 60, def: 4, weakness: Continent.Physics, expReward: 40);
        bool isWeak = unit.Weakness == Continent.Physics;
        Assert.IsTrue(isWeak);
    }

    [Test]
    public void WeaknessCheck_DifferentContinentIsNotWeakness()
    {
        var unit = new EnemyUnit(maxHp: 60, def: 4, weakness: Continent.Physics, expReward: 40);
        bool isWeak = unit.Weakness == Continent.Chemistry;
        Assert.IsFalse(isWeak);
    }

    [Test]
    public void WeaknessHit_DealsMoreDamageThanNormalHit_OnSameEnemy()
    {
        var unitA = new EnemyUnit(maxHp: 60, def: 4, weakness: Continent.Physics, expReward: 40);
        var unitB = new EnemyUnit(maxHp: 60, def: 4, weakness: Continent.Physics, expReward: 40);

        Continent attacker = Continent.Physics; // 약점 일치
        int atk = 15;

        bool isWeakA = attacker == unitA.Weakness; // true
        int dmgWeak  = CombatMath.ComputeDamage(atk, unitA.Def, isWeakA);

        bool isWeakB = Continent.Chemistry == unitB.Weakness; // false
        int dmgNorm  = CombatMath.ComputeDamage(atk, unitB.Def, isWeakB);

        Assert.Greater(dmgWeak, dmgNorm);
    }
}
