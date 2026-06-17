using UnityEngine;
using UnityEngine.Events;
using UnityEditor;

/// <summary>
/// 메조리아 허브 맵 환경 일체를 절차적으로 생성한다.
/// MesoriaSetup.Run() 에서 호출 (씬 생성 후, 저장 직전).
///
/// 생성 요소:
///   지면(200×200 따뜻한 석재), 바다(북쪽), 부두(목재), 6대륙 포탈,
///   건물 3기(연구소·도서관·모험가 길드), 중앙 광장, 중립 조약의 탑, 경계 벽, 조명 조정.
///
/// 건물에 <see cref="Interactable"/> 컴포넌트를 부착하며,
/// <c>onInteract</c> 이벤트 와이어링은 MetaUISetup.Run() 에서 수행한다.
/// </summary>
public static class MesoriaHubBuilder
{
    // ── 공통 색 팔레트 ────────────────────────────────────────────────────
    static readonly Color StoneWarm  = new Color(0.76f, 0.70f, 0.56f);   // 따뜻한 모래석
    static readonly Color StoneDark  = new Color(0.55f, 0.50f, 0.38f);   // 어두운 석재
    static readonly Color Gold       = new Color(0.78f, 0.66f, 0.25f);   // 메조리아 골드 #C8A840
    static readonly Color SeaBlue    = new Color(0.18f, 0.45f, 0.72f);   // 바다 파랑
    static readonly Color DockWood   = new Color(0.55f, 0.38f, 0.20f);   // 목재
    static readonly Color PlazaStone = new Color(0.82f, 0.75f, 0.60f);   // 광장 포장석

    // ── 대륙 포탈 색 (ContinentColors 와 동일 순서) ───────────────────────
    static readonly Color[] PortalColors =
    {
        new Color(0.17f, 0.50f, 1.00f),  // Physics   — 코발트 블루
        new Color(1.00f, 0.09f, 0.27f),  // Chemistry — 선홍 스칼렛
        new Color(0.24f, 0.77f, 0.15f),  // Biology   — 밝은 녹색
        new Color(0.77f, 0.36f, 0.13f),  // EarthSci  — 테라코타
        new Color(0.78f, 0.82f, 0.00f),  // Math      — 황록
        new Color(0.67f, 0.00f, 1.00f),  // Info      — 네온 보라
    };

    static readonly string[] PortalLabels =
        { "물리", "화학", "생명과학", "지구과학", "수학", "정보" };

    // ─────────────────────────────────────────────────────────────────────
    public static void Build()
    {
        BuildGround();
        BuildSea();
        BuildDock();
        BuildPortals();
        BuildBuildings();
        BuildCentralArea();
        BuildWalls();
        AdjustLighting();
    }

    // ── 지면 ─────────────────────────────────────────────────────────────
    static void BuildGround()
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "HubGround";
        ground.transform.localScale = new Vector3(20f, 1f, 20f); // 200 × 200
        ApplyColor(ground, StoneWarm);
    }

    // ── 바다 (북쪽 z = 70~100) ──────────────────────────────────────────
    static void BuildSea()
    {
        var sea = GameObject.CreatePrimitive(PrimitiveType.Plane);
        sea.name = "HubSea";
        sea.transform.position   = new Vector3(0f, 0.05f, 85f);
        sea.transform.localScale = new Vector3(20f, 1f, 3f); // 200 × 30
        ApplyColor(sea, SeaBlue);
        // 플레이어가 바다에 빠지지 않도록 콜라이더 제거 (벽으로 막음)
        Object.DestroyImmediate(sea.GetComponent<MeshCollider>());
    }

    // ── 부두 (목재 플랫폼, z ≈ 60~72) ───────────────────────────────────
    static void BuildDock()
    {
        var dock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dock.name = "HubDock";
        dock.transform.position   = new Vector3(0f, 0.12f, 66f);
        dock.transform.localScale = new Vector3(155f, 0.25f, 14f);
        ApplyColor(dock, DockWood);
    }

    // ── 6대륙 포탈 ───────────────────────────────────────────────────────
    static void BuildPortals()
    {
        float[] xs = { -60f, -36f, -12f, 12f, 36f, 60f };
        const float pz = 66f;

        for (int i = 0; i < 6; i++)
        {
            var root = new GameObject($"HubPortal_{PortalLabels[i]}");
            root.transform.position = new Vector3(xs[i], 0f, pz);

            var ia = root.AddComponent<Interactable>();
            ia.displayName = $"{PortalLabels[i]} 포탈";
            ia.promptText  = $"[E]  {PortalLabels[i]} 포탈";
            ia.radius      = 6f;
            // onInteract 미연결 = 스텁(준비 중) — 향후 패스트 트래블 구현 시 와이어링

            Color pc = PortalColors[i];

            // 발광 바닥 패드
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pad.name = "Pad";
            pad.transform.SetParent(root.transform, false);
            pad.transform.localPosition = new Vector3(0f, 0.12f, 0f);
            pad.transform.localScale    = new Vector3(5f, 0.12f, 5f);
            ApplyColorEmissive(pad, pc, pc * 0.5f);

            // 왼쪽 기둥
            Pillar(root.transform, "PillarL", new Vector3(-2.5f, 3.5f, 0f),
                new Vector3(0.8f, 7f, 0.8f), StoneWarm);

            // 오른쪽 기둥
            Pillar(root.transform, "PillarR", new Vector3(2.5f, 3.5f, 0f),
                new Vector3(0.8f, 7f, 0.8f), StoneWarm);

            // 아치 빔 (상단)
            Pillar(root.transform, "Beam", new Vector3(0f, 7.4f, 0f),
                new Vector3(6.2f, 0.9f, 0.9f), pc);

            // 발광 패널 (포탈 색 표시)
            var glow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glow.name = "GlowPanel";
            glow.transform.SetParent(root.transform, false);
            glow.transform.localPosition = new Vector3(0f, 3.8f, 0.2f);
            glow.transform.localScale    = new Vector3(4f, 6f, 0.08f);
            ApplyColorEmissive(glow, new Color(pc.r, pc.g, pc.b, 0.8f), pc * 0.55f);
        }
    }

    // ── 건물 3기 ────────────────────────────────────────────────────────
    static void BuildBuildings()
    {
        // 연구소 (서쪽) — R&E 패널
        BuildBuilding(
            goName:      "HubLab",
            pos:         new Vector3(-48f, 0f, 18f),
            displayName: "연구소",
            bannerColor: new Color(0.20f, 0.55f, 0.85f));  // 파랑 — 과학

        // 도서관 (동쪽) — 가챠 패널
        BuildBuilding(
            goName:      "HubLibrary",
            pos:         new Vector3(48f, 0f, 18f),
            displayName: "도서관",
            bannerColor: new Color(0.55f, 0.25f, 0.75f));  // 보라 — 지식/소환

        // 모험가 길드 본부 (남쪽, 스폰 근처) — 스텁
        BuildBuilding(
            goName:      "HubGuildHall",
            pos:         new Vector3(0f, 0f, -28f),
            displayName: "모험가 길드");                    // 배너 색 없음 → 골드
    }

    static void BuildBuilding(string goName, Vector3 pos,
        string displayName, Color? bannerColor = null)
    {
        var root = new GameObject(goName);
        root.transform.position = pos;

        // 본체 (석재 벽, y=0 기준)
        Pillar(root.transform, "Body",
            new Vector3(0f, 5f, 0f), new Vector3(14f, 10f, 12f), StoneWarm);

        // 골드 돔
        var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dome.name = "Dome";
        dome.transform.SetParent(root.transform, false);
        dome.transform.localPosition = new Vector3(0f, 11.5f, 0f);
        dome.transform.localScale    = new Vector3(11f, 6.5f, 11f);
        ApplyColor(dome, Gold);

        // 현관 기둥 왼쪽
        Pillar(root.transform, "PillarL",
            new Vector3(-3.2f, 2.5f, -6f), new Vector3(1.2f, 5f, 1.2f), StoneDark);

        // 현관 기둥 오른쪽
        Pillar(root.transform, "PillarR",
            new Vector3(3.2f, 2.5f, -6f), new Vector3(1.2f, 5f, 1.2f), StoneDark);

        // 현관 상인방 (골드)
        Pillar(root.transform, "Lintel",
            new Vector3(0f, 5.3f, -6f), new Vector3(8f, 1.1f, 1.1f), Gold);

        // 기능 배너 (건물 색 아이덴티티)
        Color banner = bannerColor ?? Gold;
        Pillar(root.transform, "Banner",
            new Vector3(0f, 8.2f, -6.07f), new Vector3(7f, 3.5f, 0.15f), banner);

        // Interactable 컴포넌트 (onInteract는 MetaUISetup이 와이어링)
        var ia = root.AddComponent<Interactable>();
        ia.displayName = displayName;
        ia.promptText  = $"[E]  {displayName}";
        ia.radius      = 9f;  // 건물 반폭 7 + 여유 2
    }

    // ── 중앙 광장 + 조약의 탑 ─────────────────────────────────────────
    static void BuildCentralArea()
    {
        // 광장 포장면 (지면보다 약간 높아 시각 구분)
        var plaza = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plaza.name = "HubPlaza";
        plaza.transform.position   = new Vector3(0f, 0.02f, 5f);
        plaza.transform.localScale = new Vector3(3f, 1f, 3f); // 30 × 30
        ApplyColor(plaza, PlazaStone);

        // 중립 조약의 탑 (골드 오벨리스크)
        var tower = new GameObject("HubTower");
        tower.transform.position = new Vector3(0f, 0f, 5f);

        // 기단
        Pillar(tower.transform, "Base",
            new Vector3(0f, 0.5f, 0f), new Vector3(4f, 1f, 4f), Gold);

        // 몸통
        Pillar(tower.transform, "Body",
            new Vector3(0f, 7f, 0f), new Vector3(2.2f, 12f, 2.2f), Gold);

        // 상부 캡 (약간 짙은 골드)
        Pillar(tower.transform, "Cap",
            new Vector3(0f, 14f, 0f), new Vector3(2.8f, 1.2f, 2.8f),
            new Color(Gold.r * 1.05f, Gold.g * 0.95f, Gold.b * 0.55f));

        // 첨탑
        Pillar(tower.transform, "Spire",
            new Vector3(0f, 17.5f, 0f), new Vector3(0.7f, 5f, 0.7f),
            new Color(Gold.r * 1.1f, Gold.g * 1.0f, Gold.b * 0.5f));
    }

    // ── 경계 벽 (비표시 콜라이더) ───────────────────────────────────────
    static void BuildWalls()
    {
        InvisWall("Wall_N", new Vector3(  0f, 1.5f,  100f), new Vector3(200f, 3f,   1f));
        InvisWall("Wall_S", new Vector3(  0f, 1.5f, -100f), new Vector3(200f, 3f,   1f));
        InvisWall("Wall_E", new Vector3(100f, 1.5f,    0f), new Vector3(  1f, 3f, 200f));
        InvisWall("Wall_W", new Vector3(-100f,1.5f,    0f), new Vector3(  1f, 3f, 200f));
    }

    // ── 조명 조정 (지중해 낮, 따뜻한 햇빛) ─────────────────────────────
    static void AdjustLighting()
    {
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.type != LightType.Directional) continue;
            l.color     = new Color(1.00f, 0.95f, 0.85f);  // 따뜻한 낮빛
            l.intensity = 1.15f;
            break;
        }
        RenderSettings.ambientLight = new Color(0.52f, 0.49f, 0.42f);  // 따뜻한 앰비언트
        RenderSettings.fog          = false;
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────

    /// <summary>Cube 프리미티브를 부모 transform에 생성해 색을 입힌다.</summary>
    static void Pillar(Transform parent, string name, Vector3 localPos, Vector3 localScale, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = localScale;
        ApplyColor(go, color);
    }

    static void InvisWall(string name, Vector3 pos, Vector3 scale)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name                 = name;
        wall.transform.position   = pos;
        wall.transform.localScale = scale;
        Object.DestroyImmediate(wall.GetComponent<MeshRenderer>());
    }

    static void ApplyColor(GameObject go, Color color)
    {
        var r = go.GetComponent<MeshRenderer>();
        if (r == null) return;
        var mat   = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        r.sharedMaterial = mat;
    }

    static void ApplyColorEmissive(GameObject go, Color color, Color emission)
    {
        var r = go.GetComponent<MeshRenderer>();
        if (r == null) return;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", emission);
        r.sharedMaterial = mat;
    }
}
