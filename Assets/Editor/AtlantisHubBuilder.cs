using UnityEngine;
using UnityEditor;

/// <summary>
/// 아틀란티스(수학 대륙) 허브 빌더 — 웅장 스케일 v3
///
/// 섬 배치 (세계 기준):
///   아크시움   topY=+30  center (0,0,0)        radius 250
///   유클리드   topY=-20  center (-1000,0,-600)  radius 350
///
/// 충돌 규칙:
///   섬 윗면 = Box → BoxCollider (CapsuleCollider 퇴화 방지)
///   바위/팁  = Cyl + RemoveCollider (시각 전용)
///   Box: top = center.y + height/2
///   Cyl: top = center.y + scale.y  (scale.y = 반높이!)
/// </summary>
public static class AtlantisHubBuilder
{
    // ── 팔레트 ───────────────────────────────────────────────────────────────
    static readonly Color MathGold    = new Color(0.78f, 0.82f, 0.00f);
    static readonly Color MathGlow    = new Color(0.88f, 0.98f, 0.12f);
    static readonly Color IslandGrass = new Color(0.55f, 0.62f, 0.42f);
    static readonly Color IslandRock  = new Color(0.38f, 0.34f, 0.28f);
    static readonly Color TreeBark    = new Color(0.32f, 0.22f, 0.10f);
    static readonly Color TreeLeaf    = new Color(0.15f, 0.48f, 0.05f);
    static readonly Color StoneWarm   = new Color(0.82f, 0.75f, 0.60f);
    static readonly Color StoneCold   = new Color(0.70f, 0.72f, 0.75f);
    static readonly Color StoneDark   = new Color(0.30f, 0.28f, 0.22f);
    static readonly Color StoneLight  = new Color(0.88f, 0.86f, 0.82f);
    static readonly Color EuclidWhite = new Color(0.90f, 0.92f, 0.88f);
    static readonly Color DarkElvPurp = new Color(0.22f, 0.12f, 0.35f);
    static readonly Color DarkElvGlow = new Color(0.55f, 0.20f, 0.90f);
    static readonly Color BridgeWood  = new Color(0.45f, 0.32f, 0.14f);
    static readonly Color SilhouetteA = new Color(0.18f, 0.20f, 0.25f);
    static readonly Color PortalOff   = new Color(0.28f, 0.28f, 0.28f);
    static readonly Color Gold        = new Color(0.78f, 0.66f, 0.25f);
    static readonly Color GoldBright  = new Color(0.98f, 0.86f, 0.32f);

    public const float AXIOM_TOP_Y  =  30f;
    public const float EUCLID_TOP_Y = -20f;

    // ── 진입점 ───────────────────────────────────────────────────────────────
    public static void Build()
    {
        BuildAxiomIsland();
        BuildEuclidIsland();
        BuildBranchBridge();
        BuildDistantSilhouettes();
        AdjustLighting();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 1. 세계수 아크시움 섬  (topY=+30, radius=250)
    // ─────────────────────────────────────────────────────────────────────────
    static void BuildAxiomIsland()
    {
        var root = new GameObject("AxiomIsland");
        FloatingIsland(root.transform, Vector2.zero, AXIOM_TOP_Y, 250f, IslandGrass);
        BuildWorldTree(root.transform);
        BuildExposedRoots(root.transform);
        BuildProofCouncil(root.transform);
        BuildLockedPortalHub(root.transform);
        BuildAxiomReturnPortal(root.transform);
    }

    // ─── 세계수 본체 ───────────────────────────────────────────────────────
    static void BuildWorldTree(Transform parent)
    {
        var tg = new GameObject("WorldTree");
        tg.transform.SetParent(parent, false);

        // ── 줄기 3단 ────────────────────────────────────────────────────────
        // Cyl: top = center + scale.y,  bottom = center - scale.y

        // 하단  Y 30 → 450   center=240  half-h=210  지름=100
        Cyl(tg, "Trunk_Base",  V(0, 240, 0),  V(100,210,100),  TreeBark);
        // 중단  Y 420→ 780   center=600  half-h=180  지름=65
        Cyl(tg, "Trunk_Mid",   V(0, 600, 0),  V(65, 180,65),   TreeBark);
        // 상단  Y 760→1000   center=880  half-h=120  지름=40
        Cyl(tg, "Trunk_Top",   V(0, 880, 0),  V(40, 120,40),   TreeBark);

        // ── 수관 (황록 발광 구) ──────────────────────────────────────────
        float[]   sz = { 700f, 550f, 500f, 420f, 360f };
        Vector3[] cp = {
            V(   0, 1120,    0),
            V(-260, 1075,  180),
            V( 260, 1060, -180),
            V(-180, 1040, -260),
            V( 210, 1025,  280),
        };
        for (int i = 0; i < sz.Length; i++)
        {
            var s = Sph(tg, $"Canopy_{i}", cp[i], V3(sz[i]), MathGold);
            SetEmissive(s, MathGold, MathGlow * 0.55f);
            RemoveCol(s);
        }

        // ── 가지 5방향  (줄기 상단 Y≈850 근처에서 뻗음) ────────────────
        float[] ba = { 0f, -55f, -125f, 125f, 55f };
        foreach (float a in ba)
        {
            float r = a * Mathf.Deg2Rad;
            var d = new Vector3(Mathf.Sin(r), -0.15f, Mathf.Cos(r)).normalized;
            var m = d * 350f + V(0, 850, 0);
            var bg = new GameObject("Branch");
            bg.transform.SetParent(tg.transform, false);
            bg.transform.localPosition = m;
            bg.transform.localRotation = Quaternion.LookRotation(d) * Quaternion.Euler(90,0,0);
            var bc = Cyl(bg.transform, "Body", Vector3.zero, V(14,120,14), TreeBark);
            RemoveCol(bc);
        }
    }

    // ─── 드러난 뿌리 + 다크엘프 구역 ─────────────────────────────────────
    static void BuildExposedRoots(Transform parent)
    {
        var rg = new GameObject("ExposedRoots");
        rg.transform.SetParent(parent, false);

        // 섬 Box 하단 = AXIOM_TOP_Y - DISK_H = 30 - 6 = 24
        const float IBOT = 24f;

        float[] ang  = { 0f, 60f, 120f, 180f, 240f, 300f };
        float[] dist = { 100f,  75f, 120f,  90f, 110f,  80f };
        float[] dep  = { 350f, 280f, 420f, 320f, 380f, 300f };
        float[] thk  = {  28f,  22f,  32f,  25f,  30f,  20f };

        for (int i = 0; i < ang.Length; i++)
        {
            float rad = ang[i] * Mathf.Deg2Rad;
            float ox  = Mathf.Sin(rad) * dist[i];
            float oz  = Mathf.Cos(rad) * dist[i];

            var dir = new Vector3(ox, -dep[i] * 0.55f, oz).normalized;
            var sp  = V(ox * 0.4f, IBOT - 3f, oz * 0.4f);
            var mp  = sp + dir * (dep[i] * 0.5f);

            var go = new GameObject($"Root_{i}");
            go.transform.SetParent(rg.transform, false);
            go.transform.localPosition = mp;
            go.transform.localRotation = Quaternion.LookRotation(dir) * Quaternion.Euler(90,0,0);
            var rc = Cyl(go.transform, "Body", Vector3.zero, V(thk[i], dep[i], thk[i]*0.6f), TreeBark);
            RemoveCol(rc);
        }
        BuildDarkElfArea(rg.transform);
    }

    static void BuildDarkElfArea(Transform parent)
    {
        var de = new GameObject("DarkElfArea");
        de.transform.SetParent(parent, false);

        // 링 통로  Cyl scale.x=600 → world-radius 300
        var ring = Cyl(de, "RingWalkway", V(0,5.8f,0), V(600,0.8f,600), DarkElvPurp);
        RemoveCol(ring);
        var inner = Cyl(de, "RingInner", V(0,6.65f,0), V(360,0.85f,360),
            new Color(0.08f,0.05f,0.12f));
        RemoveCol(inner);

        // 발판 3개 (Box — 충돌 정상)
        (Vector3 p, float ry)[] plats = {
            (V(130,14.5f,0),   90f),
            (V(-70,18.5f,120), 210f),
            (V(-60,10.5f,-130),330f),
        };
        foreach (var (p, ry) in plats)
        {
            var pg = new GameObject("Platform");
            pg.transform.SetParent(de.transform, false);
            pg.transform.localPosition = p;
            pg.transform.localRotation = Quaternion.Euler(0,ry,0);
            Box(pg.transform, "Floor",  V(0, 0.5f,0), V(55,1f,40), DarkElvPurp);
            Box(pg.transform, "Hut",    V(0, 8f,  0), V(26,14f,22),DarkElvPurp);
            var w = Box(pg.transform, "Window", V(0,8f,-11.05f), V(9,7f,0.1f), DarkElvGlow);
            SetEmissive(w, DarkElvGlow, DarkElvGlow * 0.8f);
            RemoveCol(w);
        }

        // 발광 기둥 4개
        for (int i = 0; i < 4; i++)
        {
            float a = i * 90f * Mathf.Deg2Rad;
            var p = V(Mathf.Sin(a)*200f, 22f, Mathf.Cos(a)*200f);
            Cyl(de, $"DePillar_{i}", p, V(6,15,6), DarkElvPurp);
            var cap = Sph(de, $"DePillarCap_{i}", p+V(0,15,0), V3(10), DarkElvGlow);
            SetEmissive(cap, DarkElvGlow, DarkElvGlow*0.6f);
            RemoveCol(cap);
        }
    }

    // ─── 증명 의회 ────────────────────────────────────────────────────────
    static void BuildProofCouncil(Transform parent)
    {
        var c = new GameObject("ProofCouncil");
        c.transform.SetParent(parent, false);

        // 단상: Box bottom=30(섬 표면), height=5 → center=32.5, top=35
        Box(c.transform, "Stage",    V(0,32.5f,0), V(160,5f,160), StoneWarm);
        var rim = Box(c.transform, "StageRim", V(0,35.3f,0), V(163,0.6f,163), Gold);
        SetEmissive(rim, Gold, Gold*0.3f);

        // 기둥 8개: bottom=35(stage top), half-h=25 → center=60, top=85
        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad;
            Cyl(c.transform, $"Pillar_{i}",
                V(Mathf.Sin(a)*72f, 60f, Mathf.Cos(a)*72f), V(5,25,5), EuclidWhite);
        }

        // 포디엄
        Box(c.transform, "Podium",    V(0,38f,-55f), V(35,6f,18), StoneWarm);
        var pt = Box(c.transform, "PodiumTop", V(0,41.3f,-55f), V(38,0.6f,21f), Gold);
        SetEmissive(pt, Gold, GoldBright*0.3f);
    }

    // ─── 잠긴 포탈 허브 ───────────────────────────────────────────────────
    static void BuildLockedPortalHub(Transform parent)
    {
        var hub = new GameObject("AxiomPortalHub");
        hub.transform.SetParent(parent, false);

        var gGO = new GameObject("AxiomGate");
        gGO.transform.SetParent(hub.transform, false);
        gGO.transform.localPosition = V(0,35f,0);
        var ia = gGO.AddComponent<Interactable>();
        ia.displayName = "아크시움 해방 게이트";
        ia.promptText  = "[잠김] 아크시움 해방 필요";
        ia.radius      = 40f;

        // stage top Y=35 기준
        Box(hub.transform, "GateBase",  V(0,36.5f,0),  V(65,3f,65),  StoneDark);
        // GateOrb: Cyl bottom=39.5, half-h=50 → center=89.5, top=139.5
        Cyl(hub.transform, "GateOrb",   V(0,89.5f,0),  V(45,50,45),  PortalOff);
        Box(hub.transform, "GateBeamH", V(0,142f,0),   V(90,3f,8f),  StoneDark);
        // pillars: bottom=39.5, half-h=50 → center=89.5
        Cyl(hub.transform, "GatePilL",  V(-45f,89.5f,0), V(10,50,10), StoneDark);
        Cyl(hub.transform, "GatePilR",  V( 45f,89.5f,0), V(10,50,10), StoneDark);

        // 포탈 패드 5개
        string[] ns = { "유클리드 평원","논리·집합 고원","산술의 탑","해석의 바다","대수의 숲" };
        for (int i = 0; i < 5; i++)
        {
            float a = i * 72f * Mathf.Deg2Rad;
            var pg = new GameObject($"PortalPad_{ns[i]}");
            pg.transform.SetParent(hub.transform, false);
            pg.transform.localPosition = V(Mathf.Sin(a)*120f, 35.8f, Mathf.Cos(a)*120f);
            var pd = Cyl(pg.transform, "Pad", Vector3.zero, V(24,0.8f,24), PortalOff);
            RemoveCol(pd);
        }
    }

    // ─── 메조리아 복귀 포탈 (아크시움 남쪽) ─────────────────────────────
    static void BuildAxiomReturnPortal(Transform parent)
    {
        var pg = new GameObject("ReturnPortal");
        pg.transform.SetParent(parent, false);
        pg.transform.localPosition = V(0, AXIOM_TOP_Y, -190f);

        var ia = pg.AddComponent<Interactable>();
        ia.displayName = "메조리아 복귀 포탈";
        ia.promptText  = "[E]  메조리아로 복귀";
        ia.radius      = 30f;

        var sp = pg.AddComponent<ScenePortal>();
        WirePortal(ia, sp, "Mesoria", "MathPortalSpawn");

        // 패드: Cyl radius=6 (메조리아와 동일 수준)
        var pad = Cyl(pg.transform, "Pad", V(0,0.15f,0), V(6,0.15f,6), MathGold);
        SetEmissive(pad, MathGold, MathGlow*0.4f);
        RemoveCol(pad);

        // 기둥: 좌우 ±4, 높이 11 (half-h=5.5, center=5.65)
        Cyl(pg.transform, "PillarL", V(-4f, 5.65f, 0), V(0.9f,5.5f,0.9f), StoneWarm);
        Cyl(pg.transform, "PillarR", V( 4f, 5.65f, 0), V(0.9f,5.5f,0.9f), StoneWarm);

        // 아치빔: 기둥 top(11.15) 위에
        Box(pg.transform, "ArchBeam", V(0,11.5f,0), V(10.5f,1.2f,1.0f), MathGold);

        // 발광 패널: 기둥 사이 중심
        var glow = Box(pg.transform, "GlowPanel", V(0,5.65f,0.2f), V(4.5f,9f,0.08f), MathGold);
        SetEmissive(glow, MathGold, MathGlow*0.35f);
        RemoveCol(glow);

        // 꼭대기 구슬
        var tip = Sph(pg.transform, "Tip", V(0,13f,0), V3(1.4f), MathGold);
        SetEmissive(tip, MathGold, MathGlow*0.7f);
        RemoveCol(tip);

        // 메조리아에서 포탈로 도착할 때의 스폰 포인트
        // 씬 루트에 두어야 GameObject.Find() 가 확실히 찾음
        // 위치: 아크시움 섬 표면, 복귀 포탈(z=-190) 앞 80유닛(섬 중앙 방향)
        var axiomSpawn = new GameObject("AxiomSpawn");
        axiomSpawn.transform.position = new Vector3(0f, AXIOM_TOP_Y + 1f, -110f);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 2. 유클리드 평원 섬  (topY=−20, radius=350, root world (−1000,0,−600))
    // ─────────────────────────────────────────────────────────────────────────
    static void BuildEuclidIsland()
    {
        var root = new GameObject("EuclidIsland");
        root.transform.position = V(-1000f, 0f, -600f);

        FloatingIsland(root.transform, Vector2.zero, EUCLID_TOP_Y, 350f, EuclidWhite);
        BuildEuclidCathedral(root.transform);
        BuildStraightTrees(root.transform);

        var spawn = new GameObject("AtlantisSpawn");
        spawn.transform.SetParent(root.transform, false);
        spawn.transform.localPosition = V(25f, EUCLID_TOP_Y + 0.5f, -5f);
    }

    static void BuildEuclidCathedral(Transform parent)
    {
        var cat = new GameObject("EuclidCathedral");
        cat.transform.SetParent(parent, false);
        // 섬 남쪽 끝 배치 — 다리 입구(북동쪽)와 반대편이라 스폰과 겹치지 않음
        cat.transform.localPosition = V(0f, EUCLID_TOP_Y, -250f);

        var ia = cat.AddComponent<Interactable>();
        ia.displayName = "유클리드 대성당";
        ia.promptText  = "[E]  유클리드 대성당";
        ia.radius      = 80f;

        // 기단: Box bottom=-1, top=0 → center=-0.5, height=1
        Box(cat.transform, "Foundation", V(0,-0.5f,0),  V(200,1f,130),  StoneDark);

        // 계단
        Box(cat.transform, "Step1", V(0,0.8f,-62f),  V(120,1.6f,12f), StoneWarm);
        Box(cat.transform, "Step2", V(0,2.3f,-56f),  V(100,1.5f,10f), StoneWarm);
        Box(cat.transform, "Step3", V(0,3.5f,-51f),  V( 80,1.2f, 8f), StoneCold);

        // 본체: Box bottom=1, height=110 → center=56, top=111
        Box(cat.transform, "Body", V(0,56f,0), V(160,110f,105f), EuclidWhite);

        // 정면 기둥 7개: Cyl bottom=1, half-h=56 → center=57, top=113
        float[] cx = { -66f,-44f,-22f, 0f, 22f, 44f, 66f };
        foreach (float x in cx)
            Cyl(cat.transform, $"Col{(int)x}", V(x,57f,-52.6f), V(5.5f,56f,5.5f), StoneLight);

        // 돔: Sphere center=138, scale=90
        var dome = Sph(cat.transform, "Dome", V(0,138f,0), V3(90f), EuclidWhite);
        SetEmissive(dome, EuclidWhite, StoneLight*0.12f);
        RemoveCol(dome);

        // 첨탑
        var spire = Sph(cat.transform, "SpireOrb", V(0,195f,0), V3(18f), MathGold);
        SetEmissive(spire, MathGold, MathGlow*0.6f);
        RemoveCol(spire);
    }

    static void BuildStraightTrees(Transform parent)
    {
        var tr = new GameObject("StraightTrees");
        tr.transform.SetParent(parent, false);
        tr.transform.localPosition = V(0f, EUCLID_TOP_Y, 0f);

        (float x, float z)[] spots = {
            (-150f, 90f),(-125f,-70f),(-170f, 15f),(-110f, 145f),(-90f,-135f),
            ( 150f, 80f),( 130f,-65f),( 175f, 20f),( 100f, 155f),(115f,-130f),
            ( -40f,180f),(  45f,185f),(   0f,-165f),
        };
        foreach (var (x, z) in spots)
        {
            var t = new GameObject("Tree");
            t.transform.SetParent(tr.transform, false);
            t.transform.localPosition = V(x,0f,z);
            // Cyl bottom=0, half-h=40 → center=40, top=80
            Cyl(t.transform, "Trunk", V(0,40f,0), V(3.5f,40f,3.5f), TreeBark);
            var l = Sph(t.transform, "Crown", V(0,96f,0), V3(26f), TreeLeaf);
            RemoveCol(l);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3. 세계수 가지 다리  (유클리드 → 아크시움)
    // ─────────────────────────────────────────────────────────────────────────
    static void BuildBranchBridge()
    {
        var bridge = new GameObject("BranchBridge_EuclidToAxiom");

        // 유클리드 edge: root(-1000,-600) + radius350 × dir(0.857,0.514) ≈ (-700,-420)
        // 아크시움  edge: root(0,0)        + radius250 × -dir             ≈ (-214,-129)
        var sw = V(-700f, EUCLID_TOP_Y + 0.3f, -420f);
        var ew = V(-214f, AXIOM_TOP_Y  + 0.3f, -129f);

        const int   SEGS = 45;
        const float WIDE = 20f;
        float sLen = Vector3.Distance(sw, ew) / SEGS;
        var   dir  = (ew - sw).normalized;
        var   rot  = Quaternion.LookRotation(dir);

        for (int i = 0; i < SEGS; i++)
        {
            float t  = (i + 0.5f) / SEGS;
            var seg  = new GameObject($"BridgeSeg_{i}");
            seg.transform.SetParent(bridge.transform, false);
            seg.transform.position = Vector3.Lerp(sw, ew, t);
            seg.transform.rotation = rot;

            Box(seg.transform, "Floor", Vector3.zero,   V(WIDE, 0.8f, sLen+0.1f), BridgeWood);
            Box(seg.transform, "RailL", V(-(WIDE*0.5f+0.5f),2f,0), V(0.8f,3.2f,sLen+0.1f), TreeBark);
            Box(seg.transform, "RailR", V( (WIDE*0.5f+0.5f),2f,0), V(0.8f,3.2f,sLen+0.1f), TreeBark);

            if (i % 3 == 0)
            {
                var sup = Cyl(seg.transform, "Support", V(0,-18f,0), V(5,18,5), TreeBark);
                RemoveCol(sup);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 4. 원경 실루엣 4섬 (콜라이더 없음)
    // ─────────────────────────────────────────────────────────────────────────
    static void BuildDistantSilhouettes()
    {
        var sils = new GameObject("DistantSilhouettes");
        DistantIsland(sils.transform, "Silhouette_산술의탑",    V(    0,400,1500), 80f, true);
        DistantIsland(sils.transform, "Silhouette_논리집합고원", V(-1300,200, 750), 100f,false);
        DistantIsland(sils.transform, "Silhouette_해석의바다",   V( 1300,-150,750), 90f, false);
        DistantIsland(sils.transform, "Silhouette_대수의숲",     V( 1300,  0,-750), 90f, false);
    }

    static void DistantIsland(Transform parent, string name, Vector3 wp, float r, bool tower)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = wp;

        var d = Cyl(go.transform,"Disk",  V(0,-8f,0), V(r*2f,8f,r*2f),  SilhouetteA); RemoveCol(d);
        var k = Cyl(go.transform,"Rock",  V(0,-55f,0),V(r*1.1f,47f,r*1.1f),SilhouetteA); RemoveCol(k);

        if (tower)
        {
            var t1=Box(go.transform,"T1",V(0,180f,0), V(50,360f,50), SilhouetteA); RemoveCol(t1);
            var t2=Box(go.transform,"T2",V(0,520f,0), V(34,300f,34), SilhouetteA); RemoveCol(t2);
            var t3=Box(go.transform,"T3",V(0,830f,0), V(20,220f,20), SilhouetteA); RemoveCol(t3);
        }
        else
        {
            var dm=Sph(go.transform,"Dome",V(0,r*0.5f,0), V3(r*0.7f), SilhouetteA); RemoveCol(dm);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 조명
    // ─────────────────────────────────────────────────────────────────────────
    static void AdjustLighting()
    {
        var sun = Object.FindFirstObjectByType<Light>();
        if (sun != null)
        {
            sun.color     = new Color(1.00f, 0.95f, 0.85f);
            sun.intensity = 1.2f;
            sun.transform.rotation = Quaternion.Euler(-52f, 25f, 0f);
        }
        RenderSettings.ambientMode      = UnityEngine.Rendering.AmbientMode.Skybox;
        RenderSettings.fogColor         = new Color(0.65f, 0.78f, 0.95f);
        RenderSettings.fog              = true;
        RenderSettings.fogMode          = FogMode.Linear;
        RenderSettings.fogStartDistance = 1200f;
        RenderSettings.fogEndDistance   = 3500f;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 핵심 헬퍼 — 떠 있는 섬
    // ─────────────────────────────────────────────────────────────────────────
    static void FloatingIsland(Transform parent, Vector2 cxz, float topY, float r, Color tc)
    {
        const float DH = 6f;   // Box 섬 두께 (height)
        const float RH = 80f;  // 바위 Cyl 반높이 → 실제 높이 160
        const float TH = 60f;  // 팁  Cyl 반높이 → 실제 높이 120

        // 윗면 Box: center = topY - DH/2, top = topY ✓
        Box(parent, "IslandTop",
            V(cxz.x, topY - DH*0.5f, cxz.y), V(r*2f, DH, r*2f), tc);

        // 바위 Cyl (시각 전용): top = disk bottom = topY-DH, center = top - RH
        float db = topY - DH;
        var rock = Cyl(parent, "IslandRock",
            V(cxz.x, db - RH, cxz.y), V(r*1.15f, RH, r*1.15f), IslandRock);
        RemoveCol(rock);

        float rb = (db - RH) - RH;
        var tip = Cyl(parent, "IslandTip",
            V(cxz.x, rb - TH, cxz.y), V(r*0.5f, TH, r*0.5f), IslandRock);
        RemoveCol(tip);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 프리미티브 헬퍼 (단축 이름으로 코드 길이 절감)
    // ─────────────────────────────────────────────────────────────────────────
    static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
    static Vector3 V3(float s) => Vector3.one * s;

    static GameObject Cyl(Transform p, string n, Vector3 lp, Vector3 ls, Color c)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        g.name = n; g.transform.SetParent(p, false);
        g.transform.localPosition = lp; g.transform.localScale = ls;
        ApplyColor(g, c); return g;
    }
    static GameObject Cyl(GameObject go, string n, Vector3 lp, Vector3 ls, Color c)
        => Cyl(go.transform, n, lp, ls, c);

    static GameObject Sph(Transform p, string n, Vector3 lp, Vector3 ls, Color c)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        g.name = n; g.transform.SetParent(p, false);
        g.transform.localPosition = lp; g.transform.localScale = ls;
        ApplyColor(g, c); return g;
    }
    static GameObject Sph(GameObject go, string n, Vector3 lp, Vector3 ls, Color c)
        => Sph(go.transform, n, lp, ls, c);

    static GameObject Box(Transform p, string n, Vector3 lp, Vector3 ls, Color c)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = n; g.transform.SetParent(p, false);
        g.transform.localPosition = lp; g.transform.localScale = ls;
        ApplyColor(g, c); return g;
    }

    static void RemoveCol(GameObject g)
    { var c = g.GetComponent<Collider>(); if (c) Object.DestroyImmediate(c); }

    static void ApplyColor(GameObject g, Color c)
    {
        var r = g.GetComponent<Renderer>(); if (r == null) return;
        r.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = c };
    }

    static void SetEmissive(GameObject g, Color col, Color emit)
    {
        var r = g.GetComponent<Renderer>(); if (r == null) return;
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.color = col; m.SetColor("_EmissionColor", emit); m.EnableKeyword("_EMISSION");
        r.sharedMaterial = m;
    }

    static void WirePortal(Interactable ia, ScenePortal portal, string sceneName, string spawnName = "")
    {
        // public 필드 직접 할당 — SerializedObject 보다 확실하게 씬에 저장됨
        portal.targetScene     = sceneName;
        portal.targetSpawnName = spawnName;

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
        call.FindPropertyRelative("m_Mode").enumValueIndex         = 1;
        call.FindPropertyRelative("m_CallState").enumValueIndex    = 2;
        iso.ApplyModifiedProperties();
    }
}
