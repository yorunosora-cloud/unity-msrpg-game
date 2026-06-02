using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] float walkSpeed = 6f;
    [SerializeField] float runSpeed  = 12f;
    [SerializeField] float turnSpeed = 12f;
    [SerializeField] float gravity   = -22f;
    [SerializeField] float jumpSpeed = 8f;

    CharacterController _cc;
    Animator            _animator;
    Transform           _camTransform;
    float               _velocityY;

    static readonly int SpeedHash    = Animator.StringToHash("Speed");
    static readonly int GroundedHash = Animator.StringToHash("IsGrounded");

    void Awake()
    {
        _cc           = GetComponent<CharacterController>();
        _animator     = GetComponentInChildren<Animator>();
        _camTransform = Camera.main.transform;
    }

    void Update()
    {
        float h   = Input.GetAxis("Horizontal");
        float v   = Input.GetAxis("Vertical");
        bool  run = Input.GetKey(KeyCode.LeftShift);

        // 카메라 기준 수평 방향 (Y 성분 제거)
        Vector3 camFwd   = Vector3.Scale(_camTransform.forward, new Vector3(1, 0, 1)).normalized;
        Vector3 camRight = Vector3.Scale(_camTransform.right,   new Vector3(1, 0, 1)).normalized;
        Vector3 moveDir  = camFwd * v + camRight * h;
        if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

        bool  isMoving  = moveDir.sqrMagnitude > 0.01f;
        bool  isRunning = isMoving && run;
        float speed     = isRunning ? runSpeed : walkSpeed;

        // 이동 방향으로 캐릭터 회전
        if (isMoving)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation   = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }

        // 수직 물리
        if (_cc.isGrounded)
        {
            _velocityY = -1f;
            if (Input.GetKeyDown(KeyCode.Space))
                _velocityY = jumpSpeed;
        }
        else
        {
            _velocityY += gravity * Time.deltaTime;
        }

        Vector3 velocity = moveDir * speed;
        velocity.y = _velocityY;
        _cc.Move(velocity * Time.deltaTime);

        // Animator 파라미터 업데이트
        if (_animator != null)
        {
            _animator.SetFloat(SpeedHash,   isMoving ? speed : 0f);
            _animator.SetBool(GroundedHash, _cc.isGrounded);
        }
    }
}
