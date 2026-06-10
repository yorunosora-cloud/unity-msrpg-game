using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 둥근 모서리 9-slice 스프라이트를 절차적으로 생성해 Assets/_Game/Art/UI/ 에 저장.
/// MSRPG > Generate UI Sprites 로 1회 실행. 이후 UIKit이 참조.
/// </summary>
public static class UISpriteGen
{
    const string OUT_DIR = "Assets/_Game/Art/UI";

    [MenuItem("MSRPG/Generate UI Sprites")]
    public static void Run()
    {
        Directory.CreateDirectory(Application.dataPath + "/_Game/Art/UI");

        // 패널용 (반경 8px) — 64×64 베이스 텍스처
        SaveSprite("round_panel", size: 64, radius: 8,  border: 10);
        // 버튼용 (반경 6px)
        SaveSprite("round_button", size: 48, radius: 6, border: 8);
        // 카드용 (반경 10px) — 80×80 베이스
        SaveSprite("round_card",  size: 80, radius: 10, border: 12);

        AssetDatabase.Refresh();
        Debug.Log("[MSRPG] ✅ UI 스프라이트 생성 완료 → " + OUT_DIR);
    }

    /// <summary>
    /// 흰색 둥근 사각형 텍스처를 생성하고 9-slice 스프라이트로 임포트 설정.
    /// border: 9-slice 경계 (px). 모서리 반경보다 약간 크게 설정.
    /// </summary>
    static void SaveSprite(string name, int size, int radius, int border)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;

        // 픽셀 단위로 둥근 사각형 그리기
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
            tex.SetPixel(x, y, IsInsideRoundRect(x, y, size, radius) ? Color.white : Color.clear);

        tex.Apply();

        string path = OUT_DIR + "/" + name + ".png";
        File.WriteAllBytes(Application.dataPath + "/_Game/Art/UI/" + name + ".png", tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path);

        // 9-slice 설정
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType        = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = size;
        importer.spriteBorder       = new Vector4(border, border, border, border);
        importer.alphaIsTransparency = true;
        importer.filterMode         = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        var settings = importer.GetDefaultPlatformTextureSettings();
        settings.format    = TextureImporterFormat.RGBA32;
        importer.SetPlatformTextureSettings(settings);
        importer.SaveAndReimport();
    }

    /// <summary>px(x,y)가 둥근 사각형 안에 있는지 판단.</summary>
    static bool IsInsideRoundRect(int x, int y, int size, int radius)
    {
        // 각 꼭짓점 원의 중심
        int cx = (x < radius) ? radius : (x >= size - radius ? size - 1 - radius : x);
        int cy = (y < radius) ? radius : (y >= size - radius ? size - 1 - radius : y);

        // 꼭짓점 영역에서는 원 거리 체크
        if (x < radius && y < radius)
            return Dist(x, y, radius, radius) <= radius;
        if (x >= size - radius && y < radius)
            return Dist(x, y, size - 1 - radius, radius) <= radius;
        if (x < radius && y >= size - radius)
            return Dist(x, y, radius, size - 1 - radius) <= radius;
        if (x >= size - radius && y >= size - radius)
            return Dist(x, y, size - 1 - radius, size - 1 - radius) <= radius;

        return true; // 꼭짓점 영역 외 = 항상 안쪽
    }

    static float Dist(int x, int y, int cx, int cy)
        => Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
}
