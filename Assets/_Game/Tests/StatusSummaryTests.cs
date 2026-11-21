using NUnit.Framework;

/// <summary>설계도 22 P2 — Stage D: 파티 HUD 슬롯의 상태 효과 요약 문자열.</summary>
public class StatusSummaryTests
{
    static StatusEffect Buff(string id, float dur)
        => new StatusEffect { Id = id, Kind = StatusKind.StatMod, Stat = StatKind.Atk, Value = 1.5f,
                              Duration = dur, IsBeneficial = true };

    [Test]
    public void Empty_IsEmptyString()
    {
        Assert.AreEqual(string.Empty, StatusSummary.Format(new StatusHolder()));
        Assert.AreEqual(string.Empty, StatusSummary.Format(null));
    }

    [Test]
    public void ActiveBuffs_ShowCountAndShortestRemainingSeconds()
    {
        var h = new StatusHolder();
        h.Apply(Buff("a", 12.4f), true);
        h.Apply(Buff("b", 3.2f), true);
        Assert.AreEqual("▲2 4s", StatusSummary.Format(h), "남은 시간은 가장 짧은 것(올림)");
    }

    [Test]
    public void PendingBuffs_ShowReservedMarker_NoTime()
    {
        var h = new StatusHolder();
        h.Apply(Buff("a", 10f), false);
        Assert.AreEqual("○1", StatusSummary.Format(h));
    }

    [Test]
    public void Harmful_ShowsDownMarker()
    {
        var h = new StatusHolder();
        h.Apply(new StatusEffect { Id = "burn", Kind = StatusKind.Dot, Value = 1f, Duration = 5f }, true);
        Assert.AreEqual("▼1", StatusSummary.Format(h));
    }

    [Test]
    public void Mixed_CombinesPartsInOrder()
    {
        var h = new StatusHolder();
        h.Apply(Buff("a", 5f), true);
        h.Apply(Buff("b", 5f), false);
        h.Apply(new StatusEffect { Id = "s", Kind = StatusKind.Stun, Duration = 2f }, true);
        Assert.AreEqual("▲1 5s ○1 ▼1", StatusSummary.Format(h));
    }
}
