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

    /// <summary>전투 시작 위치 기록(영벡터). 전투 이벤트 시 NotifyCombat, 매 프레임 Tick.</summary>
    public CombatAnchor Anchor { get; } = new CombatAnchor();

    /// <summary>
    /// 파티 존재 수정자(고유 시간·전자기력)의 능력치 k 배율. 활성·대기 무관, 같은 패시브 id는 1회만(설계도 §4-7 규칙 9).
    /// </summary>
    public float PresenceMult(StatKind k)
    {
        float m = 1f; HashSet<string> seen = null;
        foreach (var mem in _members)
        {
            if (!mem.HasPartyWideModifiers) continue;
            foreach (var (id, mod) in mem.PartyWideModifiers())
            {
                if (mod.Stat != k) continue;
                seen ??= new HashSet<string>();
                if (!seen.Add(id)) continue;
                m *= mod.Multiplier(0f);
            }
        }
        return m;
    }

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
            m.Party = this;
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
        _members[i].OnSwitchIn();
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
                _members[idx].OnSwitchIn();
                OnActiveChanged?.Invoke();
                return true;
            }
        }
        return false; // 전멸
    }

    // ── 시간 진행 (대기 멤버 포함) ─────────────────────────────────────────

    /// <summary>
    /// 전환 쿨타임, 파티 전원(대기 포함)의 스킬 쿨타임, 상태 효과를 dt초 진행한다.
    /// 상태 효과는 활성 여부를 전달해 버프는 활성일 때만, 해로운 효과는 항상 흐른다.
    /// </summary>
    public void Tick(float dt)
    {
        _switchCd = Mathf.Max(0f, _switchCd - dt);
        Anchor.Tick(dt);
        for (int i = 0; i < _members.Count; i++)
        {
            // 쿨타임은 CooldownRate 배율만큼 빠르게(공격속도·고유 시간 패시브용)
            _members[i].TickCooldowns(dt * _members[i].StatMult(StatKind.CooldownRate));
            _members[i].TickStatus(dt, i == ActiveIndex);
        }
    }

    /// <summary>파티 전원(대기 포함, 기절 제외)의 MP 자연회복 1틱.</summary>
    public void RegenMpTick(float percentPerSecond, float tickSeconds)
    {
        foreach (var m in _members) m.RegenMpTick(percentPerSecond, tickSeconds);
    }

    /// <summary>전투 구역 진입·이탈·씬 전환: 전원의 상태·스택·폼 초기화(HP·MP·쿨타임 유지). 설계도 22 §4-12.</summary>
    public void ResetCombatState()
    {
        foreach (var m in _members) m.ResetCombatState();
        Anchor.Clear();
        OnPartyChanged?.Invoke();
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
