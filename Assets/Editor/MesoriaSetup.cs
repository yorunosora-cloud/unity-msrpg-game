using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Cinemachine;

public static class MesoriaSetup
{
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
        sunGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // 3. 지면 (Plane × 20 스케일 = 200×200 유닛)
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(20f, 1f, 20f);
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(0.3f, 0.6f, 0.2f);
        ground.GetComponent<MeshRenderer>().sharedMaterial = mat;

        // 4. 보이지 않는 경계 벽 (Collider만 있음)
        CreateWall("Wall_N", new Vector3(  0f, 1.5f,  100f), new Vector3(200f, 3f,   1f));
        CreateWall("Wall_S", new Vector3(  0f, 1.5f, -100f), new Vector3(200f, 3f,   1f));
        CreateWall("Wall_E", new Vector3( 100f, 1.5f,   0f), new Vector3(  1f, 3f, 200f));
        CreateWall("Wall_W", new Vector3(-100f, 1.5f,   0f), new Vector3(  1f, 3f, 200f));

        // 5. 플레이어 (캡슐 임시 모델 — 나중에 Quaternius 모델로 교체 가능)
        var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0f, 1f, 0f);
        Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
        var cc = player.AddComponent<CharacterController>();
        cc.center = Vector3.zero;
        cc.radius = 0.3f;
        cc.height = 2f;
        player.AddComponent<PlayerController>();

        // 6. 카메라 추적 타겟 (어깨 높이)
        var camTarget = new GameObject("CameraTarget");
        camTarget.transform.SetParent(player.transform, false);
        camTarget.transform.localPosition = new Vector3(0f, 0.4f, 0f);

        // 7. Main Camera + CinemachineBrain
        var mainCamGO = new GameObject("Main Camera");
        mainCamGO.tag = "MainCamera";
        mainCamGO.AddComponent<Camera>();
        mainCamGO.AddComponent<AudioListener>();
        mainCamGO.AddComponent<CinemachineBrain>();
        mainCamGO.transform.position = new Vector3(0f, 3f, -8f);

        // 8. Cinemachine 3인칭 카메라
        var vcamGO = new GameObject("PlayerFollowCam");
        var vcam = vcamGO.AddComponent<CinemachineCamera>();
        vcam.Follow = camTarget.transform;
        vcam.LookAt = camTarget.transform;
        var tpf = vcamGO.AddComponent<CinemachineThirdPersonFollow>();
        tpf.ShoulderOffset = new Vector3(0.5f, 0.4f, 0f);
        tpf.CameraDistance = 5f;
        tpf.VerticalArmLength = 0.4f;

        // 9. 씬 저장
        System.IO.Directory.CreateDirectory(Application.dataPath + "/_Game/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/_Game/Scenes/Mesoria.unity");
        AssetDatabase.Refresh();

        Debug.Log("[MSRPG] ✅ Mesoria 씬 생성 완료! Assets/_Game/Scenes/Mesoria.unity 을 열고 Play 버튼을 누르세요.");
    }

    static void CreateWall(string name, Vector3 pos, Vector3 scale)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.position = pos;
        wall.transform.localScale = scale;
        // 보이지 않게 (Collider는 유지)
        Object.DestroyImmediate(wall.GetComponent<MeshRenderer>());
    }
}
