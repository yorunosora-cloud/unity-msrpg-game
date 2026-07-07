using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class ProblemMergeTests
{
    static ProblemDef MakeSeed(string id, string prompt = "seed")
    {
        var p = ScriptableObject.CreateInstance<ProblemDef>();
        p.id = id;
        p.prompt = prompt;
        return p;
    }

    static ProblemDTO MakeDto(string id, string prompt)
        => new ProblemDTO { id = id, prompt = prompt, choices = new string[0], acceptedAnswers = new string[0] };

    [Test]
    public void Apply_EmptyStore_ReturnsSeedUnchanged()
    {
        var seed = new[] { MakeSeed("a"), MakeSeed("b") };

        var result = ProblemMerge.Apply(seed, new ProblemStore());

        Assert.AreEqual(2, result.Length);
        CollectionAssert.AreEquivalent(new[] { "a", "b" }, result.Select(p => p.id));
    }

    [Test]
    public void Apply_NullStore_ReturnsSeedUnchanged()
    {
        var seed = new[] { MakeSeed("a") };

        var result = ProblemMerge.Apply(seed, null);

        Assert.AreEqual(1, result.Length);
    }

    [Test]
    public void Apply_ItemWithExistingId_ReplacesSeedEntry()
    {
        var seed  = new[] { MakeSeed("a", "old") };
        var store = new ProblemStore { items = new[] { MakeDto("a", "new") } };

        var result = ProblemMerge.Apply(seed, store);

        Assert.AreEqual(1, result.Length);
        Assert.AreEqual("new", result[0].prompt);
    }

    [Test]
    public void Apply_ItemWithNewId_AddsEntry()
    {
        var seed  = new[] { MakeSeed("a") };
        var store = new ProblemStore { items = new[] { MakeDto("b", "added") } };

        var result = ProblemMerge.Apply(seed, store);

        CollectionAssert.AreEquivalent(new[] { "a", "b" }, result.Select(p => p.id));
    }

    [Test]
    public void Apply_DeletedId_RemovesSeedEntryEvenIfStillInAssets()
    {
        var seed  = new[] { MakeSeed("a"), MakeSeed("b") };
        var store = new ProblemStore { deletedIds = new[] { "a" } };

        var result = ProblemMerge.Apply(seed, store);

        CollectionAssert.AreEquivalent(new[] { "b" }, result.Select(p => p.id));
    }

    [Test]
    public void Apply_DeletedIdAfterUpsert_UpsertWins_UnlessAlsoInDeletedIds()
    {
        // items와 deletedIds에 동시에 있으면 삭제가 최종 승리(삭제가 items 적용 후 처리됨)
        var seed  = new[] { MakeSeed("a") };
        var store = new ProblemStore
        {
            items      = new[] { MakeDto("a", "edited") },
            deletedIds = new[] { "a" },
        };

        var result = ProblemMerge.Apply(seed, store);

        Assert.AreEqual(0, result.Length);
    }
}
