using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FollowCamera : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Vector3   offset      = new(0f, 5f, -8f);
    [SerializeField] float     smoothSpeed = 10f;
    [SerializeField] float     lookHeight  = 1.4f;

    Quaternion _fixedRotation;

    void Awake()
    {
        // 오프셋과 lookHeight로 고정 회전 사전 계산
        _fixedRotation = Quaternion.LookRotation((Vector3.up * lookHeight - offset).normalized);
    }

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 goal = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, goal, Time.deltaTime * smoothSpeed);
        transform.rotation = _fixedRotation;
    }
}
