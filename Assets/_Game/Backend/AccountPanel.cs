// ⚠️ PlayFab SDK가 필요합니다.

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// 인게임 계정 정보 패널. ESC 키 또는 우상단 계정 버튼으로 열고 닫습니다.
/// 활성 캐릭터(PlayerRuntime.Active)의 이름·레벨을 표시하고, 레벨업 시 알림.
/// MesoriaSetup 에디터 메뉴가 자동으로 씬에 추가합니다.
/// </summary>
public class AccountPanel : MonoBehaviour
{
    [Header("패널")]
    [SerializeField] GameObject panel;

    [Header("정보 텍스트")]
    [SerializeField] TMP_Text usernameText;
    [SerializeField] TMP_Text levelText;

    [Header("버튼")]
    [SerializeField] Button openButton;
    [SerializeField] Button closeButton;
    [SerializeField] Button logoutButton;
    [SerializeField] Button adminLoginButton;
    [SerializeField] Button mailboxButton;

    [Header("우편함 배지")]
    [SerializeField] TMP_Text mailboxBadge;   // 0이면 비활성화(숨김) — 계정 패널 내부, 우편함 버튼 위
    [SerializeField] TMP_Text accountBadge;   // 0이면 비활성화(숨김) — 우상단 계정 열기 버튼 위, 패널을 안 열어도 보임

    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        panel.SetActive(false);
        openButton.onClick.AddListener(Open);
        closeButton.onClick.AddListener(ClosePanel);
        logoutButton.onClick.AddListener(OnLogout);
        if (adminLoginButton != null) adminLoginButton.onClick.AddListener(OnAdminLogin);
        if (mailboxButton    != null) mailboxButton.onClick.AddListener(OnMailboxClicked);

        // GameBootstrap.Awake()가 모든 Start()보다 먼저 MetaState.Init()을 호출하므로 여기서는 항상 초기화되어 있다.
        if (MetaState.IsInitialized)
        {
            MetaState.Mailbox.OnChanged += RefreshAllBadges;
            RefreshAllBadges();
        }
    }

    void OnDestroy()
    {
        if (MetaState.IsInitialized) MetaState.Mailbox.OnChanged -= RefreshAllBadges;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Toggle();
    }

    // ── 공개 메서드 ───────────────────────────────────────────────────────

    public void Open()
    {
        RefreshInfo();
        panel.SetActive(true);
    }

    public void Toggle()
    {
        if (panel.activeSelf)
            ClosePanel();
        else if (UIManager.TryOpen())
            Open();
    }

    public void ClosePanel()
    {
        panel.SetActive(false);
        UIManager.Close();
    }

    // ── 내부 ─────────────────────────────────────────────────────────────

    void RefreshInfo()
    {
        string username = PlayFabManager.Instance != null ? PlayFabManager.Instance.Username : "—";
        usernameText.text = $"아이디: {username}";

        int playerLevel = PlayerRuntime.Stats?.Level ?? 1;
        levelText.text = $"플레이어 Lv.{playerLevel}";

        RefreshAllBadges();
    }

    /// <summary>계정 패널 내부 배지 + 우상단 계정 열기 버튼 배지를 함께 갱신한다.
    /// Mailbox.OnChanged 구독으로 수령·확인 직후에도 즉시 갱신되고, 패널을 열지 않아도
    /// 우상단 배지로 미확인 우편을 알 수 있다(패널 내부 배지는 패널을 열었을 때만 보임).</summary>
    void RefreshAllBadges()
    {
        if (!MetaState.IsInitialized) return;
        int n = MetaState.Mailbox.UnreadCount();
        string text = n > 99 ? "99+" : n.ToString();

        if (mailboxBadge != null)
        {
            mailboxBadge.gameObject.SetActive(n > 0);
            if (n > 0) mailboxBadge.text = text;
        }
        if (accountBadge != null)
        {
            accountBadge.gameObject.SetActive(n > 0);
            if (n > 0) accountBadge.text = text;
        }
    }

    void OnLogout()
    {
        if (PlayFabManager.Instance != null)
            PlayFabManager.Instance.Logout();
    }

    void OnAdminLogin()
    {
        ClosePanel();
        // AdminLoginPanel은 별도 캔버스(MetaCanvas)에 있으므로 런타임 탐색
        var loginPanel = FindFirstObjectByType<AdminLoginPanel>(FindObjectsInactive.Include);
        loginPanel?.Open();
    }

    void OnMailboxClicked()
    {
        ClosePanel();
        // MailboxPanel은 별도 캔버스(MetaCanvas)에 있으므로 런타임 탐색.
        // Open() 직전에 서버 재조회해 관리자가 그 사이 보낸 우편을 반영한다.
        var mailboxPanel = FindFirstObjectByType<MailboxPanel>(FindObjectsInactive.Include);
        if (mailboxPanel == null) return;
        MetaSaveService.RefreshMailbox(() => mailboxPanel.Open());
    }
}
