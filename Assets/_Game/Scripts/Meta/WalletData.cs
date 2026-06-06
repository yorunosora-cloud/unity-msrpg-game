using System;

/// <summary>Wallet 직렬화 DTO — PlayFab UserData(JSON) 저장/복원용.</summary>
[Serializable]
public class WalletData
{
    public int gold     = 5000;
    public int paper    = 30;
    public int focus    = 120;
    public int fragment = 10;
}
