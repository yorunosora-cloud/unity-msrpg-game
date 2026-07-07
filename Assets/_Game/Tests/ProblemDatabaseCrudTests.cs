using NUnit.Framework;
using UnityEngine;

public class ProblemDatabaseCrudTests
{
    static ProblemDef MakeProb(string id)
    {
        var p = ScriptableObject.CreateInstance<ProblemDef>();
        p.id = id;
        return p;
    }

    static ProblemDatabase MakeDb(params ProblemDef[] probs)
    {
        var db = ScriptableObject.CreateInstance<ProblemDatabase>();
        db.SetProblems(probs);
        return db;
    }

    [Test]
    public void ById_FindsMatchingId()
    {
        var a  = MakeProb("a");
        var db = MakeDb(a, MakeProb("b"));

        Assert.AreEqual(a, db.ById("a"));
    }

    [Test]
    public void ById_MissingId_ReturnsNull()
    {
        var db = MakeDb(MakeProb("a"));
        Assert.IsNull(db.ById("missing"));
    }

    [Test]
    public void Upsert_NewId_Appends()
    {
        var db = MakeDb(MakeProb("a"));

        db.Upsert(MakeProb("b"));

        Assert.AreEqual(2, db.All.Length);
        Assert.IsNotNull(db.ById("b"));
    }

    [Test]
    public void Upsert_ExistingId_ReplacesInPlace()
    {
        var db  = MakeDb(MakeProb("a"));
        var updated = MakeProb("a");
        updated.prompt = "changed";

        db.Upsert(updated);

        Assert.AreEqual(1, db.All.Length);
        Assert.AreEqual("changed", db.ById("a").prompt);
    }

    [Test]
    public void Remove_ExistingId_RemovesEntry()
    {
        var db = MakeDb(MakeProb("a"), MakeProb("b"));

        db.Remove("a");

        Assert.AreEqual(1, db.All.Length);
        Assert.IsNull(db.ById("a"));
    }

    [Test]
    public void Remove_MissingId_NoOp()
    {
        var db = MakeDb(MakeProb("a"));

        db.Remove("missing");

        Assert.AreEqual(1, db.All.Length);
    }
}
