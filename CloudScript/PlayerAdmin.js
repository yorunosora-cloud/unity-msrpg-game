// PlayerAdmin.js — PlayFab Legacy CloudScript
// ─────────────────────────────────────────────────────────────────────────────
// 배포 방법:
//   Legacy CloudScript는 리비전이 하나뿐이다. Game Manager → Automation →
//   Cloud Script (legacy)의 스크립트 영역에 다음 순서로 이어붙여 저장·배포한다:
//     VerifyAdminKey.js  →  ProblemAdmin.js  →  PlayerAdmin.js
//
// ⚠️ checkAdminKey(args) 는 ProblemAdmin.js 에 이미 전역 정의되어 있다.
//    여기서 재정의하면 "duplicate function" 이 되므로 절대 다시 선언하지 않는다.
//    (재사용만 한다.)
//
// ⚠️ Legacy CloudScript의 server 객체는 GetPlayersInSegment 같은 대량 조회 API를
//    지원하지 않는다(런타임에 "is not a function"). 그래서 자체 레지스트리 방식을 쓴다:
//    로그인/회원가입 성공 시 클라이언트가 TrackPlayerLogin을 호출해 자신의 PlayFabId를
//    TitleData("players")에 등록하고, AdminGetPlayers는 그 ID 목록을 순회하며
//    GetUserAccountInfo/GetUserBans(둘 다 단건 조회라 지원됨)로 상세정보를 조립한다.
//
// 클라이언트에서 호출:
//   CloudScriptService.Execute("TrackPlayerLogin",     new { },   onOk, onErr)  // 로그인/가입 성공 시 자동
//   CloudScriptService.Execute("AdminGetPlayers",      new { key }, onOk, onErr)
//   CloudScriptService.Execute("AdminDeletePlayer",    new { key, playFabId }, onOk, onErr)
//   CloudScriptService.Execute("AdminBanPlayer",       new { key, playFabId }, onOk, onErr)
//   CloudScriptService.Execute("AdminUnbanPlayer",     new { key, playFabId }, onOk, onErr)
//   CloudScriptService.Execute("AdminGetPlayerRoster", new { key, playFabId }, onOk, onErr)
//   CloudScriptService.Execute("AdminGiveCharacter",   new { key, playFabId, charId }, onOk, onErr)
//   CloudScriptService.Execute("AdminRevokeCharacter", new { key, playFabId, charId }, onOk, onErr)
//   CloudScriptService.Execute("AdminResetAccount",    new { key, playFabId }, onOk, onErr)
//   CloudScriptService.Execute("AdminSendMail",        new { key, playFabId, title, body, attachType, kindIndex, amount }, onOk, onErr)
// ─────────────────────────────────────────────────────────────────────────────

function loadPlayerStore() {
    var res   = server.GetTitleData({ Keys: ["players"] });
    var raw   = res.Data && res.Data.players;
    var store = raw ? JSON.parse(raw) : {};
    if (!store.ids) store.ids = [];
    return store;
}

function savePlayerStore(store) {
    server.SetTitleData({ Key: "players", Value: JSON.stringify(store) });
}

// 본인 인증(세션 티켓)만으로 호출 가능 — 관리자 키 불필요. 자기 자신을 레지스트리에 등록할 뿐이라 안전.
handlers.TrackPlayerLogin = function (args, context) {
    var store = loadPlayerStore();
    if (store.ids.indexOf(currentPlayerId) === -1) {
        store.ids.push(currentPlayerId);
        savePlayerStore(store);
    }
    return { success: true };
};

handlers.AdminGetPlayers = function (args, context) {
    if (!checkAdminKey(args)) {
        log.info("AdminGetPlayers: 인증 실패");
        return { success: false, error: "auth" };
    }

    var store   = loadPlayerStore();
    var players = [];

    for (var i = 0; i < store.ids.length; i++) {
        var id = store.ids[i];
        if (id === currentPlayerId) continue; // 관리자 본인 제외

        var acct;
        try {
            acct = server.GetUserAccountInfo({ PlayFabId: id });
        } catch (e) {
            continue; // 삭제된 계정 등 — 조용히 건너뜀
        }
        var info = acct && acct.UserInfo && acct.UserInfo.TitleInfo;
        if (!info) continue;

        var bannedUntil = "";
        try {
            var bansRes = server.GetUserBans({ PlayFabId: id });
            var bans    = (bansRes && bansRes.BanData) || [];
            for (var j = 0; j < bans.length; j++) {
                if (bans[j].Active && (!bannedUntil || bans[j].Expires > bannedUntil)) {
                    bannedUntil = bans[j].Expires || "9999-12-31T23:59:59Z";
                }
            }
        } catch (e) { /* 밴 조회 실패는 무시 — 미정지로 표시 */ }

        players.push({
            playFabId:   id,
            displayName: info.DisplayName || "",
            lastLogin:   info.LastLogin   || "",
            created:     info.Created     || "",
            bannedUntil: bannedUntil
        });
    }

    // JsonUtility가 최상위 배열을 못 읽으므로 {items:[...]} 로 감싸 문자열로 반환.
    return { success: true, playersJson: JSON.stringify({ items: players }) };
};

handlers.AdminDeletePlayer = function (args, context) {
    if (!checkAdminKey(args)) return { success: false, error: "auth" };
    if (!args.playFabId)      return { success: false, error: "invalid id" };
    if (args.playFabId === currentPlayerId) return { success: false, error: "self" };

    server.DeletePlayer({ PlayFabId: args.playFabId }); // 복구 불가

    var store = loadPlayerStore();
    store.ids = store.ids.filter(function (id) { return id !== args.playFabId; });
    savePlayerStore(store);

    return { success: true };
};

handlers.AdminBanPlayer = function (args, context) {
    if (!checkAdminKey(args)) return { success: false, error: "auth" };
    if (!args.playFabId)      return { success: false, error: "invalid id" };
    if (args.playFabId === currentPlayerId) return { success: false, error: "self" };

    server.BanUsers({
        Bans: [{
            PlayFabId: args.playFabId,
            Reason:    (args.reason || "관리자 정지")
            // DurationInHours 생략 => 영구 정지
        }]
    });
    return { success: true };
};

handlers.AdminUnbanPlayer = function (args, context) {
    if (!checkAdminKey(args)) return { success: false, error: "auth" };
    if (!args.playFabId)      return { success: false, error: "invalid id" };

    server.RevokeAllBansForUser({ PlayFabId: args.playFabId });
    return { success: true };
};

// 대상 계정의 roster UserData를 읽어 보유 캐릭터 id 목록만 반환.
handlers.AdminGetPlayerRoster = function (args, context) {
    if (!checkAdminKey(args)) return { success: false, error: "auth" };
    if (!args.playFabId)      return { success: false, error: "invalid id" };

    var res   = server.GetUserData({ PlayFabId: args.playFabId, Keys: ["roster"] });
    var raw   = res.Data && res.Data.roster && res.Data.roster.Value;
    var owned = [];
    if (raw) {
        var parsed = JSON.parse(raw);
        var arr = (parsed && parsed.owned) || [];
        for (var i = 0; i < arr.length; i++) owned.push(arr[i].id);
    }
    return { success: true, ownedJson: JSON.stringify({ ids: owned }) };
};

handlers.AdminGiveCharacter = function (args, context) {
    if (!checkAdminKey(args)) return { success: false, error: "auth" };
    if (!args.playFabId || !args.charId) return { success: false, error: "invalid" };

    var res   = server.GetUserData({ PlayFabId: args.playFabId, Keys: ["roster"] });
    var raw   = res.Data && res.Data.roster && res.Data.roster.Value;
    var store = raw ? JSON.parse(raw) : {};
    if (!store.owned) store.owned = [];

    for (var i = 0; i < store.owned.length; i++)
        if (store.owned[i].id === args.charId) return { success: true }; // 이미 보유 → 무해

    // OwnedCharacter 기본 shape (JsonUtility 역직렬화 호환)
    store.owned.push({
        id: args.charId, level: 1, exp: 0, dupes: 0,
        unlockedSkillIds: [], skillProgress: []
    });
    server.UpdateUserData({ PlayFabId: args.playFabId, Data: { roster: JSON.stringify(store) } });
    return { success: true };
};

handlers.AdminRevokeCharacter = function (args, context) {
    if (!checkAdminKey(args)) return { success: false, error: "auth" };
    if (!args.playFabId || !args.charId) return { success: false, error: "invalid" };

    var res = server.GetUserData({ PlayFabId: args.playFabId, Keys: ["roster"] });
    var raw = res.Data && res.Data.roster && res.Data.roster.Value;
    if (!raw) return { success: true }; // 로스터 없음 — 회수할 것도 없음

    var store = JSON.parse(raw);
    store.owned = (store.owned || []).filter(function (c) { return c.id !== args.charId; });
    server.UpdateUserData({ PlayFabId: args.playFabId, Data: { roster: JSON.stringify(store) } });
    return { success: true };
};

// 진행 데이터 5개 키 삭제 — 대상 재접속 시 신규 상태로 시작. 계정 자체·레지스트리 등록은 유지.
handlers.AdminResetAccount = function (args, context) {
    if (!checkAdminKey(args)) return { success: false, error: "auth" };
    if (!args.playFabId)      return { success: false, error: "invalid id" };
    if (args.playFabId === currentPlayerId) return { success: false, error: "self" };

    server.UpdateUserData({
        PlayFabId:    args.playFabId,
        KeysToRemove: ["wallet", "roster", "gacha", "crystals", "studyMats"]
    });
    return { success: true };
};

// 대상 계정의 우편함(mailbox)에 우편 1건을 추가한다(설계 §14). read-modify-write.
handlers.AdminSendMail = function (args, context) {
    if (!checkAdminKey(args))            return { success: false, error: "auth" };
    if (!args.playFabId || !args.title)  return { success: false, error: "invalid" };

    var res = server.GetUserData({ PlayFabId: args.playFabId, Keys: ["mailbox"] });
    var raw = res.Data && res.Data.mailbox && res.Data.mailbox.Value;
    var box = raw ? JSON.parse(raw) : {};
    if (!box.mails) box.mails = [];

    box.mails.push({
        id:         "mail_" + Date.now() + "_" + Math.floor(Math.random() * 100000),
        sentUtc:    Date.now(),
        title:      String(args.title),
        body:       args.body ? String(args.body) : "",
        attachType: args.attachType | 0,   // 0 None / 1 Currency / 2 Crystal / 3 Consumable
        kindIndex:  args.kindIndex  | 0,
        amount:     args.amount     | 0,
        read:       false,
        claimed:    false
    });

    server.UpdateUserData({ PlayFabId: args.playFabId, Data: { mailbox: JSON.stringify(box) } });
    return { success: true };
};
