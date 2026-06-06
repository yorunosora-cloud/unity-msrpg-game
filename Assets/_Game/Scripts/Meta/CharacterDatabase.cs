using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 CharacterDef를 담는 데이터베이스.
/// Resources/CharacterDatabase.asset 으로 저장해 Resources.Load로 접근합니다.
/// </summary>
[CreateAssetMenu(menuName = "MSRPG/Character Database", fileName = "CharacterDatabase")]
public class CharacterDatabase : ScriptableObject
{
    [SerializeField] CharacterDef[] characters = new CharacterDef[0];

    public CharacterDef[] All => characters;

    /// <summary>id로 캐릭터 검색. 없으면 null.</summary>
    public CharacterDef ById(string id)
    {
        foreach (var c in characters)
            if (c != null && c.id == id) return c;
        return null;
    }

    /// <summary>특정 등급의 캐릭터 배열. 가챠 추첨에 사용.</summary>
    public CharacterDef[] ByRarity(Rarity rarity)
    {
        var result = new List<CharacterDef>();
        foreach (var c in characters)
            if (c != null && c.rarity == rarity) result.Add(c);
        return result.ToArray();
    }

    /// <summary>에디터 전용 — CharacterSeedSetup에서 호출.</summary>
    public void SetCharacters(CharacterDef[] defs) => characters = defs;
}
