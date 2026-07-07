using System;
using System.Collections.Generic;

/// <summary>플레이어가 보유한 캐릭터 목록(인벤토리).</summary>
public class Roster
{
    readonly List<OwnedCharacter> _owned = new List<OwnedCharacter>();

    public IReadOnlyList<OwnedCharacter> Owned => _owned;

    /// <summary>보유 캐릭터 수 변화 시 발생 (UI 갱신용).</summary>
    public event Action OnChanged;

    // ── 조작 ──────────────────────────────────────────────────────────────

    /// <summary>
    /// 캐릭터 추가. 신규이면 추가 후 true,
    /// 이미 보유 중이면 dupes++ 후 false.
    /// </summary>
    public bool Add(string id)
    {
        var existing = Get(id);
        if (existing != null)
        {
            existing.dupes++;
            OnChanged?.Invoke();
            return false;
        }
        _owned.Add(new OwnedCharacter { id = id });
        OnChanged?.Invoke();
        return true;
    }

    public bool Has(string id) => Get(id) != null;

    public OwnedCharacter Get(string id)
    {
        foreach (var c in _owned)
            if (c.id == id) return c;
        return null;
    }

    /// <summary>
    /// 캐릭터 회수. 보유 중이었으면 제거 후 true, 없었으면 false.
    /// </summary>
    public bool Remove(string id)
    {
        for (int i = 0; i < _owned.Count; i++)
        {
            if (_owned[i].id == id)
            {
                _owned.RemoveAt(i);
                OnChanged?.Invoke();
                return true;
            }
        }
        return false;
    }

    /// <summary>UI 갱신이 필요할 때 외부에서 OnChanged 를 강제 발화한다.</summary>
    public void NotifyChanged() => OnChanged?.Invoke();

    // ── 직렬화 ────────────────────────────────────────────────────────────

    public RosterData Export() => new RosterData { owned = _owned.ToArray() };

    public void LoadState(RosterData data)
    {
        if (data == null) return;
        _owned.Clear();
        if (data.owned != null)
            _owned.AddRange(data.owned);
        OnChanged?.Invoke();
    }
}
