using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class BoxCharacterBuilder : MonoBehaviour
{
    // 6.7두신 비율 (총 높이 ≈ 1.74 유닛)
    static readonly Vector3 HeadSize     = new(0.24f, 0.26f, 0.22f);
    static readonly Vector3 TorsoSize    = new(0.30f, 0.50f, 0.14f);
    static readonly Vector3 UpperArmSize = new(0.10f, 0.36f, 0.10f);
    static readonly Vector3 LowerArmSize = new(0.09f, 0.30f, 0.09f);
    static readonly Vector3 UpperLegSize = new(0.12f, 0.46f, 0.12f);
    static readonly Vector3 LowerLegSize = new(0.11f, 0.52f, 0.11f);

    static readonly Color HeadColor  = new(1.00f, 0.80f, 0.62f);
    static readonly Color TorsoColor = new(0.20f, 0.40f, 0.80f);
    static readonly Color ArmColor   = new(0.20f, 0.40f, 0.80f);
    static readonly Color LegColor   = new(0.20f, 0.20f, 0.50f);

    [Header("Walk Cycle")]
    [SerializeField] float cycleFrequency = 1.2f;
    [SerializeField] float legSwingDeg    = 12f;
    [SerializeField] float kneeFlexDeg    = 20f;
    [SerializeField] float armSwingDeg    = 8f;
    [SerializeField] float elbowFlexDeg   = 12f;

    [Header("Run Cycle")]
    [SerializeField] float runLegSwingDeg  = 35f;
    [SerializeField] float runKneeFlexDeg  = 55f;
    [SerializeField] float runArmSwingDeg  = 30f;
    [SerializeField] float runElbowFlexDeg = 40f;
    [SerializeField] float runBobHeight    = 0.07f;
    [SerializeField] float runMultiplier   = 2.0f;

    [Header("Walk Jump")]
    [SerializeField] float walkJumpHipFwd   = 20f;
    [SerializeField] float walkJumpKneeFlex = 35f;
    [SerializeField] float walkJumpArmBack  = 25f;
    [SerializeField] float walkJumpArmOut   = 10f;

    [Header("Run Jump")]
    [SerializeField] float runJumpHipFwd    = 35f;
    [SerializeField] float runJumpKneeFlex  = 60f;
    [SerializeField] float runJumpArmBack   = 40f;
    [SerializeField] float runJumpArmOut    = 20f;

    [Header("Crouch / Land")]
    [SerializeField] float crouchDrop         = 0.12f; // 루트 하강량
    [SerializeField] float crouchKneeFlex     = 40f;   // 웅크림 무릎 굽힘
    [SerializeField] float crouchHipFwd       = 10f;   // 웅크림 힙 앞 기울기
    [SerializeField] float crouchArmBack      = 15f;   // 웅크림 팔 반동
    [SerializeField] float crouchRecoverSpeed = 8f;    // 웅크림 복귀 속도

    [Header("Shared")]
    [SerializeField] float jumpBlendSpeed = 12f;
    [SerializeField] float walkSpeedRef   = 3f;  // PlayerController.walkSpeed 와 맞춤
    [SerializeField] float runSpeedRef    = 9f;  // PlayerController.runSpeed 와 맞춤

    CharacterController _cc;
    Transform           _characterRoot;

    // 틴트 대상 렌더러 (머리/손목 피부색 제외). Build()에서 캐시.
    Renderer[] _bodyRenderers;

    Transform _hipPivotL,      _hipPivotR;
    Transform _kneePivotL,     _kneePivotR;
    Transform _shoulderPivotL, _shoulderPivotR;
    Transform _elbowPivotL,    _elbowPivotR;

    float _phase;
    float _airBlend;
    float _crouchBlend;

    // 평타 모션 (타이머 기반 sin 아크: 0→1→0)
    float _attackTimer;
    const float AttackDuration = 0.35f;

    // 스킬 모션 (effectKind별 상이)
    enum SkillMotion { None, Strike, Aoe, HealBuff, Mark }
    SkillMotion _skillMotion;
    float       _skillTimer;
    float       _skillDuration;

    float _speed;
    bool  _isGrounded   = true;
    bool  _wasGrounded  = true;
    bool  _anticipating;
    float _takeoffSpeed;

    /// <summary>PlayerCombat Q키 평타 — 오른팔 앞으로 휘두르기.</summary>
    public void PlayAttack()
    {
        _attackTimer = AttackDuration;
    }

    /// <summary>스킬 effectKind에 대응하는 전용 모션 트리거.</summary>
    public void PlaySkill(SkillEffectKind kind)
    {
        _skillMotion = kind switch
        {
            SkillEffectKind.Strike   => SkillMotion.Strike,
            SkillEffectKind.Aoe      => SkillMotion.Aoe,
            SkillEffectKind.HealBuff => SkillMotion.HealBuff,
            SkillEffectKind.Mark     => SkillMotion.Mark,
            _                        => SkillMotion.Strike,
        };
        _skillDuration = kind switch
        {
            SkillEffectKind.Strike   => 0.35f,
            SkillEffectKind.Aoe      => 0.50f,
            SkillEffectKind.HealBuff => 0.55f,
            SkillEffectKind.Mark     => 0.42f,
            _                        => 0.40f,
        };
        _skillTimer  = _skillDuration;
        _attackTimer = 0f; // 평타 모션 중단
    }

    // PlayerController가 매 Update 후 호출
    public void SetLocomotion(float speed, bool isGrounded, bool anticipating, float landingImpact01)
    {
        _speed        = speed;
        _isGrounded   = isGrounded;
        _anticipating = anticipating;

        // 착지 충격: crouchBlend를 순간적으로 올림
        if (landingImpact01 > 0f)
            _crouchBlend = Mathf.Max(_crouchBlend, landingImpact01);
    }

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        Build();
    }

    void Build()
    {
        var root = new GameObject("Character").transform;
        root.SetParent(transform, false);
        _characterRoot = root;

        float legTotalH = UpperLegSize.y + LowerLegSize.y;
        float torsoY    = legTotalH + TorsoSize.y * 0.5f;
        float torsoTop  = legTotalH + TorsoSize.y;
        float hipX      = TorsoSize.x * 0.5f - UpperLegSize.x * 0.5f;
        float shoulderX = TorsoSize.x * 0.5f + UpperArmSize.x * 0.5f;
        float shoulderY = torsoTop - UpperArmSize.x * 0.5f;

        BuildLeg(root, -hipX, legTotalH, isLeft: true);
        BuildLeg(root,  hipX, legTotalH, isLeft: false);

        MakeBox("Torso", TorsoSize, TorsoColor, root)
            .localPosition = new Vector3(0, torsoY, 0);
        MakeBox("Head", HeadSize, HeadColor, root)
            .localPosition = new Vector3(0, torsoTop + HeadSize.y * 0.5f, 0);

        BuildArm(root, -shoulderX, shoulderY, isLeft: true);
        BuildArm(root,  shoulderX, shoulderY, isLeft: false);

        // 틴트 대상 렌더러 캐시 (머리·손목 피부색 제외)
        CacheBodyRenderers(root);
    }

    void CacheBodyRenderers(Transform root)
    {
        // 피부색(HeadColor)을 쓰는 파트 이름 목록
        var skinNames = new System.Collections.Generic.HashSet<string>
            { "Head", "LowerArm_L", "LowerArm_R" };

        var list = new System.Collections.Generic.List<Renderer>();
        foreach (var r in root.GetComponentsInChildren<Renderer>())
        {
            if (!skinNames.Contains(r.gameObject.name))
                list.Add(r);
        }
        _bodyRenderers = list.ToArray();
    }

    /// <summary>
    /// 파티 교체 시 호출 — 몸통·팔(상박)·다리 렌더러를 틴트 색으로 일괄 변경.
    /// 머리(피부색)와 손목(LowerArm, 피부색)은 변경하지 않는다.
    /// </summary>
    public void SetBodyTint(Color color)
    {
        if (_bodyRenderers == null) return;
        foreach (var r in _bodyRenderers)
            if (r != null) r.material.color = color;
    }

    void BuildLeg(Transform root, float x, float hipY, bool isLeft)
    {
        string s = isLeft ? "L" : "R";
        var hip = Pivot($"HipPivot_{s}", root, new Vector3(x, hipY, 0));
        MakeBox($"UpperLeg_{s}", UpperLegSize, LegColor, hip)
            .localPosition = new Vector3(0, -UpperLegSize.y * 0.5f, 0);
        var knee = Pivot($"KneePivot_{s}", hip, new Vector3(0, -UpperLegSize.y, 0));
        MakeBox($"LowerLeg_{s}", LowerLegSize, LegColor, knee)
            .localPosition = new Vector3(0, -LowerLegSize.y * 0.5f, 0);
        if (isLeft) { _hipPivotL = hip; _kneePivotL = knee; }
        else        { _hipPivotR = hip; _kneePivotR = knee; }
    }

    void BuildArm(Transform root, float x, float y, bool isLeft)
    {
        string s = isLeft ? "L" : "R";
        var shoulder = Pivot($"ShoulderPivot_{s}", root, new Vector3(x, y, 0));
        MakeBox($"UpperArm_{s}", UpperArmSize, ArmColor, shoulder)
            .localPosition = new Vector3(0, -UpperArmSize.y * 0.5f, 0);
        var elbow = Pivot($"ElbowPivot_{s}", shoulder, new Vector3(0, -UpperArmSize.y, 0));
        MakeBox($"LowerArm_{s}", LowerArmSize, HeadColor, elbow)
            .localPosition = new Vector3(0, -LowerArmSize.y * 0.5f, 0);
        if (isLeft) { _shoulderPivotL = shoulder; _elbowPivotL = elbow; }
        else        { _shoulderPivotR = shoulder; _elbowPivotR = elbow; }
    }

    static Transform Pivot(string name, Transform parent, Vector3 localPos)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.localPosition = localPos;
        return t;
    }

    static Transform MakeBox(string name, Vector3 size, Color color, Transform parent)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        Destroy(go.GetComponent<BoxCollider>());
        go.transform.SetParent(parent, false);
        go.transform.localScale = size;
        var shader = Shader.Find("MSRPG/ToonLit");
        var mat    = new Material(shader != null ? shader : Shader.Find("Universal Render Pipeline/Lit"));
        mat.color  = color;
        go.GetComponent<MeshRenderer>().material = mat;
        return go.transform;
    }

    void LateUpdate()
    {
        // 이륙 순간 속도 캡처 (걷기 점프 ↔ 달리기 점프 보간에 사용)
        if (_wasGrounded && !_isGrounded)
            _takeoffSpeed = _speed;
        _wasGrounded = _isGrounded;

        // ── 속도 기반 진폭 계산 ──────────────────────────────────
        float speedRange = Mathf.Max(0.01f, runSpeedRef - walkSpeedRef);
        float moveAmt = Mathf.Clamp01(_speed / walkSpeedRef);
        float runT    = Mathf.Clamp01((_speed - walkSpeedRef) / speedRange);

        float curLegSwing  = Mathf.Lerp(legSwingDeg,  runLegSwingDeg,  runT) * moveAmt;
        float curKneeFlex  = Mathf.Lerp(kneeFlexDeg,  runKneeFlexDeg,  runT) * moveAmt;
        float curArmSwing  = Mathf.Lerp(armSwingDeg,  runArmSwingDeg,  runT) * moveAmt;
        float curElbowFlex = Mathf.Lerp(elbowFlexDeg, runElbowFlexDeg, runT) * moveAmt;
        float freq         = Mathf.Lerp(cycleFrequency, cycleFrequency * runMultiplier, runT);
        float bobAmt       = runBobHeight * runT * moveAmt;

        if (moveAmt > 0.01f)
            _phase += Time.deltaTime * freq;

        float sin = Mathf.Sin(_phase * Mathf.PI * 2f);

        // ── 기본 보행 포즈 ────────────────────────────────────────
        _hipPivotL.localRotation      = Quaternion.Euler( sin * curLegSwing, 0, 0);
        _hipPivotR.localRotation      = Quaternion.Euler(-sin * curLegSwing, 0, 0);
        _kneePivotL.localRotation     = Quaternion.Euler(Mathf.Max(0f, -sin) * curKneeFlex, 0, 0);
        _kneePivotR.localRotation     = Quaternion.Euler(Mathf.Max(0f,  sin) * curKneeFlex, 0, 0);
        _shoulderPivotL.localRotation = Quaternion.Euler(-sin * curArmSwing, 0, 0);
        _shoulderPivotR.localRotation = Quaternion.Euler( sin * curArmSwing, 0, 0);
        _elbowPivotL.localRotation    = Quaternion.Euler(-Mathf.Max(0f,  sin) * curElbowFlex, 0, 0);
        _elbowPivotR.localRotation    = Quaternion.Euler(-Mathf.Max(0f, -sin) * curElbowFlex, 0, 0);

        // ── 웅크림 블렌드 (예비동작 + 착지 충격) ─────────────────
        float crouchTarget = _anticipating ? 1f : 0f;
        float crouchRate   = (crouchTarget > _crouchBlend) ? 20f : crouchRecoverSpeed;
        _crouchBlend = Mathf.MoveTowards(_crouchBlend, crouchTarget, Time.deltaTime * crouchRate);

        if (_crouchBlend > 0.01f)
        {
            float cb = _crouchBlend;
            _kneePivotL.localRotation = Quaternion.Slerp(_kneePivotL.localRotation,
                Quaternion.Euler(crouchKneeFlex, 0, 0), cb);
            _kneePivotR.localRotation = Quaternion.Slerp(_kneePivotR.localRotation,
                Quaternion.Euler(crouchKneeFlex, 0, 0), cb);
            _hipPivotL.localRotation  = Quaternion.Slerp(_hipPivotL.localRotation,
                Quaternion.Euler(-crouchHipFwd, 0, 0), cb);
            _hipPivotR.localRotation  = Quaternion.Slerp(_hipPivotR.localRotation,
                Quaternion.Euler(-crouchHipFwd, 0, 0), cb);
            _shoulderPivotL.localRotation = Quaternion.Slerp(_shoulderPivotL.localRotation,
                Quaternion.Euler(crouchArmBack, 0, 0), cb);
            _shoulderPivotR.localRotation = Quaternion.Slerp(_shoulderPivotR.localRotation,
                Quaternion.Euler(crouchArmBack, 0, 0), cb);
        }

        // ── 공중 포즈 블렌드 ──────────────────────────────────────
        _airBlend = Mathf.MoveTowards(_airBlend, _isGrounded ? 0f : 1f,
                        Time.deltaTime * jumpBlendSpeed);

        if (_airBlend > 0.01f)
        {
            float jumpRange = Mathf.Max(0.01f, runSpeedRef - walkSpeedRef);
            float runJumpT  = Mathf.Clamp01((_takeoffSpeed - walkSpeedRef) / jumpRange);
            float jt        = _airBlend;

            float hipFwd  = Mathf.Lerp(walkJumpHipFwd,   runJumpHipFwd,   runJumpT);
            float kFlex   = Mathf.Lerp(walkJumpKneeFlex,  runJumpKneeFlex,  runJumpT);
            float armBack = Mathf.Lerp(walkJumpArmBack,   runJumpArmBack,   runJumpT);
            float armOut  = Mathf.Lerp(walkJumpArmOut,    runJumpArmOut,    runJumpT);

            _hipPivotL.localRotation = Quaternion.Slerp(_hipPivotL.localRotation,
                Quaternion.Euler(-hipFwd, 0, 0), jt);
            _hipPivotR.localRotation = Quaternion.Slerp(_hipPivotR.localRotation,
                Quaternion.Euler(-hipFwd * 0.8f, 0, 0), jt);
            _kneePivotL.localRotation = Quaternion.Slerp(_kneePivotL.localRotation,
                Quaternion.Euler(kFlex, 0, 0), jt);
            _kneePivotR.localRotation = Quaternion.Slerp(_kneePivotR.localRotation,
                Quaternion.Euler(kFlex * 0.8f, 0, 0), jt);
            _shoulderPivotL.localRotation = Quaternion.Slerp(_shoulderPivotL.localRotation,
                Quaternion.Euler(armBack, 0, -armOut), jt);
            _shoulderPivotR.localRotation = Quaternion.Slerp(_shoulderPivotR.localRotation,
                Quaternion.Euler(armBack, 0,  armOut), jt);
            _elbowPivotL.localRotation = Quaternion.Slerp(_elbowPivotL.localRotation,
                Quaternion.Euler(-15f, 0, 0), jt);
            _elbowPivotR.localRotation = Quaternion.Slerp(_elbowPivotR.localRotation,
                Quaternion.Euler(-15f, 0, 0), jt);
        }

        // ── 루트 Y 오프셋 (달리기 바운스 - 웅크림) ──────────────
        float runBobY = Mathf.Abs(sin) * bobAmt * (1f - _airBlend);
        float crouchY = crouchDrop * _crouchBlend;
        _characterRoot.localPosition = new Vector3(0, runBobY - crouchY, 0);

        // ── 평타 모션 (우측 팔: sin 아크 0→1→0) ─────────────────
        if (_attackTimer > 0f)
        {
            float elapsed = AttackDuration - _attackTimer;
            float t  = elapsed / AttackDuration;
            float ab = Mathf.Sin(t * Mathf.PI);
            _attackTimer = Mathf.Max(0f, _attackTimer - Time.deltaTime);

            _shoulderPivotR.localRotation = Quaternion.Slerp(
                _shoulderPivotR.localRotation,
                Quaternion.Euler(-70f, 0f, 0f), ab);
            _elbowPivotR.localRotation = Quaternion.Slerp(
                _elbowPivotR.localRotation,
                Quaternion.Euler(15f, 0f, 0f), ab);
        }

        // ── 스킬 모션 (effectKind별 상이, 평타보다 우선) ─────────────────
        if (_skillTimer > 0f && _skillDuration > 0f)
        {
            float elapsed = _skillDuration - _skillTimer;
            float t  = elapsed / _skillDuration;               // 0→1
            float ab = Mathf.Sin(t * Mathf.PI);                // 0→1→0
            _skillTimer = Mathf.Max(0f, _skillTimer - Time.deltaTime);

            switch (_skillMotion)
            {
                // 강타: 오른팔 강하게 앞으로, 왼팔 뒤로
                case SkillMotion.Strike:
                    _shoulderPivotR.localRotation = Quaternion.Slerp(_shoulderPivotR.localRotation,
                        Quaternion.Euler(-88f, 0f, -6f), ab);
                    _elbowPivotR.localRotation = Quaternion.Slerp(_elbowPivotR.localRotation,
                        Quaternion.Euler(5f, 0f, 0f), ab);
                    _shoulderPivotL.localRotation = Quaternion.Slerp(_shoulderPivotL.localRotation,
                        Quaternion.Euler(30f, 0f, 0f), ab);
                    break;

                // 광역: 양팔 바깥으로 펼쳐 앞으로 쓸어내기
                case SkillMotion.Aoe:
                    _shoulderPivotL.localRotation = Quaternion.Slerp(_shoulderPivotL.localRotation,
                        Quaternion.Euler(-52f, 0f, -42f), ab);
                    _shoulderPivotR.localRotation = Quaternion.Slerp(_shoulderPivotR.localRotation,
                        Quaternion.Euler(-52f, 0f, 42f), ab);
                    _elbowPivotL.localRotation = Quaternion.Slerp(_elbowPivotL.localRotation,
                        Quaternion.Euler(0f, 0f, 0f), ab);
                    _elbowPivotR.localRotation = Quaternion.Slerp(_elbowPivotR.localRotation,
                        Quaternion.Euler(0f, 0f, 0f), ab);
                    break;

                // 회복·버프: 양팔 머리 위로 V자 들어올리기
                case SkillMotion.HealBuff:
                    _shoulderPivotL.localRotation = Quaternion.Slerp(_shoulderPivotL.localRotation,
                        Quaternion.Euler(-152f, 0f, -22f), ab);
                    _shoulderPivotR.localRotation = Quaternion.Slerp(_shoulderPivotR.localRotation,
                        Quaternion.Euler(-152f, 0f, 22f), ab);
                    _elbowPivotL.localRotation = Quaternion.Slerp(_elbowPivotL.localRotation,
                        Quaternion.Euler(-28f, 0f, 0f), ab);
                    _elbowPivotR.localRotation = Quaternion.Slerp(_elbowPivotR.localRotation,
                        Quaternion.Euler(-28f, 0f, 0f), ab);
                    break;

                // 표식: 오른팔 뻗어 가리키기, 왼팔 뒤로
                case SkillMotion.Mark:
                    _shoulderPivotR.localRotation = Quaternion.Slerp(_shoulderPivotR.localRotation,
                        Quaternion.Euler(-58f, 0f, -12f), ab);
                    _elbowPivotR.localRotation = Quaternion.Slerp(_elbowPivotR.localRotation,
                        Quaternion.Euler(0f, 0f, 0f), ab);
                    _shoulderPivotL.localRotation = Quaternion.Slerp(_shoulderPivotL.localRotation,
                        Quaternion.Euler(28f, 0f, 0f), ab);
                    break;
            }
        }
    }
}
