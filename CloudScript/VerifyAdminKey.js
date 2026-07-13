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
