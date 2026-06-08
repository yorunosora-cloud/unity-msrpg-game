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
    /// PlayFab ReadOnlyData "isAdmin" 값.
    /// true이면 인게임 관리자 패널 노출.
    /// </summary>
    public static bool IsAdmin { get; set; }

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
    }
}
