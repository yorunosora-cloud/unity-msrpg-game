using System;
using System.Collections.Generic;

/// <summary>플레이어가 보유 중인 캐릭터 1개의 인스턴스 데이터.</summary>
[Serializable]
public class OwnedCharacter
{
    public string id;
    public int    level = 1;
    public int    exp   = 0; // 누적 EXP (레벨업 후 잔여 보존)
    public int    dupes = 0; // 중복 획득 횟수

    // 해금된 스킬 ID 목록. 빈 목록이면 CombatCharacter 가 index 0을 기본 해금으로 간주한다.
    public List<string> unlockedSkillIds = new List<string>();
}
