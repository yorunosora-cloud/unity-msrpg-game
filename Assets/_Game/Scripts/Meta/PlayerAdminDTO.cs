using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>CloudScript AdminGetPlayers가 반환하는 플레이어 1명. 날짜는 ISO 8601 UTC 문자열.</summary>
[Serializable]
public class PlayerDTO
{
    public string playFabId;
    public string displayName;
    public string lastLogin;
    public string created;
    public string bannedUntil;

    public DateTime LastLoginUtc => ParseUtc(lastLogin);
    public DateTime CreatedUtc   => ParseUtc(created);

    /// <summary>bannedUntil이 현재보다 미래면 정지 상태(영구 정지=9999년).</summary>
    public bool IsBanned => !string.IsNullOrEmpty(bannedUntil) && ParseUtc(bannedUntil) > DateTime.UtcNow;

    public static DateTime ParseUtc(string iso)
    {
        if (string.IsNullOrEmpty(iso)) return DateTime.MinValue;
        return DateTime.TryParse(iso, CultureInfo.InvariantCulture,
                   DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt)
            ? dt : DateTime.MinValue;
    }
}

/// <summary>TitleData/CloudScript 최상위 배열 래퍼(JsonUtility는 최상위 배열 직렬화 불가).</summary>
[Serializable]
public class PlayerListDTO
{
    public PlayerDTO[] items = new PlayerDTO[0];
}

/// <summary>CloudScript AdminGetPlayerRoster가 반환하는 보유 캐릭터 id 목록 래퍼.</summary>
[Serializable]
public class RosterIdsDTO
{
    public string[] ids = new string[0];
}

/// <summary>정렬 기준 — 캐릭터 보유순 제거, 가입일(Created) 추가.</summary>
public enum PlayerSortKey { Name, PlayFabId, LastLogin, Created }

/// <summary>플레이어 목록 정렬(순수 함수 — EditMode 테스트 대상).</summary>
public static class PlayerSort
{
    public static Comparison<PlayerDTO> Comparer(PlayerSortKey key) => key switch
    {
        PlayerSortKey.Name      => (a, b) => string.Compare(a.displayName, b.displayName, StringComparison.Ordinal),
        PlayerSortKey.PlayFabId => (a, b) => string.Compare(a.playFabId,   b.playFabId,   StringComparison.Ordinal),
        PlayerSortKey.LastLogin => (a, b) => a.LastLoginUtc.CompareTo(b.LastLoginUtc),
        PlayerSortKey.Created   => (a, b) => a.CreatedUtc.CompareTo(b.CreatedUtc),
        _                       => (a, b) => 0,
    };

    /// <summary>새 리스트를 반환(입력 불변). ascending=false면 뒤집는다.</summary>
    public static List<PlayerDTO> Sort(IEnumerable<PlayerDTO> rows, PlayerSortKey key, bool ascending)
    {
        var list = new List<PlayerDTO>(rows);
        list.Sort(Comparer(key));
        if (!ascending) list.Reverse();
        return list;
    }
}
