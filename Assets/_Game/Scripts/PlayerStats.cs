using System;

public class PlayerStats
{
    public int Level   { get; private set; } = 1;
    public int Exp     { get; private set; } = 0;
    public int NextExp { get; private set; } = 100;

    public int MaxHp => 100 + Level * 50;
    public int MaxMp => 50  + Level * 20;
    public int Atk   => 10  + Level * 5;
    public int Def   => 5   + Level * 3;

    public int Hp { get; private set; }
    public int Mp { get; private set; }

    public event Action<string> OnChanged;

    public PlayerStats()
    {
        Hp = MaxHp;
        Mp = MaxMp;
    }

    public void GainExp(int amount)
    {
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
}
