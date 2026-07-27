using System;
using System.Collections.Generic;

/// <summary>우편 첨부 종류. None = 첨부 없는 공지/경고 메시지 (설계 §14).</summary>
public enum MailAttachmentType { None, Currency, Crystal, Consumable }

/// <summary>우편 1건. 관리자 발송(서버) 또는 추후 시스템 발송으로 생성된다.</summary>
[Serializable]
public class MailItem
{
    public string id;               // 서버에서 생성 (timestamp+rand)
    public long   sentUtc;          // 발송 시각 (Unix seconds) — 후속 자동삭제 확장 훅
    public string title;
    public string body;
    public MailAttachmentType attachType;
    public int    kindIndex;        // attachType별 enum 인덱스 (CurrencyKind/CrystalKind/ConsumableKind)
    public int    amount;
    public bool   read;             // 메시지 확인 또는 보상 수령 시 true
    public bool   claimed;          // 첨부 지급 완료 여부 (attachType == None이면 항상 false)
}

/// <summary>Mailbox 직렬화 DTO — PlayFab UserData(JSON) 저장/복원용.</summary>
[Serializable]
public class MailboxData
{
    public List<MailItem> mails = new List<MailItem>();
}
