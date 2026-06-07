using NUnit.Framework;
using UnityEngine;

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

    // ── Export / LoadState (서버 저장·복원) ─────────────────────────────────

    [Test]
    public void Export_ReturnsCurrentValues()
    {
        var stats = new PlayerStats();
        stats.GainExp(50);
        stats.Damage(20);

        StatsData d = stats.Export();

        Assert.AreEqual(stats.Level,   d.level);
        Assert.AreEqual(stats.Exp,     d.exp);
        Assert.AreEqual(stats.NextExp, d.nextExp);
        Assert.AreEqual(stats.Hp,      d.hp);
        Assert.AreEqual(stats.Mp,      d.mp);
    }

    [Test]
    public void LoadState_RestoresAllFields()
    {
        var origin = new PlayerStats();
        origin.GainExp(100);   // Lv2
        origin.Damage(30);     // HP 손상

        StatsData saved = origin.Export();

        var restored = new PlayerStats();
        restored.LoadState(saved);

        Assert.AreEqual(origin.Level,   restored.Level);
        Assert.AreEqual(origin.Exp,     restored.Exp);
        Assert.AreEqual(origin.NextExp, restored.NextExp);
        Assert.AreEqual(origin.Hp,      restored.Hp);
        Assert.AreEqual(origin.Mp,      restored.Mp);
    }

    [Test]
    public void LoadState_RoundTripViaJson()
    {
        var origin = new PlayerStats();
        origin.GainExp(245);   // Lv3
        origin.Damage(10);

        string json = JsonUtility.ToJson(origin.Export());
        StatsData loaded = JsonUtility.FromJson<StatsData>(json);

        var restored = new PlayerStats();
        restored.LoadState(loaded);

        Assert.AreEqual(origin.Level,   restored.Level);
        Assert.AreEqual(origin.Exp,     restored.Exp);
        Assert.AreEqual(origin.Hp,      restored.Hp);
    }

    [Test]
    public void LoadState_Null_KeepsDefaults()
    {
        var stats = new PlayerStats();
        stats.LoadState(null);

        Assert.AreEqual(1,          stats.Level);
        Assert.AreEqual(stats.MaxHp, stats.Hp);
    }

    [Test]
    public void LoadState_FiresLoadEvent()
    {
        var origin  = new PlayerStats();
        StatsData d = origin.Export();

        var restored = new PlayerStats();
        string fired  = null;
        restored.OnChanged += e => fired = e;
        restored.LoadState(d);

        Assert.AreEqual("load", fired);
    }
}
