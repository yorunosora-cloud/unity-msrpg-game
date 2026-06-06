using System;

/// <summary>가챠 천장 카운터 런타임 상태.</summary>
public class GachaState
{
    public int PityCounter { get; private set; } = 0;

    public void IncrementPity() => PityCounter++;
    public void ResetPity()     => PityCounter = 0;

    public GachaStateData Export()    => new GachaStateData { pityCounter = PityCounter };

    public void LoadState(GachaStateData data)
    {
        if (data == null) return;
        PityCounter = Math.Max(0, data.pityCounter);
    }
}

[Serializable]
public class GachaStateData
{
    public int pityCounter = 0;
}
