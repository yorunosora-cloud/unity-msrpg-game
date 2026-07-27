using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Mailbox EditMode 테스트 (설계 §14). Claim/ClaimAll이 MetaState.Wallet·Crystals를
/// 직접 조작하므로(Wallet.cs 패턴과 동일한 신뢰 모델) 각 테스트 전 MetaState.Init()으로
/// 새 상태를 준비한다.
/// </summary>
public class MailboxTests
{
    [SetUp]
    public void SetUp() => MetaState.Init();

    static MailItem Currency(string id, CurrencyKind kind, int amount, bool claimed = false)
        => new MailItem { id = id, title = "t", body = "", attachType = MailAttachmentType.Currency,
                           kindIndex = (int)kind, amount = amount, read = claimed, claimed = claimed };

    static MailItem Crystal(string id, CrystalKind kind, int amount, bool claimed = false)
        => new MailItem { id = id, title = "t", body = "", attachType = MailAttachmentType.Crystal,
                           kindIndex = (int)kind, amount = amount, read = claimed, claimed = claimed };

    static MailItem Message(string id, bool read = false)
        => new MailItem { id = id, title = "t", body = "b", attachType = MailAttachmentType.None,
                           read = read, claimed = false };

    static Mailbox WithMails(params MailItem[] mails)
    {
        var mb = new Mailbox();
        mb.LoadState(new MailboxData { mails = new List<MailItem>(mails) });
        return mb;
    }

    // ════════════════════════════════════════════════════════════════════
    // Claim
    // ════════════════════════════════════════════════════════════════════

    [Test]
    public void Claim_Currency_IncreasesWalletAndSetsClaimed()
    {
        var mb = WithMails(Currency("m1", CurrencyKind.Gold, 500));
        int prevGold = MetaState.Wallet.Gold;

        bool ok = mb.Claim("m1");

        Assert.IsTrue(ok);
        Assert.AreEqual(prevGold + 500, MetaState.Wallet.Gold);
        Assert.IsTrue(mb.Mails[0].claimed);
        Assert.IsTrue(mb.Mails[0].read);
    }

    [Test]
    public void Claim_Crystal_IncreasesCrystalsAndSetsClaimed()
    {
        var mb = WithMails(Crystal("m1", CrystalKind.Axioma, 3));

        bool ok = mb.Claim("m1");

        Assert.IsTrue(ok);
        Assert.AreEqual(3, MetaState.Crystals.Get(CrystalKind.Axioma));
        Assert.IsTrue(mb.Mails[0].claimed);
    }

    [Test]
    public void Claim_AlreadyClaimed_ReturnsFalse_NoDoubleGrant()
    {
        var mb = WithMails(Currency("m1", CurrencyKind.Gold, 500));
        mb.Claim("m1");
        int goldAfterFirst = MetaState.Wallet.Gold;

        bool second = mb.Claim("m1");

        Assert.IsFalse(second);
        Assert.AreEqual(goldAfterFirst, MetaState.Wallet.Gold);
    }

    [Test]
    public void Claim_NoAttachment_ReturnsFalse()
    {
        var mb = WithMails(Message("m1"));
        Assert.IsFalse(mb.Claim("m1"));
    }

    [Test]
    public void Claim_UnknownId_ReturnsFalse()
    {
        var mb = WithMails(Currency("m1", CurrencyKind.Gold, 500));
        Assert.IsFalse(mb.Claim("missing"));
    }

    [Test]
    public void Claim_Consumable_SetsClaimed_NoThrow_UntilShopImplemented()
    {
        var mb = WithMails(new MailItem { id = "m1", attachType = MailAttachmentType.Consumable, kindIndex = 0, amount = 1 });
        bool ok = mb.Claim("m1");
        Assert.IsTrue(ok);
        Assert.IsTrue(mb.Mails[0].claimed);
    }

    // ════════════════════════════════════════════════════════════════════
    // MarkRead
    // ════════════════════════════════════════════════════════════════════

    [Test]
    public void MarkRead_SetsReadTrue()
    {
        var mb = WithMails(Message("m1"));
        bool ok = mb.MarkRead("m1");
        Assert.IsTrue(ok);
        Assert.IsTrue(mb.Mails[0].read);
    }

    [Test]
    public void MarkRead_AlreadyRead_ReturnsFalse()
    {
        var mb = WithMails(Message("m1", read: true));
        Assert.IsFalse(mb.MarkRead("m1"));
    }

    // ════════════════════════════════════════════════════════════════════
    // UnreadCount
    // ════════════════════════════════════════════════════════════════════

    [Test]
    public void UnreadCount_MixedMessagesAndRewards()
    {
        var mb = WithMails(
            Message("m1"),                                  // unread message → pending
            Message("m2", read: true),                       // read message → not pending
            Currency("m3", CurrencyKind.Gold, 100),           // unclaimed reward → pending
            Currency("m4", CurrencyKind.Gold, 100, claimed: true)); // claimed reward → not pending

        Assert.AreEqual(2, mb.UnreadCount());
    }

    [Test]
    public void UnreadCount_DecreasesAfterClaim()
    {
        var mb = WithMails(Currency("m1", CurrencyKind.Gold, 100), Message("m2"));
        Assert.AreEqual(2, mb.UnreadCount());
        mb.Claim("m1");
        Assert.AreEqual(1, mb.UnreadCount());
        mb.MarkRead("m2");
        Assert.AreEqual(0, mb.UnreadCount());
    }

    // ════════════════════════════════════════════════════════════════════
    // ClaimAll
    // ════════════════════════════════════════════════════════════════════

    [Test]
    public void ClaimAll_ClaimsAllUnclaimedRewards_ReturnsCount()
    {
        var mb = WithMails(
            Currency("m1", CurrencyKind.Gold, 100),
            Crystal("m2", CrystalKind.Axioma, 2),
            Currency("m3", CurrencyKind.Gold, 50, claimed: true), // 이미 수령
            Message("m4"));                                       // 첨부 없음, 대상 아님

        int claimedCount = mb.ClaimAll();

        Assert.AreEqual(2, claimedCount);
        Assert.IsTrue(mb.Mails[0].claimed);
        Assert.IsTrue(mb.Mails[1].claimed);
        Assert.IsFalse(mb.Mails[3].claimed); // 메시지는 claimed 대상 아님
        Assert.IsFalse(mb.Mails[3].read);    // ClaimAll이 메시지를 읽음 처리하지 않음
    }

    [Test]
    public void ClaimAll_NoRewards_ReturnsZero()
    {
        var mb = WithMails(Message("m1"));
        Assert.AreEqual(0, mb.ClaimAll());
    }

    // ════════════════════════════════════════════════════════════════════
    // 직렬화
    // ════════════════════════════════════════════════════════════════════

    [Test]
    public void Export_LoadState_RoundTrip()
    {
        var mb = WithMails(Currency("m1", CurrencyKind.Gold, 500), Message("m2"));
        string json = JsonUtility.ToJson(mb.Export());

        var restored = JsonUtility.FromJson<MailboxData>(json);
        var mb2 = new Mailbox();
        mb2.LoadState(restored);

        Assert.AreEqual(2, mb2.Mails.Count);
        Assert.AreEqual("m1", mb2.Mails[0].id);
        Assert.AreEqual(500, mb2.Mails[0].amount);
        Assert.AreEqual("m2", mb2.Mails[1].id);
    }

    [Test]
    public void LoadState_Null_ResultsInEmptyMailbox()
    {
        var mb = new Mailbox();
        mb.LoadState(null);
        Assert.AreEqual(0, mb.Mails.Count);
    }

    [Test]
    public void OnChanged_FiresOnClaim()
    {
        var mb = WithMails(Currency("m1", CurrencyKind.Gold, 100));
        int fired = 0;
        mb.OnChanged += () => fired++;
        mb.Claim("m1");
        Assert.AreEqual(1, fired);
    }
}
