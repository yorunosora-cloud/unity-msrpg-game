using System.Collections;
using UnityEngine;

/// <summary>
/// 씬 로드 직후 PlayerPrefs "_PortalSpawn" 키를 확인해
/// 해당 이름의 스폰 포인트 GO 위치로 플레이어를 순간이동시킨다.
/// 한 프레임 대기 후 이동 — Start 시점에 씬이 완전히 초기화된 뒤 적용.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class SpawnManager : MonoBehaviour
{
    void Start()
    {
        string spawnName = PlayerPrefs.GetString("_PortalSpawn", "");
        if (string.IsNullOrEmpty(spawnName)) return;

        PlayerPrefs.DeleteKey("_PortalSpawn");
        StartCoroutine(TeleportNextFrame(spawnName));
    }

    IEnumerator TeleportNextFrame(string spawnName)
    {
        // 한 프레임 대기: 다른 모든 Start() / Awake() 가 끝난 뒤 이동
        yield return null;

        var spawnGO = GameObject.Find(spawnName);
        if (spawnGO == null)
        {
            Debug.LogWarning($"[SpawnManager] 스폰 포인트 '{spawnName}' 을 찾지 못했습니다.");
            yield break;
        }

        var cc = GetComponent<CharacterController>();
        cc.enabled = false;
        transform.position = spawnGO.transform.position;
        Physics.SyncTransforms();   // CC 내부 상태 즉시 동기화
        cc.enabled = true;

        Debug.Log($"[SpawnManager] '{spawnName}' 로 이동 완료 → {spawnGO.transform.position}");
    }
}
