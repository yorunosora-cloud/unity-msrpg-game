/// <summary>
/// 메타 레이어 전역 상태 홀더.
/// GameBootstrap.Start()에서 Init() 후 MetaSaveService.Load()로 복원합니다.
/// </summary>
public static class MetaState
{
    public static Wallet               Wallet         { get; private set; }
    public static Roster               Roster         { get; private set; }
    public static GachaState           GachaState     { get; private set; }
    public static CrystalWallet        Crystals       { get; private set; }
    public static StudyMaterialWallet  StudyMaterials { get; private set; }

    /// <summary>
    /// 세션 한정 관리자 플래그. CloudScript VerifyAdminKey 인증 성공 시 true로 세팅.
    /// 게임 재시작(Init 호출) 시 false로 초기화됨.
    /// </summary>
    public static bool IsAdmin { get; set; }

    /// <summary>
    /// 세션 한정 관리자 CloudScript 인증 키. VerifyAdminKey 성공 시 캐시되어
    /// UpsertProblem/DeleteProblem 같은 관리자 전용 CloudScript 호출에 재사용된다.
    /// 저장되지 않으며, 게임 재시작 시 빈 문자열로 초기화된다.
    /// </summary>
    public static string AdminKey { get; set; } = "";

    /// <summary>Init() 호출 여부. false이면 아직 초기화 전.</summary>
    public static bool IsInitialized => Wallet != null;

    /// <summary>모든 메타 상태를 기본값으로 초기화합니다.</summary>
    public static void Init()
    {
        Wallet         = new Wallet();
        Roster         = new Roster();
        GachaState     = new GachaState();
        Crystals       = new CrystalWallet();
        StudyMaterials = new StudyMaterialWallet();
        IsAdmin        = false;
        AdminKey       = "";
    }
}
