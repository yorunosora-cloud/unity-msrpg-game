using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Interactable.onInteract 에 와이어링해 씬을 전환한다.
/// 빌더(MesoriaHubBuilder, AtlantisHubBuilder)에서 public 필드를 직접 할당한다.
/// </summary>
public class ScenePortal : MonoBehaviour
{
    public string targetScene;
    public string targetSpawnName;

    public void Go()
    {
        if (!string.IsNullOrEmpty(targetSpawnName))
            PlayerPrefs.SetString("_PortalSpawn", targetSpawnName);
        SceneManager.LoadScene(targetScene);
    }
}
