using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 범용 월드 상호작용 컴포넌트. 건물·NPC·오브젝트에 부착한다.
/// OnEnable/OnDisable로 정적 레지스트리 <see cref="All"/>에 자동 등록·해제.
/// <para>에디터 셋업에서 <c>onInteract</c>에 대상 메서드를 와이어링한다.</para>
/// </summary>
public class Interactable : MonoBehaviour
{
    [Header("정보")]
    public string displayName = "상호작용";
    public string promptText  = "E: 상호작용";

    [Header("범위")]
    public float radius = 3f;

    [Header("이벤트")]
    public UnityEvent onInteract = new UnityEvent();

    /// <summary>현재 활성화된 모든 Interactable 레지스트리.</summary>
    public static readonly List<Interactable> All = new List<Interactable>();

    void OnEnable()  => All.Add(this);
    void OnDisable() => All.Remove(this);

    /// <summary>상호작용 발동. onInteract 이벤트 호출.</summary>
    public void Activate() => onInteract?.Invoke();

    /// <summary>
    /// <paramref name="pos"/> 에서 가장 가까우면서 자신의 radius 이내에 있는
    /// Interactable을 반환한다. 없으면 null.
    /// </summary>
    public static Interactable FindNearest(Vector3 pos, List<Interactable> list)
    {
        Interactable nearest = null;
        float        minDist = float.MaxValue;

        foreach (var item in list)
        {
            if (item == null) continue;
            float dist = Vector3.Distance(pos, item.transform.position);
            if (dist < item.radius && dist < minDist)
            {
                minDist = dist;
                nearest = item;
            }
        }
        return nearest;
    }
}
