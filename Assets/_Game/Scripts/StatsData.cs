using System;

/// <summary>
/// PlayerStats 직렬화 DTO — PlayFab UserData(JSON) 저장/복원에 사용.
/// JsonUtility 호환을 위해 [Serializable] + 소문자 필드명.
/// </summary>
[Serializable]
public class StatsData
{
    public int level   = 1;
    public int exp     = 0;
    public int nextExp = 100;
    public int hp;
    public int mp;
}
