using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Interactable.FindNearest 단위 테스트.
/// EditMode에서 임시 GameObject를 생성·파기하며 검증한다.
/// </summary>
public class InteractionTests
{
    // 테스트 중 생성된 임시 오브젝트 추적 → TearDown에서 일괄 파기
    readonly List<GameObject> _temp = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        foreach (var go in _temp)
            if (go != null) Object.DestroyImmediate(go);
        _temp.Clear();
    }

    Interactable MakeInteractable(Vector3 worldPos, float radius)
    {
        var go = new GameObject("TestInteractable");
        go.transform.position = worldPos;
        var ic = go.AddComponent<Interactable>();
        ic.radius = radius;
        _temp.Add(go);
        return ic;
    }

    // ── FindNearest ───────────────────────────────────────────────────────

    [Test]
    public void FindNearest_EmptyList_ReturnsNull()
    {
        var result = Interactable.FindNearest(Vector3.zero, new List<Interactable>());
        Assert.IsNull(result);
    }

    [Test]
    public void FindNearest_TargetWithinRadius_ReturnsThat()
    {
        var a = MakeInteractable(new Vector3(2f, 0f, 0f), radius: 5f);
        var result = Interactable.FindNearest(Vector3.zero, new List<Interactable> { a });
        Assert.AreEqual(a, result);
    }

    [Test]
    public void FindNearest_TargetOutsideRadius_ReturnsNull()
    {
        var a = MakeInteractable(new Vector3(10f, 0f, 0f), radius: 3f);
        var result = Interactable.FindNearest(Vector3.zero, new List<Interactable> { a });
        Assert.IsNull(result);
    }

    [Test]
    public void FindNearest_TargetExactlyAtRadius_ReturnsNull()
    {
        // dist == radius → radius 조건은 엄격 미만(< radius)이므로 반환 안 함
        var a = MakeInteractable(new Vector3(3f, 0f, 0f), radius: 3f);
        var result = Interactable.FindNearest(Vector3.zero, new List<Interactable> { a });
        Assert.IsNull(result);
    }

    [Test]
    public void FindNearest_MultipleTargets_ReturnsClosest()
    {
        var far   = MakeInteractable(new Vector3(4f, 0f, 0f), radius: 10f);
        var close = MakeInteractable(new Vector3(1f, 0f, 0f), radius: 10f);
        var result = Interactable.FindNearest(
            Vector3.zero, new List<Interactable> { far, close });
        Assert.AreEqual(close, result);
    }

    [Test]
    public void FindNearest_OneWithinOneOutside_ReturnsWithin()
    {
        var inside  = MakeInteractable(new Vector3(2f, 0f, 0f), radius: 5f);
        var outside = MakeInteractable(new Vector3(8f, 0f, 0f), radius: 3f);
        var result  = Interactable.FindNearest(
            Vector3.zero, new List<Interactable> { outside, inside });
        Assert.AreEqual(inside, result);
    }

    [Test]
    public void FindNearest_NullEntryInList_SkipsNullReturnsValid()
    {
        var valid  = MakeInteractable(new Vector3(1f, 0f, 0f), radius: 5f);
        var list   = new List<Interactable> { null, valid, null };
        var result = Interactable.FindNearest(Vector3.zero, list);
        Assert.AreEqual(valid, result);
    }
}
