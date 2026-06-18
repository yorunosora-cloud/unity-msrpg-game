using UnityEngine;
using UnityEditor;

/// <summary>
/// 메조리아 시티 1: 실크로드 중앙 광장 (Silk Road Central Plaza)
/// 콘셉트: 콘스탄티노플 + 실크로드 + 지중해 항구 대륙 도시
///
/// 레이아웃 (남→북):
///   스폰(0,0,-100) → 남쪽 성문 → 종합길드 → 포탈 링(반지름 40) → 분수(중앙) → 항구(z≈165)
///   서쪽(x≈-87): 통합 학술원 / 동쪽(x≈+87): 지식의 거래소
///
/// 루트 GO 이름 고정 (MetaUISetup.WireBuilding 이름 기반 탐색):
///   HubLab, HubLibrary, HubGuildHall, HubPortal_*
/// 프리팹 없으면 절차적 폴백 → 씬 생성 절대 안 깨짐.
/// </summary>
public static class MesoriaHubBuilder
{
    // ── 프리팹 경로 ──────────────────────────────────────────────────────────
    const string BASE    = "Assets/Synty/PolygonGeneric/Prefabs/Base/";         // SM_Bld_Base_*
    const string BLD     = "Assets/Synty/PolygonGeneric/Prefabs/Building/";     // SM_Gen_Bld_Background_*
    const string ENV     = "Assets/Synty/PolygonGeneric/Prefabs/Environment/";  // SM_Gen_Env_*
    const string PROPS   = "Assets/Synty/PolygonGeneric/Prefabs/Props/";        // SM_Gen_Prop_*
    const string STARTER = "Assets/Synty/PolygonStarter/Prefabs/";              // SM_Generic_*, SM_PolygonPrototype_*

    // 건물 모듈 (Base/)
    const string P_PILLAR_A   = BASE + "SM_Bld_Base_Pillar_01.prefab";
    const string P_PILLAR_C   = BASE + "SM_Bld_Base_Pillar_05.prefab";

    // 배경 건물 실루엣 (Building/)
    const string P_BG_A = BLD + "SM_Gen_Bld_Background_01.prefab";
    const string P_BG_B = BLD + "SM_Gen_Bld_Background_03.prefab";
    const string P_BG_C = BLD + "SM_Gen_Bld_Background_05.prefab";
    const string P_BG_D = BLD + "SM_Gen_Bld_Background_07.prefab";
    const string P_BG_E = BLD + "SM_Gen_Bld_Background_09.prefab";

    // 환경 (Environment/)
    const string P_WATER     = ENV + "SM_Gen_Env_Water_Plane_01.prefab";
    const string P_TREE_A    = ENV + "SM_Gen_Env_Tree_01.prefab";
    const string P_TREE_B    = ENV + "SM_Gen_Env_Tree_02.prefab";
    const string P_TREE_C    = ENV + "SM_Gen_Env_Tree_03.prefab";
    const string P_ROCK_A    = ENV + "SM_Gen_Env_Rock_01.prefab";
    const string P_ROCK_B    = ENV + "SM_Gen_Env_Rock_03.prefab";
    const string P_BUSH_A    = ENV + "SM_Gen_Env_Bush_01.prefab";
    const string P_MOUNTAIN  = ENV + "SM_Gen_Env_Mountain_01.prefab";
    const string P_MOUNTAIN2 = ENV + "SM_Gen_Env_Mountain_02.prefab";
    const string P_MOUNTAIN3 = ENV + "SM_Gen_Env_Mountain_03.prefab";

    // 소품 (Props/)
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
    static readonly Color StoneWarm  = new Color(0.82f, 0.75f, 0.60f);
    static readonly Color StoneCold  = new Color(0.70f, 0.72f, 0.75f);
    static readonly Color StoneDark  = new Color(0.38f, 0.34f, 0.27f);
    static readonly Color Gold       = new Color(0.78f, 0.66f, 0.25f);
    static readonly Color GoldBright = new Color(0.98f, 0.86f, 0.32f);
    static readonly Color SeaBlue    = new Color(0.18f, 0.45f, 0.72f);
    static readonly Color WaterBlue  = new Color(0.32f, 0.68f, 0.92f);
    static readonly Color DockWood   = new Color(0.52f, 0.36f, 0.18f);

    static readonly Color[] PortalColors =
    {
        new Color(0.17f, 0.50f, 1.00f),  // 물리     - 파랑
        new Color(1.00f, 0.09f, 0.27f),  // 화학     - 빨강
        new Color(0.24f, 0.77f, 0.15f),  // 생명과학 - 초록
        new Color(0.77f, 0.36f, 0.13f),  // 지구과학 - 갈색
        new Color(0.90f, 0.82f, 0.00f),  // 수학     - 황금
        new Color(0.67f, 0.00f, 1.00f),  // 정보     - 보라
    };
    static readonly string[] PortalLabels =
        { "물리", "화학", "생명과학", "지구과학", "수학", "정보" };

    // ── 레이아웃 상수 ─────────────────────────────────────────────────────────
    const float PORTAL_R    = 40f;    // 포탈 링 반지름
    const float BAZAAR_R    = 63f;    // 바자르 노점 반지름
    const float BUILDING_R  = 87f;    // 주요 건물 반지름
    const float HARBOR_Z    = 165f;   // 항구 부두 Z
    const float GROUND_HALF = 250f;   // 지면 반폭 → 500×500

    // ─────────────────────────────────────────────────────────────────────────
    public static void Build()
    {
        BuildGround();
        BuildFountain();
        BuildPortalRing();
        BuildMainBuildings();
        BuildKnowledgeBazaar();
        BuildGrandAvenue();
        BuildHarbor();
        BuildBackgroundCity();
        BuildScenery();
        BuildWalls();
        AdjustLighting();
    }

    // ── 1. 지면 (500×500) ────────────────────────────────────────────────────
    static void BuildGround()
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Plane);
        g.name = "HubGround";
        g.transform.localScale = new Vector3(GROUND_HALF * 0.1f, 1f, GROUND_HALF * 0.1f);
        ApplyColor(g, StoneWarm);
    }

    // ── 2. 중앙 분수 (3단 비잔틴) ────────────────────────────────────────────
    static void BuildFountain()
    {
        var root = new GameObject("HubFountain");

        // 1단 기반
        Cyl(root.transform, "Base_Ring",  new Vector3(0f, 0.20f, 0f), new Vector3(16f, 0.20f, 16f), StoneWarm);
        Cyl(root.transform, "Basin1",     new Vector3(0f, 0.60f, 0f), new Vector3(14f, 0.80f, 14f), StoneCold);
        Cyl(root.transform, "Basin1_Rim", new Vector3(0f, 1.35f, 0f), new Vector3(14f, 0.20f, 14f), StoneDark);
        var w1 = Cyl(root.transform, "Water1", new Vector3(0f, 1.40f, 0f), new Vector3(12.5f, 0.05f, 12.5f), WaterBlue);
        SetEmissive(w1, WaterBlue, WaterBlue * 0.55f); RemoveCollider(w1);

        // 중심 기둥 1
        Cyl(root.transform, "Column1", new Vector3(0f, 0.50f, 0f), new Vector3(1.6f, 7.0f, 1.6f), Gold);

        // 1단 장식: 동상 4기 (45° 간격)
        for (int i = 0; i < 4; i++)
        {
            float a = i * 90f * Mathf.Deg2Rad;
            var p = new Vector3(Mathf.Sin(a) * 6.5f, 0f, Mathf.Cos(a) * 6.5f);
            Prop(root.transform, P_PLINTH,   p + new Vector3(0f, 0.25f, 0f), Vector3.one * 0.85f, $"Plinth_{i}");
            Prop(root.transform, P_STATUE_A, p + new Vector3(0f, 1.40f, 0f), Vector3.one * 0.75f, $"Statue_{i}");
        }

        // 2단 분지
        Cyl(root.transform, "Basin2",     new Vector3(0f, 7.20f, 0f), new Vector3(8.5f, 0.70f, 8.5f), StoneCold);
        Cyl(root.transform, "Basin2_Rim", new Vector3(0f, 7.75f, 0f), new Vector3(8.5f, 0.20f, 8.5f), StoneDark);
        var w2 = Cyl(root.transform, "Water2", new Vector3(0f, 7.80f, 0f), new Vector3(7.2f, 0.05f, 7.2f), WaterBlue);
        SetEmissive(w2, WaterBlue, WaterBlue * 0.45f); RemoveCollider(w2);

        // 중심 기둥 2
        Cyl(root.transform, "Column2", new Vector3(0f, 8.00f, 0f), new Vector3(0.9f, 4.5f, 0.9f), Gold);

        // 3단 분지
        Cyl(root.transform, "Basin3",     new Vector3(0f, 12.50f, 0f), new Vector3(4.2f, 0.50f, 4.2f), StoneCold);
        Cyl(root.transform, "Basin3_Rim", new Vector3(0f, 12.85f, 0f), new Vector3(4.2f, 0.15f, 4.2f), StoneDark);
        var w3 = Cyl(root.transform, "Water3", new Vector3(0f, 12.90f, 0f), new Vector3(3.2f, 0.05f, 3.2f), WaterBlue);
        SetEmissive(w3, WaterBlue, WaterBlue * 0.35f); RemoveCollider(w3);

        // 정상 황금 구슬
        var orb = Sphere(root.transform, "TopOrb", new Vector3(0f, 14.2f, 0f), Vector3.one * 2.2f, Gold);
        SetEmissive(orb, Gold, GoldBright * 0.7f); RemoveCollider(orb);
    }

    // ── 3. 6대륙 포탈 링 (육각형, 반지름 40) ─────────────────────────────────
    static void BuildPortalRing()
    {
        for (int i = 0; i < 6; i++)
        {
            float deg = i * 60f;
            float rad = deg * Mathf.Deg2Rad;
            var   pos = new Vector3(Mathf.Sin(rad) * PORTAL_R, 0f, Mathf.Cos(rad) * PORTAL_R);
            float ry  = (deg + 180f) % 360f; // 분수 방향(안쪽) 바라봄
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
        ia.promptText  = $"[E]  {name} 대륙으로 이동";
        ia.radius      = 7f;

        // 발광 바닥 패드
        var pad = Cyl(root.transform, "Pad", new Vector3(0f, 0.15f, 0f), new Vector3(6f, 0.15f, 6f), pc);
        SetEmissive(pad, pc, pc * 0.55f); RemoveCollider(pad);

        // 계단 (포탈 앞, 바깥쪽 = local -Z)
        Box(root.transform, "Step1", new Vector3(0f, 0.10f, -5.5f), new Vector3(4.5f, 0.20f, 1.2f), StoneWarm);
        Box(root.transform, "Step2", new Vector3(0f, 0.25f, -4.5f), new Vector3(4.2f, 0.20f, 1.0f), StoneWarm);
        Box(root.transform, "Step3", new Vector3(0f, 0.40f, -3.6f), new Vector3(4.0f, 0.20f, 0.8f), StoneCold);

        // 기둥 4개
        Pillar(root.transform, "PillarL_In",  new Vector3(-2.6f, 0f, 0f), StoneWarm);
        Pillar(root.transform, "PillarL_Out", new Vector3(-4.4f, 0f, 0f), StoneCold);
        Pillar(root.transform, "PillarR_In",  new Vector3( 2.6f, 0f, 0f), StoneWarm);
        Pillar(root.transform, "PillarR_Out", new Vector3( 4.4f, 0f, 0f), StoneCold);

        // 상단 아치 빔 (대륙 색)
        Box(root.transform, "ArchBeam", new Vector3(0f, 9.5f, 0f), new Vector3(10.5f, 1.2f, 1.0f), pc);

        // 발광 포탈 패널
        var glow = Cube(root.transform, "GlowPanel",
            new Vector3(0f, 4.6f, 0.15f), new Vector3(4.5f, 7.5f, 0.08f));
        SetEmissive(glow, new Color(pc.r, pc.g, pc.b, 0.9f), pc * 0.6f);

        // 포탈 최상단 장식 (금색 사다리꼴형 큐브)
        var top = Box(root.transform, "TopDeco", new Vector3(0f, 11.2f, 0f), new Vector3(3.2f, 1.5f, 0.7f), Gold);
        SetEmissive(top, Gold, GoldBright * 0.25f);
    }

    // ── 4. 주요 건물 3기 ─────────────────────────────────────────────────────
    static void BuildMainBuildings()
    {
        // 종합 길드 본부: 남쪽 입구 (플레이어가 처음 만남)
        ByzantineBuilding("HubGuildHall", "종합 길드 본부", "[E]  종합 길드",
            new Vector3(0f, 0f, -BUILDING_R), 0f,
            w: 22f, h: 11f, d: 16f, wall: StoneCold, dome: Gold);

        // 통합 학술원: 서쪽
        ByzantineBuilding("HubLab", "통합 학술원", "[E]  통합 학술원",
            new Vector3(-BUILDING_R, 0f, 10f), 90f,
            w: 24f, h: 13f, d: 18f, wall: StoneWarm, dome: Gold);

        // 지식의 거래소: 동쪽
        ByzantineBuilding("HubLibrary", "지식의 거래소", "[E]  지식의 거래소",
            new Vector3(BUILDING_R, 0f, 10f), -90f,
            w: 24f, h: 13f, d: 18f, wall: StoneWarm, dome: GoldBright);
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

        // 기단
        Box(root.transform, "Foundation", new Vector3(0f, -0.4f, 0f), new Vector3(w + 4f, 0.8f, d + 4f), StoneDark);

        // 계단 3단 (입구 = local -Z)
        float fz = d * 0.5f;
        Box(root.transform, "Step1", new Vector3(0f, 0.20f, -(fz + 1.8f)), new Vector3(w * 0.60f, 0.40f, 2.0f), StoneWarm);
        Box(root.transform, "Step2", new Vector3(0f, 0.55f, -(fz + 0.7f)), new Vector3(w * 0.55f, 0.40f, 1.6f), StoneCold);
        Box(root.transform, "Step3", new Vector3(0f, 0.90f, -(fz + 0.1f)), new Vector3(w * 0.50f, 0.40f, 0.9f), StoneWarm);

        // 건물 본체
        Box(root.transform, "Body", new Vector3(0f, h * 0.5f + 0.2f, 0f), new Vector3(w, h, d), wall);

        // 입구 아치 오목 (어두운 색으로 표현)
        Box(root.transform, "DoorCut", new Vector3(0f, h * 0.33f, -(d * 0.5f + 0.05f)),
            new Vector3(w * 0.22f, h * 0.65f, 0.4f), StoneDark);

        // 비잔틴 돔 (받침 실린더 + 황금 구)
        var dBase = Cyl(root.transform, "DomeBase",
            new Vector3(0f, h + 0.2f, 0f), new Vector3(w * 0.55f, 1.5f, w * 0.55f), StoneCold);
        RemoveCollider(dBase);
        var dSphere = Sphere(root.transform, "Dome",
            new Vector3(0f, h + 1.5f + w * 0.28f, 0f),
            new Vector3(w * 0.55f, w * 0.57f, w * 0.55f), dome);
        SetEmissive(dSphere, dome, dome * 0.3f); RemoveCollider(dSphere);

        // 4 모서리 첨탑 (미나렛)
        float mx = w * 0.42f, mz = d * 0.42f;
        float mh = h * 0.65f;
        float my = h * 0.58f + mh * 0.5f;
        foreach (var mp in new[] {
            new Vector3(-mx, my, -mz), new Vector3(mx, my, -mz),
            new Vector3(-mx, my,  mz), new Vector3(mx, my,  mz) })
        {
            Cyl(root.transform, "Minaret", mp, new Vector3(1.2f, mh, 1.2f), StoneDark);
            var tip = Sphere(root.transform, "MinaretTip", mp + new Vector3(0f, mh * 0.55f, 0f), Vector3.one * 1.8f, dome);
            SetEmissive(tip, dome, dome * 0.22f); RemoveCollider(tip);
        }

        // 입구 기둥 4개 (전면)
        float px = w * 0.28f;
        Pillar(root.transform, "EntryPillar_LL", new Vector3(-px * 1.6f, 0f, -(d * 0.5f - 0.5f)), StoneCold);
        Pillar(root.transform, "EntryPillar_L",  new Vector3(-px * 0.6f, 0f, -(d * 0.5f - 0.5f)), StoneWarm);
        Pillar(root.transform, "EntryPillar_R",  new Vector3( px * 0.6f, 0f, -(d * 0.5f - 0.5f)), StoneWarm);
        Pillar(root.transform, "EntryPillar_RR", new Vector3( px * 1.6f, 0f, -(d * 0.5f - 0.5f)), StoneCold);

        // 입구 상단 황금 배너
        var banner = Box(root.transform, "Banner",
            new Vector3(0f, h * 0.88f, -(d * 0.5f + 0.05f)),
            new Vector3(w * 0.45f, h * 0.17f, 0.15f), Gold);
        SetEmissive(banner, Gold, Gold * 0.2f);
    }

    // ── 5. 지식 거래 시장 (포탈 사이마다 노점 6개) ───────────────────────────
    static void BuildKnowledgeBazaar()
    {
        var root = new GameObject("HubBazaar");

        string[] stallColors = { "#C44", "#4A8", "#88C", "#CA6", "#6AC", "#A6C" };

        for (int i = 0; i < 6; i++)
        {
            float deg = i * 60f + 30f;          // 포탈 사이 (30° 어긋남)
            float rad = deg * Mathf.Deg2Rad;
            var   pos = new Vector3(Mathf.Sin(rad) * BAZAAR_R, 0f, Mathf.Cos(rad) * BAZAAR_R);

            var stall = new GameObject($"Stall_{i}");
            stall.transform.SetParent(root.transform);
            stall.transform.position = pos;
            stall.transform.rotation = Quaternion.Euler(0f, deg + 180f, 0f); // 분수 바라봄

            // 천막 지붕 (컬러풀)
            ColorUtility.TryParseHtmlString(stallColors[i % stallColors.Length], out Color sc);
            Box(stall.transform, "Awning",  new Vector3(0f, 3.6f, 0f),   new Vector3(6.5f, 0.25f, 4.2f), sc);
            Box(stall.transform, "PostL",   new Vector3(-2.8f, 1.8f, 0f), new Vector3(0.30f, 3.6f, 0.30f), StoneDark);
            Box(stall.transform, "PostR",   new Vector3( 2.8f, 1.8f, 0f), new Vector3(0.30f, 3.6f, 0.30f), StoneDark);

            // 판매대 + 소품
            Prop(stall.transform, P_TABLE,    new Vector3(0f, 0.5f, 0.5f),   Vector3.one * 1.4f, "Table");
            Prop(stall.transform, P_POT_A,    new Vector3(-1.5f, 1.1f, 0.5f), Vector3.one * 0.7f, "Pot");
            Prop(stall.transform, P_BARREL_W, new Vector3( 1.9f, 0.5f, 0f),  Vector3.one * 0.9f, "Barrel");
            Prop(stall.transform, P_SACK,     new Vector3(-1.9f, 0.5f, 0f),  Vector3.one * 0.8f, "Sack");
            Prop(stall.transform, P_COIN_PILE, new Vector3(0f, 1.1f, 0.3f),  Vector3.one * 0.55f, "Coins");
        }
    }

    // ── 6. 남쪽 대로 (스폰 → 길드 본부 진입로) ──────────────────────────────
    static void BuildGrandAvenue()
    {
        var root = new GameObject("HubGrandAvenue");

        // 대로 석판
        Box(root.transform, "Road", new Vector3(0f, 0.01f, -115f), new Vector3(18f, 0.02f, 50f), StoneCold);

        // 5쌍 가로수 + 가로등
        for (int i = 0; i < 5; i++)
        {
            float z = -93f - i * 9f;
            Prop(root.transform, P_TREE_A, new Vector3(-12f, 0f, z), Vector3.one * 1.9f, $"TreeL_{i}");
            Prop(root.transform, P_TREE_A, new Vector3( 12f, 0f, z), Vector3.one * 1.9f, $"TreeR_{i}");
            // 가로등 기둥
            Box(root.transform, $"LampPostL_{i}", new Vector3(-9f, 2.5f, z), new Vector3(0.35f, 5f, 0.35f), StoneDark);
            Box(root.transform, $"LampPostR_{i}", new Vector3( 9f, 2.5f, z), new Vector3(0.35f, 5f, 0.35f), StoneDark);
            var lhL = Box(root.transform, $"LampHeadL_{i}", new Vector3(-9f, 5.4f, z), new Vector3(1.0f, 0.4f, 1.0f), Gold);
            var lhR = Box(root.transform, $"LampHeadR_{i}", new Vector3( 9f, 5.4f, z), new Vector3(1.0f, 0.4f, 1.0f), Gold);
            SetEmissive(lhL, Gold, GoldBright * 0.65f);
            SetEmissive(lhR, Gold, GoldBright * 0.65f);
        }

        // 남쪽 성문
        Box(root.transform, "GateL",    new Vector3(-10f,  6f, -140f), new Vector3(2.2f, 12f, 2.5f), StoneDark);
        Box(root.transform, "GateR",    new Vector3( 10f,  6f, -140f), new Vector3(2.2f, 12f, 2.5f), StoneDark);
        Box(root.transform, "GateArch", new Vector3(  0f, 11.8f, -140f), new Vector3(22f, 2.2f, 2.5f), StoneDark);
        var gd = Box(root.transform, "GateDeco", new Vector3(0f, 13.5f, -140f), new Vector3(14f, 1.8f, 0.9f), Gold);
        SetEmissive(gd, Gold, GoldBright * 0.3f);
    }

    // ── 7. 북쪽 항구 (그랜드 하버 — 다층 구조) ──────────────────────────────
    // 메조리아 지형: 평야 + 반도. 산 없음. 항구는 반도 북단에 위치.
    static void BuildHarbor()
    {
        var root = new GameObject("HubHarbor");
        root.transform.position = new Vector3(0f, 0f, HARBOR_Z);

        // ─ 다층 부두 (1층 메인 + 2층 상단 플랫폼) ─
        // 1층 부두 바닥
        Box(root.transform, "DockFloor1", new Vector3(0f, 0.15f, 0f), new Vector3(160f, 0.30f, 22f), DockWood);
        // 2층 상단 플랫폼 (중앙부, 약간 높게)
        Box(root.transform, "DockFloor2", new Vector3(0f, 1.8f, -4f), new Vector3(80f, 0.30f, 14f), DockWood);

        // 부두 말뚝
        for (int i = -4; i <= 4; i++)
            Cyl(root.transform, $"Pile_{i+4}", new Vector3(i * 18f, -1.5f, 10f), new Vector3(1.0f, 7f, 1.0f), DockWood);

        // 층간 계단
        Box(root.transform, "DockStairL", new Vector3(-42f, 1.0f, -4f), new Vector3(5f, 1.8f, 4f), DockWood);
        Box(root.transform, "DockStairR", new Vector3( 42f, 1.0f, -4f), new Vector3(5f, 1.8f, 4f), DockWood);

        // 항구 소품 (1층)
        Prop(root.transform, P_BARREL_W,   new Vector3(-50f, 0.5f,  3f), Vector3.one * 1.9f, "BarrelA");
        Prop(root.transform, P_BARREL_W,   new Vector3(-50f, 0.5f, -2f), Vector3.one * 1.9f, "BarrelB");
        Prop(root.transform, P_CRATE,      new Vector3( 50f, 0.5f,  3f), Vector3.one * 2.0f, "CrateA");
        Prop(root.transform, P_SACK,       new Vector3( 50f, 0.5f, -2f), Vector3.one * 2.0f, "SackA");
        Prop(root.transform, P_BARREL_M,   new Vector3(-20f, 0.5f,  6f), Vector3.one * 1.6f, "BarrelM");
        Prop(root.transform, P_SACK_STACK, new Vector3( 20f, 0.5f,  6f), Vector3.one * 2.2f, "SackStack");
        Prop(root.transform, P_CHEST,      new Vector3(-70f, 0.5f,  0f), Vector3.one * 1.4f, "Chest");
        Prop(root.transform, P_POT_B,      new Vector3( 70f, 0.5f,  0f), Vector3.one * 1.1f, "Pot");

        // 항구 소품 (2층 플랫폼)
        Prop(root.transform, P_BARREL_W,  new Vector3(-20f, 2.3f, -4f), Vector3.one * 1.4f, "Barrel2L");
        Prop(root.transform, P_CRATE,     new Vector3( 20f, 2.3f, -4f), Vector3.one * 1.6f, "Crate2R");
        Prop(root.transform, P_POT_A,     new Vector3(  0f, 2.3f, -8f), Vector3.one * 1.0f, "Pot2");

        // 창고 2동 (1층 양 끝)
        Box(root.transform, "WarehouseW", new Vector3(-70f, 6f, -4f), new Vector3(26f, 12f, 14f), StoneDark);
        Box(root.transform, "WarehouseE", new Vector3( 70f, 6f, -4f), new Vector3(26f, 12f, 14f), StoneDark);

        // 창고 지붕 (황금 트림)
        var rwL = Box(root.transform, "WarehouseRoofW", new Vector3(-70f, 12.8f, -4f), new Vector3(27f, 1.5f, 15f), Gold);
        var rwR = Box(root.transform, "WarehouseRoofE", new Vector3( 70f, 12.8f, -4f), new Vector3(27f, 1.5f, 15f), Gold);
        SetEmissive(rwL, Gold, Gold * 0.15f); SetEmissive(rwR, Gold, Gold * 0.15f);

        // ─ 바다 (반도 북단, 부두 너머 world z≈215) ─
        Sea(null, "HubSea_N", new Vector3(0f, 0.1f, 215f), new Vector3(300f, 1f, 80f));

        // ─ 반도 동/서 바다 ─
        Sea(null, "HubSea_E", new Vector3(230f, 0.1f, 90f), new Vector3(80f, 1f, 200f));
        Sea(null, "HubSea_W", new Vector3(-230f, 0.1f, 90f), new Vector3(80f, 1f, 200f));
    }

    static void Sea(Transform parent, string name, Vector3 worldPos, Vector3 scale)
    {
        var go = LoadPrefab(P_WATER);
        if (go != null)
        {
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position   = worldPos;
            go.transform.localScale = scale;
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

    // ── 8. 배경 도시 실루엣 ──────────────────────────────────────────────────
    static void BuildBackgroundCity()
    {
        var root = new GameObject("HubBackgroundCity");

        string[] bgs = { P_BG_A, P_BG_B, P_BG_C, P_BG_D, P_BG_E };
        var spots = new (Vector3 pos, float ry, float sc, int bi)[]
        {
            (new Vector3(-175f, 0f,   0f),   90f, 5f, 0),
            (new Vector3(-155f, 0f,  45f),   70f, 4f, 1),
            (new Vector3(-155f, 0f, -45f),  100f, 4f, 2),
            (new Vector3( 175f, 0f,   0f),  -90f, 5f, 0),
            (new Vector3( 155f, 0f,  45f),  -70f, 4f, 3),
            (new Vector3( 155f, 0f, -45f), -100f, 4f, 4),
            (new Vector3(-105f, 0f, -155f),  135f, 3f, 1),
            (new Vector3(   0f, 0f, -165f),  180f, 4f, 2),
            (new Vector3( 105f, 0f, -155f),  225f, 3f, 3),
        };

        foreach (var (pos, ry, sc, bi) in spots)
        {
            var go = LoadPrefab(bgs[bi]);
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
                fb.transform.position   = pos + new Vector3(0f, 12f, 0f);
                fb.transform.localScale = new Vector3(30f, 24f + bi * 5f, 8f);
                fb.transform.rotation   = Quaternion.Euler(0f, ry, 0f);
                ApplyColor(fb, StoneDark);
                fb.isStatic = true;
            }
        }
    }

    // ── 9. 씬 드레싱 (나무·바위·덤불) ────────────────────────────────────────
    static void BuildScenery()
    {
        var root = new GameObject("HubScenery");

        // 중간 링 나무 (반지름 110~130, 항구 방향 제외)
        float[] angles  = { 15f, 45f, 75f, 105f, 135f, 165f, 195f, 225f, 255f, 285f, 315f, 345f };
        string[] treePs = { P_TREE_A, P_TREE_B, P_TREE_C, P_TREE_A, P_TREE_B, P_TREE_C,
                             P_TREE_A, P_TREE_B, P_TREE_C, P_TREE_A, P_TREE_B, P_TREE_C };
        for (int i = 0; i < angles.Length; i++)
        {
            float r   = 112f + (i % 3) * 8f;
            float rad = angles[i] * Mathf.Deg2Rad;
            var   p   = new Vector3(Mathf.Sin(rad) * r, 0f, Mathf.Cos(rad) * r);
            if (p.z > HARBOR_Z - 25f) continue;
            Prop(root.transform, treePs[i], p, Vector3.one * (2.0f + (i % 3) * 0.35f), $"Tree_{i}");
        }

        // 분수 주변 덤불 링 (반지름 19)
        for (int i = 0; i < 8; i++)
        {
            float rad = i * 45f * Mathf.Deg2Rad;
            var p = new Vector3(Mathf.Sin(rad) * 19f, 0f, Mathf.Cos(rad) * 19f);
            Prop(root.transform, P_BUSH_A, p, Vector3.one * 1.25f, $"FBush_{i}");
        }

        // 항구 접근로 양쪽 나무
        for (int i = 0; i < 4; i++)
        {
            float z = 100f + i * 15f;
            Prop(root.transform, P_TREE_B, new Vector3(-22f, 0f, z), Vector3.one * 2.3f, $"HarborTL_{i}");
            Prop(root.transform, P_TREE_B, new Vector3( 22f, 0f, z), Vector3.one * 2.3f, $"HarborTR_{i}");
        }

        // 동·서쪽 바위 클러스터
        foreach (var (rx, rz) in new[] { (-120f, -60f), (-108f, -85f), (120f, -60f), (112f, -88f) })
        {
            Prop(root.transform, P_ROCK_A, new Vector3(rx, 0f, rz),          Vector3.one * 3.2f, "Rock");
            Prop(root.transform, P_ROCK_B, new Vector3(rx + 4f, 0f, rz + 2f), Vector3.one * 2.0f, "RockB");
        }
    }

    // ── 10. 경계 벽 (불가시 충돌체) ─────────────────────────────────────────
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
        go.name            = name;
        go.transform.position   = pos;
        go.transform.localScale = size;
        go.isStatic        = true;
        var r = go.GetComponent<Renderer>(); if (r) r.enabled = false;
    }

    // ── 11. 지중해 조명 ──────────────────────────────────────────────────────
    static void AdjustLighting()
    {
        var sun = Object.FindFirstObjectByType<Light>();
        if (sun == null) return;
        sun.color     = new Color(1.00f, 0.93f, 0.78f);
        sun.intensity = 1.18f;
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

    // 기둥: Synty Pillar 또는 실린더 폴백
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
}
