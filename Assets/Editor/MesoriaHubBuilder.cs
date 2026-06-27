using UnityEngine;
using UnityEditor;

/// <summary>
/// 메조리아 허브 맵 v2 — 실크로드 척추 + 광장 골격
/// 레이아웃 (남→북):
///   스폰(0,0,-100) → 남쪽 성문(z≈-130) → 길드 가로(z≈-70~-110)
///   → 실크로드 대광장(z=0) → 중립 조약의 탑(z≈+65) → 그랜드 하버(z≈+150)
///   서쪽(x≈-87): 통합 학술원 / 동쪽(x≈+87): 지식의 거래소
///
/// 루트 GO 이름 보존 (MetaUISetup.WireBuilding 이름 기반):
///   HubLab, HubLibrary, HubGuildHall, HubPortal_*
/// 신규: HubTreatyTower, HubGuildRow, HubCityBlocks
/// </summary>
public static class MesoriaHubBuilder
{
    // ── 프리팹 경로 ──────────────────────────────────────────────────────────
    const string BASE  = "Assets/Synty/PolygonGeneric/Prefabs/Base/";
    const string BLD   = "Assets/Synty/PolygonGeneric/Prefabs/Building/";
    const string ENV   = "Assets/Synty/PolygonGeneric/Prefabs/Environment/";
    const string PROPS = "Assets/Synty/PolygonGeneric/Prefabs/Props/";

    const string P_PILLAR_A  = BASE + "SM_Bld_Base_Pillar_01.prefab";
    const string P_PILLAR_C  = BASE + "SM_Bld_Base_Pillar_05.prefab";

    const string P_BG_A = BLD + "SM_Gen_Bld_Background_01.prefab";
    const string P_BG_B = BLD + "SM_Gen_Bld_Background_02.prefab";
    const string P_BG_C = BLD + "SM_Gen_Bld_Background_05.prefab";
    const string P_BG_D = BLD + "SM_Gen_Bld_Background_07.prefab";
    const string P_BG_E = BLD + "SM_Gen_Bld_Background_09.prefab";
    const string P_BG_F = BLD + "SM_Gen_Bld_Background_11.prefab";

    const string P_WATER    = ENV + "SM_Gen_Env_Water_Plane_01.prefab";
    const string P_TREE_A   = ENV + "SM_Gen_Env_Tree_01.prefab";
    const string P_TREE_B   = ENV + "SM_Gen_Env_Tree_02.prefab";
    const string P_TREE_C   = ENV + "SM_Gen_Env_Tree_03.prefab";
    const string P_ROCK_A   = ENV + "SM_Gen_Env_Rock_01.prefab";
    const string P_ROCK_B   = ENV + "SM_Gen_Env_Rock_03.prefab";
    const string P_BUSH_A   = ENV + "SM_Gen_Env_Bush_01.prefab";

    const string P_BARREL_W   = PROPS + "SM_Gen_Prop_Barrel_Wood_01.prefab";
    const string P_BARREL_M   = PROPS + "SM_Gen_Prop_Barrel_Metal_01.prefab";
    const string P_CRATE      = PROPS + "SM_Gen_Prop_Crate_01.prefab";
    const string P_SACK       = PROPS + "SM_Gen_Prop_Sack_01.prefab";
    const string P_SACK_STACK = PROPS + "SM_Gen_Prop_Sack_Stack_01.prefab";
    const string P_POT_A      = PROPS + "SM_Gen_Prop_Pot_01.prefab";
    const string P_POT_B      = PROPS + "SM_Gen_Prop_Pot_03.prefab";
    const string P_CHEST      = PROPS + "SM_Gen_Prop_Chest_01.prefab";
    const string P_STATUE_A   = PROPS + "SM_Gen_Prop_Statue_01.prefab";
    const string P_PLINTH     = PROPS + "SM_Gen_Prop_Plinth_01.prefab";
    const string P_TABLE      = PROPS + "SM_Gen_Prop_Table_01.prefab";
    const string P_COIN_PILE  = PROPS + "SM_Gen_Prop_Coin_Pile_01.prefab";

    // ── 팔레트 ───────────────────────────────────────────────────────────────
    static readonly Color StoneWarm  = new Color(0.92f, 0.88f, 0.78f); // 크림화이트
    static readonly Color StoneCold  = new Color(0.70f, 0.72f, 0.75f);
    static readonly Color StoneDark  = new Color(0.22f, 0.14f, 0.08f); // 짙은 목재 갈색
    static readonly Color StonePave  = new Color(0.48f, 0.46f, 0.43f); // 짙은 회석 — 황토 지면과 대비
    static readonly Color StoneLight = new Color(0.88f, 0.86f, 0.80f);
    static readonly Color GrassGreen = new Color(0.28f, 0.52f, 0.18f);
    static readonly Color Gold       = new Color(0.78f, 0.66f, 0.25f);
    static readonly Color GoldBright = new Color(0.98f, 0.86f, 0.32f);
    static readonly Color SeaBlue    = new Color(0.18f, 0.45f, 0.72f);
    static readonly Color WaterBlue  = new Color(0.32f, 0.68f, 0.92f);
    static readonly Color DockWood   = new Color(0.52f, 0.36f, 0.18f);
    static readonly Color TowerSlate  = new Color(0.28f, 0.25f, 0.38f);
    static readonly Color TowerGold   = new Color(0.85f, 0.72f, 0.20f);
    static readonly Color TileRed      = new Color(0.55f, 0.25f, 0.12f); // 적갈색 기와
    static readonly Color PlasterCream = new Color(0.94f, 0.91f, 0.82f); // 회반죽
    static readonly Color TimberBrown  = new Color(0.28f, 0.18f, 0.10f); // 목재 빔

    static readonly Color[] PortalColors =
    {
        new Color(0.17f, 0.50f, 1.00f),
        new Color(1.00f, 0.09f, 0.27f),
        new Color(0.24f, 0.77f, 0.15f),
        new Color(0.77f, 0.36f, 0.13f),
        new Color(0.90f, 0.82f, 0.00f),
        new Color(0.67f, 0.00f, 1.00f),
    };
    static readonly string[] PortalLabels =
        { "물리", "화학", "생명과학", "지구과학", "수학", "정보" };

    static readonly Color[] DockColors =
    {
        new Color(0.17f, 0.50f, 1.00f),
        new Color(1.00f, 0.09f, 0.27f),
        new Color(0.24f, 0.77f, 0.15f),
        new Color(0.77f, 0.36f, 0.13f),
        new Color(0.90f, 0.82f, 0.00f),
        new Color(0.67f, 0.00f, 1.00f),
    };
    static readonly string[] DockNames =
        { "증명 부두", "연금 부두", "생명 항만", "지구 항만", "수학 부두", "데이터 항만" };

    // ── 레이아웃 상수 ─────────────────────────────────────────────────────────
    const float PORTAL_R    = 40f;
    const float BAZAAR_R    = 63f;
    const float BUILDING_R  = 87f;
    const float HARBOR_Z    = 155f;
    const float GROUND_HALF = 250f;
    const float SPINE_W     = 26f;
    const float PLAZA_R     = 50f;
    const float GATE_Z      = -130f;
    const float TOWER_Z     = 65f;

    // ─────────────────────────────────────────────────────────────────────────
    public static void Build()
    {
        BuildGround();
        BuildPaving();
        BuildFountain();
        BuildPortalRing();
        BuildBazaar();
        BuildTreatyTower();
        BuildMainBuildings();
        BuildGuildRow();
        BuildCityBlocks();
        BuildSouthGate();
        BuildHarbor();
        BuildBackgroundCity();
        BuildScenery();
        BuildWalls();
        AdjustLighting();
    }

    // ── 1. 지면 ──────────────────────────────────────────────────────────────
    static void BuildGround()
    {
        // 전체 지면 — GROUND_HALF=250 → scale 0.2 = 500×500 (벽 경계와 일치)
        // scale 0.1은 250×250이라 성문(z=-130) 포함 외곽이 바닥 없이 뻥 뚫림
        var g = GameObject.CreatePrimitive(PrimitiveType.Plane);
        g.name = "HubGround";
        g.transform.localScale = new Vector3(GROUND_HALF * 0.2f, 1f, GROUND_HALF * 0.2f);
        ApplyColor(g, new Color(0.46f, 0.38f, 0.26f)); // 황토색 흙

        // 외곽 잔디 구역 (동/서/남)
        foreach (var (pos, sc) in new (Vector3 p, Vector3 s)[] {
            (new Vector3(-175f, 0.001f,  0f),  new Vector3(10f, 1f, 25f)),
            (new Vector3( 175f, 0.001f,  0f),  new Vector3(10f, 1f, 25f)),
            (new Vector3(   0f, 0.001f,-185f), new Vector3(25f, 1f,  7f)),
        })
        {
            var gp = GameObject.CreatePrimitive(PrimitiveType.Plane);
            gp.name = "HubGrassOuter";
            gp.transform.position   = pos;
            gp.transform.localScale = sc;
            ApplyColor(gp, new Color(0.28f, 0.48f, 0.16f)); // 짙은 잔디
            Object.DestroyImmediate(gp.GetComponent<MeshCollider>());
        }
    }

    // ── 2. 포장 시스템 (척추 대로 + 광장 + 연결로) ───────────────────────────
    static void BuildPaving()
    {
        var root = new GameObject("HubPaving");

        // ─ 실크로드 대로 (척추) z=-130 ~ z=+140, 폭 26 ─
        const float SLAB_Y  = 0.25f;  // 지면 위 0.25 — 눈에 띄는 턱 효과
        const float SLAB_H  = 0.50f;  // 도로 두께 (top=0.50)
        const float PLAZA_Y = 0.20f;
        const float PLAZA_H = 0.40f;

        float spineLen = HARBOR_Z - 15f - GATE_Z;
        float spineZ   = (GATE_Z + (HARBOR_Z - 15f)) * 0.5f;

        // 중세 자갈 포장 팔레트
        Color cobbleStone = new Color(0.56f, 0.51f, 0.40f);  // 따뜻한 석회암
        Color cobbleGrout = new Color(0.16f, 0.12f, 0.08f);  // 회반죽 줄눈
        Color curbColor   = new Color(0.65f, 0.60f, 0.48f);  // 연석

        // 도로 기반 (자갈 포장)
        Box(root.transform, "SpineRoad",
            new Vector3(0f, SLAB_Y, spineZ),
            new Vector3(SPINE_W, SLAB_H, spineLen), cobbleStone);

        float roadTop = SLAB_Y + SLAB_H * 0.5f + 0.01f;
        float halfW   = SPINE_W * 0.5f;  // 13f

        // 가로 줄눈 (Z 방향, 2.5유닛 간격) — 자갈 행 구분
        for (float gz = GATE_Z + 2.5f; gz < HARBOR_Z - 15f; gz += 2.5f)
            Box(root.transform, $"GroutZ_{(int)(gz + 200f)}",
                new Vector3(0f, roadTop, gz),
                new Vector3(SPINE_W - 0.3f, 0.022f, 0.20f), cobbleGrout);

        // 세로 줄눈 (X 방향, 3.25유닛 간격) — 자갈 열 구분
        for (float gx = -halfW + 3.25f; gx < halfW; gx += 3.25f)
            Box(root.transform, $"GroutX_{(int)((gx + halfW) * 10f)}",
                new Vector3(gx, roadTop, spineZ),
                new Vector3(0.20f, 0.022f, spineLen - 0.3f), cobbleGrout);

        // 중앙 배수로 (중세 도로 특징 — 오수가 중앙 홈으로 흐름)
        Box(root.transform, "SpineDrain",
            new Vector3(0f, roadTop, spineZ),
            new Vector3(0.90f, 0.025f, spineLen), cobbleGrout);

        // 연석 (도로 양쪽 경계 — 도로보다 약간 높게)
        float curbX = halfW + 0.75f;
        Box(root.transform, "CurbW",
            new Vector3(-curbX, SLAB_Y + 0.05f, spineZ),
            new Vector3(1.5f, SLAB_H + 0.10f, spineLen), curbColor);
        Box(root.transform, "CurbE",
            new Vector3( curbX, SLAB_Y + 0.05f, spineZ),
            new Vector3(1.5f, SLAB_H + 0.10f, spineLen), curbColor);

        // 인도 (연석 바깥 플래그스톤)
        float swX = curbX + 0.75f + 6f;
        Box(root.transform, "SpineSidewalkW",
            new Vector3(-swX, SLAB_Y - 0.05f, spineZ),
            new Vector3(12f, SLAB_H - 0.1f, spineLen), StoneWarm);
        Box(root.transform, "SpineSidewalkE",
            new Vector3( swX, SLAB_Y - 0.05f, spineZ),
            new Vector3(12f, SLAB_H - 0.1f, spineLen), StoneWarm);

        // ─ 실크로드 대광장 (중심 z=0, 100x100) ─
        Box(root.transform, "PlazaFloor",
            new Vector3(0f, PLAZA_Y, 0f),
            new Vector3(PLAZA_R * 2f, PLAZA_H, PLAZA_R * 2f), StoneLight);

        // 광장 격자 패턴 (플로어 상단면 위 1cm)
        float gridTop = PLAZA_Y + PLAZA_H * 0.5f + 0.01f;
        for (int i = -4; i <= 4; i++)
        {
            if (i == 0) continue;
            Box(root.transform, $"PlazaGridH_{i}",
                new Vector3(0f, gridTop, i * 11f),
                new Vector3(PLAZA_R * 2f, 0.02f, 0.55f), StonePave);
            Box(root.transform, $"PlazaGridV_{i}",
                new Vector3(i * 11f, gridTop, 0f),
                new Vector3(0.55f, 0.02f, PLAZA_R * 2f), StonePave);
        }

        // 광장 테두리 (경계석)
        float bTop = PLAZA_Y + PLAZA_H * 0.5f + 0.05f;
        Box(root.transform, "PlazaBorderN", new Vector3(0f, bTop,  PLAZA_R), new Vector3(PLAZA_R * 2f + 3f, 0.10f, 1.5f), StoneDark);
        Box(root.transform, "PlazaBorderS", new Vector3(0f, bTop, -PLAZA_R), new Vector3(PLAZA_R * 2f + 3f, 0.10f, 1.5f), StoneDark);
        Box(root.transform, "PlazaBorderE", new Vector3( PLAZA_R, bTop, 0f), new Vector3(1.5f, 0.10f, PLAZA_R * 2f + 3f), StoneDark);
        Box(root.transform, "PlazaBorderW", new Vector3(-PLAZA_R, bTop, 0f), new Vector3(1.5f, 0.10f, PLAZA_R * 2f + 3f), StoneDark);

        // ─ 연결로: 광장 → 학술원(서) ─
        float labBridgeX  = -(PLAZA_R + (BUILDING_R - PLAZA_R) * 0.5f);
        float labBridgeLen = BUILDING_R - PLAZA_R - 12f;
        Box(root.transform, "PathToLab",
            new Vector3(labBridgeX, SLAB_Y - 0.02f, 10f),
            new Vector3(labBridgeLen, SLAB_H - 0.04f, 14f), StonePave);

        // ─ 연결로: 광장 → 거래소(동) ─
        Box(root.transform, "PathToLib",
            new Vector3(-labBridgeX, SLAB_Y - 0.02f, 10f),
            new Vector3(labBridgeLen, SLAB_H - 0.04f, 14f), StonePave);

        // ─ 길드 가로 진입광장 ─
        Box(root.transform, "GuildRowPlaza",
            new Vector3(0f, SLAB_Y - 0.02f, -85f),
            new Vector3(SPINE_W + 20f, SLAB_H - 0.04f, 50f), StonePave);

        // ─ 조약의 탑 주변 광장 (z=+65) ─
        Box(root.transform, "TowerPlaza",
            new Vector3(0f, PLAZA_Y, TOWER_Z),
            new Vector3(44f, PLAZA_H, 44f), StoneLight);
        float tGridTop = PLAZA_Y + PLAZA_H * 0.5f + 0.01f;
        for (int i = -2; i <= 2; i++)
        {
            if (i == 0) continue;
            Box(root.transform, $"TowerGridH_{i}",
                new Vector3(0f, tGridTop, TOWER_Z + i * 9f),
                new Vector3(44f, 0.02f, 0.45f), StonePave);
            Box(root.transform, $"TowerGridV_{i}",
                new Vector3(i * 9f, tGridTop, TOWER_Z),
                new Vector3(0.45f, 0.02f, 44f), StonePave);
        }

        // ─ 척추 남쪽 가로등 (6쌍) ─
        for (int i = 0; i < 6; i++)
        {
            float z = GATE_Z + 5f + i * 18f;
            LampPost(root.transform, new Vector3(-(SPINE_W * 0.5f + 2.5f), 0f, z), $"LampW_{i}");
            LampPost(root.transform, new Vector3(  SPINE_W * 0.5f + 2.5f, 0f, z),  $"LampE_{i}");
        }

        // ─ 척추 북쪽 가로등 (4쌍, 광장 북단~탑) ─
        for (int i = 0; i < 4; i++)
        {
            float z = 5f + i * 14f;
            LampPost(root.transform, new Vector3(-(SPINE_W * 0.5f + 2.5f), 0f, z), $"LampNW_{i}");
            LampPost(root.transform, new Vector3(  SPINE_W * 0.5f + 2.5f, 0f, z),  $"LampNE_{i}");
        }
    }

    static void LampPost(Transform parent, Vector3 pos, string n)
    {
        var go = new GameObject(n);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        Box(go.transform, "Post",  new Vector3(0f, 3.0f, 0f), new Vector3(0.35f, 6.0f, 0.35f), StoneDark);
        var head = Box(go.transform, "Head", new Vector3(0f, 6.3f, 0f), new Vector3(1.0f, 0.45f, 1.0f), Gold);
        SetEmissive(head, Gold, GoldBright * 0.7f);
    }

    // ── 3. 중앙 분수 (3단 비잔틴) ────────────────────────────────────────────
    static void BuildFountain()
    {
        var root = new GameObject("HubFountain");

        Cyl(root.transform, "Base_Ring",  new Vector3(0f, 0.20f, 0f), new Vector3(16f, 0.20f, 16f), StoneWarm);
        Cyl(root.transform, "Basin1",     new Vector3(0f, 0.60f, 0f), new Vector3(14f, 0.80f, 14f), StoneCold);
        Cyl(root.transform, "Basin1_Rim", new Vector3(0f, 1.35f, 0f), new Vector3(14f, 0.20f, 14f), StoneDark);
        var w1 = Cyl(root.transform, "Water1", new Vector3(0f, 1.40f, 0f), new Vector3(12.5f, 0.05f, 12.5f), WaterBlue);
        SetEmissive(w1, WaterBlue, WaterBlue * 0.55f); RemoveCollider(w1);

        Cyl(root.transform, "Column1", new Vector3(0f, 0.50f, 0f), new Vector3(1.6f, 7.0f, 1.6f), Gold);

        for (int i = 0; i < 4; i++)
        {
            float a = i * 90f * Mathf.Deg2Rad;
            var p = new Vector3(Mathf.Sin(a) * 6.5f, 0f, Mathf.Cos(a) * 6.5f);
            Prop(root.transform, P_PLINTH,   p + new Vector3(0f, 0.25f, 0f), Vector3.one * 0.85f, $"Plinth_{i}");
            Prop(root.transform, P_STATUE_A, p + new Vector3(0f, 1.40f, 0f), Vector3.one * 0.75f, $"Statue_{i}");
        }

        Cyl(root.transform, "Basin2",     new Vector3(0f, 7.20f, 0f), new Vector3(8.5f, 0.70f, 8.5f), StoneCold);
        Cyl(root.transform, "Basin2_Rim", new Vector3(0f, 7.75f, 0f), new Vector3(8.5f, 0.20f, 8.5f), StoneDark);
        var w2 = Cyl(root.transform, "Water2", new Vector3(0f, 7.80f, 0f), new Vector3(7.2f, 0.05f, 7.2f), WaterBlue);
        SetEmissive(w2, WaterBlue, WaterBlue * 0.45f); RemoveCollider(w2);

        Cyl(root.transform, "Column2", new Vector3(0f, 8.00f, 0f), new Vector3(0.9f, 4.5f, 0.9f), Gold);

        Cyl(root.transform, "Basin3",     new Vector3(0f, 12.50f, 0f), new Vector3(4.2f, 0.50f, 4.2f), StoneCold);
        Cyl(root.transform, "Basin3_Rim", new Vector3(0f, 12.85f, 0f), new Vector3(4.2f, 0.15f, 4.2f), StoneDark);
        var w3 = Cyl(root.transform, "Water3", new Vector3(0f, 12.90f, 0f), new Vector3(3.2f, 0.05f, 3.2f), WaterBlue);
        SetEmissive(w3, WaterBlue, WaterBlue * 0.35f); RemoveCollider(w3);

        var orb = Sphere(root.transform, "TopOrb", new Vector3(0f, 14.2f, 0f), Vector3.one * 2.2f, Gold);
        SetEmissive(orb, Gold, GoldBright * 0.7f); RemoveCollider(orb);

        var fmm = root.AddComponent<MapMarker>();
        fmm.kind        = MapMarker.IconKind.Building;
        fmm.displayName = "중앙 분수";
        fmm.iconColor   = WaterBlue;
        fmm.footprintW  = 26f;
        fmm.footprintD  = 26f;
    }

    // ── 4. 6대륙 포탈 링 (반지름 40, 보존) ──────────────────────────────────
    static void BuildPortalRing()
    {
        for (int i = 0; i < 6; i++)
        {
            float deg = i * 60f;
            float rad = deg * Mathf.Deg2Rad;
            var   pos = new Vector3(Mathf.Sin(rad) * PORTAL_R, 0f, Mathf.Cos(rad) * PORTAL_R);
            float ry  = (deg + 180f) % 360f;
            BuildPortal(i, pos, ry);
        }
    }

    static void BuildPortal(int idx, Vector3 wPos, float rotY)
    {
        Color  pc   = PortalColors[idx];
        string name = PortalLabels[idx];

        var root = new GameObject($"HubPortal_{name}");
        root.transform.position = wPos;
        root.transform.rotation = Quaternion.Euler(0f, rotY, 0f);

        var ia = root.AddComponent<Interactable>();
        ia.displayName = $"{name} 포탈";
        ia.promptText  = (name == "수학")
            ? "[E]  아틀란티스로 이동"
            : $"[E]  {name} 대륙으로 이동";
        ia.radius      = 7f;

        if (name == "수학")
        {
            var sp = root.AddComponent<ScenePortal>();
            WirePortal(ia, sp, "Atlantis", "AxiomSpawn");

            // 복귀 도착 스폰 포인트 — 씬 루트에 두어야 GameObject.Find() 가 확실히 찾음
            // rotY = deg+180 이므로 forward 가 중앙을 향함 → +8f = 포탈 통과 직후 위치
            var mathSpawn = new GameObject("MathPortalSpawn");
            mathSpawn.transform.position = wPos + root.transform.TransformDirection(Vector3.forward) * 8f
                                           + Vector3.up * 0.5f;
        }

        var pad = Cyl(root.transform, "Pad", new Vector3(0f, 0.15f, 0f), new Vector3(6f, 0.15f, 6f), pc);
        SetEmissive(pad, pc, pc * 0.55f); RemoveCollider(pad);

        Box(root.transform, "Step1", new Vector3(0f, 0.10f, -5.5f), new Vector3(4.5f, 0.20f, 1.2f), StoneWarm);
        Box(root.transform, "Step2", new Vector3(0f, 0.25f, -4.5f), new Vector3(4.2f, 0.20f, 1.0f), StoneWarm);
        Box(root.transform, "Step3", new Vector3(0f, 0.40f, -3.6f), new Vector3(4.0f, 0.20f, 0.8f), StoneCold);

        Pillar(root.transform, "PillarL_In",  new Vector3(-2.6f, 0f, 0f), StoneWarm);
        Pillar(root.transform, "PillarL_Out", new Vector3(-4.4f, 0f, 0f), StoneCold);
        Pillar(root.transform, "PillarR_In",  new Vector3( 2.6f, 0f, 0f), StoneWarm);
        Pillar(root.transform, "PillarR_Out", new Vector3( 4.4f, 0f, 0f), StoneCold);

        Box(root.transform, "ArchBeam", new Vector3(0f, 9.5f, 0f), new Vector3(10.5f, 1.2f, 1.0f), pc);

        var glow = Cube(root.transform, "GlowPanel", new Vector3(0f, 4.6f, 0.15f), new Vector3(4.5f, 7.5f, 0.08f));
        SetEmissive(glow, new Color(pc.r, pc.g, pc.b, 0.9f), pc * 0.6f);

        var top = Box(root.transform, "TopDeco", new Vector3(0f, 11.2f, 0f), new Vector3(3.2f, 1.5f, 0.7f), Gold);
        SetEmissive(top, Gold, GoldBright * 0.25f);

        var pm = root.AddComponent<MapMarker>();
        pm.kind        = MapMarker.IconKind.Portal;
        pm.displayName = name;
        pm.iconColor   = pc;
    }

    // ── 5. 바자르 노점 ────────────────────────────────────────────────────────
    static void BuildBazaar()
    {
        var root = new GameObject("HubBazaar");

        string[] stallColors = { "#C44", "#4A8", "#88C", "#CA6", "#6AC", "#A6C" };

        for (int i = 0; i < 6; i++)
        {
            float deg = i * 60f + 30f;
            float rad = deg * Mathf.Deg2Rad;
            var   pos = new Vector3(Mathf.Sin(rad) * BAZAAR_R, 0f, Mathf.Cos(rad) * BAZAAR_R);

            var stall = new GameObject($"Stall_{i}");
            stall.transform.SetParent(root.transform);
            stall.transform.position = pos;
            stall.transform.rotation = Quaternion.Euler(0f, deg + 180f, 0f);

            ColorUtility.TryParseHtmlString(stallColors[i % stallColors.Length], out Color sc);
            Box(stall.transform, "Awning",  new Vector3(0f, 3.6f, 0f),    new Vector3(6.5f, 0.25f, 4.2f), sc);
            Box(stall.transform, "PostL",   new Vector3(-2.8f, 1.8f, 0f), new Vector3(0.30f, 3.6f, 0.30f), StoneDark);
            Box(stall.transform, "PostR",   new Vector3( 2.8f, 1.8f, 0f), new Vector3(0.30f, 3.6f, 0.30f), StoneDark);
            Box(stall.transform, "Counter", new Vector3(0f, 0.9f, 0.3f),  new Vector3(5.5f, 0.8f, 1.2f), StoneWarm);

            Prop(stall.transform, P_TABLE,     new Vector3(0f, 0.5f, 0.5f),   Vector3.one * 1.4f, "Table");
            Prop(stall.transform, P_POT_A,     new Vector3(-1.5f, 1.1f, 0.5f), Vector3.one * 0.7f, "Pot");
            Prop(stall.transform, P_BARREL_W,  new Vector3( 1.9f, 0.5f, 0f),  Vector3.one * 0.9f, "Barrel");
            Prop(stall.transform, P_SACK,      new Vector3(-1.9f, 0.5f, 0f),  Vector3.one * 0.8f, "Sack");
            Prop(stall.transform, P_COIN_PILE, new Vector3(0f, 1.1f, 0.3f),   Vector3.one * 0.55f, "Coins");
        }
    }

    // ── 6. 중립 조약의 탑 (HubTreatyTower, z=+65) ───────────────────────────
    static void BuildTreatyTower()
    {
        var root = new GameObject("HubTreatyTower");
        root.transform.position = new Vector3(0f, 0f, TOWER_Z);

        // 기단 (넓은 8각 플랫폼)
        Cyl(root.transform, "Base1",    new Vector3(0f, 0.5f, 0f),  new Vector3(22f, 1.0f, 22f), StoneDark);
        Cyl(root.transform, "Base2",    new Vector3(0f, 1.2f, 0f),  new Vector3(16f, 0.8f, 16f), StoneCold);
        Cyl(root.transform, "Base3",    new Vector3(0f, 1.8f, 0f),  new Vector3(11f, 0.6f, 11f), StoneWarm);

        // 기단 계단 (사방)
        foreach (var (dx, dz, ry) in new (float, float, float)[] {
            (0f, -11f, 0f), (0f, 11f, 180f), (-11f, 0f, 90f), (11f, 0f, -90f) })
        {
            var s = new GameObject("Stair");
            s.transform.SetParent(root.transform, false);
            s.transform.localPosition = new Vector3(dx, 0f, dz);
            s.transform.localRotation = Quaternion.Euler(0f, ry, 0f);
            Box(s.transform, "S1", new Vector3(0f, 0.2f, -3.0f),  new Vector3(5f, 0.4f, 2.0f), StoneDark);
            Box(s.transform, "S2", new Vector3(0f, 0.6f, -1.5f),  new Vector3(5f, 0.4f, 1.6f), StoneCold);
            Box(s.transform, "S3", new Vector3(0f, 1.0f, -0.2f),  new Vector3(5f, 0.4f, 1.2f), StoneWarm);
        }

        // 1층 탑신 (사각 기둥)
        Box(root.transform, "Tower1", new Vector3(0f, 8.2f, 0f), new Vector3(8f, 13f, 8f), TowerSlate);

        // 1층 기둥 4개
        foreach (var (px, pz) in new (float, float)[] { (-4f,-4f),(4f,-4f),(-4f,4f),(4f,4f) })
            Cyl(root.transform, "Pillar", new Vector3(px, 7.5f, pz), new Vector3(1.0f, 12f, 1.0f), StoneDark);

        // 1층 발코니
        Cyl(root.transform, "Balcony1", new Vector3(0f, 15.0f, 0f), new Vector3(12f, 0.6f, 12f), StoneCold);
        var bRim1 = Cyl(root.transform, "Balcony1Rim", new Vector3(0f, 15.3f, 0f), new Vector3(12f, 0.2f, 12f), Gold);
        SetEmissive(bRim1, Gold, Gold * 0.3f);

        // 2층 탑신
        Box(root.transform, "Tower2", new Vector3(0f, 22.5f, 0f), new Vector3(5.5f, 16f, 5.5f), TowerSlate);

        // 2층 발코니
        Cyl(root.transform, "Balcony2", new Vector3(0f, 31.2f, 0f), new Vector3(8f, 0.5f, 8f), StoneCold);
        var bRim2 = Cyl(root.transform, "Balcony2Rim", new Vector3(0f, 31.5f, 0f), new Vector3(8f, 0.2f, 8f), Gold);
        SetEmissive(bRim2, Gold, Gold * 0.35f);

        // 3층 첨탑 (테이퍼형)
        Box(root.transform, "Spire1", new Vector3(0f, 37.5f, 0f), new Vector3(4.0f, 8f, 4.0f), TowerSlate);
        Box(root.transform, "Spire2", new Vector3(0f, 44.0f, 0f), new Vector3(2.8f, 6f, 2.8f), TowerSlate);
        Box(root.transform, "Spire3", new Vector3(0f, 49.5f, 0f), new Vector3(1.8f, 5f, 1.8f), TowerSlate);

        // 첨탑 최상단 황금 구슬
        var tip = Sphere(root.transform, "SpireTip", new Vector3(0f, 53.5f, 0f), Vector3.one * 2.8f, TowerGold);
        SetEmissive(tip, TowerGold, GoldBright * 0.8f); RemoveCollider(tip);

        // 첨탑 발광 링 (3단)
        for (int i = 0; i < 3; i++)
        {
            var ring = Cyl(root.transform, $"GlowRing_{i}",
                new Vector3(0f, 16f + i * 10f, 0f),
                new Vector3(3.5f - i * 0.3f, 0.3f, 3.5f - i * 0.3f),
                TowerGold);
            SetEmissive(ring, TowerGold, GoldBright * (0.5f - i * 0.08f));
            RemoveCollider(ring);
        }

        // 탑 기단 모서리 장식 기둥 8개
        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad;
            var p = new Vector3(Mathf.Sin(a) * 10f, 3.5f, Mathf.Cos(a) * 10f);
            Cyl(root.transform, $"DecoCol_{i}", p, new Vector3(0.7f, 5f, 0.7f), StoneDark);
            var dcap = Sphere(root.transform, $"DecoColCap_{i}", p + Vector3.up * 3.0f, Vector3.one * 1.1f, Gold);
            SetEmissive(dcap, Gold, GoldBright * 0.2f); RemoveCollider(dcap);
        }

        var tmm = root.AddComponent<MapMarker>();
        tmm.kind        = MapMarker.IconKind.Building;
        tmm.displayName = "조약의 탑";
        tmm.iconColor   = TowerGold;
        tmm.footprintW  = 22f;
        tmm.footprintD  = 22f;
    }

    // ── 7. 주요 건물 3기 ─────────────────────────────────────────────────────
    static void BuildMainBuildings()
    {
        // 통합 학술원 (서, HubLab)
        ByzantineBuilding("HubLab", "통합 학술원", "[E]  통합 학술원",
            new Vector3(-BUILDING_R, 0f, 10f), 90f,
            w: 26f, h: 15f, d: 20f, wall: StoneWarm, dome: Gold);

        // 지식의 거래소 (동, HubLibrary)
        ByzantineBuilding("HubLibrary", "지식의 거래소", "[E]  지식의 거래소",
            new Vector3(BUILDING_R, 0f, 10f), -90f,
            w: 26f, h: 15f, d: 20f, wall: StoneWarm, dome: GoldBright);
    }

    static void ByzantineBuilding(string goName, string display, string prompt,
        Vector3 pos, float rotY, float w, float h, float d, Color wall, Color dome)
    {
        var root = new GameObject(goName);
        root.transform.position = pos;
        root.transform.rotation = Quaternion.Euler(0f, rotY, 0f);

        var ia = root.AddComponent<Interactable>();
        ia.displayName = display;
        ia.promptText  = prompt;
        ia.radius      = 10f;

        Box(root.transform, "Foundation", new Vector3(0f, -0.4f, 0f), new Vector3(w + 5f, 0.8f, d + 5f), StoneDark);

        float fz = d * 0.5f;
        Box(root.transform, "Step1", new Vector3(0f, 0.20f, -(fz + 2.2f)), new Vector3(w * 0.60f, 0.40f, 2.2f), StoneWarm);
        Box(root.transform, "Step2", new Vector3(0f, 0.55f, -(fz + 0.9f)), new Vector3(w * 0.55f, 0.40f, 1.8f), StoneCold);
        Box(root.transform, "Step3", new Vector3(0f, 0.90f, -(fz + 0.1f)), new Vector3(w * 0.50f, 0.40f, 1.0f), StoneWarm);

        Box(root.transform, "Body", new Vector3(0f, h * 0.5f + 0.2f, 0f), new Vector3(w, h, d), PlasterCream);

        // 목조 빔 격자 — 가로 빔 3줄
        float[] beamYs = { h * 0.25f + 0.2f, h * 0.52f + 0.2f, h * 0.78f + 0.2f };
        foreach (float by in beamYs)
            Box(root.transform, "BeamH", new Vector3(0f, by, -(d*0.5f+0.05f)), new Vector3(w+0.2f, 0.5f, 0.3f), TimberBrown);

        // 세로 빔 2줄
        float[] beamXs = { -w*0.28f, w*0.28f };
        foreach (float bx in beamXs)
            Box(root.transform, "BeamV", new Vector3(bx, h*0.5f+0.2f, -(d*0.5f+0.05f)), new Vector3(0.4f, h+0.4f, 0.3f), TimberBrown);

        // 박공 지붕 (Gabled Roof)
        float roofH = h * 0.45f;
        float roofY = h + 0.2f + roofH * 0.5f;
        Box(root.transform, "RoofBase",    new Vector3(0f, h+0.2f, 0f),            new Vector3(w+2f, 0.5f, d+2f), TileRed);
        Box(root.transform, "RoofSlope_F", new Vector3(0f, roofY, -(d*0.3f)),      new Vector3(w+1f, roofH, 1.0f), TileRed);
        Box(root.transform, "RoofSlope_B", new Vector3(0f, roofY,  (d*0.3f)),      new Vector3(w+1f, roofH, 1.0f), TileRed);
        Box(root.transform, "RoofPeak",    new Vector3(0f, h+0.2f+roofH, 0f),      new Vector3(w+0.5f, 0.6f, 1.2f), TimberBrown);

        float px = w * 0.28f;
        Pillar(root.transform, "EntryPillar_LL", new Vector3(-px * 1.6f, 0f, -(d * 0.5f - 0.5f)), StoneDark);
        Pillar(root.transform, "EntryPillar_L",  new Vector3(-px * 0.6f, 0f, -(d * 0.5f - 0.5f)), StoneDark);
        Pillar(root.transform, "EntryPillar_R",  new Vector3( px * 0.6f, 0f, -(d * 0.5f - 0.5f)), StoneDark);
        Pillar(root.transform, "EntryPillar_RR", new Vector3( px * 1.6f, 0f, -(d * 0.5f - 0.5f)), StoneDark);

        var banner = Box(root.transform, "Banner",
            new Vector3(0f, h * 0.88f, -(d * 0.5f + 0.05f)),
            new Vector3(w * 0.45f, h * 0.17f, 0.15f), Gold);
        SetEmissive(banner, Gold, Gold * 0.2f);

        Box(root.transform, "DoorCut", new Vector3(0f, h * 0.33f, -(d * 0.5f + 0.05f)),
            new Vector3(w * 0.22f, h * 0.65f, 0.4f), StoneDark);

        var mm = root.AddComponent<MapMarker>();
        mm.kind        = MapMarker.IconKind.Building;
        mm.displayName = display;
        mm.iconColor   = dome;
        mm.footprintW  = w;
        mm.footprintD  = d;
    }

    // ── 8. 길드 가로 (HubGuildRow, z≈-60~-110) ──────────────────────────────
    static void BuildGuildRow()
    {
        var root = new GameObject("HubGuildRow");

        // 모험가 길드 본부 (HubGuildHall) — 서쪽 길드 구역 앵커 (중앙 도로 비움)
        ByzantineBuilding("HubGuildHall", "모험가 길드 본부", "[E]  모험가 길드",
            new Vector3(-65f, 0f, -80f), 90f,
            w: 28f, h: 14f, d: 20f, wall: StoneCold, dome: Gold);

        // 길드 파사드 (양측 각 3채)
        var guilds = new (string n, string display, Color wall, Color dome, float sx, float sz)[]
        {
            ("MarketGuild",   "상인 길드",   new Color(0.72f, 0.60f, 0.30f), new Color(0.85f, 0.60f, 0.10f), -50f, -70f),
            ("CraftGuild",    "제작 길드",   new Color(0.55f, 0.50f, 0.45f), StoneCold,                        -50f, -95f),
            ("SageGuild",     "현자 길드",   new Color(0.35f, 0.45f, 0.65f), new Color(0.50f, 0.70f, 1.00f),  -50f,-118f),
            ("AlchemyGuild",  "연금술 길드", new Color(0.60f, 0.35f, 0.45f), new Color(0.85f, 0.25f, 0.35f),   50f, -70f),
            ("ExplorerGuild", "탐험가 길드", new Color(0.45f, 0.55f, 0.38f), new Color(0.40f, 0.80f, 0.30f),   50f, -95f),
            ("DataGuild",     "데이터 길드", new Color(0.40f, 0.40f, 0.55f), new Color(0.65f, 0.30f, 1.00f),   50f,-118f),
        };

        foreach (var (n, display, wall, dome, sx, sz) in guilds)
        {
            float ry = sx < 0f ? 90f : -90f;
            GuildFacade(root.transform, n, display, new Vector3(sx, 0f, sz), ry, wall, dome);
        }

        // 길드 가로 노점 (척추 양측)
        for (int i = 0; i < 4; i++)
        {
            float z = -68f - i * 9f;
            GuildStall(root.transform, new Vector3(-18f, 0f, z), $"StallW_{i}");
            GuildStall(root.transform, new Vector3( 18f, 0f, z), $"StallE_{i}");
        }
    }

    static void GuildFacade(Transform parent, string n, string display, Vector3 pos, float ry, Color wall, Color dome)
    {
        var root = new GameObject(n);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = pos;
        root.transform.localRotation = Quaternion.Euler(0f, ry, 0f);

        Color bodyColor = new Color((wall.r+PlasterCream.r)*0.5f, (wall.g+PlasterCream.g)*0.5f, (wall.b+PlasterCream.b)*0.5f);
        Box(root.transform, "Body", new Vector3(0f, 7f, 0f), new Vector3(18f, 14f, 12f), bodyColor);
        Box(root.transform, "Foundation", new Vector3(0f, -0.3f, 0f), new Vector3(20f, 0.6f, 14f), StoneDark);

        // 박공 지붕
        float gRoofH = 7f;
        Box(root.transform, "RoofBase",   new Vector3(0f, 14f, 0f),                       new Vector3(19f, 0.5f, 13f), TileRed);
        Box(root.transform, "RoofPeak",   new Vector3(0f, 14f+gRoofH, 0f),                new Vector3(18f, 0.6f, 0.8f), TimberBrown);
        Box(root.transform, "RoofSlopeF", new Vector3(0f, 14f+gRoofH*0.5f, -(6f*0.4f)),  new Vector3(18.5f, gRoofH, 0.8f), TileRed);
        Box(root.transform, "RoofSlopeB", new Vector3(0f, 14f+gRoofH*0.5f,  (6f*0.4f)),  new Vector3(18.5f, gRoofH, 0.8f), TileRed);

        // 가로 빔 2줄
        Box(root.transform, "BeamH1", new Vector3(0f, 4f, -6.12f), new Vector3(18.5f, 0.4f, 0.2f), TimberBrown);
        Box(root.transform, "BeamH2", new Vector3(0f, 9f, -6.05f), new Vector3(18.5f, 0.4f, 0.2f), TimberBrown);

        Pillar(root.transform, "PL", new Vector3(-7f, 0f, -6f), StoneCold);
        Pillar(root.transform, "PR", new Vector3( 7f, 0f, -6f), StoneCold);

        Box(root.transform, "Door", new Vector3(0f, 4f, -6.05f), new Vector3(4f, 8f, 0.3f), StoneDark);
        var banner = Box(root.transform, "Banner", new Vector3(0f, 12.5f, -6.05f), new Vector3(8f, 2f, 0.2f), dome);
        SetEmissive(banner, dome, dome * 0.18f);

        var mm = root.AddComponent<MapMarker>();
        mm.kind        = MapMarker.IconKind.Building;
        mm.displayName = display;
        mm.iconColor   = dome;
        mm.footprintW  = 18f;
        mm.footprintD  = 12f;
    }

    static void GuildStall(Transform parent, Vector3 pos, string n)
    {
        var s = new GameObject(n);
        s.transform.SetParent(parent, false);
        s.transform.localPosition = pos;
        Box(s.transform, "Awning", new Vector3(0f, 3.4f, 0f),    new Vector3(5.5f, 0.2f, 3.5f), StoneWarm);
        Box(s.transform, "PostL",  new Vector3(-2.4f, 1.7f, 0f), new Vector3(0.25f, 3.4f, 0.25f), StoneDark);
        Box(s.transform, "PostR",  new Vector3( 2.4f, 1.7f, 0f), new Vector3(0.25f, 3.4f, 0.25f), StoneDark);
        Prop(s.transform, P_TABLE, new Vector3(0f, 0.5f, 0f), Vector3.one * 1.2f, "Table");
    }

    // ── 9. 4분면 시가지 블록 (HubCityBlocks) ──────────────────────────────────
    static void BuildCityBlocks()
    {
        var root = new GameObject("HubCityBlocks");

        var blocks = new (float xs, float zMin, float zMax, string area, Color wall, Color roof)[]
        {
            (-1f, 35f, 100f, "Research",  PlasterCream,   TileRed), // zMin 35: 포탈 링(z≈20) 남쪽 여백 확보
            ( 1f, 35f, 100f, "Academic",  StoneWarm,      TileRed),
            (-1f,-115f,-25f, "Commerce",  PlasterCream,   TileRed), // zMax -25: 스폰(z=-100) 북쪽 여백
            ( 1f,-115f,-25f, "Resid",     StoneWarm,      TileRed),
        };

        Random.InitState(42); // 고정 시드 → 항상 같은 배치

        int bIdx = 0;
        foreach (var (xs, zMin, zMax, area, wall, roof) in blocks)
        {
            var quadRoot = new GameObject($"Quad_{area}");
            quadRoot.transform.SetParent(root.transform, false);

            float[] zOffsets = { zMin+12f, zMin+30f, zMin+48f, zMin+66f };
            float[] xOffsets = { 35f, 58f, 82f };

            foreach (float zBase in zOffsets)
            {
                if (zBase > zMax - 10f) continue;
                foreach (float xBase in xOffsets)
                {
                    // 30% 슬롯 스킵 — 골목·광장 공간
                    if (Random.value < 0.30f) { bIdx++; continue; }

                    float jx     = Random.Range(-8f, 8f);
                    float jz     = Random.Range(-8f, 8f);
                    float rot    = Random.Range(-10f, 10f);
                    float buildH = Random.Range(8f, 22f);
                    float buildW = Random.Range(12f, 24f);
                    float buildD = Random.Range(10f, 18f);
                    float roofH  = buildH * 0.3f;
                    float roofY  = buildH + 0.2f + roofH * 0.5f;

                    // 튜더 반목조 건물 — Synty 프리팹 대신 항상 커스텀 빌드
                    var fb = new GameObject($"CityBld_{area}_{bIdx}");
                    fb.transform.SetParent(quadRoot.transform, false);
                    fb.transform.localPosition = new Vector3(xs * (xBase + jx), 0f, zBase + jz);
                    fb.transform.localRotation = Quaternion.Euler(0f, rot + (xs < 0f ? 90f : -90f), 0f);

                    Box(fb.transform, "Body",        new Vector3(0f, buildH*0.5f, 0f),                       new Vector3(buildW,       buildH,  buildD),       wall);
                    Box(fb.transform, "Found",       new Vector3(0f, -0.3f, 0f),                             new Vector3(buildW+0.8f,  0.6f,   buildD+0.8f),   StoneDark);
                    // 가로 빔 2줄 (1/3, 2/3 높이)
                    Box(fb.transform, "BeamH1",      new Vector3(0f, buildH*0.33f, -(buildD*0.5f+0.05f)),    new Vector3(buildW+0.2f,  0.35f,  0.25f),         TimberBrown);
                    Box(fb.transform, "BeamH2",      new Vector3(0f, buildH*0.67f, -(buildD*0.5f+0.05f)),    new Vector3(buildW+0.2f,  0.35f,  0.25f),         TimberBrown);
                    // 세로 빔 2줄 (좌우 30%)
                    Box(fb.transform, "BeamV1",      new Vector3(-buildW*0.3f, buildH*0.5f, -(buildD*0.5f+0.05f)), new Vector3(0.3f, buildH+0.2f, 0.25f),     TimberBrown);
                    Box(fb.transform, "BeamV2",      new Vector3( buildW*0.3f, buildH*0.5f, -(buildD*0.5f+0.05f)), new Vector3(0.3f, buildH+0.2f, 0.25f),     TimberBrown);
                    // 박공 지붕
                    Box(fb.transform, "RoofBase",    new Vector3(0f, buildH+0.2f, 0f),                       new Vector3(buildW+1.5f,  0.4f,   buildD+1.5f),   roof);
                    Box(fb.transform, "RoofSlope_F", new Vector3(0f, roofY, -(buildD*0.3f)),                 new Vector3(buildW+0.8f,  roofH,  0.8f),          roof);
                    Box(fb.transform, "RoofSlope_B", new Vector3(0f, roofY,  (buildD*0.3f)),                 new Vector3(buildW+0.8f,  roofH,  0.8f),          roof);
                    Box(fb.transform, "RoofPeak",    new Vector3(0f, buildH+0.2f+roofH, 0f),                 new Vector3(buildW+0.3f,  0.5f,   0.8f),          TimberBrown);
                    fb.isStatic = true;
                    bIdx++;
                }
            }
        }
    }

    // ── 10. 그랜드 하버 (다층, 대륙별 색 부두) ───────────────────────────────
    static void BuildHarbor()
    {
        var root = new GameObject("HubHarbor");
        root.transform.position = new Vector3(0f, 0f, HARBOR_Z);

        // ─ 하버 기단 (전체 부두 플랫폼) ─
        Box(root.transform, "DockBase",   new Vector3(0f, -0.2f, 0f),  new Vector3(180f, 0.4f, 35f), StoneDark);
        Box(root.transform, "DockFloor1", new Vector3(0f,  0.16f, 0f), new Vector3(180f, 0.3f, 35f), DockWood);

        // ─ 2층 상단 플랫폼 (중앙부) ─
        Box(root.transform, "DockFloor2", new Vector3(0f, 2.2f, -5f), new Vector3(90f, 0.3f, 18f), DockWood);

        // ─ 층간 계단 ─
        Box(root.transform, "StairL", new Vector3(-47f, 1.2f, -5f), new Vector3(6f, 2.2f, 5f), DockWood);
        Box(root.transform, "StairR", new Vector3( 47f, 1.2f, -5f), new Vector3(6f, 2.2f, 5f), DockWood);

        // ─ 대륙별 부두 (6개, 가로 배치) ─
        for (int i = 0; i < 6; i++)
        {
            float x = -75f + i * 30f;
            Color dc = DockColors[i];
            string dn = DockNames[i];

            // 부두 색 포장 (바닥 색 악센트)
            var dpad = Box(root.transform, $"DockPad_{i}",
                new Vector3(x, 0.18f, 12f), new Vector3(22f, 0.05f, 8f), dc);
            SetEmissive(dpad, dc, dc * 0.25f);

            // 부두 표지 기둥 (양측)
            foreach (float sign in new[] { -1f, 1f })
            {
                Cyl(root.transform, $"DockPost_{i}_{(sign>0?1:0)}",
                    new Vector3(x + sign * 10f, 4f, 14f),
                    new Vector3(0.8f, 8f, 0.8f), DockWood);
                var dhead = Box(root.transform, $"DockHead_{i}_{(sign>0?1:0)}",
                    new Vector3(x + sign * 10f, 8.5f, 14f),
                    new Vector3(1.5f, 0.6f, 1.5f), dc);
                SetEmissive(dhead, dc, dc * 0.5f);
            }

            // 창고 (대형)
            Box(root.transform, $"Warehouse_{i}",
                new Vector3(x, 7.5f, -6f), new Vector3(20f, 15f, 12f), StoneDark);
            var wRoof = Box(root.transform, $"WHRoof_{i}",
                new Vector3(x, 15.5f, -6f), new Vector3(21f, 1.8f, 13f), dc);
            SetEmissive(wRoof, dc, dc * 0.18f);

            // 부두 소품
            Prop(root.transform, P_BARREL_W,   new Vector3(x - 7f, 0.5f,  7f), Vector3.one * 1.7f, $"BWA_{i}");
            Prop(root.transform, P_CRATE,       new Vector3(x + 6f, 0.5f, 10f), Vector3.one * 1.8f, $"Cr_{i}");
            Prop(root.transform, P_SACK_STACK,  new Vector3(x - 4f, 0.5f,  3f), Vector3.one * 2.0f, $"Sk_{i}");
        }

        // ─ 부두 말뚝 ─
        for (int i = -5; i <= 5; i++)
            Cyl(root.transform, $"Pile_{i+5}", new Vector3(i * 16f, -2.0f, 14f), new Vector3(1.1f, 8f, 1.1f), DockWood);

        // ─ 중앙 하버 등대 ─
        Cyl(root.transform, "LighthouseBase", new Vector3(0f, 9f, -10f),    new Vector3(5f, 18f, 5f),    StoneCold);
        Cyl(root.transform, "LighthouseMid",  new Vector3(0f, 21f, -10f),   new Vector3(3.5f, 6f, 3.5f), StoneWarm);
        var lhTop = Cyl(root.transform, "LighthouseTop",  new Vector3(0f, 26f, -10f),   new Vector3(4f, 2.5f, 4f),   Gold);
        SetEmissive(lhTop, Gold, GoldBright * 0.9f); RemoveCollider(lhTop);

        // ─ 바다 ─
        Sea(null, "HubSea_N", new Vector3(0f, 0.1f, HARBOR_Z + 60f), new Vector3(300f, 1f, 80f));
        Sea(null, "HubSea_E", new Vector3(230f, 0.1f, 90f),           new Vector3(80f, 1f, 200f));
        Sea(null, "HubSea_W", new Vector3(-230f, 0.1f, 90f),          new Vector3(80f, 1f, 200f));

        var hm = root.AddComponent<MapMarker>();
        hm.kind        = MapMarker.IconKind.Harbor;
        hm.displayName = "그랜드 하버";
        hm.iconColor   = new Color(0.32f, 0.68f, 0.92f);
        hm.footprintW  = 180f;
        hm.footprintD  = 35f;
    }

    static void Sea(Transform parent, string name, Vector3 worldPos, Vector3 scale)
    {
        var go = LoadPrefab(P_WATER);
        if (go != null)
        {
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position   = worldPos;
            // Synty Plane 기본 크기 = 10×10 → Unity Plane과 동일하게 0.1f 보정
            go.transform.localScale = new Vector3(scale.x * 0.1f, 1f, scale.z * 0.1f);
            RemoveCollider(go);
        }
        else
        {
            var fb = GameObject.CreatePrimitive(PrimitiveType.Plane);
            fb.name = name;
            if (parent != null) fb.transform.SetParent(parent, false);
            fb.transform.position   = worldPos;
            fb.transform.localScale = new Vector3(scale.x * 0.1f, 1f, scale.z * 0.1f);
            ApplyColor(fb, SeaBlue);
            Object.DestroyImmediate(fb.GetComponent<MeshCollider>());
        }
    }

    // ── 11. 배경 도시 (더 가깝고 높게) ──────────────────────────────────────
    static void BuildBackgroundCity()
    {
        var root = new GameObject("HubBackgroundCity");

        string[] bgs = { P_BG_A, P_BG_B, P_BG_C, P_BG_D, P_BG_E, P_BG_F };
        var spots = new (Vector3 pos, float ry, float sc, int bi)[]
        {
            // 서쪽 스카이라인 (가깝게: x≈130~155)
            (new Vector3(-130f, 0f,   0f),  90f, 6.5f, 0),
            (new Vector3(-140f, 0f,  40f),  75f, 5.5f, 1),
            (new Vector3(-140f, 0f, -40f), 105f, 5.5f, 2),
            (new Vector3(-120f, 0f,  75f),  60f, 5.0f, 3),
            (new Vector3(-120f, 0f, -75f), 115f, 5.0f, 4),
            // 동쪽 스카이라인
            (new Vector3( 130f, 0f,   0f), -90f, 6.5f, 0),
            (new Vector3( 140f, 0f,  40f), -75f, 5.5f, 5),
            (new Vector3( 140f, 0f, -40f),-105f, 5.5f, 1),
            (new Vector3( 120f, 0f,  75f), -60f, 5.0f, 2),
            (new Vector3( 120f, 0f, -75f),-115f, 5.0f, 3),
            // 남쪽 스카이라인
            (new Vector3(-80f, 0f, -155f),  140f, 4.5f, 4),
            (new Vector3(  0f, 0f, -165f),  180f, 5.0f, 5),
            (new Vector3( 80f, 0f, -155f),  220f, 4.5f, 0),
            // 북동/북서 배경
            (new Vector3(-90f, 0f, 120f),   50f, 4.5f, 1),
            (new Vector3( 90f, 0f, 120f),  -50f, 4.5f, 2),
        };

        foreach (var (pos, ry, sc, bi) in spots)
        {
            var go = LoadPrefab(bgs[bi % bgs.Length]);
            if (go != null)
            {
                go.transform.SetParent(root.transform);
                go.transform.position   = pos;
                go.transform.rotation   = Quaternion.Euler(0f, ry, 0f);
                go.transform.localScale = Vector3.one * sc;
                go.isStatic = true;
            }
            else
            {
                var fb = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fb.transform.SetParent(root.transform);
                fb.transform.position   = pos + new Vector3(0f, 18f, 0f);
                fb.transform.localScale = new Vector3(28f, 36f + bi * 5f, 10f);
                fb.transform.rotation   = Quaternion.Euler(0f, ry, 0f);
                ApplyColor(fb, StoneDark);
                fb.isStatic = true;
            }
        }
    }

    // ── 12. 씬 드레싱 ────────────────────────────────────────────────────────
    static void BuildScenery()
    {
        var root = new GameObject("HubScenery");

        // 척추 양측 가로수 (남쪽 구간)
        for (int i = 0; i < 6; i++)
        {
            float z = GATE_Z + 8f + i * 18f;
            if (z > -62f) break;
            Prop(root.transform, P_TREE_A, new Vector3(-20f, 0f, z), Vector3.one * 2.2f, $"SpineTreeWS_{i}");
            Prop(root.transform, P_TREE_A, new Vector3( 20f, 0f, z), Vector3.one * 2.2f, $"SpineTreeES_{i}");
        }

        // 광장 주변 가로수 (광장 경계선 바깥)
        for (int i = 0; i < 4; i++)
        {
            float z = -PLAZA_R - 3f + i * (PLAZA_R * 2f / 3f);
            Prop(root.transform, P_TREE_B, new Vector3(-PLAZA_R - 5f, 0f, z), Vector3.one * 2.4f, $"PlazaTreeW_{i}");
            Prop(root.transform, P_TREE_B, new Vector3( PLAZA_R + 5f, 0f, z), Vector3.one * 2.4f, $"PlazaTreeE_{i}");
        }

        // 분수 주변 덤불 링
        for (int i = 0; i < 8; i++)
        {
            float rad = i * 45f * Mathf.Deg2Rad;
            var p = new Vector3(Mathf.Sin(rad) * 19f, 0f, Mathf.Cos(rad) * 19f);
            Prop(root.transform, P_BUSH_A, p, Vector3.one * 1.25f, $"FBush_{i}");
        }

        // 조약의 탑 주변 기념 나무 (4그루)
        for (int i = 0; i < 4; i++)
        {
            float a = i * 90f * Mathf.Deg2Rad;
            var p = new Vector3(Mathf.Sin(a) * 28f, 0f, TOWER_Z + Mathf.Cos(a) * 28f);
            Prop(root.transform, P_TREE_C, p, Vector3.one * 2.8f, $"TowerTree_{i}");
        }

        // 항구 접근로 가로수
        for (int i = 0; i < 5; i++)
        {
            float z = 90f + i * 12f;
            Prop(root.transform, P_TREE_B, new Vector3(-24f, 0f, z), Vector3.one * 2.3f, $"HarborTL_{i}");
            Prop(root.transform, P_TREE_B, new Vector3( 24f, 0f, z), Vector3.one * 2.3f, $"HarborTR_{i}");
        }

        // 동/서 바위 클러스터
        foreach (var (rx, rz) in new[] { (-115f,-60f),(-100f,-90f),(115f,-60f),(102f,-90f) })
        {
            Prop(root.transform, P_ROCK_A, new Vector3(rx, 0f, rz),          Vector3.one * 3.5f, "Rock");
            Prop(root.transform, P_ROCK_B, new Vector3(rx + 5f, 0f, rz + 2f), Vector3.one * 2.2f, "RockB");
            Prop(root.transform, P_BUSH_A, new Vector3(rx - 3f, 0f, rz - 3f), Vector3.one * 1.4f, "RockBush");
        }
    }

    // ── 13. 경계 벽 (불가시 충돌체) ─────────────────────────────────────────
    static void BuildWalls()
    {
        float hw = GROUND_HALF, wh = 20f, wt = 2f;
        Wall("Wall_N", new Vector3(  0f, wh * 0.5f,  hw), new Vector3(hw * 2f, wh, wt));
        Wall("Wall_S", new Vector3(  0f, wh * 0.5f, -hw), new Vector3(hw * 2f, wh, wt));
        Wall("Wall_E", new Vector3( hw,  wh * 0.5f,  0f), new Vector3(wt, wh, hw * 2f));
        Wall("Wall_W", new Vector3(-hw,  wh * 0.5f,  0f), new Vector3(wt, wh, hw * 2f));
    }

    static void Wall(string name, Vector3 pos, Vector3 size)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name                 = name;
        go.transform.position   = pos;
        go.transform.localScale = size;
        go.isStatic             = true;
        var r = go.GetComponent<Renderer>(); if (r) r.enabled = false;
    }

    // ── 14. 남쪽 성문 (z=-130) ──────────────────────────────────────────────
    static void BuildSouthGate()
    {
        var root = new GameObject("HubSouthGate");
        root.transform.position = new Vector3(0f, 0f, GATE_Z);

        // 성문 기둥 (좌/우)
        Box(root.transform, "GateL",    new Vector3(-13f,  7f, 0f), new Vector3(3f, 14f, 3f), StoneDark);
        Box(root.transform, "GateR",    new Vector3( 13f,  7f, 0f), new Vector3(3f, 14f, 3f), StoneDark);

        // 아치 빔
        Box(root.transform, "GateArch", new Vector3(0f, 13.5f, 0f), new Vector3(30f, 2.5f, 3f), StoneDark);

        // 황금 장식 배너
        var gd = Box(root.transform, "GateDeco", new Vector3(0f, 16.0f, 0f), new Vector3(18f, 2.0f, 1.0f), Gold);
        SetEmissive(gd, Gold, GoldBright * 0.35f);

        // 성문 옆 망루 (좌/우)
        Box(root.transform, "TowerL", new Vector3(-18f, 8f, 0f), new Vector3(5f, 16f, 5f), StoneCold);
        Box(root.transform, "TowerR", new Vector3( 18f, 8f, 0f), new Vector3(5f, 16f, 5f), StoneCold);
        var tcL = Sphere(root.transform, "TowerCapL", new Vector3(-18f, 17.5f, 0f), Vector3.one * 5.5f, Gold);
        var tcR = Sphere(root.transform, "TowerCapR", new Vector3( 18f, 17.5f, 0f), Vector3.one * 5.5f, Gold);
        SetEmissive(tcL, Gold, Gold * 0.28f); RemoveCollider(tcL);
        SetEmissive(tcR, Gold, Gold * 0.28f); RemoveCollider(tcR);

        // 성벽 (동/서 방향)
        Box(root.transform, "WallW", new Vector3(-75f, 7f, 0f), new Vector3(110f, 14f, 2.5f), StoneDark);
        Box(root.transform, "WallE", new Vector3( 75f, 7f, 0f), new Vector3(110f, 14f, 2.5f), StoneDark);

        // 성벽 흉벽 (크레넬레이션)
        for (int i = -5; i <= 5; i++)
        {
            Box(root.transform, $"MerlW_{i+5}", new Vector3(-75f + i * 11f, 14.5f, 0f), new Vector3(4f, 2f, 3f), StoneCold);
            Box(root.transform, $"MerlE_{i+5}", new Vector3( 75f + i * 11f, 14.5f, 0f), new Vector3(4f, 2f, 3f), StoneCold);
        }

        var gm = root.AddComponent<MapMarker>();
        gm.kind        = MapMarker.IconKind.Gate;
        gm.displayName = "남쪽 성문";
        gm.iconColor   = new Color(0.80f, 0.70f, 0.50f);
        gm.footprintW  = 160f;
        gm.footprintD  = 8f;
    }

    // ── 15. 조명 ─────────────────────────────────────────────────────────────
    static void AdjustLighting()
    {
        var sun = Object.FindFirstObjectByType<Light>();
        if (sun == null) return;
        sun.color     = new Color(1.00f, 0.93f, 0.78f);
        sun.intensity = 1.22f;
        sun.transform.rotation = Quaternion.Euler(-48f, 28f, 0f);
    }

    // ── 프리팹 헬퍼 ──────────────────────────────────────────────────────────
    static GameObject LoadPrefab(string path)
    {
        var p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        return p == null ? null : (GameObject)PrefabUtility.InstantiatePrefab(p);
    }

    static void Prop(Transform parent, string path, Vector3 localPos, Vector3 scale, string n)
    {
        var go = LoadPrefab(path);
        if (go != null)
        {
            go.name = n;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale    = scale;
        }
        else
        {
            var fb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fb.name = n;
            fb.transform.SetParent(parent, false);
            fb.transform.localPosition = localPos;
            fb.transform.localScale    = scale * 0.45f;
            ApplyColor(fb, StoneWarm);
        }
    }

    static void Pillar(Transform parent, string n, Vector3 localPos, Color fallback)
    {
        var go = LoadPrefab(P_PILLAR_A);
        if (go != null)
        {
            go.name = n;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale    = Vector3.one * 1.3f;
        }
        else
        {
            Cyl(parent, n, localPos, new Vector3(0.9f, 9f, 0.9f), fallback);
        }
    }

    // ── 절차적 프리미티브 ─────────────────────────────────────────────────────
    static GameObject Cyl(Transform p, string n, Vector3 lp, Vector3 ls, Color c)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = n; go.transform.SetParent(p, false);
        go.transform.localPosition = lp; go.transform.localScale = ls;
        ApplyColor(go, c); return go;
    }

    static GameObject Sphere(Transform p, string n, Vector3 lp, Vector3 ls, Color c)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = n; go.transform.SetParent(p, false);
        go.transform.localPosition = lp; go.transform.localScale = ls;
        ApplyColor(go, c); return go;
    }

    static GameObject Cube(Transform p, string n, Vector3 lp, Vector3 ls)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = n; go.transform.SetParent(p, false);
        go.transform.localPosition = lp; go.transform.localScale = ls;
        return go;
    }

    static GameObject Box(Transform p, string n, Vector3 lp, Vector3 ls, Color c)
    {
        var go = Cube(p, n, lp, ls);
        ApplyColor(go, c); return go;
    }

    static void RemoveCollider(GameObject go)
    {
        var c = go.GetComponent<Collider>();
        if (c) Object.DestroyImmediate(c);
    }

    static void ApplyColor(GameObject go, Color c)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null) return;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = c };
        r.sharedMaterial = mat;
    }

    static void SetEmissive(GameObject go, Color col, Color emit)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null) return;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = col;
        mat.SetColor("_EmissionColor", emit);
        mat.EnableKeyword("_EMISSION");
        r.sharedMaterial = mat;
    }

    // Interactable.onInteract → ScenePortal.Go 를 퍼시스턴트 리스너로 등록한다.
    static void WirePortal(Interactable ia, ScenePortal portal, string sceneName, string spawnName = "")
    {
        // public 필드 직접 할당 — SerializedObject 보다 확실하게 씬에 저장됨
        portal.targetScene     = sceneName;
        portal.targetSpawnName = spawnName;

        // onInteract → Go 와이어링
        var iso   = new UnityEditor.SerializedObject(ia);
        var calls = iso.FindProperty("onInteract")
                       .FindPropertyRelative("m_PersistentCalls")
                       .FindPropertyRelative("m_Calls");
        calls.ClearArray();
        calls.InsertArrayElementAtIndex(0);
        var call = calls.GetArrayElementAtIndex(0);
        call.FindPropertyRelative("m_Target").objectReferenceValue = portal;
        call.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue =
            $"{typeof(ScenePortal).FullName}, {typeof(ScenePortal).Assembly.GetName().Name}";
        call.FindPropertyRelative("m_MethodName").stringValue      = "Go";
        call.FindPropertyRelative("m_Mode").enumValueIndex         = 1; // Void
        call.FindPropertyRelative("m_CallState").enumValueIndex    = 2; // RuntimeOnly
        iso.ApplyModifiedProperties();
    }
}
