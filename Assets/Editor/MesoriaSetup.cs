using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Cinemachine;

public static class MesoriaSetup
{
    const string PlayerModelPath = "Assets/Casual_Male.fbx";

    [MenuItem("MSRPG/Setup Mesoria Scene (Phase 1)")]
    public static void Run()
    {
        // 1. 새 빈 씬
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 2. 조명
        var sunGO = new GameObject("Directional Light");
        var light = sunGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
        sunGO.transform.rotation = Quaternion.Euler(-50f, 30f, 0f);

        // 3. 지면
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(20f, 1f, 20f);
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(0.3f, 0.6f, 0.2f);
        ground.GetComponent<MeshRenderer>().sharedMaterial = mat;

        // 4. 보이지 않는 경계 벽
        CreateWall("Wall_N", new Vector3(  0f, 1.5f,  100f), new Vector3(200f, 3f,   1f));
        CreateWall("Wall_S", new Vector3(  0f, 1.5f, -100f), new Vector3(200f, 3f,   1f));
        CreateWall("Wall_E", new Vector3( 100f, 1.5f,   0f), new Vector3(  1f, 3f, 200f));
        CreateWall("Wall_W", new Vector3(-100f, 1.5f,   0f), new Vector3(  1f, 3f, 200f));

        // 5. 플레이어 루트 (CharacterController + PlayerController)
        var player = new GameObject("Player");
        player.transform.position = new Vector3(0f, 0f, 0f);
        var cc = player.AddComponent<CharacterController>();
        cc.center = new Vector3(0f, 1f, 0f);
        cc.radius = 0.3f;
        cc.height = 2f;
        player.AddComponent<PlayerController>();

        // 6. 캐릭터 모델 붙이기 (Humanoid 설정 후)
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
        var vcam = vcamGO.AddComponent<CinemachineCamera>();
        vcam.Follow = camTarget.transform;
        vcam.LookAt = camTarget.transform;
        var tpf = vcamGO.AddComponent<CinemachineThirdPersonFollow>();
        tpf.ShoulderOffset = new Vector3(0.5f, 0.4f, 0f);
        tpf.CameraDistance = 5f;
        tpf.VerticalArmLength = 0.4f;

        // 10. 씬 저장
        System.IO.Directory.CreateDirectory(Application.dataPath + "/_Game/Scenes");
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
        {
            if (child.name != "CameraTarget")
                toDelete.Add(child.gameObject);
        }
        foreach (var go in toDelete)
            Object.DestroyImmediate(go);

        // 캡슐 메시 제거 (있으면)
        var mr = player.GetComponent<MeshRenderer>();
        var mf = player.GetComponent<MeshFilter>();
        if (mr) Object.DestroyImmediate(mr);
        if (mf) Object.DestroyImmediate(mf);

        AttachCharacterModel(player);
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[MSRPG] ✅ 플레이어 모델 교체 완료!");
    }

    static void AttachCharacterModel(GameObject player)
    {
        // Humanoid rig 설정
        var importer = AssetImporter.GetAtPath(PlayerModelPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[MSRPG] {PlayerModelPath} 를 찾을 수 없습니다. 캡슐로 대체합니다.");
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Model_Placeholder";
            Object.DestroyImmediate(capsule.GetComponent<CapsuleCollider>());
            capsule.transform.SetParent(player.transform, false);
            capsule.transform.localPosition = new Vector3(0f, 1f, 0f);
            return;
        }

        if (importer.animationType != ModelImporterAnimationType.Human)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            importer.SaveAndReimport();
            Debug.Log("[MSRPG] Humanoid rig 설정 완료.");
        }

        var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerModelPath);
        var modelGO = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, player.transform);
        modelGO.transform.localPosition = Vector3.zero;
        modelGO.transform.localRotation = Quaternion.identity;
        modelGO.transform.localScale    = Vector3.one;
    }

    static void CreateWall(string name, Vector3 pos, Vector3 scale)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.position = pos;
        wall.transform.localScale = scale;
        Object.DestroyImmediate(wall.GetComponent<MeshRenderer>());
    }
}
