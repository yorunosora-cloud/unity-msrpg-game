using System;

/// <summary>플레이어가 보유 중인 캐릭터 1개의 인스턴스 데이터.</summary>
[Serializable]
public class OwnedCharacter
{
    public string id;
    public int    level = 1;
    public int    dupes = 0; // 중복 획득 횟수
}
