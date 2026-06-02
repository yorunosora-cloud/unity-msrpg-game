using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using Unity.Cinemachine;

public static class MesoriaSetup
{
    const string PlayerModelPath    = "Assets/Casual_Male.fbx";
    const string AnimControllerPath = "Assets/_Game/Characters/PlayerAnimator.controller";

    [MenuItem("MSRPG/Setup Mesoria Scene (Phase 1)")]
    public static void Run()
    {
        // 1. 새 빈 씬
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 2. 조명
        var sunGO = new GameObject("Directional Light");
        var light  = sunGO.AddComponent<Light>();
        light.type      = LightType.Directional;
        light.intensity = 1f;
        sunGO.transform.rotation = Quaternion.Euler(-50f, 30f, 0f);

        // 3. 지면
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(20f, 1f, 20f);
        var mat   = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(0.3f, 0.6f, 0.2f);
        ground.GetComponent<MeshRenderer>().sharedMaterial = mat;

        // 4. 보이지 않는 경계 벽
        CreateWall("Wall_N", new Vector3(  0f, 1.5f,  100f), new Vector3(200f, 3f,   1f));
        CreateWall("Wall_S", new Vector3(  0f, 1.5f, -100f), new Vector3(200f, 3f,   1f));
        CreateWall("Wall_E", new Vector3( 100f, 1.5f,   0f), new Vector3(  1f, 3f, 200f));
        CreateWall("Wall_W", new Vector3(-100f, 1.5f,   0f), new Vector3(  1f, 3f, 200f));

        // 5. 플레이어 루트
        var player = new GameObject("Player");
        player.transform.position = Vector3.zero;
        var cc  = player.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 1f, 0f);
        cc.radius = 0.3f;
        cc.height = 2f;
        player.AddComponent<PlayerController>();

        // 6. 캐릭터 모델 + Animator 설정
        AttachCharacterModel(player);

        // 7. 카메라 추적 타겟
        var camTarget = new GameObject("CameraTarget");
        camTarget.transform.SetParent(player.transform, false);
        camTarget.transform.localPosition = new Vector3(0f, 1.4f, 0f);

        // 8. Main Camera + CinemachineBrain
        var mainCamGO = new GameObject("Main Camera");
        mainCamGO.tag = "MainCamera";
        mainCamGO.AddComponent<Camera>();
        mainCamGO.AddComponent<AudioListener>();
        mainCamGO.AddComponent<CinemachineBrain>();
        mainCamGO.transform.position = new Vector3(0f, 3f, -8f);

        // 9. Cinemachine 3인칭 카메라
        var vcamGO = new GameObject("PlayerFollowCam");
        var vcam   = vcamGO.AddComponent<CinemachineCamera>();
        vcam.Follow = camTarget.transform;
        vcam.LookAt = camTarget.transform;
        var tpf = vcamGO.AddComponent<CinemachineThirdPersonFollow>();
        tpf.ShoulderOffset    = new Vector3(0.5f, 0.4f, 0f);
        tpf.CameraDistance    = 5f;
        tpf.VerticalArmLength = 0.4f;

        // 10. 씬 저장
        System.IO.Directory.CreateDirectory(Application.dataPath + "/_Game/Scenes");
        System.IO.Directory.CreateDirectory(Application.dataPath + "/_Game/Characters");
        EditorSceneManager.SaveScene(scene, "Assets/_Game/Scenes/Mesoria.unity");
        AssetDatabase.Refresh();

        Debug.Log("[MSRPG] ✅ Mesoria 씬 생성 완료! ▶ Play 버튼을 누르세요.");
    }

    [MenuItem("MSRPG/Replace Player Model")]
    public static void ReplacePlayerModel()
    {
        var player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogError("[MSRPG] Player 오브젝트를 찾을 수 없습니다. Mesoria 씬을 먼저 열어주세요.");
            return;
        }

        // 기존 모델 자식 제거 (CameraTarget 제외)
        var toDelete = new System.Collections.Generic.List<GameObject>();
        foreach (Transform child in player.transform)
            if (child.name != "CameraTarget") toDelete.Add(child.gameObject);
        foreach (var go in toDelete) Object.DestroyImmediate(go);

        // 루트 캡슐 메시 제거
        var mr = player.GetComponent<MeshRenderer>();
        var mf = player.GetComponent<MeshFilter>();
        if (mr) Object.DestroyImmediate(mr);
        if (mf) Object.DestroyImmediate(mf);

        AttachCharacterModel(player);
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[MSRPG] ✅ 플레이어 모델 교체 완료!");
    }

    // ── 내부 유틸 ──────────────────────────────────────────────

    static void AttachCharacterModel(GameObject player)
    {
        // 1. Humanoid rig 설정
        var importer = AssetImporter.GetAtPath(PlayerModelPath) as ModelImporter;
        if (importer == null)
        {
            FallbackCapsule(player);
            return;
        }
        if (importer.animationType != ModelImporterAnimationType.Human)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            importer.SaveAndReimport();
        }

        // 2. 모델 인스턴스
        var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerModelPath);
        var modelGO     = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, player.transform);
        modelGO.transform.localPosition = Vector3.zero;
        modelGO.transform.localRotation = Quaternion.identity;
        modelGO.transform.localScale    = Vector3.one;

        // 3. Animator Controller 생성 (없으면) 후 연결
        var controller = GetOrCreateAnimatorController();
        var anim       = modelGO.GetComponentInChildren<Animator>();
        if (anim == null) anim = modelGO.AddComponent<Animator>();
        anim.runtimeAnimatorController = controller;
    }

    static AnimatorController GetOrCreateAnimatorController()
    {
        // 이미 있으면 재사용
        var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimControllerPath);
        if (existing != null) return existing;

        // 새로 생성 — Speed(float), IsGrounded(bool) 파라미터 + Idle 상태
        System.IO.Directory.CreateDirectory(Application.dataPath + "/_Game/Characters");
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(AnimControllerPath);
        ctrl.AddParameter("Speed",      AnimatorControllerParameterType.Float);
        ctrl.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);

        // Idle 기본 상태 (애니메이션 클립 없이도 오류 방지)
        var root  = ctrl.layers[0].stateMachine;
        var idle  = root.AddState("Idle");
        var walk  = root.AddState("Walk");
        root.defaultState = idle;

        var toWalk = idle.AddTransition(walk);
        toWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        toWalk.hasExitTime = false;

        var toIdle = walk.AddTransition(idle);
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        toIdle.hasExitTime = false;

        AssetDatabase.SaveAssets();
        return ctrl;
    }

    static void FallbackCapsule(GameObject player)
    {
        Debug.LogWarning($"[MSRPG] {PlayerModelPath} 없음. 캡슐 플레이스홀더 사용.");
        var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.name = "Model_Placeholder";
        Object.DestroyImmediate(capsule.GetComponent<CapsuleCollider>());
        capsule.transform.SetParent(player.transform, false);
        capsule.transform.localPosition = new Vector3(0f, 1f, 0f);
    }

    static void CreateWall(string name, Vector3 pos, Vector3 scale)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.position   = pos;
        wall.transform.localScale = scale;
        Object.DestroyImmediate(wall.GetComponent<MeshRenderer>());
    }
}
