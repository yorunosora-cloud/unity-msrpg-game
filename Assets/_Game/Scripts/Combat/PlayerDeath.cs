using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// 플레이어 사망/부활 흐름 관리. Player GameObject에 부착.
/// PlayerStats.IsDead 감지 → 이동·전투 비활성 → 사망 오버레이 + 카운트다운 → 부활.
/// </summary>
public class PlayerDeath : MonoBehaviour
{
    [SerializeField] GameObject deathOverlay;
    [SerializeField] TMP_Text   countdownText;

    [SerializeField] float  respawnDelay = 3f;
    [SerializeField] Vector3 spawnPoint  = Vector3.zero;

    PlayerController _controller;
    PlayerCombat     _combat;
    PlayerStats      _stats;
    bool             _isDying;

    void Start()
    {
        _controller = GetComponent<PlayerController>();
        _combat     = GetComponent<PlayerCombat>();
        TrySubscribe();
    }

    void Update()
    {
        if (_stats == null) TrySubscribe();
    }

    void OnDestroy()
    {
        if (_stats != null) _stats.OnChanged -= OnStatsChanged;
    }

    void TrySubscribe()
    {
        var s = PlayerRuntime.Stats;
        if (s == null || s == _stats) return;
        _stats = s;
        _stats.OnChanged += OnStatsChanged;
    }

    void OnStatsChanged(string _)
    {
        if (!_isDying && _stats != null && _stats.IsDead)
            StartCoroutine(DeathRoutine());
    }

    IEnumerator DeathRoutine()
    {
        _isDying = true;

        if (_controller) _controller.enabled = false;
        if (_combat)     _combat.enabled     = false;
        if (deathOverlay) deathOverlay.SetActive(true);

        float t = respawnDelay;
        while (t > 0f)
        {
            if (countdownText) countdownText.text = Mathf.CeilToInt(t).ToString();
            yield return null;
            t -= Time.deltaTime;
        }

        Respawn();
    }

    void Respawn()
    {
        _stats.RestoreAll();

        // CharacterController 비활성 후 텔레포트 (활성 상태에서 position 변경 시 충돌 오작동)
        var cc = GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            transform.position = spawnPoint;
            cc.enabled = true;
        }
        else
        {
            transform.position = spawnPoint;
        }

        EnemySpawner.RespawnAll();

        if (deathOverlay) deathOverlay.SetActive(false);
        if (_controller)  _controller.enabled = true;
        if (_combat)      _combat.enabled     = true;
        _isDying = false;
    }
}
