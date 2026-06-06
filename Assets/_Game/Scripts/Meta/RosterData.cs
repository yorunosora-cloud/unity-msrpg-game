using System;

/// <summary>Roster 직렬화 DTO.</summary>
[Serializable]
public class RosterData
{
    public OwnedCharacter[] owned = new OwnedCharacter[0];
}
