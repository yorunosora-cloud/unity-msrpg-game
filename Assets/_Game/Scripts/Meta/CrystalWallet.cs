using System;

/// <summary>
/// 결정(進化 재화) 잔액 보관 — 9종 CrystalKind 인덱스 배열 기반.
/// Wallet과 동일한 패턴: Add / Get / TrySpend / OnChanged / Export / LoadState.
/// </summary>
public class CrystalWallet
{
    const int Count = 9; // CrystalKind 값 수

    readonly int[] _amounts = new int[Count];

    /// <summary>잔액 변화 시 발생 (UI 갱신용).</summary>
    public event Action OnChanged;

    // ── 조작 ──────────────────────────────────────────────────────────────

    public void Add(CrystalKind kind, int amount)
    {
        if (amount <= 0) return;
        _amounts[(int)kind] += amount;
        OnChanged?.Invoke();
    }

    public int Get(CrystalKind kind) => _amounts[(int)kind];

    public bool CanAfford(CrystalKind kind, int amount) => Get(kind) >= amount;

    /// <summary>잔액이 충분하면 차감 후 true, 부족하면 false.</summary>
    public bool TrySpend(CrystalKind kind, int amount)
    {
        if (!CanAfford(kind, amount)) return false;
        _amounts[(int)kind] -= amount;
        OnChanged?.Invoke();
        return true;
    }

    // ── 직렬화 ────────────────────────────────────────────────────────────

    public CrystalWalletData Export()
    {
        var data = new CrystalWalletData { amounts = new int[Count] };
        Array.Copy(_amounts, data.amounts, Count);
        return data;
    }

    public void LoadState(CrystalWalletData data)
    {
        if (data == null || data.amounts == null) return;
        int len = Math.Min(data.amounts.Length, Count);
        for (int i = 0; i < len; i++)
            _amounts[i] = Math.Max(0, data.amounts[i]);
        OnChanged?.Invoke();
    }
}

/// <summary>CrystalWallet 직렬화 DTO — PlayFab UserData(JSON) 저장/복원용.</summary>
[Serializable]
public class CrystalWalletData
{
    public int[] amounts = new int[9];
}
