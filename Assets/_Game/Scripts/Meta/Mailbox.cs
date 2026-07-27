using System;
using System.Collections.Generic;

/// <summary>
/// 런타임 우편함 저장소 (설계 §14). Wallet.cs 패턴 모방 — Add 대신 서버 발송(AdminSendMail)이
/// 채우고, 클라이언트는 조회/읽음/수령만 담당한다.
/// </summary>
public class Mailbox
{
    /// <summary>목록 변화 시 발생 (UI 갱신용).</summary>
    public event Action OnChanged;

    List<MailItem> _mails = new List<MailItem>();
    public IReadOnlyList<MailItem> Mails => _mails;

    /// <summary>미확인 개수 = 미읽음 메시지 + 미수령 보상.</summary>
    public int UnreadCount()
    {
        int n = 0;
        foreach (var m in _mails)
        {
            bool pending = m.attachType == MailAttachmentType.None ? !m.read : !m.claimed;
            if (pending) n++;
        }
        return n;
    }

    /// <summary>첨부 없는 메시지를 읽음 처리. 이미 읽었으면 false.</summary>
    public bool MarkRead(string id)
    {
        var m = Find(id);
        if (m == null || m.read) return false;
        m.read = true;
        OnChanged?.Invoke();
        return true;
    }

    /// <summary>첨부 지급 + claimed 세팅. 이미 claimed거나 첨부 없음이면 멱등(false, 재지급 없음).</summary>
    public bool Claim(string id)
    {
        var m = Find(id);
        if (m == null || m.attachType == MailAttachmentType.None || m.claimed) return false;

        switch (m.attachType)
        {
            case MailAttachmentType.Currency:
                MetaState.Wallet.Add((CurrencyKind)m.kindIndex, m.amount);
                break;
            case MailAttachmentType.Crystal:
                MetaState.Crystals.Add((CrystalKind)m.kindIndex, m.amount);
                break;
            case MailAttachmentType.Consumable:
                // TODO: 12 상점(ConsumableInventory) 구현 후 지급 로직 연결. 현재는 claimed만 세팅.
                break;
        }
        m.claimed = true;
        m.read    = true;
        OnChanged?.Invoke();
        return true;
    }

    /// <summary>미수령 보상 우편 전체 수령. 반환값 = 수령 건수.</summary>
    public int ClaimAll()
    {
        int n = 0;
        foreach (var m in _mails)
            if (m.attachType != MailAttachmentType.None && !m.claimed && Claim(m.id)) n++;
        return n;
    }

    MailItem Find(string id)
    {
        foreach (var m in _mails)
            if (m.id == id) return m;
        return null;
    }

    // ── 직렬화 ────────────────────────────────────────────────────────────

    public MailboxData Export() => new MailboxData { mails = new List<MailItem>(_mails) };

    public void LoadState(MailboxData data)
    {
        _mails = (data != null && data.mails != null) ? data.mails : new List<MailItem>();
        OnChanged?.Invoke();
    }
}
