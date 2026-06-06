using System;

/// <summary>
/// 재화 지갑 (설계 §12).
/// 시작 잔액: 골드 5000 / 논문 30 / 집중력 120 / 조각 10.
/// </summary>
public class Wallet
{
    public int Gold     { get; private set; } = 5000;
    public int Paper    { get; private set; } = 30;
    public int Focus    { get; private set; } = 120;
    public int Fragment { get; private set; } = 10;

    /// <summary>재화 변화 시 발생 (UI 갱신용).</summary>
    public event Action OnChanged;

    // ── 조작 ──────────────────────────────────────────────────────────────

    public void Add(CurrencyKind kind, int amount)
    {
        if (amount <= 0) return;
        switch (kind)
        {
            case CurrencyKind.Gold:     Gold     += amount; break;
            case CurrencyKind.Paper:    Paper    += amount; break;
            case CurrencyKind.Focus:    Focus    += amount; break;
            case CurrencyKind.Fragment: Fragment += amount; break;
        }
        OnChanged?.Invoke();
    }

    public bool CanAfford(CurrencyKind kind, int amount) => Get(kind) >= amount;

    /// <summary>잔액이 충분하면 차감하고 true, 부족하면 false.</summary>
    public bool TrySpend(CurrencyKind kind, int amount)
    {
        if (!CanAfford(kind, amount)) return false;
        switch (kind)
        {
            case CurrencyKind.Gold:     Gold     -= amount; break;
            case CurrencyKind.Paper:    Paper    -= amount; break;
            case CurrencyKind.Focus:    Focus    -= amount; break;
            case CurrencyKind.Fragment: Fragment -= amount; break;
        }
        OnChanged?.Invoke();
        return true;
    }

    public int Get(CurrencyKind kind) => kind switch
    {
        CurrencyKind.Gold     => Gold,
        CurrencyKind.Paper    => Paper,
        CurrencyKind.Focus    => Focus,
        CurrencyKind.Fragment => Fragment,
        _                     => 0,
    };

    // ── 직렬화 ────────────────────────────────────────────────────────────

    public WalletData Export() => new WalletData
    {
        gold     = Gold,
        paper    = Paper,
        focus    = Focus,
        fragment = Fragment,
    };

    public void LoadState(WalletData data)
    {
        if (data == null) return;
        Gold     = Math.Max(0, data.gold);
        Paper    = Math.Max(0, data.paper);
        Focus    = Math.Max(0, data.focus);
        Fragment = Math.Max(0, data.fragment);
        OnChanged?.Invoke();
    }
}
