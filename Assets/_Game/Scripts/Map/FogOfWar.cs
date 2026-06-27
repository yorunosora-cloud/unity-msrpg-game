using UnityEngine;

public class FogOfWar : MonoBehaviour
{
    public const int GRID = 8;

    [SerializeField] float mapHalf    = 250f;
    [SerializeField] float viewRange  = 50f;   // 미니맵 viewRange와 동일

    readonly bool[,] _revealed = new bool[GRID, GRID];

    public bool IsCellRevealed(int x, int y)
    {
        if (x < 0 || x >= GRID || y < 0 || y >= GRID) return false;
        return _revealed[x, y];
    }

    void Update()
    {
        // 매 프레임 플레이어 현재 위치 기준으로 완전 재계산
        // → 미니맵에 보이는 범위와 정확히 일치
        System.Array.Clear(_revealed, 0, _revealed.Length);
        RevealInRange(transform.position, viewRange);
    }

    public Vector2Int WorldToCell(Vector3 worldPos)
    {
        int x = Mathf.Clamp(Mathf.FloorToInt((worldPos.x + mapHalf) / (mapHalf * 2f) * GRID), 0, GRID - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt((worldPos.z + mapHalf) / (mapHalf * 2f) * GRID), 0, GRID - 1);
        return new Vector2Int(x, y);
    }

    void RevealInRange(Vector3 pos, float range)
    {
        float cellSize = mapHalf * 2f / GRID;
        float rangeSq  = range * range;
        for (int x = 0; x < GRID; x++)
        for (int y = 0; y < GRID; y++)
        {
            float cx = -mapHalf + (x + 0.5f) * cellSize;
            float cz = -mapHalf + (y + 0.5f) * cellSize;
            float dx = cx - pos.x;
            float dz = cz - pos.z;
            if (dx*dx + dz*dz <= rangeSq)
                _revealed[x, y] = true;
        }
    }
}
