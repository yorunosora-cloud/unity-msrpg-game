using UnityEditor;
using UnityEngine;

public static class EuclidSetup
{
    [MenuItem("MSRPG/Setup Euclid Boss Room")]
    public static void Build()
    {
        var bossRoot = new GameObject("EuclidBossRoom");

        var floorMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        floorMat.color = new Color(0.92f, 0.92f, 0.88f);

        var altarMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        altarMat.color = new Color(0.85f, 0.75f, 0.3f);

        var floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        floor.name = "ArenaFloor";
        floor.transform.SetParent(bossRoot.transform, false);
        floor.transform.localScale = new Vector3(30f, 0.1f, 30f);
        floor.GetComponent<Renderer>().material = floorMat;

        for (int i = 0; i < 8; i++)
        {
            float   angle  = i * 45f * Mathf.Deg2Rad;
            Vector3 pos    = new Vector3(Mathf.Sin(angle) * 9f, 3f, Mathf.Cos(angle) * 9f);
            var     pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = $"Pillar_{i}";
            pillar.transform.SetParent(bossRoot.transform, false);
            pillar.transform.localPosition = pos;
            pillar.transform.localScale    = new Vector3(0.8f, 3f, 0.8f);
            pillar.GetComponent<Renderer>().material = floorMat;
        }

        var altar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        altar.name = "Altar";
        altar.transform.SetParent(bossRoot.transform, false);
        altar.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        altar.transform.localScale    = new Vector3(6f, 0.2f, 6f);
        altar.GetComponent<Renderer>().material = altarMat;

        var bossGO = new GameObject("Euclid");
        bossGO.transform.SetParent(bossRoot.transform, false);
        bossGO.transform.localPosition = new Vector3(0f, 0f, 4f);
        bossGO.AddComponent<Boss>();
        var brain = bossGO.AddComponent<BossBrain>();

        var weakPointGO = new GameObject("WeakPoint");
        weakPointGO.transform.SetParent(bossGO.transform, false);
        weakPointGO.transform.localPosition = new Vector3(0f, 1.5f, -0.5f);
        weakPointGO.AddComponent<BossWeakPoint>();

        var def = AssetDatabase.LoadAssetAtPath<BossDef>("Assets/_Game/Data/Bosses/Euclid.asset");
        if (def != null)
        {
            var so   = new SerializedObject(bossGO.GetComponent<Boss>());
            var prop = so.FindProperty("_def");
            if (prop != null) { prop.objectReferenceValue = def; so.ApplyModifiedProperties(); }
        }
        else
        {
            Debug.LogWarning("[EuclidSetup] Euclid.asset not found. Connect BossDef manually.");
        }

        var triggerGO = new GameObject("BossRoomTrigger");
        triggerGO.transform.SetParent(bossRoot.transform, false);
        triggerGO.transform.localPosition = new Vector3(0f, 1f, -12f);
        var triggerCol = triggerGO.AddComponent<BoxCollider>();
        triggerCol.isTrigger = true;
        triggerCol.size      = new Vector3(8f, 3f, 1f);
        var trigger = triggerGO.AddComponent<BossRoomTrigger>();

        var triggerSO   = new SerializedObject(trigger);
        var brainProp   = triggerSO.FindProperty("_brain");
        if (brainProp != null) { brainProp.objectReferenceValue = brain; }

        var doorGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        doorGO.name = "SealDoor";
        doorGO.transform.SetParent(bossRoot.transform, false);
        doorGO.transform.localPosition = new Vector3(0f, 1.5f, -13f);
        doorGO.transform.localScale    = new Vector3(8f, 3f, 0.3f);
        doorGO.GetComponent<Renderer>().material = altarMat;
        doorGO.SetActive(false);

        var doorsProp = triggerSO.FindProperty("_sealDoors");
        if (doorsProp != null)
        {
            doorsProp.arraySize = 1;
            doorsProp.GetArrayElementAtIndex(0).objectReferenceValue = doorGO;
        }
        triggerSO.ApplyModifiedProperties();

        bossGO.AddComponent<BossMission>();

        EditorUtility.SetDirty(bossRoot);
        Debug.Log("[EuclidSetup] Euclid Boss Room created. Check BossDef assignment.");
    }
}
