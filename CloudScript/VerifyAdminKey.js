// VerifyAdminKey.js — PlayFab Legacy CloudScript
// ─────────────────────────────────────────────────────────────────────────────
// 배포 방법:
//   1. PlayFab Game Manager → Automation → Cloud Script (legacy)
//   2. "+ New Revision" 또는 현재 Revision의 스크립트 영역에 이 파일 내용을 붙여넣기
//   3. "Save and Deploy Revision" 클릭
//
// 비밀 키 설정:
//   Game Manager → Title Settings → Title Data → [Internal] 탭
//   Key: adminKey  /  Value: (원하는 비밀 키 문자열)
//
// 클라이언트에서 호출:
//   CloudScriptService.Execute("VerifyAdminKey", new { key = "입력한 키" }, onOk, onErr)
// ─────────────────────────────────────────────────────────────────────────────

handlers.VerifyAdminKey = function (args, context) {
    // 비밀 키는 Title Internal Data에만 보관 — 클라이언트 빌드에 없음
    var res    = server.GetTitleInternalData({ Keys: ["adminKey"] });
    var secret = res.Data && res.Data.adminKey;

    if (!secret) {
        log.info("VerifyAdminKey: TitleInternalData 'adminKey' 가 설정되어 있지 않습니다.");
        return { success: false };
    }

    var ok = !!(args && args.key === secret);

    if (!ok) {
        // 실패 기록 (향후 브루트포스 잠금 구현 시 활용)
        // TODO: 단기간 연속 실패 시 일시 잠금 (PlayFab PlayStream Event 또는 Entity Data 카운터)
        log.info("VerifyAdminKey: 키 불일치 — PlayFabId=" + (context && context.clientSessionTicket ? "있음" : "없음"));
    }

    return { success: ok };
};





// ProblemAdmin.js — PlayFab Legacy CloudScript
// ─────────────────────────────────────────────────────────────────────────────
// 배포 방법:
//   Legacy CloudScript는 리비전이 하나뿐이다. Game Manager → Automation →
//   Cloud Script (legacy)의 스크립트 영역에는 VerifyAdminKey.js 내용 뒤에
//   이 파일 내용을 이어붙여 하나의 스크립트로 저장·배포한다.
//
// 클라이언트에서 호출:
//   CloudScriptService.Execute("UpsertProblem", new { key, problem = dto }, onOk, onErr)
//   CloudScriptService.Execute("DeleteProblem", new { key, id }, onOk, onErr)
// ─────────────────────────────────────────────────────────────────────────────

function checkAdminKey(args) {
    var res    = server.GetTitleInternalData({ Keys: ["adminKey"] });
    var secret = res.Data && res.Data.adminKey;
    return !!(secret && args && args.key === secret);
}

function loadStore() {
    var res = server.GetTitleData({ Keys: ["problems"] });
    var raw = res.Data && res.Data.problems;
    var store = raw ? JSON.parse(raw) : {};
    if (!store.items) store.items = [];
    if (!store.deletedIds) store.deletedIds = [];
    return store;
}

function saveStore(store) {
    server.SetTitleData({ Key: "problems", Value: JSON.stringify(store) });
}

handlers.UpsertProblem = function (args, context) {
    if (!checkAdminKey(args)) {
        log.info("UpsertProblem: 인증 실패");
        return { success: false, error: "auth" };
    }
    if (!args.problem || !args.problem.id) {
        return { success: false, error: "invalid problem" };
    }

    var store = loadStore();

    var found = false;
    for (var i = 0; i < store.items.length; i++) {
        if (store.items[i].id === args.problem.id) {
            store.items[i] = args.problem;
            found = true;
            break;
        }
    }
    if (!found) store.items.push(args.problem);

    // 이전에 삭제 표시된 id를 다시 추가/수정하면 tombstone 해제
    store.deletedIds = store.deletedIds.filter(function (id) { return id !== args.problem.id; });

    saveStore(store);
    return { success: true };
};

handlers.DeleteProblem = function (args, context) {
    if (!checkAdminKey(args)) {
        log.info("DeleteProblem: 인증 실패");
        return { success: false, error: "auth" };
    }
    if (!args.id) {
        return { success: false, error: "invalid id" };
    }

    var store = loadStore();

    store.items = store.items.filter(function (p) { return p.id !== args.id; });
    if (store.deletedIds.indexOf(args.id) === -1) store.deletedIds.push(args.id);

    saveStore(store);
    return { success: true };
};

