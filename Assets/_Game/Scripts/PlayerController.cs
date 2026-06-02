using UnityEngine;
using UnityEngine.InputSystem;

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
        if (Camera.main != null)
            _camTransform = Camera.main.transform;
        else
        {
            _camTransform = transform;
            Debug.LogWarning("[PlayerController] Camera.main not found. Falling back to self transform.");
        }
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        float h = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f)
                - (kb.aKey.isPressed || kb.leftArrowKey.isPressed  ? 1f : 0f);
        float v = (kb.wKey.isPressed || kb.upArrowKey.isPressed    ? 1f : 0f)
                - (kb.sKey.isPressed || kb.downArrowKey.isPressed  ? 1f : 0f);
        bool  run  = kb.leftShiftKey.isPressed;
        bool  jump = kb.spaceKey.wasPressedThisFrame;

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
            if (jump) _velocityY = jumpSpeed;
        }
        else
        {
            _velocityY += gravity * Time.deltaTime;
            _velocityY  = Mathf.Max(_velocityY, -50f);
        }

        Vector3 velocity = moveDir * speed;
        velocity.y = _velocityY;
        _cc.Move(velocity * Time.deltaTime);

        // Animator 파라미터 업데이트 (컨트롤러 없으면 스킵)
        if (_animator != null && _animator.runtimeAnimatorController != null)
        {
            _animator.SetFloat(SpeedHash,   isMoving ? speed : 0f);
            _animator.SetBool(GroundedHash, _cc.isGrounded);
        }
    }
}
