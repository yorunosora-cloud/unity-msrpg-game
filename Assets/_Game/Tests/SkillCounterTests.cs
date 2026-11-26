using NUnit.Framework;

/// <summary>설계도 구현계획 23-A 단계 A — 스킬 카운터 순수 로직.</summary>
public class SkillCounterTests
{
    static CounterSpec Spec(int max = 5, CounterDecay decay = CounterDecay.None, float sec = 0f, int msMax = 0)
        => new CounterSpec { id = "c", maxStacks = max, milestoneMaxStacks = msMax, decay = decay, decaySeconds = sec };

    // ── 증가·상한·소모 ────────────────────────────────────────────────────

    [Test]
    public void Add_ClampsAtMax_AndReportsFull()
    {
        var c = new SkillCounter(Spec(3));
        c.Add(2);
        Assert.AreEqual(2, c.Value); Assert.IsFalse(c.IsFull);
        c.Add(5);
        Assert.AreEqual(3, c.Value); Assert.IsTrue(c.IsFull);
    }

    [Test]
    public void Add_IgnoresNonPositive()
    {
        var c = new SkillCounter(Spec());
        c.Add(0); c.Add(-2);
        Assert.AreEqual(0, c.Value);
    }

    [Test]
    public void ConsumeAll_ReturnsValue_AndEmpties()
    {
        var c = new SkillCounter(Spec());
        c.Add(4);
        Assert.AreEqual(4, c.ConsumeAll());
        Assert.AreEqual(0, c.Value);
        Assert.AreEqual(0, c.ConsumeAll(), "비어 있으면 0");
    }

    [Test]
    public void Consume_Partial_AndMoreThanHeld()
    {
        var c = new SkillCounter(Spec());
        c.Add(4);
        Assert.AreEqual(3, c.Consume(3)); Assert.AreEqual(1, c.Value);
        Assert.AreEqual(1, c.Consume(9), "가진 만큼만 소모"); Assert.AreEqual(0, c.Value);
    }

    [Test]
    public void Clear_ResetsValueAndMovingProgress()
    {
        var c = new SkillCounter(Spec());
        c.Add(3); c.AccumulateMoving(0.6f, true);
        c.Clear();
        Assert.AreEqual(0, c.Value);
        c.AccumulateMoving(0.5f, true);
        Assert.AreEqual(0, c.Value, "이동 진행도도 초기화");
    }

    // ── 시간 감소 ─────────────────────────────────────────────────────────

    [Test]
    public void DecayOneEvery_LosesOneStackPerInterval_OnlyWhileActive()
    {
        var c = new SkillCounter(Spec(5, CounterDecay.DecayOneEvery, 3f));
        c.Add(3);
        c.Tick(2.9f, true); Assert.AreEqual(3, c.Value);
        c.Tick(0.2f, false); Assert.AreEqual(3, c.Value, "대기 중에는 시간이 흐르지 않는다");
        c.Tick(0.2f, true); Assert.AreEqual(2, c.Value);
        c.Tick(6f, true); Assert.AreEqual(0, c.Value);
    }

    [Test]
    public void DecayOneEvery_AddResetsTheIdleTimer()
    {
        var c = new SkillCounter(Spec(5, CounterDecay.DecayOneEvery, 3f));
        c.Add(2);
        c.Tick(2.5f, true);
        c.Add(1);
        c.Tick(2.5f, true);
        Assert.AreEqual(3, c.Value, "증가할 때마다 감소 타이머가 다시 시작");
        c.Tick(0.6f, true);
        Assert.AreEqual(2, c.Value);
    }

    [Test]
    public void ResetAfterIdle_ClearsAfterTheWindow()
    {
        var c = new SkillCounter(Spec(5, CounterDecay.ResetAfterIdle, 5f));
        c.Add(4);
        c.Tick(4.9f, true); Assert.AreEqual(4, c.Value);
        c.Tick(0.2f, true); Assert.AreEqual(0, c.Value);
    }

    [Test]
    public void NoDecay_NeverChangesWithTime()
    {
        var c = new SkillCounter(Spec(5, CounterDecay.None, 1f));
        c.Add(3); c.Tick(100f, true);
        Assert.AreEqual(3, c.Value);
    }

    // ── 마일스톤 상한 ─────────────────────────────────────────────────────

    [Test]
    public void Milestone_RaisesMax_AndLoweringClampsTheValue()
    {
        var c = new SkillCounter(Spec(5, msMax: 7));
        c.Add(9); Assert.AreEqual(5, c.Value);
        c.SetMilestone(true); Assert.AreEqual(7, c.Max);
        c.Add(9); Assert.AreEqual(7, c.Value);
        c.SetMilestone(false); Assert.AreEqual(5, c.Max); Assert.AreEqual(5, c.Value, "상한이 줄면 값도 잘린다");
    }

    [Test]
    public void Milestone_WithoutMilestoneMax_KeepsMax()
    {
        var c = new SkillCounter(Spec(5));
        c.SetMilestone(true);
        Assert.AreEqual(5, c.Max);
    }

    // ── 이동 누적(1초마다 +1) ─────────────────────────────────────────────

    [Test]
    public void AccumulateMoving_GainsOnePerSecondMoving_KeepsRemainder()
    {
        var c = new SkillCounter(Spec(5));
        Assert.AreEqual(0, c.AccumulateMoving(0.6f, true));
        Assert.AreEqual(1, c.AccumulateMoving(0.6f, true), "누적 1.2초 → +1, 0.2초 남음");
        Assert.AreEqual(2, c.AccumulateMoving(1.8f, true), "남은 0.2 + 1.8 = 2.0초 → +2");
        Assert.AreEqual(3, c.Value);
    }

    [Test]
    public void AccumulateMoving_StopResetsProgress_AndGainsNothing()
    {
        var c = new SkillCounter(Spec(5));
        c.AccumulateMoving(0.9f, true);
        Assert.AreEqual(0, c.AccumulateMoving(1f, false));
        c.AccumulateMoving(0.9f, true);
        Assert.AreEqual(0, c.Value, "정지로 진행도가 끊겼으므로 0.9초부터 다시");
    }

    [Test]
    public void AccumulateMoving_GainMultiplier_DoublesTheRate()
    {
        var c = new SkillCounter(Spec(9));
        Assert.AreEqual(2, c.AccumulateMoving(1f, true, 2f));
        Assert.AreEqual(2, c.Value);
    }

    [Test]
    public void AccumulateMoving_IsClampedAtMax()
    {
        var c = new SkillCounter(Spec(2));
        c.AccumulateMoving(10f, true);
        Assert.AreEqual(2, c.Value);
    }

    // ── 위력 공식 ─────────────────────────────────────────────────────────

    [Test]
    public void Formula_QuadraticJoule_FiveStacksIsThreePointFive()
    {
        Assert.AreEqual(3.5f, CounterFormula.Scale(CounterMode.Quadratic, 1f, 0.1f, 5), 0.0001f);
        Assert.AreEqual(1.0f, CounterFormula.Scale(CounterMode.Quadratic, 1f, 0.1f, 0), 0.0001f);
        Assert.AreEqual(1f + 0.12f * 25, CounterFormula.Scale(CounterMode.Quadratic, 1f, 0.12f, 5), 0.0001f, "Lv.3 계수 0.12");
    }

    [Test]
    public void Formula_LinearDielectricBreakdown_FiveStacksIsOnePointNineFive()
    {
        Assert.AreEqual(1.95f, CounterFormula.Scale(CounterMode.Linear, 0.7f, 0.25f, 5), 0.0001f);
        Assert.AreEqual(0.7f, CounterFormula.Scale(CounterMode.Linear, 0.7f, 0.25f, 0), 0.0001f);
    }

    [Test]
    public void Formula_NoneMode_ReturnsBase_AndNegativeStacksTreatedAsZero()
    {
        Assert.AreEqual(1.8f, CounterFormula.Scale(CounterMode.None, 1.8f, 0.5f, 5), 0.0001f);
        Assert.AreEqual(1f, CounterFormula.Scale(CounterMode.Linear, 1f, 0.5f, -3), 0.0001f);
    }

    // ── 사용 이력 창(서로 다른 스킬 수) ──────────────────────────────────

    [Test]
    public void DistinctWindow_CountsDifferentSkillsWithinWindow()
    {
        var w = new DistinctSkillWindow(5f);
        w.Record("a", 0f); w.Record("b", 1f); w.Record("a", 2f);
        Assert.AreEqual(2, w.Count(2f), "a가 두 번이어도 1종");
        Assert.AreEqual(2, w.Count(5f), "a는 2초에 갱신, b는 1초 — 5초 시점에 둘 다 창 안");
    }

    [Test]
    public void DistinctWindow_ExpiresOldSkills_BoundaryInclusive()
    {
        var w = new DistinctSkillWindow(5f);
        w.Record("a", 0f); w.Record("b", 3f);
        Assert.AreEqual(2, w.Count(5f), "정확히 5초 전은 창 안");
        Assert.AreEqual(1, w.Count(5.01f), "a 만료");
        Assert.AreEqual(0, w.Count(8.5f));
    }

    [Test]
    public void DistinctWindow_RefreshingASkillKeepsItAlive_AndClearEmpties()
    {
        var w = new DistinctSkillWindow(5f);
        w.Record("a", 0f); w.Record("a", 4f);
        Assert.AreEqual(1, w.Count(8f), "재사용으로 갱신되어 아직 창 안");
        w.Clear();
        Assert.AreEqual(0, w.Count(8f));
    }

    [Test]
    public void DistinctWindow_IgnoresEmptyIds()
    {
        var w = new DistinctSkillWindow(5f);
        w.Record(null, 0f); w.Record("", 0f);
        Assert.AreEqual(0, w.Count(0f));
    }
}
