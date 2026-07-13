using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class PlayerAdminTests
{
    static PlayerDTO P(string id, string name, string login = "", string created = "", string banned = "")
        => new PlayerDTO { playFabId = id, displayName = name, lastLogin = login, created = created, bannedUntil = banned };

    [Test]
    public void PlayerListDTO_JsonRoundTrip_PreservesFields()
    {
        var original = new PlayerListDTO { items = new[]
        {
            P("PF-1", "철수", "2026-07-01T10:00:00Z", "2026-01-01T00:00:00Z", ""),
            P("PF-2", "영희", "2026-07-05T12:30:00Z", "2026-02-15T00:00:00Z", "9999-12-31T23:59:59Z"),
        }};

        string json     = JsonUtility.ToJson(original);
        var    restored = JsonUtility.FromJson<PlayerListDTO>(json);

        Assert.AreEqual(2, restored.items.Length);
        Assert.AreEqual("PF-1", restored.items[0].playFabId);
        Assert.AreEqual("영희", restored.items[1].displayName);
        Assert.AreEqual("9999-12-31T23:59:59Z", restored.items[1].bannedUntil);
    }

    [Test]
    public void PlayerListDTO_ParsesServerShape_ItemsWrapper()
    {
        const string serverJson =
            "{\"items\":[{\"playFabId\":\"A\",\"displayName\":\"n\",\"lastLogin\":\"\",\"created\":\"\",\"bannedUntil\":\"\"}]}";
        var wrapper = JsonUtility.FromJson<PlayerListDTO>(serverJson);
        Assert.AreEqual(1, wrapper.items.Length);
        Assert.AreEqual("A", wrapper.items[0].playFabId);
    }

    [Test] public void IsBanned_FutureDate_True()  => Assert.IsTrue(P("a", "a", banned: "9999-12-31T23:59:59Z").IsBanned);
    [Test] public void IsBanned_Empty_False()      => Assert.IsFalse(P("a", "a").IsBanned);
    [Test] public void IsBanned_PastDate_False()   => Assert.IsFalse(P("a", "a", banned: "2000-01-01T00:00:00Z").IsBanned);

    [Test]
    public void Sort_ByName_Ascending()
    {
        var rows   = new[] { P("2", "다"), P("1", "가"), P("3", "나") };
        var sorted = PlayerSort.Sort(rows, PlayerSortKey.Name, true);
        CollectionAssert.AreEqual(new[] { "가", "나", "다" }, sorted.Select(r => r.displayName));
    }

    [Test]
    public void Sort_ByCreated_Descending_NewestFirst()
    {
        var rows = new[]
        {
            P("1", "a", created: "2026-01-01T00:00:00Z"),
            P("2", "b", created: "2026-03-01T00:00:00Z"),
            P("3", "c", created: "2026-02-01T00:00:00Z"),
        };
        var sorted = PlayerSort.Sort(rows, PlayerSortKey.Created, false);
        CollectionAssert.AreEqual(new[] { "2", "3", "1" }, sorted.Select(r => r.playFabId));
    }

    [Test]
    public void Sort_ByLastLogin_Ascending_EmptyTreatedAsOldest()
    {
        var rows = new[]
        {
            P("1", "a", login: "2026-07-05T00:00:00Z"),
            P("2", "b", login: ""),
            P("3", "c", login: "2026-07-01T00:00:00Z"),
        };
        var sorted = PlayerSort.Sort(rows, PlayerSortKey.LastLogin, true);
        CollectionAssert.AreEqual(new[] { "2", "3", "1" }, sorted.Select(r => r.playFabId));
    }

    [Test]
    public void Sort_ReturnsNewList_InputUnmodified()
    {
        var rows = new[] { P("2", "다"), P("1", "가") };
        PlayerSort.Sort(rows, PlayerSortKey.Name, true);
        Assert.AreEqual("다", rows[0].displayName);
    }
}
