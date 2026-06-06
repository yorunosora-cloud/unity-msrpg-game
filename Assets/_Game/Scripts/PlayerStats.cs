using System;

public class PlayerStats
{
    // ──────────────────────────────────────────────────────────────────────────
    // 서버 저장/복원
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>현재 스탯을 직렬화 가능한 DTO로 내보냅니다.</summary>
    public StatsData Export() => new StatsData
    {
        level   = Level,
        exp     = Exp,
        nextExp = NextExp,
        hp      = Hp,
        mp      = Mp,
    };

    /// <summary>서버에서 불러온 DTO로 스탯을 복원합니다. null이면 초기값 유지.</summary>
    public void LoadState(StatsData data)
    {
        if (data == null) return;
        Level   = Math.Max(1, data.level);
        Exp     = Math.Max(0, data.exp);
        NextExp = Math.Max(100, data.nextExp);
        Hp      = Math.Clamp(data.hp, 0, MaxHp);
        Mp      = Math.Clamp(data.mp, 0, MaxMp);
        OnChanged?.Invoke("load");
    }


    public int Level   { get; private set; } = 1;
    public int Exp     { get; private set; } = 0;
    public int NextExp { get; private set; } = 100;

    public int MaxHp => 100 + Level * 50;
    public int MaxMp => 50  + Level * 20;
    public int Atk   => 10  + Level * 5;
    public int Def   => 5   + Level * 3;

    public int Hp { get; private set; }
    public int Mp { get; private set; }

    public bool IsDead => Hp <= 0;

    public event Action<string> OnChanged;

    public PlayerStats()
    {
        Hp = MaxHp;
        Mp = MaxMp;
    }

    public void GainExp(int amount)
    {
        if (amount <= 0) return;
        int prevLevel = Level;
        Exp += amount;
        while (Exp >= NextExp)
        {
            Exp    -= NextExp;
            Level++;
            NextExp = (int)Math.Floor(NextExp * 1.45);
            Hp      = MaxHp;
            Mp      = MaxMp;
            OnChanged?.Invoke("levelup");
        }
        if (Level == prevLevel)
            OnChanged?.Invoke("exp");
    }

    public void Damage(int amount)
    {
        Hp = Math.Max(0, Hp - amount);
        OnChanged?.Invoke("hp");
    }

    public void Heal(int amount)
    {
        Hp = Math.Min(MaxHp, Hp + amount);
        OnChanged?.Invoke("hp");
    }

    public bool UseMp(int amount)
    {
        if (Mp < amount) return false;
        Mp -= amount;
        OnChanged?.Invoke("mp");
        return true;
    }

    /// <summary>부활 시 HP·MP를 레벨 기준 최대치로 완전 회복합니다.</summary>
    public void RestoreAll()
    {
        Hp = MaxHp;
        Mp = MaxMp;
        OnChanged?.Invoke("restore");
    }
}
