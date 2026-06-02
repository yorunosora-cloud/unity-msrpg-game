using NUnit.Framework;

public class PlayerStatsTests
{
    [Test]
    public void GainExp_UnderThreshold_DoesNotLevelUp()
    {
        var stats = new PlayerStats();
        stats.GainExp(50);
        Assert.AreEqual(1, stats.Level);
        Assert.AreEqual(50, stats.Exp);
    }

    [Test]
    public void GainExp_AtThreshold_LevelsUp()
    {
        var stats = new PlayerStats();
        stats.GainExp(100);
        Assert.AreEqual(2, stats.Level);
        Assert.AreEqual(0, stats.Exp);
    }

    [Test]
    public void GainExp_DoubleThreshold_LevelsTwice()
    {
        // Lv1 nextExp=100 → Lv2 nextExp=145 → 합계 245로 두 번 레벨업
        var stats = new PlayerStats();
        stats.GainExp(245);
        Assert.AreEqual(3, stats.Level);
        Assert.AreEqual(0, stats.Exp);
    }

    [Test]
    public void LevelUp_MaxHpIncreases()
    {
        var stats = new PlayerStats();
        int initialMaxHp = stats.MaxHp;
        stats.GainExp(100);
        Assert.Greater(stats.MaxHp, initialMaxHp);
    }

    [Test]
    public void LevelUp_RestoresHpToFull()
    {
        var stats = new PlayerStats();
        stats.Damage(50);
        stats.GainExp(100);
        Assert.AreEqual(stats.MaxHp, stats.Hp);
    }

    [Test]
    public void Damage_ReducesHp()
    {
        var stats = new PlayerStats();
        int before = stats.Hp;
        stats.Damage(30);
        Assert.AreEqual(before - 30, stats.Hp);
    }

    [Test]
    public void Damage_DoesNotGoBelowZero()
    {
        var stats = new PlayerStats();
        stats.Damage(99999);
        Assert.AreEqual(0, stats.Hp);
    }

    [Test]
    public void Heal_RestoresHp()
    {
        var stats = new PlayerStats();
        stats.Damage(50);
        stats.Heal(30);
        Assert.AreEqual(stats.MaxHp - 50 + 30, stats.Hp);
    }

    [Test]
    public void Heal_DoesNotExceedMaxHp()
    {
        var stats = new PlayerStats();
        stats.Heal(9999);
        Assert.AreEqual(stats.MaxHp, stats.Hp);
    }

    [Test]
    public void UseMp_SucceedsWhenEnough()
    {
        var stats = new PlayerStats();
        bool ok = stats.UseMp(30);
        Assert.IsTrue(ok);
        Assert.AreEqual(stats.MaxMp - 30, stats.Mp);
    }

    [Test]
    public void UseMp_FailsWhenNotEnough()
    {
        var stats = new PlayerStats();
        bool ok = stats.UseMp(99999);
        Assert.IsFalse(ok);
        Assert.AreEqual(stats.MaxMp, stats.Mp);
    }

    [Test]
    public void GainExp_ZeroAmount_DoesNothing()
    {
        var stats = new PlayerStats();
        stats.GainExp(0);
        Assert.AreEqual(1, stats.Level);
        Assert.AreEqual(0, stats.Exp);
    }

    [Test]
    public void GainExp_NegativeAmount_DoesNothing()
    {
        var stats = new PlayerStats();
        stats.GainExp(-50);
        Assert.AreEqual(1, stats.Level);
        Assert.AreEqual(0, stats.Exp);
    }

    [Test]
    public void IsDead_FalseWhenAlive()
    {
        var stats = new PlayerStats();
        Assert.IsFalse(stats.IsDead);
    }

    [Test]
    public void IsDead_TrueWhenHpIsZero()
    {
        var stats = new PlayerStats();
        stats.Damage(99999);
        Assert.IsTrue(stats.IsDead);
    }

    [Test]
    public void GainExp_OnLevelUp_FiresLevelupNotExp()
    {
        var stats = new PlayerStats();
        var events = new System.Collections.Generic.List<string>();
        stats.OnChanged += e => events.Add(e);
        stats.GainExp(100);
        Assert.Contains("levelup", events);
        Assert.IsFalse(events.Contains("exp"));
    }
}
