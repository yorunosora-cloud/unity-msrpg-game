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
