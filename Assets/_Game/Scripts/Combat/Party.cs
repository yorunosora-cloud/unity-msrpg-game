using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 파티 (최대 3명) 런타임 상태. 순수 C# 클래스 (MonoBehaviour 아님).
/// 활성 캐릭터 전환, 기절(downed) 관리, 전멸 감지를 담당한다.
/// </summary>
public class Party
{
    readonly List<CombatCharacter> _members = new();

    /// <summary>파티 최대 인원 (설계도 22 규칙 2).</summary>
    public const int MaxSize = 5;

    /// <summary>수동 전환 쿨타임(초) (설계도 22 규칙 15).</summary>
    public const float SwitchCooldown = 1f;

    float _switchCd;

    /// <summary>수동 전환까지 남은 쿨타임(초). 0이면 전환 가능.</summary>
    public float SwitchCooldownRemaining => _switchCd;

    public IReadOnlyList<CombatCharacter> Members    => _members;
    public int                            ActiveIndex { get; private set; }
    public CombatCharacter                Active      => _members.Count > 0 ? _members[ActiveIndex] : null;

    /// <summary>활성 캐릭터가 바뀌었을 때 발생.</summary>
    public event Action OnActiveChanged;

    /// <summary>파티 구성(전원 회복 등)이 변경됐을 때 발생.</summary>
    public event Action OnPartyChanged;

    public Party(IEnumerable<CombatCharacter> members)
    {
        foreach (var m in members)
        {
            if (_members.Count >= MaxSize) break;
            _members.Add(m);
        }
        ActiveIndex = 0;
    }

    // ── 상태 ──────────────────────────────────────────────────────────────

    /// <summary>파티 전원이 기절(HP == 0)인 경우 true. 멤버 0명이면 true.</summary>
    public bool AllDowned
    {
        get
        {
            if (_members.Count == 0) return true;
            foreach (var m in _members)
                if (!m.IsDowned) return false;
            return true;
        }
    }

    // ── 전환 ──────────────────────────────────────────────────────────────

    /// <summary>
    /// 인덱스 i의 멤버를 활성으로 전환.
    /// 범위 밖, 기절 멤버, 이미 활성 상태이면 false.
    /// </summary>
    public bool SwitchTo(int i)
    {
        if (i < 0 || i >= _members.Count) return false;
        if (_members[i].IsDowned) return false;
        if (i == ActiveIndex) return false;
        if (_switchCd > 0f) return false;   // 전환 쿨타임 중

        ActiveIndex = i;
        _switchCd   = SwitchCooldown;
        OnActiveChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 살아있는 다음 멤버로 자동 전환(순환 탐색). 없으면 false(전멸).
    /// </summary>
    public bool AutoSwitchToNext()
    {
        int count = _members.Count;
        for (int i = 1; i < count; i++)
        {
            int idx = (ActiveIndex + i) % count;
            if (!_members[idx].IsDowned)
            {
                ActiveIndex = idx;
                OnActiveChanged?.Invoke();
                return true;
            }
        }
        return false; // 전멸
    }

    // ── 시간 진행 (대기 멤버 포함) ─────────────────────────────────────────

    /// <summary>
    /// 전환 쿨타임과 파티 전원(대기 포함)의 스킬 쿨타임을 dt초 진행한다.
    /// 버프 타이머는 포함하지 않는다(활성일 때만 흐르는 규칙은 P2의 StatusHolder에서 처리).
    /// </summary>
    public void Tick(float dt)
    {
        _switchCd = Mathf.Max(0f, _switchCd - dt);
        foreach (var m in _members) m.TickCooldowns(dt);
    }

    /// <summary>파티 전원(대기 포함, 기절 제외)의 MP 자연회복 1틱.</summary>
    public void RegenMpTick(float percentPerSecond, float tickSeconds)
    {
        foreach (var m in _members) m.RegenMpTick(percentPerSecond, tickSeconds);
    }

    /// <summary>파티 전원 HP/MP 풀 회복, 활성 인덱스 0 복귀, 전환 쿨타임 초기화.</summary>
    public void RestoreAll()
    {
        foreach (var m in _members) m.RestoreFull();
        ActiveIndex = 0;
        _switchCd   = 0f;
        OnActiveChanged?.Invoke();
        OnPartyChanged?.Invoke();
    }
}
