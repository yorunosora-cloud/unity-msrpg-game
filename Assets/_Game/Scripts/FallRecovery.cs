using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FallRecovery : MonoBehaviour
{
    [SerializeField] float fallThreshold = -3f;
    [SerializeField] float maxAirTime = 3f;

    CharacterController _cc;
    Vector3 _lastSafe;
    bool _recovering;
    float _airTimer;

    void Start()
    {
        _cc = GetComponent<CharacterController>();
        _lastSafe = transform.position;
    }

    void Update()
    {
        if (_recovering) return;

        if (_cc.isGrounded && transform.position.y > fallThreshold)
        {
            _lastSafe = transform.position;
            _airTimer = 0f;
        }
        else if (!_cc.isGrounded)
        {
            _airTimer += Time.deltaTime;
        }

        if (transform.position.y < fallThreshold || _airTimer > maxAirTime)
            Recover();
    }

    void Recover()
    {
        _recovering = true;
        _cc.enabled = false;
        transform.position = _lastSafe + Vector3.up * 2f;
        _cc.enabled = true;
        _airTimer = 0f;
        _recovering = false;
    }
}
