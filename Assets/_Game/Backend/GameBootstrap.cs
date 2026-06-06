// ⚠️ PlayFab SDK가 필요합니다.

using UnityEngine;

/// <summary>
/// Mesoria 씬 진입 시 PlayerStats·MetaState를 서버에서 불러오고,
/// 씬 종료·앱 일시정지 시 자동 저장합니다.
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    /// <summary>현재 플레이 세션의 PlayerStats 인스턴스.</summary>
    public static PlayerStats PlayerStats { get; private set; }

    void Awake()
    {
        // Awake는 모든 Start보다 먼저 실행 → CombatHud.Start()가 구독하기 전에 반드시 설정됨
        PlayerStats = new PlayerStats();
        PlayerRuntime.Stats = PlayerStats;
        MetaState.Init();
    }

    void Start()
    {
        if (PlayFabManager.Instance == null)
        {
            Debug.LogWarning("[GameBootstrap] PlayFabManager가 없습니다. 로컬 기본값을 사용합니다.");
            return;
        }

        // PlayerStats 복원
        SaveService.Load(
            PlayerStats,
            onDone:  () => Debug.Log("[GameBootstrap] PlayerStats 복원 완료"),
            onError: msg => Debug.LogWarning($"[GameBootstrap] PlayerStats 복원 실패: {msg}"));

        // 메타 레이어(재화·로스터·가챠·관리자) 복원
        MetaSaveService.Load(
            onDone:  () => Debug.Log("[GameBootstrap] MetaState 복원 완료"),
            onError: msg => Debug.LogWarning($"[GameBootstrap] MetaState 복원 실패 (기본값 사용): {msg}"));
    }

    void OnApplicationQuit()                        => AutoSave();
    void OnApplicationPause(bool pausing) { if (pausing) AutoSave(); }

    void AutoSave()
    {
        if (PlayFabManager.Instance == null) return;

        if (PlayerStats != null)
            SaveService.Save(PlayerStats,
                onDone:  () => Debug.Log("[GameBootstrap] PlayerStats 자동 저장 완료"),
                onError: msg => Debug.LogWarning($"[GameBootstrap] PlayerStats 자동 저장 실패: {msg}"));

        if (MetaState.IsInitialized)
            MetaSaveService.Save(
                onDone:  () => Debug.Log("[GameBootstrap] MetaState 자동 저장 완료"),
                onError: msg => Debug.LogWarning($"[GameBootstrap] MetaState 자동 저장 실패: {msg}"));
    }
}
