// ⚠️ PlayFab SDK가 필요합니다.

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// 인게임 계정 정보 패널. ESC 키 또는 우상단 계정 버튼으로 열고 닫습니다.
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

    // ─────────────────────────────────────────────────────────────────────────

    PlayerStats _stats;

    void Start()
    {
        panel.SetActive(false);
        openButton.onClick.AddListener(Open);
        closeButton.onClick.AddListener(ClosePanel);
        logoutButton.onClick.AddListener(OnLogout);
        TrySubscribe();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Toggle();
        if (_stats == null) TrySubscribe(); // Awake 순서 방어
    }

    void OnDestroy()
    {
        if (_stats != null) _stats.OnChanged -= OnStatsChanged;
    }

    void TrySubscribe()
    {
        var s = GameBootstrap.PlayerStats;
        if (s == null || s == _stats) return;
        _stats = s;
        _stats.OnChanged += OnStatsChanged;
    }

    void OnStatsChanged(string reason)
    {
        if (panel.activeSelf) RefreshInfo(); // 패널 열려있으면 즉시 갱신

        if (reason == "levelup")
        {
            // 레벨업 알림 — 플레이어 위치 위에 스폰
            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null)
                DamageNumber.SpawnLevelUp(playerGO.transform.position, _stats.Level);
        }
    }

    // ── 공개 메서드 ───────────────────────────────────────────────────────────

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

    // ── 내부 ─────────────────────────────────────────────────────────────────

    void RefreshInfo()
    {
        string username = PlayFabManager.Instance != null ? PlayFabManager.Instance.Username : "—";
        usernameText.text = $"아이디: {username}";

        var stats = GameBootstrap.PlayerStats;
        levelText.text = stats != null ? $"레벨: {stats.Level}   EXP: {stats.Exp} / {stats.NextExp}" : "레벨: —";
    }

    void OnLogout()
    {
        if (PlayFabManager.Instance != null)
            PlayFabManager.Instance.Logout();
    }
}
