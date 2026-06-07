using System;
using System.Collections.Generic;

/// <summary>
/// 플레이어 파티 (최대 3명) 런타임 상태. 순수 C# 클래스 (MonoBehaviour 아님).
/// 활성 캐릭터 전환, 기절(downed) 관리, 전멸 감지를 담당한다.
/// </summary>
public class Party
{
    readonly List<CombatCharacter> _members = new();

    public IReadOnlyList<CombatCharacter> Members    => _members;
    public int                            ActiveIndex { get; private set; }
    public CombatCharacter                Active      => _members.Count > 0 ? _members[ActiveIndex] : null;

    /// <summary>활성 캐릭터가 바뀌었을 때 발생.</summary>
    public event Action OnActiveChanged;

    /// <summary>파티 구성(전원 회복 등)이 변경됐을 때 발생.</summary>
    public event Action OnPartyChanged;

    public Party(IEnumerable<CombatCharacter> members)
    {
        foreach (var m in members) _members.Add(m);
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

        ActiveIndex = i;
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

    /// <summary>파티 전원 HP/MP 풀 회복, 활성 인덱스 0 복귀.</summary>
    public void RestoreAll()
    {
        foreach (var m in _members) m.RestoreFull();
        ActiveIndex = 0;
        OnActiveChanged?.Invoke();
        OnPartyChanged?.Invoke();
    }
}
