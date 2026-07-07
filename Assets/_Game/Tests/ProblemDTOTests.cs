using NUnit.Framework;
using UnityEngine;

public class ProblemDTOTests
{
    static ProblemDef MakeDef()
    {
        var d = ScriptableObject.CreateInstance<ProblemDef>();
        d.id              = "p1";
        d.skillId         = "sk1";
        d.difficulty      = ProblemDifficulty.Mid;
        d.subject         = Continent.Chemistry;
        d.country         = "bonding";
        d.type            = ProblemType.MultipleChoice;
        d.prompt          = "질문";
        d.choices         = new[] { "a", "b", "c", "d" };
        d.correctIndex    = 2;
        d.acceptedAnswers = new[] { "x", "y" };
        d.explanation     = "해설";
        return d;
    }

    [Test]
    public void FromDef_ApplyTo_RoundTripsAllFields()
    {
        var original = MakeDef();
        var dto = ProblemDTO.FromDef(original);

        var restored = ScriptableObject.CreateInstance<ProblemDef>();
        dto.ApplyTo(restored);

        Assert.AreEqual(original.id, restored.id);
        Assert.AreEqual(original.skillId, restored.skillId);
        Assert.AreEqual(original.difficulty, restored.difficulty);
        Assert.AreEqual(original.subject, restored.subject);
        Assert.AreEqual(original.country, restored.country);
        Assert.AreEqual(original.type, restored.type);
        Assert.AreEqual(original.prompt, restored.prompt);
        CollectionAssert.AreEqual(original.choices, restored.choices);
        Assert.AreEqual(original.correctIndex, restored.correctIndex);
        CollectionAssert.AreEqual(original.acceptedAnswers, restored.acceptedAnswers);
        Assert.AreEqual(original.explanation, restored.explanation);
    }

    [Test]
    public void ProblemStore_DefaultsToEmptyArrays()
    {
        var store = new ProblemStore();
        Assert.IsNotNull(store.items);
        Assert.IsNotNull(store.deletedIds);
        Assert.AreEqual(0, store.items.Length);
        Assert.AreEqual(0, store.deletedIds.Length);
    }
}
