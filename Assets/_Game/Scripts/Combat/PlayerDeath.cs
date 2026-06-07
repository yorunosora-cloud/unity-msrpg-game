using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// 파티 전원 기절(AllDowned) 감지 → 사망 오버레이 + 카운트다운 → 전원 회복·부활.
/// 단일 캐릭터 기절은 PartyController가 자동 교체로 처리한다.
/// Player GameObject에 부착.
/// </summary>
public class PlayerDeath : MonoBehaviour
{
    [SerializeField] GameObject deathOverlay;
    [SerializeField] TMP_Text   countdownText;

    [SerializeField] float   respawnDelay = 3f;
    [SerializeField] Vector3 spawnPoint   = Vector3.zero;

    PlayerController _controller;
    PlayerCombat     _combat;
    PartyController  _partyController;
    bool             _isDying;

    // 마지막으로 구독한 파티 인스턴스 (재구성 시 재구독)
    Party _subscribedParty;

    void Start()
    {
        _controller      = GetComponent<PlayerController>();
        _combat          = GetComponent<PlayerCombat>();
        _partyController = GetComponent<PartyController>();
        TrySubscribeParty();
    }

    void Update()
    {
        // 파티가 아직 빌드 중일 수 있어 매 프레임 재시도
        TrySubscribeParty();
    }

    void OnDestroy()
    {
        if (_subscribedParty != null)
        {
            _subscribedParty.OnPartyChanged -= OnPartyStateChanged;
            foreach (var m in _subscribedParty.Members)
                m.OnChanged -= OnMemberChangedHandler;
        }
    }

    // ── 구독 ──────────────────────────────────────────────────────────────

    void TrySubscribeParty()
    {
        var party = PlayerRuntime.Party;
        if (party == null || party == _subscribedParty) return;

        // 이전 구독 해제
        if (_subscribedParty != null)
            _subscribedParty.OnPartyChanged -= OnPartyStateChanged;

        _subscribedParty = party;
        _subscribedParty.OnPartyChanged += OnPartyStateChanged;
        SubscribeMembers(party);
    }

    void SubscribeMembers(Party party)
    {
        foreach (var m in party.Members)
            m.OnChanged += OnMemberChangedHandler;
    }

    void OnPartyStateChanged()
    {
        // 파티 재구성 후 → 새 파티로 재구독
        TrySubscribeParty();
    }

    void OnMemberChangedHandler(string _) => CheckAllDowned();

    void CheckAllDowned()
    {
        if (_isDying) return;
        var party = PlayerRuntime.Party;
        if (party != null && party.AllDowned)
            StartCoroutine(DeathRoutine());
    }

    // ── 사망/부활 흐름 ────────────────────────────────────────────────────

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
        // 파티 전원 HP/MP 풀 회복 + 활성 0번 복귀
        var party = PlayerRuntime.Party;
        if (party != null)
        {
            party.RestoreAll();
            PlayerRuntime.Active = party.Active;
        }

        // CharacterController 비활성 후 텔레포트
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
