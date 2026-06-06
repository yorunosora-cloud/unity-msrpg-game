using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class GridFloor : MonoBehaviour
{
    [SerializeField] Color cellColor = new(0.52f, 0.52f, 0.52f);
    [SerializeField] Color lineColor = new(0.25f, 0.25f, 0.25f);
    [SerializeField] Vector2 tiling  = new(100f, 100f);

    void Awake()
    {
        // 16×16 텍스처, 테두리 2px = 그리드 선
        const int size   = 16;
        const int border = 2;

        var tex = new Texture2D(size, size, TextureFormat.RGB24, mipChain: false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode   = TextureWrapMode.Repeat;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            bool isLine = x < border || y < border;
            tex.SetPixel(x, y, isLine ? lineColor : cellColor);
        }
        tex.Apply();

        var mr  = GetComponent<MeshRenderer>();
        var mat = mr.material;
        mat.color            = Color.white;
        mat.SetTexture("_BaseMap", tex);
        mat.SetTextureScale("_BaseMap", tiling);
    }
}
