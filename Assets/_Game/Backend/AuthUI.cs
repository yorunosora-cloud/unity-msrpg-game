// ⚠️ PlayFab SDK가 필요합니다.

using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// 로그인/회원가입 UI 컨트롤러.
/// MSRPG > Setup Login Scene 에디터 메뉴로 생성된 Login 씬에서 사용합니다.
/// </summary>
public class AuthUI : MonoBehaviour
{
    [Header("패널")]
    [SerializeField] GameObject loginPanel;
    [SerializeField] GameObject registerPanel;

    [Header("로그인 입력")]
    [SerializeField] TMP_InputField loginIdOrEmailInput;
    [SerializeField] TMP_InputField loginPasswordInput;
    [SerializeField] Button         loginButton;

    [Header("회원가입 입력")]
    [SerializeField] TMP_InputField registerEmailInput;
    [SerializeField] TMP_InputField registerUsernameInput;
    [SerializeField] TMP_InputField registerPasswordInput;
    [SerializeField] Button         registerButton;

    [Header("공용")]
    [SerializeField] TMP_Text statusText;
    [SerializeField] string   gameSceneName = "Mesoria";

    bool _isRegistering;

    static readonly Regex EmailRegex    = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    static readonly Regex UsernameRegex = new Regex(@"^[A-Za-z0-9_\-]{3,24}$");

    // ── 생명주기 ──────────────────────────────────────────────────────────────

    void Start()
    {
        ShowLogin();

        if (PlayFabManager.Instance == null)
        {
            Debug.LogError("[AuthUI] PlayFabManager 없음.");
            return;
        }
        PlayFabManager.Instance.OnAuthSuccess += HandleAuthSuccess;
        PlayFabManager.Instance.OnAuthError   += HandleAuthError;
    }

    void OnDestroy()
    {
        if (PlayFabManager.Instance == null) return;
        PlayFabManager.Instance.OnAuthSuccess -= HandleAuthSuccess;
        PlayFabManager.Instance.OnAuthError   -= HandleAuthError;
    }

    // ── 버튼 이벤트 ───────────────────────────────────────────────────────────

    public void OnLoginClicked()
    {
        string id  = loginIdOrEmailInput.text.Trim();
        string pwd = loginPasswordInput.text;

        if (!ValidateLogin(id, pwd)) return;

        _isRegistering = false;
        SetBusy(true);
        ShowStatus("로그인 중...", Color.white);
        PlayFabManager.Instance.Login(id, pwd);
    }

    public void OnRegisterClicked()
    {
        string email    = registerEmailInput.text.Trim();
        string username = registerUsernameInput.text.Trim();
        string pwd      = registerPasswordInput.text;

        if (!ValidateRegister(email, username, pwd)) return;

        _isRegistering = true;
        SetBusy(true);
        ShowStatus("가입 처리 중...", Color.white);
        PlayFabManager.Instance.Register(email, username, pwd);
    }

    public void ShowLogin()
    {
        loginPanel.SetActive(true);
        registerPanel.SetActive(false);
        ClearStatus();
    }

    public void ShowRegister()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(true);
        ClearStatus();
    }

    // ── 입력값 검증 ───────────────────────────────────────────────────────────

    bool ValidateLogin(string idOrEmail, string password)
    {
        if (string.IsNullOrEmpty(idOrEmail))
        { ShowStatus("아이디 또는 이메일을 입력해주세요.", Color.yellow); return false; }

        if (string.IsNullOrEmpty(password))
        { ShowStatus("비밀번호를 입력해주세요.", Color.yellow); return false; }

        return true;
    }

    bool ValidateRegister(string email, string username, string password)
    {
        if (string.IsNullOrEmpty(email))
        { ShowStatus("이메일을 입력해주세요.", Color.yellow); return false; }

        if (!EmailRegex.IsMatch(email))
        { ShowStatus("올바른 이메일 형식이 아닙니다. (예: abc@gmail.com)", Color.yellow); return false; }

        if (string.IsNullOrEmpty(username))
        { ShowStatus("아이디를 입력해주세요.", Color.yellow); return false; }

        if (!UsernameRegex.IsMatch(username))
        { ShowStatus("아이디는 영문·숫자·_·- 만 사용 가능하며 3~24자여야 합니다.", Color.yellow); return false; }

        if (string.IsNullOrEmpty(password))
        { ShowStatus("비밀번호를 입력해주세요.", Color.yellow); return false; }

        if (password.Length < 6)
        { ShowStatus("비밀번호는 6자 이상이어야 합니다.", Color.yellow); return false; }

        return true;
    }

    // ── PlayFab 콜백 ─────────────────────────────────────────────────────────

    void HandleAuthSuccess()
    {
        SetBusy(false);

        if (_isRegistering)
        {
            ShowStatus("가입 완료! 게임 불러오는 중...", Color.green);
            StartCoroutine(LoadSceneDelayed(3f));
        }
        else
        {
            ShowStatus("로그인 성공! 게임 불러오는 중...", Color.green);
            StartCoroutine(LoadSceneDelayed(0.8f));
        }
    }

    void HandleAuthError(string msg)
    {
        SetBusy(false);
        ShowStatus(msg, Color.red);
    }

    IEnumerator LoadSceneDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(gameSceneName);
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────────

    void SetBusy(bool busy)
    {
        loginButton.interactable    = !busy;
        registerButton.interactable = !busy;
    }

    void ShowStatus(string msg, Color color)
    {
        if (statusText == null) return;
        statusText.text  = msg;
        statusText.color = color;
    }

    void ClearStatus()
    {
        if (statusText == null) return;
        statusText.text = "";
    }
}
