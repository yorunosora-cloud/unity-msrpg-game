using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 관리자/개발자 치트 패널.
/// 에디터·dev 빌드·admin 계정(MetaState.IsAdmin)에서만 접근 가능.
/// F1로 토글하거나 QuickBar의 [관] 버튼으로 열 수 있습니다.
/// </summary>
public class AdminPanel : MonoBehaviour
{
    [Header("재화 지급")]
    [SerializeField] Button addGoldButton;
    [SerializeField] Button addPaperButton;
    [SerializeField] Button addFocusButton;
    [SerializeField] Button addFragmentButton;

    [Header("캐릭터 지급 (ID 직접 입력)")]
    [SerializeField] TMP_InputField characterIdInput;
    [SerializeField] Button          giveCharacterButton;

    [Header("가챠 디버그")]
    [SerializeField] Button debugRollOneButton;
    [SerializeField] Button debugRollTenButton;

    [Header("결정 지급")]
    [SerializeField] Button giveCrystalsButton;

    [Header("계정")]
    [SerializeField] Button resetAccountButton;
    [SerializeField] Button forceSaveButton;
    [SerializeField] Button forceLoadButton;

    [Header("공용")]
    [SerializeField] TMP_Text statusText;
    [SerializeField] Button   closeButton;

    // ── 생명주기 ──────────────────────────────────────────────────────────

    void Awake()
    {
        // 권한 없으면 컴포넌트 비활성화 (패널은 MetaPanelController가 숨김)
        if (!ShouldAllow())
            enabled = false;
    }

    void OnEnable()
    {
        ShowStatus("관리자 패널", Color.yellow);
    }

    // ── 재화 지급 ─────────────────────────────────────────────────────────

    public void OnAddGold()
    {
        MetaState.Wallet.Add(CurrencyKind.Gold,     1000);
        ShowStatus("+골드 1,000", Color.green);
    }

    public void OnAddPaper()
    {
        MetaState.Wallet.Add(CurrencyKind.Paper,    100);
        ShowStatus("+논문 100", Color.green);
    }

    public void OnAddFocus()
    {
        MetaState.Wallet.Add(CurrencyKind.Focus,    100);
        ShowStatus("+집중력 100", Color.green);
    }

    public void OnAddFragment()
    {
        MetaState.Wallet.Add(CurrencyKind.Fragment, 50);
        ShowStatus("+조각 50", Color.green);
    }

    // ── 결정 지급 ─────────────────────────────────────────────────────────

    /// <summary>모든 결정 종류에 각 10개씩 지급 (표시·저장 테스트용).</summary>
    public void OnGiveCrystals()
    {
        if (!MetaState.IsInitialized) return;
        foreach (CrystalKind kind in System.Enum.GetValues(typeof(CrystalKind)))
            MetaState.Crystals.Add(kind, 10);
        ShowStatus("+결정 전종 각 10", Color.cyan);
    }

    // ── 캐릭터 지급 ───────────────────────────────────────────────────────

    public void OnGiveCharacter()
    {
        if (characterIdInput == null) return;
        string id = characterIdInput.text.Trim();
        if (string.IsNullOrEmpty(id)) { ShowStatus("캐릭터 ID를 입력하세요.", Color.yellow); return; }

        var db  = Resources.Load<CharacterDatabase>("CharacterDatabase");
        var def = db?.ById(id);
        if (def == null) { ShowStatus($"'{id}' 없음 — 시드 캐릭터 ID 확인.", Color.red); return; }

        bool isNew = MetaState.Roster.Add(id);
        ShowStatus(isNew ? $"{def.nameKo} 지급!" : $"{def.nameKo} 중복 (+dupes)", Color.cyan);
    }

    // ── 가챠 디버그 (논문 자동 보충) ────────────────────────────────────

    public void OnDebugRollOne()
    {
        var svc = MakeService();
        if (svc == null) return;
        MetaState.Wallet.Add(CurrencyKind.Paper, GachaConfig.CostSingle);
        var r = svc.RollOne();
        ShowStatus(r.HasValue
            ? $"[{r.Value.rarity}] {r.Value.def?.nameKo ?? "?"}  {(r.Value.isNew ? "[신규]" : "[중복]")}"
            : "뽑기 실패", Color.white);
    }

    public void OnDebugRollTen()
    {
        var svc = MakeService();
        if (svc == null) return;
        MetaState.Wallet.Add(CurrencyKind.Paper, GachaConfig.CostTen);
        var results = svc.RollTen();
        if (results == null) { ShowStatus("뽑기 실패", Color.red); return; }

        var sb = new System.Text.StringBuilder();
        foreach (var r in results)
            sb.AppendLine($"[{r.rarity}] {r.def?.nameKo ?? "?"} {(r.isNew ? "[신규]" : "")}");
        ShowStatus(sb.ToString().TrimEnd(), Color.white);
    }

    // ── 계정 ──────────────────────────────────────────────────────────────

    public void OnResetAccount()
    {
        MetaState.Init();
        ShowStatus("계정 초기화 완료 (저장 안 됨)", Color.red);
    }

    public void OnForceSave()
    {
        MetaSaveService.Save(
            () => ShowStatus("저장 완료", Color.green),
            err => ShowStatus($"저장 실패: {err}", Color.red));
    }

    public void OnForceLoad()
    {
        MetaSaveService.Load(
            () => ShowStatus("불러오기 완료", Color.green),
            err => ShowStatus($"불러오기 실패: {err}", Color.red));
    }

    public void OnCloseClicked()
    {
        gameObject.SetActive(false); // OnDisable → UIManager.Close()
    }

    void OnDisable() => UIManager.Close();

    // ── 내부 ──────────────────────────────────────────────────────────────

    GachaService MakeService()
    {
        if (!MetaState.IsInitialized) return null;
        var db = Resources.Load<CharacterDatabase>("CharacterDatabase");
        if (db == null) return null;
        return new GachaService(db, MetaState.Wallet, MetaState.Roster,
                                MetaState.GachaState, MetaState.Crystals);
    }

    void ShowStatus(string msg, Color color)
    {
        if (statusText == null) return;
        statusText.text  = msg;
        statusText.color = color;
    }

    /// <summary>admin 패널 접근 허용 조건.</summary>
    public static bool ShouldAllow() =>
        Application.isEditor || Debug.isDebugBuild || MetaState.IsAdmin;
}
