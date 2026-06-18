using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] float walkSpeed          = 3f;
    [SerializeField] float runSpeed           = 9f;
    [SerializeField] float turnSpeed          = 12f;
    [SerializeField] float groundAccel        = 30f;   // 가속률 (units/s²)
    [SerializeField] float groundDecel        = 45f;   // 감속률
    [SerializeField] float airSteerDegPerSec  = 35f;   // 공중 조향 속도 (방향만, 크기 고정)

    [Header("Jump")]
    [SerializeField] float gravity            = -22f;
    [SerializeField] float fallMultiplier     = 1.6f;  // 하강 시 중력 배율 → 또렷한 착지
    [SerializeField] float jumpSpeed          = 8f;
    [SerializeField] float anticipationTime   = 0.06f; // 웅크림 대기 시간

    enum JumpPhase { Grounded, Anticipating, Airborne }

    CharacterController _cc;
    Animator            _animator;
    BoxCharacterBuilder _builder;

    Vector3   _horizVel;
    float     _velocityY;
    JumpPhase _jumpPhase;
    float     _antTimer;

    static readonly int SpeedHash    = Animator.StringToHash("Speed");
    static readonly int GroundedHash = Animator.StringToHash("IsGrounded");

    void Awake()
    {
        _cc       = GetComponent<CharacterController>();
        _animator = GetComponentInChildren<Animator>();
        _builder  = GetComponent<BoxCharacterBuilder>();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        float h, v;
        bool  run, jump;
        if (UIManager.IsAnyPanelOpen)
        {
            h = 0f; v = 0f; run = false; jump = false;
        }
        else
        {
            h    = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f)
                 - (kb.aKey.isPressed || kb.leftArrowKey.isPressed  ? 1f : 0f);
            v    = (kb.wKey.isPressed || kb.upArrowKey.isPressed    ? 1f : 0f)
                 - (kb.sKey.isPressed || kb.downArrowKey.isPressed  ? 1f : 0f);
            run  = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
            jump = kb.spaceKey.wasPressedThisFrame;
        }

        Vector3 desiredDir = new Vector3(h, 0f, v);
        if (desiredDir.sqrMagnitude > 1f) desiredDir.Normalize();
        bool hasInput = desiredDir.sqrMagnitude > 0.01f;

        float   targetSpeed = hasInput ? (run ? runSpeed : walkSpeed) : 0f;
        Vector3 targetVel   = hasInput ? desiredDir * targetSpeed : Vector3.zero;

        // 수직 속도: 공중엔 중력, 지상/예비동작엔 작은 하방 압력
        if (_jumpPhase == JumpPhase.Airborne)
        {
            float g = _velocityY < 0f ? gravity * fallMultiplier : gravity;
            _velocityY += g * Time.deltaTime;
            _velocityY  = Mathf.Max(_velocityY, -50f);
        }
        else
        {
            _velocityY = -1f;
        }

        // 수평 상태 머신
        switch (_jumpPhase)
        {
            case JumpPhase.Grounded:
                if (hasInput)
                {
                    Quaternion rot = Quaternion.LookRotation(desiredDir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, rot,
                                            turnSpeed * Time.deltaTime);
                }
                float accel = (hasInput && targetVel.sqrMagnitude > _horizVel.sqrMagnitude)
                            ? groundAccel : groundDecel;
                _horizVel = Vector3.MoveTowards(_horizVel, targetVel, accel * Time.deltaTime);

                if (jump && _cc.isGrounded)
                {
                    _jumpPhase = JumpPhase.Anticipating;
                    _antTimer  = anticipationTime;
                }
                break;

            case JumpPhase.Anticipating:
                // 수평 속도 고정 (달리던 속도가 점프에 그대로 실림)
                _antTimer -= Time.deltaTime;
                if (_antTimer <= 0f)
                {
                    _velocityY = jumpSpeed;
                    _jumpPhase = JumpPhase.Airborne;
                }
                break;

            case JumpPhase.Airborne:
                // 공중 약한 조향: 방향만 회전, 속도 크기 불변
                if (hasInput && _horizVel.magnitude > 0.01f)
                {
                    float currAngle = Mathf.Atan2(_horizVel.x, _horizVel.z) * Mathf.Rad2Deg;
                    float wantAngle = Mathf.Atan2(desiredDir.x, desiredDir.z) * Mathf.Rad2Deg;
                    float newAngle  = Mathf.MoveTowardsAngle(currAngle, wantAngle,
                                          airSteerDegPerSec * Time.deltaTime);
                    float mag = _horizVel.magnitude;
                    float rad = newAngle * Mathf.Deg2Rad;
                    _horizVel = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * mag;
                }
                if (_horizVel.sqrMagnitude > 0.01f)
                {
                    Quaternion rot = Quaternion.LookRotation(_horizVel.normalized);
                    transform.rotation = Quaternion.Slerp(transform.rotation, rot,
                                            turnSpeed * Time.deltaTime);
                }
                break;
        }

        float prevVelY = _velocityY;
        _cc.Move((_horizVel + Vector3.up * _velocityY) * Time.deltaTime);

        // 착지 감지
        float landingImpact = 0f;
        if (_jumpPhase == JumpPhase.Airborne && _cc.isGrounded)
        {
            landingImpact = Mathf.Clamp01(Mathf.Abs(prevVelY) / 12f);
            _jumpPhase    = JumpPhase.Grounded;
        }
        // 발판 끝에서 그냥 걸어나간 경우 (점프 없이 공중)
        if (_jumpPhase == JumpPhase.Grounded && !_cc.isGrounded)
            _jumpPhase = JumpPhase.Airborne;
        // 예비동작 중 발판 끝에서 떨어진 경우
        if (_jumpPhase == JumpPhase.Anticipating && !_cc.isGrounded)
            _jumpPhase = JumpPhase.Airborne;

        _builder?.SetLocomotion(_horizVel.magnitude, _cc.isGrounded,
                                _jumpPhase == JumpPhase.Anticipating, landingImpact);

        if (_animator != null && _animator.runtimeAnimatorController != null)
        {
            _animator.SetFloat(SpeedHash,   _horizVel.magnitude);
            _animator.SetBool(GroundedHash, _cc.isGrounded);
        }
    }
}
