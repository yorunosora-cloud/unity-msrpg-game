using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// 메조리아 허브 맵 v2 — 실크로드 척추 + 광장 골격
/// 레이아웃 (남→북):
///   스폰(0,0,-100) → 남쪽 성문(z≈-130) → 길드 가로(z≈-70~-110)
///   → 실크로드 대광장(z=0) → 중립 조약의 탑(z≈+65) → 그랜드 하버(z≈+150)
///   서쪽(x≈-87, z≈+10): 통합 학술원(HubLab)
///   서쪽(x≈-87, z≈+55): 도서관(HubLibrary) ← 신규
///   동쪽(x≈+87, z≈+10): 지식의 거래소(HubExchange)
///   남서(x≈-65, z≈-80): 모험가 길드 본부(HubGuildHall) ← 재디자인
///
/// 루트 GO 이름 보존 (MetaUISetup.WireBuilding 이름 기반):
///   HubLab, HubLibrary, HubGuildHall, HubPortal_*
/// 신규: HubExchange, HubTreatyTower, HubGuildRow, HubCityBlocks
///
/// 내부 입장 훅 (향후 ScenePortal 연결 예정):
///   LibraryEntrance  → 씬 "LibraryInterior" / 복귀 스폰 "LibraryReturnSpawn"
///   GuildEntrance    → 씬 "GuildInterior"   / 복귀 스폰 "GuildReturnSpawn"
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

    // ── 구역별 포장재 팔레트 (BuildDistrictPaving) ─────────────────────────
    static readonly Color PaveBase   = new Color(0.55f, 0.51f, 0.42f); // 중립 자갈 베이스
    static readonly Color SlateCool  = new Color(0.50f, 0.55f, 0.60f); // 지식 구역 — 회청 석판
    static readonly Color BrickOchre = new Color(0.62f, 0.42f, 0.26f); // 상업 구역 — 황토 벽돌
    static readonly Color DeckWarm   = new Color(0.46f, 0.35f, 0.22f); // 생활 구역 — 목재/자갈
    static readonly Color GraniteWet = new Color(0.34f, 0.37f, 0.42f); // 항구 구역 — 젖은 화강암
    static readonly Color RoadLight  = new Color(0.72f, 0.68f, 0.58f); // 십자 도로 악센트

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
    const float PORTAL_R    = 52f;   // 포탈 링 반지름 (40 → 52)
    const float BAZAAR_R    = 82f;   // 바자르 노점 반지름 (63 → 82)
    const float BUILDING_R  = 130f;  // 주요 건물 동/서 거리 (87 → 130) — city block 최대 108+jitter≈116 바깥
    const float HARBOR_Z    = 540f;  // 그랜드 하버 예약 북쪽 한계 (170 → 540)
    const float GROUND_HALF = 600f;  // 전체 지면 반폭 (330 → 600)
    const float SPINE_W     = 30f;   // 실크로드 대로 폭
    const float PLAZA_R     = 65f;   // 중앙 광장 반지름
    const float GATE_Z      = -560f; // 남쪽 성문 = 남쪽 벽 (-130 → -560)
    const float TOWER_Z     = 75f;   // 조약의 탑 z

    // ─────────────────────────────────────────────────────────────────────────
    public static void Build()
    {
        BuildGround();
        BuildRoadNetwork();
        BuildFountain();
        BuildPortalRing();
        BuildSouthGate();
        // ── 핵심 건물 (구역별 배치) ──────────────────────────────────────────
        BuildAcademy();     // 서/지식 — 통합 학술원  (HubLab)
        BuildLibrary();     // 서/지식 — 도서관        (HubLibrary)
        BuildExchange();    // 동/상업 — 지식의 거래소 (HubExchange)
        BuildGuildHall();   // 남/생활 — 모험가 길드   (HubGuildHall)
        BuildFillerBuildings(); // 도로망 사이 빈 공간 채움 (구역색 + 골목 인접 + 섹터 블록)
        BuildWalls();
        AdjustLighting();
    }

    // ── 1. 지면 ──────────────────────────────────────────────────────────────
    // 포장 활성 영역: x=±165, z=-125 ~ +170 → PaveBase 연속 바닥
    // 그 바깥(벽까지)은 황토 흙 + 가장자리 잔디
    const float PAVE_HALF_X = 560f;   // (165 → 560) 도시 전체 반폭
    const float PAVE_Z_MIN  = -560f;  // GATE_Z
    const float PAVE_Z_MAX  =  560f;

    static void BuildGround()
    {
        var dirtColor  = new Color(0.46f, 0.38f, 0.26f); // 황토 흙
        var grassColor = new Color(0.28f, 0.48f, 0.16f); // 짙은 잔디

        // ─ 전체 황토 지면 (벽 경계까지) ─
        var g = GameObject.CreatePrimitive(PrimitiveType.Plane);
        g.name = "HubGround";
        g.transform.localScale = new Vector3(GROUND_HALF * 0.2f, 1f, GROUND_HALF * 0.2f);
        ApplyColor(g, dirtColor);
        // MeshCollider 유지 — 플레이어 지면 충돌체

        // ─ 포장 활성 구역 전체를 PaveBase 얇은 바닥으로 깖 ─
        // (구역 에이프런 + 도로 + 광장이 이 위에 올라감)
        float paveW  = PAVE_HALF_X * 2f;
        float paveD  = PAVE_Z_MAX - PAVE_Z_MIN;
        float paveCZ = (PAVE_Z_MIN + PAVE_Z_MAX) * 0.5f;
        var paveBase = new GameObject("HubPaveBase");
        Box(paveBase.transform, "PaveFloor",
            new Vector3(0f, 0.01f, paveCZ),
            new Vector3(paveW, 0.04f, paveD), PaveBase);

        // ─ 외곽 잔디 (포장 경계 바깥 벽 사이) ─
        foreach (var (pos, sc) in new (Vector3 p, Vector3 s)[]
        {
            (new Vector3(-580f, 0.001f,   0f), new Vector3(30f, 1f, 80f)),
            (new Vector3( 580f, 0.001f,   0f), new Vector3(30f, 1f, 80f)),
            (new Vector3(   0f, 0.001f, 580f), new Vector3(80f, 1f, 30f)),
        })
        {
            var gp = GameObject.CreatePrimitive(PrimitiveType.Plane);
            gp.name = "HubGrassOuter";
            gp.transform.position   = pos;
            gp.transform.localScale = sc;
            ApplyColor(gp, grassColor);
            Object.DestroyImmediate(gp.GetComponent<MeshCollider>());
        }
    }

    // ── 2. 구역별 포장 시스템 ─────────────────────────────────────────────────
    // 4구역 에이프런(구역색) → 십자 도로(RoadLight) → 중앙 광장(StoneLight+격자)
    static void BuildDistrictPaving()
    {
        var root = new GameObject("HubDistrictPaving");

        const float SLAB_Y  = 0.05f;  // 에이프런 y (PaveBase=0.04 위)
        const float ROAD_Y  = 0.12f;  // 도로 y (에이프런 위)
        const float PLAZA_Y = 0.20f;  // 광장 y (도로 위)
        const float T       = 0.10f;  // 슬래브 두께

        // ── A. 4구역 에이프런 ─────────────────────────────────────────────────
        // 서(지식): SlateCool, 동(상업): BrickOchre, 남(생활): DeckWarm, 북(항구): GraniteWet
        // 에이프런들이 가운데(광장 반경 안)에서 겹쳐도 도로·광장이 위에서 덮음
        Box(root.transform, "ApronWest",
            new Vector3(-115f, SLAB_Y, 20f),
            new Vector3(100f, T, 200f), SlateCool);
        Box(root.transform, "ApronEast",
            new Vector3( 115f, SLAB_Y, 20f),
            new Vector3(100f, T, 200f), BrickOchre);
        Box(root.transform, "ApronSouth",
            new Vector3(0f, SLAB_Y, -100f),
            new Vector3(170f, T, 70f), DeckWarm);
        Box(root.transform, "ApronNorth",
            new Vector3(0f, SLAB_Y, 110f),
            new Vector3(170f, T, 130f), GraniteWet);

        // ── B. 십자 도로 ──────────────────────────────────────────────────────
        const float ROAD_T  = 0.12f;
        const float EW_W    = 22f;    // 동/서 횡단로 폭

        float spineLen = PAVE_Z_MAX - PAVE_Z_MIN;
        float spineCZ  = (PAVE_Z_MIN + PAVE_Z_MAX) * 0.5f;
        float ewLen    = PAVE_HALF_X * 2f;

        // N/S 척추 대로
        Box(root.transform, "RoadSpineNS",
            new Vector3(0f, ROAD_Y, spineCZ),
            new Vector3(SPINE_W, ROAD_T, spineLen), RoadLight);

        // E/W 횡단로 (z=0 기준)
        Box(root.transform, "RoadEW",
            new Vector3(0f, ROAD_Y, 0f),
            new Vector3(ewLen, ROAD_T, EW_W), RoadLight);

        // 도로 연석 (척추)
        float curbX  = SPINE_W * 0.5f + 0.75f;
        float curbY  = ROAD_Y + ROAD_T * 0.5f + 0.05f;
        float curbLen = spineLen;
        Box(root.transform, "CurbNS_W", new Vector3(-curbX, curbY, spineCZ),
            new Vector3(1.5f, ROAD_T + 0.10f, curbLen), StonePave);
        Box(root.transform, "CurbNS_E", new Vector3( curbX, curbY, spineCZ),
            new Vector3(1.5f, ROAD_T + 0.10f, curbLen), StonePave);

        // 도로 연석 (횡단로)
        float curbZ = EW_W * 0.5f + 0.75f;
        Box(root.transform, "CurbEW_N", new Vector3(0f, curbY, curbZ),
            new Vector3(ewLen, ROAD_T + 0.10f, 1.5f), StonePave);
        Box(root.transform, "CurbEW_S", new Vector3(0f, curbY, -curbZ),
            new Vector3(ewLen, ROAD_T + 0.10f, 1.5f), StonePave);

        // 가로등 — 척추 남쪽 (6쌍)
        for (int i = 0; i < 6; i++)
        {
            float lz = PAVE_Z_MIN + 5f + i * 18f;
            LampPost(root.transform, new Vector3(-(SPINE_W * 0.5f + 2.5f), 0f, lz), $"LampS_W{i}");
            LampPost(root.transform, new Vector3(  SPINE_W * 0.5f + 2.5f, 0f, lz), $"LampS_E{i}");
        }
        // 가로등 — 척추 북쪽 (4쌍)
        for (int i = 0; i < 4; i++)
        {
            float lz = PLAZA_R + 5f + i * 14f;
            LampPost(root.transform, new Vector3(-(SPINE_W * 0.5f + 2.5f), 0f, lz), $"LampN_W{i}");
            LampPost(root.transform, new Vector3(  SPINE_W * 0.5f + 2.5f, 0f, lz), $"LampN_E{i}");
        }
        // 가로등 — 횡단로 (서/동 각 3쌍)
        for (int i = 0; i < 3; i++)
        {
            float lx = PLAZA_R + 10f + i * 28f;
            LampPost(root.transform, new Vector3(-lx, 0f,  EW_W * 0.5f + 2.5f), $"LampEW_W{i}N");
            LampPost(root.transform, new Vector3(-lx, 0f, -EW_W * 0.5f - 2.5f), $"LampEW_W{i}S");
            LampPost(root.transform, new Vector3( lx, 0f,  EW_W * 0.5f + 2.5f), $"LampEW_E{i}N");
            LampPost(root.transform, new Vector3( lx, 0f, -EW_W * 0.5f - 2.5f), $"LampEW_E{i}S");
        }

        // ── C. 중앙 광장 (도로 위 최상단) ────────────────────────────────────
        const float PLAZA_T  = 0.40f;
        float plazaFloorY = PLAZA_Y;

        Box(root.transform, "PlazaFloor",
            new Vector3(0f, plazaFloorY, 0f),
            new Vector3(PLAZA_R * 2f, PLAZA_T, PLAZA_R * 2f), StoneLight);

        // 광장 격자 패턴
        float gridTop = plazaFloorY + PLAZA_T * 0.5f + 0.01f;
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

        // 광장 테두리 경계석
        float bTop = plazaFloorY + PLAZA_T * 0.5f + 0.05f;
        Box(root.transform, "PlazaBorderN", new Vector3(0f, bTop,  PLAZA_R), new Vector3(PLAZA_R * 2f + 3f, 0.10f, 1.5f), StoneDark);
        Box(root.transform, "PlazaBorderS", new Vector3(0f, bTop, -PLAZA_R), new Vector3(PLAZA_R * 2f + 3f, 0.10f, 1.5f), StoneDark);
        Box(root.transform, "PlazaBorderE", new Vector3( PLAZA_R, bTop, 0f), new Vector3(1.5f, 0.10f, PLAZA_R * 2f + 3f), StoneDark);
        Box(root.transform, "PlazaBorderW", new Vector3(-PLAZA_R, bTop, 0f), new Vector3(1.5f, 0.10f, PLAZA_R * 2f + 3f), StoneDark);
    }

    // ── (구) 2. 포장 시스템 — BuildDistrictPaving으로 교체됨, 미호출 ──────────
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

    // ── 2b. 도로망 뼈대 (콘스탄티노플형) ─────────────────────────────────────────
    // 십자 대로 + 환상로 3겹(16각형) + 대각 방사 4가지 + 절차 골목
    // BuildDistrictPaving 대체. 구역 에이프런 없음(민바닥 위 도로만).
    static void BuildRoadNetwork()
    {
        var root = new GameObject("HubRoadNetwork");

        // 대로·환상·방사·골목 전 티어 동일 높이로 통일(티어 경계 단차 제거).
        // 광장만 의도된 단차(PlazaBorder 연석)로 높게 유지.
        const float ROAD_Y  = 0.12f; // 도로 전 티어 공통 높이
        const float PLAZA_Y = 0.20f; // 광장 높이 (의도된 단차)
        const float ROAD_T  = 0.15f; // 도로 두께 (전 티어 동일)

        // ── A. 메세 대로 십자 ─────────────────────────────────────────────────
        float spineLen = PAVE_Z_MAX - PAVE_Z_MIN; // 1120
        float ewLen    = PAVE_HALF_X * 2f;         // 1120
        float spineCZ  = (PAVE_Z_MIN + PAVE_Z_MAX) * 0.5f;

        // N/S 척추 대로
        Box(root.transform, "RoadSpineNS",
            new Vector3(0f, ROAD_Y, spineCZ),
            new Vector3(SPINE_W, ROAD_T, spineLen), RoadLight);
        // E/W 메세
        Box(root.transform, "RoadMeseEW",
            new Vector3(0f, ROAD_Y, 0f),
            new Vector3(ewLen, ROAD_T, 26f), RoadLight);

        // ── B. 환상로 3겹 (16각형 근사) ─────────────────────────────────────────
        // RingRoad가 각 변 + 꼭짓점 이음새 패치를 함께 생성(미터 갭/오버슈트 없음)
        RingRoad(root.transform, 140f, 16, 18f, ROAD_Y, ROAD_T, RoadLight); // 내환
        RingRoad(root.transform, 300f, 16, 16f, ROAD_Y, ROAD_T, RoadLight); // 중환
        RingRoad(root.transform, 460f, 16, 14f, ROAD_Y, ROAD_T, RoadLight); // 외환

        // ── C. 대각 방사 가지 4개 (내환 → 외환) ────────────────────────────────
        // 방사 양끝은 항상 환상로의 기존 꼭짓점(정확히 22.5°배수)과 겹침 →
        // RingRoad가 이미 놓은 이음새 패치가 방사-환상 접합부도 함께 커버함.
        float[] diagAngles = { 45f, 135f, 225f, 315f };
        for (int i = 0; i < 4; i++)
        {
            float rad = diagAngles[i] * Mathf.Deg2Rad;
            float sx = Mathf.Sin(rad), sz = Mathf.Cos(rad);
            var pa = new Vector2(140f * sx, 140f * sz);
            var pb = new Vector2(460f * sx, 460f * sz);
            RoadSeg(root.transform, $"Spoke_{(int)diagAngles[i]}",
                pa, pb, 14f, ROAD_Y, ROAD_T, RoadLight);
        }

        // ── D. 골목 (GenerateAlleySegments 공유 — BuildFillerBuildings도 동일 목록 사용) ──
        foreach (var seg in GenerateAlleySegments())
            RoadSeg(root.transform, seg.name, seg.a, seg.b, seg.width, ROAD_Y, ROAD_T, StonePave);

        // ── E. 중앙 광장 (십자·환상 위 최상단) ──────────────────────────────────
        const float PLAZA_T = 0.40f;

        Box(root.transform, "PlazaFloor",
            new Vector3(0f, PLAZA_Y, 0f),
            new Vector3(PLAZA_R * 2f, PLAZA_T, PLAZA_R * 2f), StoneLight);

        float gridTop = PLAZA_Y + PLAZA_T * 0.5f + 0.01f;
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
        float bTop = PLAZA_Y + PLAZA_T * 0.5f + 0.05f;
        Box(root.transform, "PlazaBorderN", new Vector3(0f, bTop,  PLAZA_R), new Vector3(PLAZA_R * 2f + 3f, 0.10f, 1.5f), StoneDark);
        Box(root.transform, "PlazaBorderS", new Vector3(0f, bTop, -PLAZA_R), new Vector3(PLAZA_R * 2f + 3f, 0.10f, 1.5f), StoneDark);
        Box(root.transform, "PlazaBorderE", new Vector3( PLAZA_R, bTop, 0f), new Vector3(1.5f, 0.10f, PLAZA_R * 2f + 3f), StoneDark);
        Box(root.transform, "PlazaBorderW", new Vector3(-PLAZA_R, bTop, 0f), new Vector3(1.5f, 0.10f, PLAZA_R * 2f + 3f), StoneDark);

        // ── F. 메인 가로등 (척추 + E/W 메세 도로변, 80단위 간격) ─────────────────
        float spineOff = SPINE_W * 0.5f + 2.5f;
        for (float lz = PAVE_Z_MIN + 10f; lz < PAVE_Z_MAX; lz += 80f)
        {
            if (Mathf.Abs(lz) < PLAZA_R + 5f) continue; // 광장 안쪽 제외
            LampPost(root.transform, new Vector3(-spineOff, 0f, lz), $"LampSpW_{(int)(lz + 600f)}");
            LampPost(root.transform, new Vector3( spineOff, 0f, lz), $"LampSpE_{(int)(lz + 600f)}");
        }
        float ewOff = 13f + 2.5f; // (26/2) + 2.5
        for (float lx = -PAVE_HALF_X + 10f; lx < PAVE_HALF_X; lx += 80f)
        {
            if (Mathf.Abs(lx) < PLAZA_R + 5f) continue;
            LampPost(root.transform, new Vector3(lx, 0f,  ewOff), $"LampMeN_{(int)(lx + 600f)}");
            LampPost(root.transform, new Vector3(lx, 0f, -ewOff), $"LampMeS_{(int)(lx + 600f)}");
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

    // 골목(방사 스텁 + 현 연결) 시드 고정 절차 생성 목록.
    // BuildRoadNetwork(포장)와 BuildFillerBuildings(인접 건물)가 동일 목록을 공유해
    // 항상 같은 골목 위치에 건물이 바짝 붙는다.
    static List<(string name, Vector2 a, Vector2 b, float width)> GenerateAlleySegments()
    {
        var list = new List<(string name, Vector2 a, Vector2 b, float width)>();
        var rng = new System.Random(20260629);
        float[] allRadAngles = { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };
        float[] ringR = { 140f, 300f, 460f };

        for (int s = 0; s < 8; s++)
        {
            float a1 = allRadAngles[s];
            float a2 = allRadAngles[(s + 1) % 8];
            float aJit = (float)(rng.NextDouble() - 0.5) * 8f;
            float aMid = (a1 + a2) * 0.5f + aJit;

            for (int b = 0; b < 2; b++) // 0=내~중, 1=중~외
            {
                float r0 = ringR[b], r1 = ringR[b + 1];

                // 방사 스텁: 섹터 중간 각도로 안쪽 링 → 바깥 링 (60% 확률)
                if (rng.NextDouble() < 0.60)
                {
                    float ra = aMid * Mathf.Deg2Rad;
                    var pa = new Vector2(r0 * Mathf.Sin(ra), r0 * Mathf.Cos(ra));
                    var pb = new Vector2(r1 * Mathf.Sin(ra), r1 * Mathf.Cos(ra));
                    list.Add(($"Alley_S{s}B{b}_Stub", pa, pb, 6f));
                }

                // 현 연결: 같은 밴드 내 인접 방사 사이를 잇는 가로 골목 (65% 확률)
                if (rng.NextDouble() < 0.65)
                {
                    float rChord = Mathf.Lerp(r0, r1, 0.30f + (float)rng.NextDouble() * 0.40f);
                    float j1 = (float)(rng.NextDouble() - 0.5) * 6f;
                    float j2 = (float)(rng.NextDouble() - 0.5) * 6f;
                    float ra1 = (a1 + j1) * Mathf.Deg2Rad;
                    float ra2 = (a2 + j2) * Mathf.Deg2Rad;
                    var pc = new Vector2(rChord * Mathf.Sin(ra1), rChord * Mathf.Cos(ra1));
                    var pd = new Vector2(rChord * Mathf.Sin(ra2), rChord * Mathf.Cos(ra2));
                    list.Add(($"Alley_S{s}B{b}_Chord", pc, pd, 5f));
                }
            }
        }
        return list;
    }

    // 주요 도로(십자·환상3·방사4) 중심선 목록 — 골목과 별개로 프론티지 채움에 사용.
    // BuildRoadNetwork의 도로 배치와 동일한 반지름/폭 리터럴을 공유(의도적 중복, GenerateAlleySegments와 동일 관례).
    static List<(string name, Vector2 a, Vector2 b, float width)> GenerateMainRoadSegments()
    {
        var list = new List<(string name, Vector2 a, Vector2 b, float width)>
        {
            ("SpineNS", new Vector2(0f, PAVE_Z_MIN), new Vector2(0f, PAVE_Z_MAX), SPINE_W),
            ("MeseEW", new Vector2(-PAVE_HALF_X, 0f), new Vector2(PAVE_HALF_X, 0f), 26f),
        };

        (float R, int sides, float width)[] rings =
        {
            (140f, 16, 18f), (300f, 16, 16f), (460f, 16, 14f),
        };
        foreach (var (R, sides, width) in rings)
        {
            for (int i = 0; i < sides; i++)
            {
                float a0 = i       * Mathf.PI * 2f / sides;
                float a1 = (i + 1) * Mathf.PI * 2f / sides;
                var pa = new Vector2(R * Mathf.Sin(a0), R * Mathf.Cos(a0));
                var pb = new Vector2(R * Mathf.Sin(a1), R * Mathf.Cos(a1));
                list.Add(($"Ring{(int)R}_{i}", pa, pb, width));
            }
        }

        float[] diagAngles = { 45f, 135f, 225f, 315f };
        foreach (float deg in diagAngles)
        {
            float rad = deg * Mathf.Deg2Rad;
            float sx = Mathf.Sin(rad), sz = Mathf.Cos(rad);
            list.Add(($"Spoke_{(int)deg}", new Vector2(140f * sx, 140f * sz), new Vector2(460f * sx, 460f * sz), 14f));
        }

        return list;
    }

    // ── 2d. 일반 채움 건물 (도로망 골격 사이 빈 공간) ────────────────────────────
    // 구역(십자 도로 기준 서=지식/동=상업/남=생활/북=항구)별 벽색 차별.
    // A. 도로 프론티지 — 모든 주요 도로(십자·환상·방사)+골목 양옆에 문이 도로를 향하도록 촘촘히 배열.
    // B. 섹터(8) × 밴드(3, 링 사이) 내부 그리드 — 프론티지 뒤 블록 안쪽까지 촘촘히 채움.
    static void BuildFillerBuildings()
    {
        var root = new GameObject("HubFillerBuildings");
        int idx = 0;
        var placed = new List<(Vector2 pos, float radius)>();

        var landmarks = new (float x, float z, float r)[]
        {
            (-105f, 35f, 26f),   // 학술원
            (-105f, -35f, 26f),  // 도서관
            (105f, 35f, 26f),    // 거래소
            (-40f, -85f, 24f),   // 길드
        };

        // 모든 주요 도로 + 골목을 한 번에 합친 목록 — 건물이 어떤 도로 위에도 올라가지 않도록
        // 배치 전 항상 이 목록 전체와 겹침을 검사한다(자기 자신이 속한 도로만 이름으로 제외).
        var allRoads = GenerateMainRoadSegments();
        allRoads.AddRange(GenerateAlleySegments());

        bool TooClose(float x, float z, float radius)
        {
            float r = Mathf.Sqrt(x * x + z * z);
            if (r < 75f + radius) return true; // 광장 + 포탈 링
            if (Mathf.Abs(z - GATE_Z) < 22f + radius && Mathf.Abs(x) < 40f + radius) return true; // 남문
            foreach (var (lx, lz, lr) in landmarks)
                if ((x - lx) * (x - lx) + (z - lz) * (z - lz) < (lr + radius) * (lr + radius)) return true;
            foreach (var p in placed)
                if ((x - p.pos.x) * (x - p.pos.x) + (z - p.pos.y) * (z - p.pos.y) < (radius + p.radius) * (radius + p.radius)) return true;
            return false;
        }

        bool OverlapsRoad(float x, float z, float radius, (string name, Vector2 a, Vector2 b, float width) seg)
        {
            Vector2 pt = new Vector2(x, z);
            Vector2 d  = seg.b - seg.a;
            float len  = d.magnitude;
            if (len < 0.001f) return Vector2.Distance(pt, seg.a) < seg.width * 0.5f + radius;
            d /= len;
            float t = Mathf.Clamp(Vector2.Dot(pt - seg.a, d), 0f, len);
            Vector2 closest = seg.a + d * t;
            return Vector2.Distance(pt, closest) < seg.width * 0.5f + radius;
        }

        bool OverlapsAnyRoad(float x, float z, float radius, string excludeName)
        {
            foreach (var seg in allRoads)
            {
                if (excludeName != null && seg.name == excludeName) continue;
                if (OverlapsRoad(x, z, radius, seg)) return true;
            }
            return false;
        }

        Color WallColorFor(float x, float z) =>
            Mathf.Abs(x) >= Mathf.Abs(z) ? (x < 0f ? SlateCool : BrickOchre)
                                          : (z < 0f ? DeckWarm : GraniteWet);

        // 정면(문) = 로컬 -Z. rotY 회전 후 -Z가 세계 공간에서 faceDir를 향하게 하는 각도.
        float FaceRotY(Vector2 faceDir) => Mathf.Atan2(-faceDir.x, -faceDir.y) * Mathf.Rad2Deg;

        // placedClearance: 같은 줄(row)의 인접 건물끼리는 오검출 없게 폭(buildW) 기준의
        // 느슨한 값을 넘기고, 서로 다른 패스(그리드 등)끼리는 대각선 기준의 보수적 값을 넘긴다.
        bool SpawnFiller(float x, float z, float rotY, float buildW, float buildD, float buildH,
            float placedClearance, bool withDoor, string ownRoadName)
        {
            float roadRadius = 0.5f * Mathf.Sqrt(buildW * buildW + buildD * buildD);
            if (TooClose(x, z, placedClearance)) return false;
            if (OverlapsAnyRoad(x, z, roadRadius, ownRoadName)) return false;

            Color wall = WallColorFor(x, z);
            var fb = new GameObject($"Filler_{idx++}");
            fb.transform.SetParent(root.transform, false);
            fb.transform.localPosition = new Vector3(x, 0f, z);
            fb.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);

            Box(fb.transform, "Body",  new Vector3(0f, buildH * 0.5f, 0f), new Vector3(buildW, buildH, buildD), wall);
            Box(fb.transform, "Found", new Vector3(0f, -0.3f, 0f), new Vector3(buildW + 0.8f, 0.6f, buildD + 0.8f), StoneDark);
            float roofH = buildH * 0.3f;
            float roofY = buildH + 0.2f + roofH * 0.5f;
            Box(fb.transform, "RoofBase",    new Vector3(0f, buildH + 0.2f, 0f), new Vector3(buildW + 1.5f, 0.4f, buildD + 1.5f), TileRed);
            Box(fb.transform, "RoofSlope_F", new Vector3(0f, roofY, -(buildD * 0.3f)), new Vector3(buildW + 0.8f, roofH, 0.8f), TileRed);
            Box(fb.transform, "RoofSlope_B", new Vector3(0f, roofY,  (buildD * 0.3f)), new Vector3(buildW + 0.8f, roofH, 0.8f), TileRed);
            Box(fb.transform, "RoofPeak",    new Vector3(0f, buildH + 0.2f + roofH, 0f), new Vector3(buildW + 0.3f, 0.5f, 0.8f), TimberBrown);

            if (withDoor)
            {
                Box(fb.transform, "BeamH1", new Vector3(0f, buildH * 0.33f, -(buildD * 0.5f + 0.05f)), new Vector3(buildW + 0.2f, 0.35f, 0.25f), TimberBrown);
                Box(fb.transform, "Door",   new Vector3(0f, 1.1f,           -(buildD * 0.5f + 0.08f)), new Vector3(1.6f, 2.2f, 0.15f), TimberBrown);
            }
            fb.isStatic = true;

            placed.Add((new Vector2(x, z), placedClearance));
            return true;
        }

        // 도로 중심선 목록을 따라 양옆에 문이 도로를 향하도록 촘촘히 줄지어 채움.
        void LineFrontage(List<(string name, Vector2 a, Vector2 b, float width)> segs,
            System.Random rng, float edgePad, float roadMargin,
            float wMin, float wMax, float dMin, float dMax, float hMin, float hMax)
        {
            foreach (var seg in segs)
            {
                Vector2 full = seg.b - seg.a;
                float totalLen = full.magnitude;
                if (totalLen < wMin + edgePad * 2f) continue;
                Vector2 d = full / totalLen;
                Vector2 p = new Vector2(-d.y, d.x);

                foreach (float side in new[] { -1f, 1f })
                {
                    float t = edgePad;
                    while (t < totalLen - edgePad)
                    {
                        float buildW = wMin + (float)rng.NextDouble() * (wMax - wMin);
                        if (t + buildW > totalLen - edgePad) break;

                        float buildD = dMin + (float)rng.NextDouble() * (dMax - dMin);
                        float buildH = hMin + (float)rng.NextDouble() * (hMax - hMin);
                        float centerT = t + buildW * 0.5f;
                        Vector2 posOnLine = seg.a + d * centerT;
                        float offset = seg.width * 0.5f + buildD * 0.5f + roadMargin;
                        Vector2 pos = posOnLine + p * (offset * side);
                        Vector2 faceDir = -(p * side); // 건물 정면이 도로 중심선을 향함

                        // 같은 줄 이웃과의 간격은 폭(buildW) 절반이면 충분 — 대각선 기준을 쓰면
                        // 정상적으로 붙어있는 이웃까지 오검출로 튕겨 나가 줄이 끊어진다.
                        SpawnFiller(pos.x, pos.y, FaceRotY(faceDir), buildW, buildD, buildH,
                            buildW * 0.5f, true, seg.name);

                        float gap = 0.5f + (float)rng.NextDouble() * 1.0f; // 촘촘히 — 최소 간격만
                        t += buildW + gap;
                    }
                }
            }
        }

        // ── A. 도로 프론티지 — 십자·환상·방사 대로 + 골목, 모든 도로 양옆에 문이 도로를 향하도록 배열 ──
        // 크기는 학술원(28×15×20) 정도의 스케일로 통일.
        var mainRng  = new System.Random(20260629 + 111);
        var alleyRng = new System.Random(20260629 + 777);

        LineFrontage(GenerateMainRoadSegments(), mainRng, edgePad: 6f, roadMargin: 5.5f,
            wMin: 24f, wMax: 32f, dMin: 17f, dMax: 23f, hMin: 12f, hMax: 17f);

        // 골목 — 도로변보다 여유폭을 확 줄여 캐릭터 1명이 겨우 지나갈 정도로 바짝 붙임(건물 자체 크기는 동일).
        LineFrontage(GenerateAlleySegments(), alleyRng, edgePad: 3f, roadMargin: 0.6f,
            wMin: 24f, wMax: 32f, dMin: 17f, dMax: 23f, hMin: 12f, hMax: 17f);

        // ── B. 섹터(8) × 밴드(3, 링 사이) 내부 그리드 — 프론티지 뒤 블록 안쪽까지 촘촘히 채움 ──
        var gridRng = new System.Random(20260629 + 999);
        float[] sectorAngles = { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };
        float[] bandMin = { 155f, 315f, 475f };
        float[] bandMax = { 285f, 445f, 575f };
        const float radialStep = 25f;
        const float arcWidth   = 27f;

        for (int s = 0; s < 8; s++)
        {
            float a1 = sectorAngles[s];
            float a2 = a1 + 45f;
            for (int b = 0; b < 3; b++)
            {
                float r0 = bandMin[b], r1 = bandMax[b];
                for (float r = r0 + radialStep * 0.5f; r < r1; r += radialStep)
                {
                    float angStep = (arcWidth / r) * Mathf.Rad2Deg;
                    float aPad    = angStep * 0.6f; // 스포크(섹터 경계) 회피 여유
                    for (float ang = a1 + aPad; ang < a2 - aPad; ang += angStep)
                    {
                        if (gridRng.NextDouble() < 0.10) continue; // 자연스러운 빈틈만 소량

                        float jr  = r + ((float)gridRng.NextDouble() - 0.5f) * (radialStep * 0.3f);
                        float jang = ang + ((float)gridRng.NextDouble() - 0.5f) * angStep * 0.3f;
                        float rad = jang * Mathf.Deg2Rad;
                        float x = jr * Mathf.Sin(rad);
                        float z = jr * Mathf.Cos(rad);

                        float buildW = 22f + (float)gridRng.NextDouble() * 10f;
                        float buildD = 16f + (float)gridRng.NextDouble() * 8f;
                        float buildH = 11f + (float)gridRng.NextDouble() * 7f;
                        // 정면이 도심(광장) 쪽을 향하도록 — 내부 블록 건물의 일관된 기본 방향.
                        // 그리드 칸끼리는 배치가 덜 엄격하므로 대각선 기준의 보수적 간격을 사용.
                        float roadRadius = 0.5f * Mathf.Sqrt(buildW * buildW + buildD * buildD);
                        SpawnFiller(x, z, jang, buildW, buildD, buildH, roadRadius, false, null);
                    }
                }
            }
        }
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

    // ── 7. 주요 건물 4기 ─────────────────────────────────────────────────────
    static void BuildMainBuildings()
    {
        // 통합 학술원 (서, HubLab) — 튜더 반목조 양식
        ByzantineBuilding("HubLab", "통합 학술원", "[E]  통합 학술원",
            new Vector3(-BUILDING_R, 0f, 10f), 90f,
            w: 26f, h: 15f, d: 20f, wall: StoneWarm, dome: Gold);

        // 도서관 (서북, HubLibrary) — 학술원 북쪽 지식 구역
        // MetaUISetup.WireBuilding("HubLibrary", …, "OpenLibrary") 가 가챠 패널로 자동 연결됨
        LibraryBuilding("HubLibrary", "도서관", "[E]  도서관",
            new Vector3(-BUILDING_R, 0f, 55f), 90f);

        // 지식의 거래소 (동, HubExchange) — 상점·거래 허브 (향후 OpenExchange 연결)
        ByzantineBuilding("HubExchange", "지식의 거래소", "[E]  지식의 거래소",
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

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// 통합 학술원 (HubLab) — 튜더 반목조 몸체 + 고전 포치(기둥+페디먼트) 융합.
    ///   · 정면 아키트레이브 + 삼각 페디먼트 — "학자들이 모이는 신전" 정체성
    ///   · 아키트레이브 위 6개 과목색 스터드(PortalColors) — 모든 대륙 학문의 집결지 상징
    /// </summary>
    static void AcademyBuilding(string goName, string display, string prompt, Vector3 pos, float rotY)
    {
        const float W = 28f, H = 15f, D = 20f;

        var root = new GameObject(goName);
        root.transform.position = pos;
        root.transform.rotation = Quaternion.Euler(0f, rotY, 0f);

        var ia = root.AddComponent<Interactable>();
        ia.displayName = display;
        ia.promptText  = prompt;
        ia.radius      = 10f;

        Box(root.transform, "Foundation", new Vector3(0f, -0.4f, 0f), new Vector3(W + 5f, 0.8f, D + 5f), StoneDark);

        float fz = D * 0.5f;
        Box(root.transform, "Step1", new Vector3(0f, 0.20f, -(fz + 2.2f)), new Vector3(W * 0.60f, 0.40f, 2.2f), StoneWarm);
        Box(root.transform, "Step2", new Vector3(0f, 0.55f, -(fz + 0.9f)), new Vector3(W * 0.55f, 0.40f, 1.8f), StoneCold);
        Box(root.transform, "Step3", new Vector3(0f, 0.90f, -(fz + 0.1f)), new Vector3(W * 0.50f, 0.40f, 1.0f), StoneWarm);

        Box(root.transform, "Body", new Vector3(0f, H * 0.5f + 0.2f, 0f), new Vector3(W, H, D), PlasterCream);

        // 목조 빔 격자
        float[] beamYs = { H * 0.25f + 0.2f, H * 0.52f + 0.2f, H * 0.78f + 0.2f };
        foreach (float by in beamYs)
            Box(root.transform, "BeamH", new Vector3(0f, by, -(D * 0.5f + 0.05f)), new Vector3(W + 0.2f, 0.5f, 0.3f), TimberBrown);
        float[] beamXs = { -W * 0.28f, W * 0.28f };
        foreach (float bx in beamXs)
            Box(root.transform, "BeamV", new Vector3(bx, H * 0.5f + 0.2f, -(D * 0.5f + 0.05f)), new Vector3(0.4f, H + 0.4f, 0.3f), TimberBrown);

        // 박공 지붕
        float roofH = H * 0.45f;
        float roofY = H + 0.2f + roofH * 0.5f;
        Box(root.transform, "RoofBase",    new Vector3(0f, H + 0.2f, 0f),       new Vector3(W + 2f, 0.5f, D + 2f), TileRed);
        Box(root.transform, "RoofSlope_F", new Vector3(0f, roofY, -(D * 0.3f)), new Vector3(W + 1f, roofH, 1.0f), TileRed);
        Box(root.transform, "RoofSlope_B", new Vector3(0f, roofY,  (D * 0.3f)), new Vector3(W + 1f, roofH, 1.0f), TileRed);
        Box(root.transform, "RoofPeak",    new Vector3(0f, H + 0.2f + roofH, 0f), new Vector3(W + 0.5f, 0.6f, 1.2f), TimberBrown);

        float px = W * 0.28f;
        Pillar(root.transform, "EntryPillar_LL", new Vector3(-px * 1.6f, 0f, -(D * 0.5f - 0.5f)), StoneDark);
        Pillar(root.transform, "EntryPillar_L",  new Vector3(-px * 0.6f, 0f, -(D * 0.5f - 0.5f)), StoneDark);
        Pillar(root.transform, "EntryPillar_R",  new Vector3( px * 0.6f, 0f, -(D * 0.5f - 0.5f)), StoneDark);
        Pillar(root.transform, "EntryPillar_RR", new Vector3( px * 1.6f, 0f, -(D * 0.5f - 0.5f)), StoneDark);

        // ── 정면 포치 (아키트레이브 + 삼각 페디먼트) — 학술원의 신전풍 정체성 ──
        float porchZ = -(D * 0.5f + 1.4f);
        float archY  = 9.2f;
        Box(root.transform, "PorchArchitrave", new Vector3(0f, archY, porchZ), new Vector3(px * 3.6f, 1.0f, 1.6f), StoneWarm);
        float pedH = 3.2f;
        float pedY = archY + 0.5f + pedH * 0.5f;
        Box(root.transform, "PorchPedimentBack", new Vector3(0f, pedY, porchZ), new Vector3(px * 3.6f, pedH, 0.3f), StoneWarm);
        var pedL = Box(root.transform, "PorchPedSlope_L", new Vector3(-px * 0.9f, pedY, porchZ), new Vector3(px * 1.9f, 0.6f, 1.5f), StoneWarm);
        pedL.transform.localRotation = Quaternion.Euler(0f, 0f, 14f);
        var pedR = Box(root.transform, "PorchPedSlope_R", new Vector3( px * 0.9f, pedY, porchZ), new Vector3(px * 1.9f, 0.6f, 1.5f), StoneWarm);
        pedR.transform.localRotation = Quaternion.Euler(0f, 0f, -14f);

        // 아키트레이브 위 과목색 스터드 — "모든 대륙 학자가 모이는 곳" 상징
        for (int i = 0; i < PortalColors.Length; i++)
        {
            float sx = (i - (PortalColors.Length - 1) * 0.5f) * (px * 3.6f / PortalColors.Length);
            var stud = Sphere(root.transform, $"SubjectStud_{i}",
                new Vector3(sx, archY + 0.55f, porchZ - 0.7f), Vector3.one * 0.7f, PortalColors[i]);
            SetEmissive(stud, PortalColors[i], PortalColors[i] * 0.5f);
            RemoveCollider(stud);
        }

        var banner = Box(root.transform, "Banner",
            new Vector3(0f, H * 0.88f, -(D * 0.5f + 0.05f)),
            new Vector3(W * 0.45f, H * 0.17f, 0.15f), Gold);
        SetEmissive(banner, Gold, Gold * 0.2f);

        Box(root.transform, "DoorCut", new Vector3(0f, H * 0.33f, -(D * 0.5f + 0.05f)),
            new Vector3(W * 0.22f, H * 0.65f, 0.4f), StoneDark);

        var mm = root.AddComponent<MapMarker>();
        mm.kind        = MapMarker.IconKind.Building;
        mm.displayName = display;
        mm.iconColor   = Gold;
        mm.footprintW  = W;
        mm.footprintD  = D;
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// 지식의 거래소 (HubExchange) — 바자르 파빌리온 지붕 + 천막/노점.
    ///   · 작은 돔 3개(중앙+좌우)로 이스탄불 대바자르풍 실루엣
    ///   · 입구 위 줄무늬 차양(천막) + 계단 옆 상자·자루 노점 + 랜턴
    /// </summary>
    static void ExchangeBuilding(string goName, string display, string prompt, Vector3 pos, float rotY)
    {
        const float W = 28f, H = 15f, D = 20f;

        var root = new GameObject(goName);
        root.transform.position = pos;
        root.transform.rotation = Quaternion.Euler(0f, rotY, 0f);

        var ia = root.AddComponent<Interactable>();
        ia.displayName = display;
        ia.promptText  = prompt;
        ia.radius      = 10f;

        Box(root.transform, "Foundation", new Vector3(0f, -0.4f, 0f), new Vector3(W + 5f, 0.8f, D + 5f), StoneDark);

        float fz = D * 0.5f;
        Box(root.transform, "Step1", new Vector3(0f, 0.20f, -(fz + 2.2f)), new Vector3(W * 0.60f, 0.40f, 2.2f), StoneWarm);
        Box(root.transform, "Step2", new Vector3(0f, 0.55f, -(fz + 0.9f)), new Vector3(W * 0.55f, 0.40f, 1.8f), StoneCold);
        Box(root.transform, "Step3", new Vector3(0f, 0.90f, -(fz + 0.1f)), new Vector3(W * 0.50f, 0.40f, 1.0f), StoneWarm);

        Box(root.transform, "Body", new Vector3(0f, H * 0.5f + 0.2f, 0f), new Vector3(W, H, D), StoneWarm);

        float px = W * 0.28f;
        Pillar(root.transform, "EntryPillar_LL", new Vector3(-px * 1.6f, 0f, -(D * 0.5f - 0.5f)), StoneDark);
        Pillar(root.transform, "EntryPillar_L",  new Vector3(-px * 0.6f, 0f, -(D * 0.5f - 0.5f)), StoneDark);
        Pillar(root.transform, "EntryPillar_R",  new Vector3( px * 0.6f, 0f, -(D * 0.5f - 0.5f)), StoneDark);
        Pillar(root.transform, "EntryPillar_RR", new Vector3( px * 1.6f, 0f, -(D * 0.5f - 0.5f)), StoneDark);

        // ── 평지붕 받침 (돔 기단) ──
        Box(root.transform, "RoofFlat", new Vector3(0f, H + 0.2f, 0f), new Vector3(W + 2f, 0.6f, D + 2f), StoneCold);

        // ── 바자르 돔 3개 (중앙 + 좌/우) — 이스탄불 대바자르풍 실루엣 ──
        float[] domeXs     = { -W * 0.30f, 0f, W * 0.30f };
        float[] domeScales  = { 0.72f, 1.0f, 0.72f };
        for (int i = 0; i < domeXs.Length; i++)
        {
            float dx    = domeXs[i];
            float sc    = domeScales[i];
            float drumH = 2.4f * sc;
            float drumR = 4.2f * sc;
            float drumY = H + 0.5f + drumH * 0.5f;
            Cyl(root.transform, $"BazaarDrum_{i}", new Vector3(dx, drumY, 0f), new Vector3(drumR * 2f, drumH, drumR * 2f), StoneWarm);
            float domeY = H + 0.5f + drumH;
            Sphere(root.transform, $"BazaarDome_{i}", new Vector3(dx, domeY, 0f), new Vector3(drumR * 2.15f, drumR * 1.6f, drumR * 2.15f), GoldBright);
            var finial = Sphere(root.transform, $"BazaarFinial_{i}", new Vector3(dx, domeY + drumR * 0.9f, 0f), Vector3.one * (0.8f * sc), Gold);
            SetEmissive(finial, Gold, Gold * 0.4f);
            RemoveCollider(finial);
        }

        // ── 입구 위 줄무늬 차양(천막) ──
        float awningZ = -(D * 0.5f + 0.9f);
        float awningY = H * 0.62f;
        var awningColors = new[] { GoldBright, BrickOchre };
        for (int i = 0; i < 5; i++)
        {
            float ax = (i - 2f) * (W * 0.18f);
            var strip = Box(root.transform, $"AwningStrip_{i}", new Vector3(ax, awningY, awningZ), new Vector3(W * 0.19f, 0.3f, 2.6f), awningColors[i % 2]);
            strip.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);
        }
        Cyl(root.transform, "AwningPoleL", new Vector3(-W * 0.32f, awningY * 0.5f, awningZ - 1.3f), new Vector3(0.25f, awningY, 0.25f), TimberBrown);
        Cyl(root.transform, "AwningPoleR", new Vector3( W * 0.32f, awningY * 0.5f, awningZ - 1.3f), new Vector3(0.25f, awningY, 0.25f), TimberBrown);

        // ── 계단 옆 노점 (상자·자루 더미 + 랜턴) ──
        var crateColors = new[] { TimberBrown, StoneWarm, BrickOchre };
        foreach (float sideSign in new[] { -1f, 1f })
        {
            float cx = sideSign * (W * 0.34f);
            float cz = -(fz + 1.5f);
            float stackY = 0.2f;
            for (int i = 0; i < crateColors.Length; i++)
            {
                float cs = 1.3f - i * 0.15f;
                var crate = Box(root.transform, $"MarketCrate_{sideSign}_{i}", new Vector3(cx, stackY, cz), new Vector3(cs, 0.9f, cs), crateColors[i]);
                crate.transform.localRotation = Quaternion.Euler(0f, i * 8f * sideSign, 0f);
                stackY += 0.85f;
            }
            Cyl(root.transform, $"LanternPole_{sideSign}", new Vector3(cx, 2.5f, cz + 1.3f), new Vector3(0.2f, 5f, 0.2f), TimberBrown);
            var lantern = Sphere(root.transform, $"Lantern_{sideSign}", new Vector3(cx, 4.9f, cz + 1.3f), Vector3.one * 0.9f, GoldBright);
            SetEmissive(lantern, GoldBright, GoldBright * 0.75f);
            RemoveCollider(lantern);
        }

        // ── 지붕선 골드 페넌트 리본 ──
        for (int i = 0; i < 4; i++)
        {
            float fx = (i - 1.5f) * (W * 0.22f);
            Cyl(root.transform, $"ExFlagPole_{i}", new Vector3(fx, H + 1.2f, -(D * 0.5f + 0.2f)), new Vector3(0.15f, 1.4f, 0.15f), TimberBrown);
            var pennant = Box(root.transform, $"ExPennant_{i}", new Vector3(fx + 0.6f, H + 1.7f, -(D * 0.5f + 0.2f)), new Vector3(1.1f, 0.7f, 0.05f), i % 2 == 0 ? GoldBright : BrickOchre);
            pennant.transform.localRotation = Quaternion.Euler(0f, 10f, 0f);
        }

        Box(root.transform, "DoorCut", new Vector3(0f, H * 0.33f, -(D * 0.5f + 0.05f)),
            new Vector3(W * 0.22f, H * 0.65f, 0.4f), StoneDark);

        var mm = root.AddComponent<MapMarker>();
        mm.kind        = MapMarker.IconKind.Building;
        mm.displayName = display;
        mm.iconColor   = GoldBright;
        mm.footprintW  = W;
        mm.footprintD  = D;
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// 도서관 (HubLibrary) — 장엄한 석조 도서관. 학술원(튜더 양식)과 외형 차별화.
    ///   · 정면 고창(아치 창 3개) — StoneDark 프레임 + 은은한 학술 블루 발광
    ///   · 측면 열람탑(Cyl 기둥 + Sphere 지식 오브)
    ///   · 평지붕 + 파라펫 + 학술 배너 (BlueTeal 악센트)
    ///   · 내부 입장 훅: 자식 GO "LibraryEntrance" + 씬 루트 "LibraryReturnSpawn"
    ///     → 향후 ScenePortal.targetScene = "LibraryInterior" 로 연결
    /// </summary>
    static void LibraryBuilding(string goName, string display, string prompt, Vector3 pos, float rotY)
    {
        const float W  = 30f;   // 폭
        const float H  = 18f;   // 본체 높이
        const float D  = 22f;   // 깊이
        // 학술 블루-틸 악센트 (기존 팔레트에 없는 도서관 전용)
        var BlueTeal = new Color(0.35f, 0.55f, 0.65f);

        var root = new GameObject(goName);
        root.transform.position = pos;
        root.transform.rotation = Quaternion.Euler(0f, rotY, 0f);

        var ia = root.AddComponent<Interactable>();
        ia.displayName = display;
        ia.promptText  = prompt;
        ia.radius      = 10f;

        // ── 기단 + 계단 ──
        Box(root.transform, "Foundation", new Vector3(0f, -0.4f, 0f), new Vector3(W + 6f, 0.8f, D + 6f), StoneDark);
        float fz = D * 0.5f;
        Box(root.transform, "Step1", new Vector3(0f, 0.20f, -(fz + 2.5f)), new Vector3(W * 0.55f, 0.40f, 2.5f), StoneWarm);
        Box(root.transform, "Step2", new Vector3(0f, 0.55f, -(fz + 1.1f)), new Vector3(W * 0.50f, 0.40f, 2.0f), StoneCold);
        Box(root.transform, "Step3", new Vector3(0f, 0.90f, -(fz + 0.2f)), new Vector3(W * 0.45f, 0.40f, 1.2f), StoneWarm);

        // ── 계단 옆 책더미 소품 ('도서관' 정체성 보강) ──
        var bookColors = new[] { new Color(0.55f, 0.20f, 0.16f), BlueTeal, TimberBrown, StoneWarm };
        foreach (float sideSign in new[] { -1f, 1f })
        {
            float bx = sideSign * (W * 0.30f);
            float bz = -(fz + 1.6f);
            float stackY = 0.15f;
            for (int i = 0; i < bookColors.Length; i++)
            {
                float bw = 1.6f - i * 0.12f;
                var book = Box(root.transform, $"BookStack_{sideSign}_{i}",
                    new Vector3(bx + (i % 2 == 0 ? 0.15f : -0.15f) * sideSign, stackY, bz),
                    new Vector3(bw, 0.22f, 1.1f), bookColors[i]);
                book.transform.localRotation = Quaternion.Euler(0f, (i - 1.5f) * 4f * sideSign, 0f);
                stackY += 0.24f;
            }
        }

        // ── 본체 (석조) ──
        Box(root.transform, "Body", new Vector3(0f, H * 0.5f + 0.2f, 0f), new Vector3(W, H, D), StoneWarm);

        // ── 정면 아치 창 3개 — StoneDark 프레임 + 발광 유리 패널 ──
        float[] winXs = { -W * 0.30f, 0f, W * 0.30f };
        for (int i = 0; i < 3; i++)
        {
            float wx = winXs[i];
            float wFrontZ = -(D * 0.5f + 0.05f);
            // 창 프레임 외곽 (약간 돌출)
            Box(root.transform, $"WinFrame_{i}",
                new Vector3(wx, H * 0.58f, wFrontZ),
                new Vector3(5.5f, H * 0.62f, 0.35f), StoneDark);
            // 발광 유리 패널
            var glass = Box(root.transform, $"WinGlass_{i}",
                new Vector3(wx, H * 0.58f, wFrontZ - 0.05f),
                new Vector3(4.2f, H * 0.55f, 0.1f), BlueTeal);
            SetEmissive(glass, BlueTeal, BlueTeal * 0.45f);
            RemoveCollider(glass);
            // 창 상단 키스톤 장식
            Box(root.transform, $"WinKey_{i}",
                new Vector3(wx, H * 0.58f + H * 0.32f, wFrontZ),
                new Vector3(3.0f, 1.0f, 0.4f), StoneLight);
        }

        // ── 정면 입구 기둥 4개 ──
        float px = W * 0.30f;
        Pillar(root.transform, "EntryPillar_LL", new Vector3(-px * 1.55f, 0f, -(D * 0.5f - 0.5f)), StoneDark);
        Pillar(root.transform, "EntryPillar_L",  new Vector3(-px * 0.55f, 0f, -(D * 0.5f - 0.5f)), StoneDark);
        Pillar(root.transform, "EntryPillar_R",  new Vector3( px * 0.55f, 0f, -(D * 0.5f - 0.5f)), StoneDark);
        Pillar(root.transform, "EntryPillar_RR", new Vector3( px * 1.55f, 0f, -(D * 0.5f - 0.5f)), StoneDark);

        // ── 평지붕 + 파라펫 (브레스트워크) ──
        Box(root.transform, "RoofFlat", new Vector3(0f, H + 0.2f, 0f), new Vector3(W + 2f, 0.6f, D + 2f), StoneCold);
        // 파라펫 — 네 면
        Box(root.transform, "ParapetF", new Vector3(0f,    H + 1.4f, -(D * 0.5f + 0.8f)), new Vector3(W + 2f, 2.2f, 0.6f), StoneDark);
        Box(root.transform, "ParapetB", new Vector3(0f,    H + 1.4f,  (D * 0.5f + 0.8f)), new Vector3(W + 2f, 2.2f, 0.6f), StoneDark);
        Box(root.transform, "ParapetL", new Vector3(-(W * 0.5f + 0.8f), H + 1.4f, 0f),    new Vector3(0.6f, 2.2f, D + 2f), StoneDark);
        Box(root.transform, "ParapetR", new Vector3( (W * 0.5f + 0.8f), H + 1.4f, 0f),    new Vector3(0.6f, 2.2f, D + 2f), StoneDark);
        // 파라펫 상단 코핑 스톤
        Box(root.transform, "CopingF", new Vector3(0f,    H + 2.6f, -(D * 0.5f + 0.8f)), new Vector3(W + 2.8f, 0.4f, 0.9f), StoneLight);
        Box(root.transform, "CopingB", new Vector3(0f,    H + 2.6f,  (D * 0.5f + 0.8f)), new Vector3(W + 2.8f, 0.4f, 0.9f), StoneLight);
        Box(root.transform, "CopingL", new Vector3(-(W * 0.5f + 0.8f), H + 2.6f, 0f),    new Vector3(0.9f, 0.4f, D + 2.8f), StoneLight);
        Box(root.transform, "CopingR", new Vector3( (W * 0.5f + 0.8f), H + 2.6f, 0f),    new Vector3(0.9f, 0.4f, D + 2.8f), StoneLight);

        // ── 중앙 로톤다 돔 (도서관 실루엣의 핵심 — "도서관임"을 멀리서도 알림) ──
        float domeBaseY = H + 2.9f;   // 코핑 위
        float drumH     = 5.0f;
        float drumR     = 9.0f;
        float drumY     = domeBaseY + drumH * 0.5f;
        Cyl(root.transform, "DomeDrum", new Vector3(0f, drumY, 0f), new Vector3(drumR * 2f, drumH, drumR * 2f), StoneLight);

        // 드럼 둘레 수직 리브 12개
        for (int i = 0; i < 12; i++)
        {
            float ang = i * 30f;
            float rad = ang * Mathf.Deg2Rad;
            float rx  = (drumR + 0.1f) * Mathf.Sin(rad);
            float rz  = (drumR + 0.1f) * Mathf.Cos(rad);
            var rib = Box(root.transform, $"DomeRib_{i}", new Vector3(rx, drumY, rz), new Vector3(0.6f, drumH + 0.3f, 0.5f), StoneDark);
            rib.transform.localRotation = Quaternion.Euler(0f, ang, 0f);
        }

        // 클리어스토리 발광 창 띠 (리브 안쪽 — 열람실 불빛이 새어나오는 느낌)
        var clerestory = Cyl(root.transform, "DomeClerestory",
            new Vector3(0f, drumY, 0f), new Vector3(drumR * 1.93f, drumH * 0.7f, drumR * 1.93f), BlueTeal);
        SetEmissive(clerestory, BlueTeal, BlueTeal * 0.55f);
        RemoveCollider(clerestory);

        // 유리 돔 (드럼 위, 상반구만 노출)
        float domeTopY = domeBaseY + drumH;
        var glassDome = Sphere(root.transform, "GlassDome", new Vector3(0f, domeTopY, 0f), new Vector3(19f, 12f, 19f), BlueTeal);
        SetEmissive(glassDome, BlueTeal, BlueTeal * 0.4f);
        RemoveCollider(glassDome);
        // 돔 밑동 금 링
        Cyl(root.transform, "DomeGoldRing", new Vector3(0f, domeTopY, 0f), new Vector3(19.6f, 0.6f, 19.6f), Gold);

        // 정상 랜턴(큐폴라) — 지식의 등불
        float lanternY = domeTopY + 6.2f;
        Cyl(root.transform, "DomeLantern", new Vector3(0f, lanternY, 0f), new Vector3(2.6f, 2.6f, 2.6f), StoneLight);
        var lanternTip = Sphere(root.transform, "DomeLanternTip", new Vector3(0f, lanternY + 1.8f, 0f), Vector3.one * 2.0f, GoldBright);
        SetEmissive(lanternTip, GoldBright, GoldBright * 0.7f);
        RemoveCollider(lanternTip);

        // ── 측면 보조탑 (오른쪽 뒤쪽, 슬림하게 — 주 실루엣은 중앙 돔이 담당) ──
        float towerX =  W * 0.58f;
        float towerZ =  D * 0.28f;
        float towerH = H + 6f;
        Cyl(root.transform, "TowerCyl", new Vector3(towerX, towerH * 0.5f, towerZ), new Vector3(3f, towerH, 3f), StoneDark);
        Cyl(root.transform, "TowerCap", new Vector3(towerX, towerH + 0.3f, towerZ), new Vector3(4f, 0.5f, 4f), StoneCold);

        // ── 정면 펼친 책 엠블럼 (배너 대체 — 책 모티프로 도서관 정체성 강조) ──
        float bookY = H - 1.5f;
        float bookZ = -(D * 0.5f + 0.05f);
        Box(root.transform, "BookCover", new Vector3(0f, bookY, bookZ), new Vector3(W * 0.36f, H * 0.13f, 0.15f), StoneLight);
        var spine = Box(root.transform, "BookSpine",
            new Vector3(0f, bookY, bookZ - 0.04f), new Vector3(0.5f, H * 0.13f + 0.1f, 0.12f), BlueTeal);
        SetEmissive(spine, BlueTeal, BlueTeal * 0.35f);
        // 펼쳐진 페이지 결 (좌/우 대각선)
        foreach (float sideSign in new[] { -1f, 1f })
        {
            for (int p = 0; p < 3; p++)
            {
                float px2 = sideSign * (0.9f + p * (W * 0.11f));
                var page = Box(root.transform, $"BookPageLine_{sideSign}_{p}",
                    new Vector3(px2, bookY, bookZ - 0.02f), new Vector3(0.10f, H * 0.11f, 0.08f), StoneDark);
                page.transform.localRotation = Quaternion.Euler(0f, 0f, sideSign * -6f);
            }
        }

        // ── 입구 문 ──
        Box(root.transform, "DoorCut", new Vector3(0f, H * 0.30f, -(D * 0.5f + 0.05f)),
            new Vector3(W * 0.18f, H * 0.60f, 0.4f), StoneDark);

        // ── 내부 입장 훅 ──────────────────────────────────────────────────────
        // 향후 ScenePortal을 이 GO에 붙이고 WirePortal(ia, sp, "LibraryInterior", "LibraryReturnSpawn")
        var entrance = new GameObject("LibraryEntrance");
        entrance.transform.SetParent(root.transform, false);
        entrance.transform.localPosition = new Vector3(0f, 0f, -(D * 0.5f + 1.5f)); // 문 바로 앞

        // 씬 루트에 복귀 스폰 포인트 배치 (SpawnManager가 _PortalSpawn 키로 찾음)
        var returnSpawn = new GameObject("LibraryReturnSpawn");
        returnSpawn.transform.position = pos + Quaternion.Euler(0f, rotY, 0f) * new Vector3(0f, 0.5f, -(D * 0.5f + 5f));
        // ─────────────────────────────────────────────────────────────────────

        var mm = root.AddComponent<MapMarker>();
        mm.kind        = MapMarker.IconKind.Building;
        mm.displayName = display;
        mm.iconColor   = BlueTeal;
        mm.footprintW  = W;
        mm.footprintD  = D;
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// 모험가 길드 본부 (HubGuildHall) — 눈에 띄는 랜드마크.
    ///   · 넓은 석벽 본체 + 중앙 종탑 (본체보다 높음)
    ///   · 길드 엠블럼 (방패형 + X자 교차 장식) + 발광 배너
    ///   · 입구 양옆 횃불
    ///   · 내부 입장 훅: 자식 GO "GuildEntrance" + 씬 루트 "GuildReturnSpawn"
    ///     → 향후 ScenePortal.targetScene = "GuildInterior" 로 연결
    /// </summary>
    static void GuildHall(Vector3 pos, float rotY)
    {
        const float W  = 34f;
        const float H  = 16f;
        const float D  = 24f;
        const float TH = 28f;  // 종탑 높이

        var TorchOrange = new Color(1.0f, 0.45f, 0.05f); // 횃불 발광색

        var root = new GameObject("HubGuildHall");
        root.transform.position = pos;
        root.transform.rotation = Quaternion.Euler(0f, rotY, 0f);

        var ia = root.AddComponent<Interactable>();
        ia.displayName = "모험가 길드 본부";
        ia.promptText  = "[E]  모험가 길드";
        ia.radius      = 12f;

        // ── 기단 + 계단 ──
        Box(root.transform, "Foundation", new Vector3(0f, -0.4f, 0f), new Vector3(W + 6f, 0.8f, D + 6f), StoneDark);
        float fz = D * 0.5f;
        Box(root.transform, "Step1", new Vector3(0f, 0.20f, -(fz + 2.8f)), new Vector3(W * 0.55f, 0.40f, 2.8f), StoneWarm);
        Box(root.transform, "Step2", new Vector3(0f, 0.58f, -(fz + 1.2f)), new Vector3(W * 0.50f, 0.40f, 2.2f), StoneCold);
        Box(root.transform, "Step3", new Vector3(0f, 0.95f, -(fz + 0.2f)), new Vector3(W * 0.45f, 0.40f, 1.2f), StoneWarm);

        // ── 본체 (StoneCold 석벽) ──
        Box(root.transform, "Body", new Vector3(0f, H * 0.5f + 0.2f, 0f), new Vector3(W, H, D), StoneCold);

        // ── 정면 목재 빔 (수평 3줄 + 수직 2줄) ──
        float[] beamYs = { H * 0.25f + 0.2f, H * 0.52f + 0.2f, H * 0.78f + 0.2f };
        foreach (float by in beamYs)
            Box(root.transform, "BeamH", new Vector3(0f, by, -(D * 0.5f + 0.05f)), new Vector3(W + 0.2f, 0.6f, 0.3f), TimberBrown);
        float[] beamXs = { -W * 0.30f, W * 0.30f };
        foreach (float bx in beamXs)
            Box(root.transform, "BeamV", new Vector3(bx, H * 0.5f + 0.2f, -(D * 0.5f + 0.05f)), new Vector3(0.4f, H + 0.4f, 0.3f), TimberBrown);

        // ── 대각 목재 브레이스 (거친 목조 아지트 질감) ──
        var braceA = Box(root.transform, "BraceX1",
            new Vector3(-W * 0.15f, H * 0.5f + 0.2f, -(D * 0.5f + 0.08f)), new Vector3(0.35f, H * 0.85f, 0.25f), TimberBrown);
        braceA.transform.localRotation = Quaternion.Euler(0f, 0f, 24f);
        var braceB = Box(root.transform, "BraceX2",
            new Vector3(W * 0.15f, H * 0.5f + 0.2f, -(D * 0.5f + 0.08f)), new Vector3(0.35f, H * 0.85f, 0.25f), TimberBrown);
        braceB.transform.localRotation = Quaternion.Euler(0f, 0f, -24f);

        // ── 박공 지붕 ──
        float roofH = H * 0.40f;
        float roofY = H + 0.2f + roofH * 0.5f;
        Box(root.transform, "RoofBase",    new Vector3(0f, H + 0.2f, 0f),       new Vector3(W + 2f, 0.5f, D + 2f), TileRed);
        Box(root.transform, "RoofSlope_F", new Vector3(0f, roofY, -(D * 0.28f)), new Vector3(W + 1f, roofH, 1.0f), TileRed);
        Box(root.transform, "RoofSlope_B", new Vector3(0f, roofY,  (D * 0.28f)), new Vector3(W + 1f, roofH, 1.0f), TileRed);
        Box(root.transform, "RoofPeak",    new Vector3(0f, H + 0.2f + roofH, 0f), new Vector3(W + 0.5f, 0.6f, 1.2f), TimberBrown);

        // ── 과목색 깃발(펜넌트) — 다양한 모험가들이 모여드는 활기 표현 ──
        float ridgeY = H + 0.2f + roofH;
        float[] flagXs = { -W * 0.32f, -W * 0.12f, W * 0.08f, W * 0.26f, W * 0.40f };
        for (int i = 0; i < flagXs.Length; i++)
        {
            var flagColor = PortalColors[i % PortalColors.Length];
            float poleH = 3.5f + (i % 2) * 0.8f;
            Cyl(root.transform, $"FlagPole_{i}", new Vector3(flagXs[i], ridgeY + poleH * 0.5f, 0f), new Vector3(0.25f, poleH, 0.25f), TimberBrown);
            var pennant = Box(root.transform, $"Pennant_{i}",
                new Vector3(flagXs[i] + 0.9f, ridgeY + poleH - 0.6f, 0f), new Vector3(1.8f, 1.0f, 0.06f), flagColor);
            SetEmissive(pennant, flagColor, flagColor * 0.25f);
            pennant.transform.localRotation = Quaternion.Euler(0f, 12f, 0f);
        }

        // ── 중앙 종탑 ──
        float twX = 0f;
        float twZ = D * 0.10f; // 약간 뒤쪽
        Box(root.transform, "TowerBase", new Vector3(twX, TH * 0.3f, twZ), new Vector3(8f, TH * 0.6f, 8f), StoneCold);
        Box(root.transform, "TowerUpper", new Vector3(twX, TH * 0.75f, twZ), new Vector3(6.5f, TH * 0.3f, 6.5f), StoneDark);
        // 종탑 지붕 (피라미드형) — 4개의 기울어진 박스로 표현
        float spireY = TH * 1.05f;
        Box(root.transform, "TowerRoof_F", new Vector3(twX, spireY, twZ - 2.0f), new Vector3(6.5f, TH * 0.18f, 1.0f), TileRed);
        Box(root.transform, "TowerRoof_B", new Vector3(twX, spireY, twZ + 2.0f), new Vector3(6.5f, TH * 0.18f, 1.0f), TileRed);
        // 꼭대기 금 첨탑
        Cyl(root.transform, "TowerSpire", new Vector3(twX, TH * 1.25f, twZ), new Vector3(0.8f, TH * 0.22f, 0.8f), Gold);
        var spireTop = Sphere(root.transform, "TowerSpireTop", new Vector3(twX, TH * 1.38f, twZ), Vector3.one * 1.5f, GoldBright);
        SetEmissive(spireTop, GoldBright, GoldBright * 0.5f);
        RemoveCollider(spireTop);
        // 탑 코너 가장자리 장식
        foreach (var (ox, oz) in new (float, float)[] { (-3.5f,-3.5f),(-3.5f,3.5f),(3.5f,-3.5f),(3.5f,3.5f) })
            Cyl(root.transform, $"TowerCorner",
                new Vector3(twX + ox, TH * 0.5f, twZ + oz), new Vector3(0.8f, TH * 0.6f + 2f, 0.8f), StoneDark);

        // ── 길드 엠블럼 (정면 중앙 방패 + X자 교차) ──
        float emblemY = H * 0.55f;
        float emblemZ = -(D * 0.5f + 0.1f);
        // 방패형 박스
        Box(root.transform, "EmblemShield", new Vector3(0f, emblemY, emblemZ),
            new Vector3(5.5f, 6.5f, 0.3f), StoneDark);
        // X자 교차 장식
        var cross1 = Box(root.transform, "EmblemCross1",
            new Vector3(0f, emblemY, emblemZ - 0.1f), new Vector3(0.6f, 5.5f, 0.25f), Gold);
        SetEmissive(cross1, Gold, Gold * 0.3f);
        var cross2 = Box(root.transform, "EmblemCross2",
            new Vector3(0f, emblemY, emblemZ - 0.1f), new Vector3(5.5f, 0.6f, 0.25f), Gold);
        SetEmissive(cross2, Gold, Gold * 0.3f);

        // ── 발광 배너 ──
        var banner = Box(root.transform, "Banner",
            new Vector3(0f, H * 0.90f, -(D * 0.5f + 0.05f)),
            new Vector3(W * 0.40f, H * 0.15f, 0.15f), Gold);
        SetEmissive(banner, Gold, Gold * 0.22f);

        // ── 현수 방패 간판 (아지트/여관 간판 느낌) ──
        float signZ = -(D * 0.5f + 0.3f);
        Box(root.transform, "SignBracket", new Vector3(0f, H * 0.62f, signZ), new Vector3(0.25f, 0.25f, 1.4f), TimberBrown);
        Cyl(root.transform, "SignChain", new Vector3(0f, H * 0.55f, signZ + 0.65f), new Vector3(0.08f, 0.7f, 0.08f), StoneDark);
        Box(root.transform, "SignPlate", new Vector3(0f, H * 0.46f, signZ + 0.65f), new Vector3(2.0f, 2.4f, 0.2f), StoneCold);
        var signTrim = Box(root.transform, "SignTrim", new Vector3(0f, H * 0.46f, signZ + 0.53f), new Vector3(1.6f, 2.0f, 0.05f), Gold);
        SetEmissive(signTrim, Gold, Gold * 0.3f);

        // ── 정면 기둥 ──
        float px = W * 0.30f;
        Pillar(root.transform, "EntryPillar_LL", new Vector3(-px * 1.5f, 0f, -(D * 0.5f - 0.5f)), StoneDark);
        Pillar(root.transform, "EntryPillar_L",  new Vector3(-px * 0.5f, 0f, -(D * 0.5f - 0.5f)), StoneDark);
        Pillar(root.transform, "EntryPillar_R",  new Vector3( px * 0.5f, 0f, -(D * 0.5f - 0.5f)), StoneDark);
        Pillar(root.transform, "EntryPillar_RR", new Vector3( px * 1.5f, 0f, -(D * 0.5f - 0.5f)), StoneDark);

        // ── 입구 횃불 (양옆) ──
        foreach (float tx in new[]{ -px * 1.9f, px * 1.9f })
        {
            Cyl(root.transform, "TorchPole", new Vector3(tx, 3.5f, -(D * 0.5f - 0.3f)), new Vector3(0.35f, 7f, 0.35f), TimberBrown);
            var flame = Sphere(root.transform, "TorchFlame", new Vector3(tx, 7.5f, -(D * 0.5f - 0.3f)), Vector3.one * 1.0f, TorchOrange);
            SetEmissive(flame, TorchOrange, TorchOrange * 0.8f);
            RemoveCollider(flame);
        }

        // ── 의뢰 게시판 (문 왼편) — 모험가 길드의 핵심 상징 ──
        float qbX = -px * 1.3f;
        float qbZ = -(D * 0.5f - 0.15f);
        Box(root.transform, "QuestBoardFrame", new Vector3(qbX, 3.2f, qbZ), new Vector3(3.4f, 4.2f, 0.35f), TimberBrown);
        Box(root.transform, "QuestBoardPanel", new Vector3(qbX, 3.2f, qbZ - 0.06f), new Vector3(2.9f, 3.6f, 0.10f), StoneWarm);
        var noticeColors = new[] { StoneLight, PlasterCream, StoneLight, PlasterCream };
        for (int i = 0; i < noticeColors.Length; i++)
        {
            float nx = qbX + ((i % 2 == 0) ? -0.7f : 0.7f);
            float ny = 4.0f - (i / 2) * 1.4f;
            var notice = Box(root.transform, $"QuestNotice_{i}", new Vector3(nx, ny, qbZ - 0.12f), new Vector3(0.9f, 1.1f, 0.05f), noticeColors[i]);
            notice.transform.localRotation = Quaternion.Euler(0f, 0f, (i % 2 == 0 ? 1f : -1f) * 5f);
        }

        // ── 무기 거치대 (문 오른편) ──
        float wrX = px * 1.3f;
        float wrZ = -(D * 0.5f - 0.15f);
        Box(root.transform, "WeaponRackFrame", new Vector3(wrX, 2.6f, wrZ), new Vector3(2.6f, 0.3f, 0.3f), TimberBrown);
        Cyl(root.transform, "WeaponRackPostL", new Vector3(wrX - 1.1f, 1.5f, wrZ), new Vector3(0.25f, 3.0f, 0.25f), TimberBrown);
        Cyl(root.transform, "WeaponRackPostR", new Vector3(wrX + 1.1f, 1.5f, wrZ), new Vector3(0.25f, 3.0f, 0.25f), TimberBrown);
        for (int i = 0; i < 3; i++)
        {
            float swX = wrX - 0.7f + i * 0.7f;
            var hilt = Box(root.transform, $"SwordHilt_{i}", new Vector3(swX, 1.1f, wrZ + 0.1f), new Vector3(0.18f, 0.6f, 0.18f), TimberBrown);
            hilt.transform.localRotation = Quaternion.Euler(0f, 0f, (i - 1) * 10f);
            var blade = Box(root.transform, $"SwordBlade_{i}", new Vector3(swX, 2.4f, wrZ + 0.1f), new Vector3(0.12f, 2.6f, 0.05f), StoneCold);
            blade.transform.localRotation = Quaternion.Euler(0f, 0f, (i - 1) * 10f);
        }
        var shield = Cyl(root.transform, "RackShield", new Vector3(wrX, 3.6f, wrZ + 0.15f), new Vector3(1.6f, 0.15f, 1.6f), StoneCold);
        shield.transform.localRotation = Quaternion.Euler(90f, 0f, 8f);
        var shieldBoss = Sphere(root.transform, "RackShieldBoss", new Vector3(wrX, 3.6f, wrZ + 0.05f), Vector3.one * 0.5f, Gold);
        SetEmissive(shieldBoss, Gold, Gold * 0.3f);
        RemoveCollider(shieldBoss);

        // ── 입구 문 ──
        Box(root.transform, "DoorCut", new Vector3(0f, H * 0.32f, -(D * 0.5f + 0.05f)),
            new Vector3(W * 0.18f, H * 0.64f, 0.4f), StoneDark);

        // ── 내부 입장 훅 ──────────────────────────────────────────────────────
        // 향후 ScenePortal을 이 GO에 붙이고 WirePortal(ia, sp, "GuildInterior", "GuildReturnSpawn")
        var entrance = new GameObject("GuildEntrance");
        entrance.transform.SetParent(root.transform, false);
        entrance.transform.localPosition = new Vector3(0f, 0f, -(D * 0.5f + 1.5f));

        // 씬 루트에 복귀 스폰 포인트 배치
        var returnSpawn = new GameObject("GuildReturnSpawn");
        returnSpawn.transform.position = pos + Quaternion.Euler(0f, rotY, 0f) * new Vector3(0f, 0.5f, -(D * 0.5f + 5f));
        // ─────────────────────────────────────────────────────────────────────

        var mm = root.AddComponent<MapMarker>();
        mm.kind        = MapMarker.IconKind.Building;
        mm.displayName = "모험가 길드";
        mm.iconColor   = Gold;
        mm.footprintW  = W;
        mm.footprintD  = D;
    }

    // ── 8. 길드 가로 (HubGuildRow, z≈-60~-110) ──────────────────────────────
    static void BuildGuildRow()
    {
        var root = new GameObject("HubGuildRow");

        // 모험가 길드 본부 (HubGuildHall) — 서쪽 길드 구역 랜드마크 (중앙 도로 비움)
        // 향후 상호작용: WireBuilding("HubGuildHall", controller, "OpenGuild") 또는
        //               WirePortal → ScenePortal("GuildInterior", "GuildReturnSpawn")
        GuildHall(new Vector3(-110f, 0f, -80f), 90f);

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

            float[] zOffsets = { zMin+12f, zMin+30f, zMin+48f, zMin+66f, zMin+84f };
            float[] xOffsets = { 42f, 72f, 108f };  // BUILDING_R=130 바깥: max 108+jitter≈116 < 130

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

    // ── 핵심 4건물 래퍼 ─────────────────────────────────────────────────────────
    // 서/지식 — 통합 학술원 (HubLab) : MetaUISetup.WireBuilding("HubLab",…,"OpenLab") 자동 연결
    static void BuildAcademy()
    {
        // E/W 횡단로(z=0) 북쪽에 위치, rotY=0: 로컬 -Z → 월드 -Z (남쪽/횡단로 방향)
        AcademyBuilding("HubLab", "통합 학술원", "[E]  통합 학술원",
            new Vector3(-105f, 0f, 35f), 0f);
    }

    // 서/지식 — 도서관 (HubLibrary) : MetaUISetup.WireBuilding("HubLibrary",…,"OpenLibrary") 자동 연결
    static void BuildLibrary()
    {
        // E/W 횡단로(z=0) 남쪽에 위치, rotY=180: 로컬 -Z → 월드 +Z (북쪽/횡단로 방향)
        LibraryBuilding("HubLibrary", "도서관", "[E]  도서관",
            new Vector3(-105f, 0f, -35f), 180f);
    }

    // 동/상업 — 지식의 거래소 (HubExchange) : 향후 상점 연결 예정
    static void BuildExchange()
    {
        // E/W 횡단로(z=0) 북쪽에 위치, rotY=0: 로컬 -Z → 월드 -Z (남쪽/횡단로 방향)
        ExchangeBuilding("HubExchange", "지식의 거래소", "[E]  지식의 거래소",
            new Vector3(105f, 0f, 35f), 0f);
    }

    // 남/생활 — 모험가 길드 본부 (HubGuildHall) : 내부 훅만(즉시 연결 없음)
    // 척추 도로(x=0, 폭30) 서쪽에 배치. 동쪽(+x)은 여관·식당 확장 슬롯.
    static void BuildGuildHall()
    {
        // rotY=-90: 로컬 -Z → 월드 +X (동쪽/척추 도로 방향)
        GuildHall(new Vector3(-40f, 0f, -85f), -90f);
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

        // 성벽 (동/서 방향) — GROUND_HALF=600에 맞게 확장
        Box(root.transform, "WallW", new Vector3(-310f, 7f, 0f), new Vector3(580f, 14f, 2.5f), StoneDark);
        Box(root.transform, "WallE", new Vector3( 310f, 7f, 0f), new Vector3(580f, 14f, 2.5f), StoneDark);

        // 성벽 흉벽 (크레넬레이션) — 16단위 간격, 성문 개구부(±22) 제외
        for (float mx = -595f; mx <= -25f; mx += 16f)
            Box(root.transform, $"MerlW_{(int)(mx + 600f)}", new Vector3(mx, 14.5f, 0f), new Vector3(5f, 2f, 3f), StoneCold);
        for (float mx = 25f; mx <= 595f; mx += 16f)
            Box(root.transform, $"MerlE_{(int)(mx + 600f)}", new Vector3(mx, 14.5f, 0f), new Vector3(5f, 2f, 3f), StoneCold);

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

    // ── 도로 세그먼트 헬퍼 ───────────────────────────────────────────────────────

    // a → b 를 잇는 임의 방향 도로 판. 콜라이더 제거(지면 충돌은 HubGround 담당).
    // 길이는 두 점 사이 정확한 거리 — 오버슈트 없음(꼭짓점 이음새는 RoadJoint가 담당).
    static GameObject RoadSeg(Transform parent, string name,
        Vector2 a, Vector2 b, float width, float y, float thickness, Color c)
    {
        Vector2 d    = b - a;
        float   len  = d.magnitude;
        Vector2 mid  = (a + b) * 0.5f;
        float   rotY = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg; // +Z 기준
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(mid.x, y, mid.y);
        go.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
        go.transform.localScale    = new Vector3(width, thickness, len);
        ApplyColor(go, c);
        RemoveCollider(go);
        return go;
    }

    // 도로 꼭짓점 이음새 패치 — 두 도로판이 각도를 이루며 만나는 지점의 미터 갭을
    // 정사각형으로 덮어 매끈하게 연결(오버슈트로 인한 돌출 없이).
    static GameObject RoadJoint(Transform parent, string name,
        Vector2 pos, float size, float y, float thickness, Color c)
    {
        var go = Box(parent, name,
            new Vector3(pos.x, y, pos.y), new Vector3(size, thickness, size), c);
        RemoveCollider(go);
        return go;
    }

    // 반지름 R 원을 sides 각형으로 근사한 환상로. 각 변 + 꼭짓점 이음새 패치를 함께 생성.
    static void RingRoad(Transform parent, float R, int sides,
        float width, float y, float thickness, Color c)
    {
        for (int i = 0; i < sides; i++)
        {
            float a0 = i       * Mathf.PI * 2f / sides;
            float a1 = (i + 1) * Mathf.PI * 2f / sides;
            var pa = new Vector2(R * Mathf.Sin(a0), R * Mathf.Cos(a0));
            var pb = new Vector2(R * Mathf.Sin(a1), R * Mathf.Cos(a1));
            RoadSeg(parent, $"Ring{(int)R}_{i}", pa, pb, width, y, thickness, c);
            RoadJoint(parent, $"Ring{(int)R}_Joint{i}", pa, width * 1.6f, y, thickness, c);
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
