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
