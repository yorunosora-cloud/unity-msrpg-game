// ⚠️ PlayFab SDK가 필요합니다.

using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using PlayFab;
using PlayFab.ClientModels;

/// <summary>
/// PlayFab 인증 싱글톤 — 씬 전환 후에도 유지됩니다(DontDestroyOnLoad).
/// </summary>
public class PlayFabManager : MonoBehaviour
{
    public static PlayFabManager Instance { get; private set; }

    [Header("PlayFab Settings")]
    [Tooltip("PlayFab Game Manager에서 발급받은 Title ID (예: AB12C)")]
    [SerializeField] private string titleId = "";

    /// <summary>로그인한 플레이어의 아이디(Username). 로그인 전엔 빈 문자열.</summary>
    public string Username { get; private set; } = "";

    // ── 이벤트 ───────────────────────────────────────────────────────────────
    public event Action         OnAuthSuccess;
    public event Action<string> OnAuthError;

    static readonly GetPlayerCombinedInfoRequestParams InfoParams =
        new GetPlayerCombinedInfoRequestParams { GetUserAccountInfo = true };

    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!string.IsNullOrEmpty(titleId))
            PlayFabSettings.TitleId = titleId;

        if (string.IsNullOrEmpty(PlayFabSettings.TitleId))
            Debug.LogError("[PlayFab] Title ID가 설정되지 않았습니다! PlayFab > Edit Settings를 확인하세요.");
    }

    // ── 회원가입 ──────────────────────────────────────────────────────────────

    public void Register(string email, string username, string password)
    {
        PlayFabClientAPI.RegisterPlayFabUser(
            new RegisterPlayFabUserRequest
            {
                Email                       = email,
                Username                    = username,
                Password                    = password,
                RequireBothUsernameAndEmail = true,
                DisplayName                 = username,
            },
            result =>
            {
                Username = result.Username ?? username;
                Debug.Log($"[PlayFab] 회원가입 성공: {Username}");
                OnAuthSuccess?.Invoke();
            },
            err => HandleError(err));
    }

    // ── 로그인 ────────────────────────────────────────────────────────────────

    /// <summary>'@' 포함이면 이메일로, 아니면 아이디로 로그인합니다.</summary>
    public void Login(string idOrEmail, string password)
    {
        if (idOrEmail.Contains("@"))
        {
            PlayFabClientAPI.LoginWithEmailAddress(
                new LoginWithEmailAddressRequest
                {
                    Email                 = idOrEmail,
                    Password              = password,
                    InfoRequestParameters = InfoParams,
                },
                result =>
                {
                    Username = result.InfoResultPayload?.AccountInfo?.Username ?? idOrEmail;
                    Debug.Log($"[PlayFab] 로그인 성공 (이메일): {Username}");
                    OnAuthSuccess?.Invoke();
                },
                err => HandleError(err));
        }
        else
        {
            PlayFabClientAPI.LoginWithPlayFab(
                new LoginWithPlayFabRequest
                {
                    Username              = idOrEmail,
                    Password              = password,
                    InfoRequestParameters = InfoParams,
                },
                result =>
                {
                    Username = result.InfoResultPayload?.AccountInfo?.Username ?? idOrEmail;
                    Debug.Log($"[PlayFab] 로그인 성공 (아이디): {Username}");
                    OnAuthSuccess?.Invoke();
                },
                err => HandleError(err));
        }
    }

    // ── 로그아웃 ──────────────────────────────────────────────────────────────

    /// <summary>PlayFab 세션을 끊고 로그인 씬으로 돌아갑니다.</summary>
    public void Logout()
    {
        PlayFabClientAPI.ForgetAllCredentials();
        Username = "";
        SceneManager.LoadScene("Login");
    }

    // ── 오류 처리 ─────────────────────────────────────────────────────────────

    void HandleError(PlayFabError error)
    {
        Debug.LogWarning($"[PlayFab] 오류: {error.GenerateErrorReport()}");
        OnAuthError?.Invoke(ToKorean(error));
    }

    static string ToKorean(PlayFabError e) => e.Error switch
    {
        PlayFabErrorCode.InvalidParams              => "입력값이 올바르지 않습니다.",
        PlayFabErrorCode.EmailAddressNotAvailable   => "이미 사용 중인 이메일입니다.",
        PlayFabErrorCode.UsernameNotAvailable       => "이미 사용 중인 아이디입니다.",
        PlayFabErrorCode.InvalidUsernameOrPassword  => "아이디/이메일 또는 비밀번호가 틀렸습니다.",
        PlayFabErrorCode.AccountNotFound            => "계정을 찾을 수 없습니다.",
        _                                           => $"오류가 발생했습니다. ({e.Error})",
    };
}
