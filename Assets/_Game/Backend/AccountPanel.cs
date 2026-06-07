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

    // ─────────────────────────────────────────────────────────────────────────

    CombatCharacter _active;
    Party           _party;

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
        TrySubscribe(); // 파티 빌드 전일 수 있어 매 프레임 재시도
    }

    void OnDestroy()
    {
        UnsubscribeActive();
        UnsubscribeParty();
    }

    // ── 구독 ──────────────────────────────────────────────────────────────

    void TrySubscribe()
    {
        var newParty = PlayerRuntime.Party;
        if (newParty != null && newParty != _party)
        {
            UnsubscribeParty();
            _party = newParty;
            _party.OnActiveChanged += OnActiveCharacterChanged;
        }

        var newActive = PlayerRuntime.Active;
        if (newActive != null && newActive != _active)
        {
            UnsubscribeActive();
            _active = newActive;
            _active.OnChanged += OnActiveChanged;
        }
    }

    void UnsubscribeActive()
    {
        if (_active != null)
        {
            _active.OnChanged -= OnActiveChanged;
            _active = null;
        }
    }

    void UnsubscribeParty()
    {
        if (_party != null)
        {
            _party.OnActiveChanged -= OnActiveCharacterChanged;
            _party = null;
        }
    }

    void OnActiveCharacterChanged()
    {
        UnsubscribeActive();
        TrySubscribe();
        if (panel.activeSelf) RefreshInfo();
    }

    void OnActiveChanged(string reason)
    {
        if (panel.activeSelf) RefreshInfo();

        if (reason == "levelup")
        {
            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null && _active != null)
                DamageNumber.SpawnLevelUp(playerGO.transform.position, _active.Level);
        }
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

        if (_active != null)
            levelText.text = $"{_active.DisplayName}  Lv.{_active.Level}   EXP: {_active.Exp} / {_active.NextExp}";
        else
            levelText.text = "레벨: —";
    }

    void OnLogout()
    {
        if (PlayFabManager.Instance != null)
            PlayFabManager.Instance.Logout();
    }
}
