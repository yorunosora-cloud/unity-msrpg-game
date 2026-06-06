using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 스폰 데이터를 보관하고 리스폰 시 모든 적을 재생성한다.
/// Enemy.Start()에서 TryRegister를 호출해 자동 등록. 리스폰된 적은 중복 등록 방지.
/// </summary>
public static class EnemySpawner
{
    struct SpawnSpec { public Vector3 Position; public Continent Weakness; }

    static readonly List<SpawnSpec> _templates = new();

    /// <summary>
    /// Enemy.Start()가 호출. 동일 위치가 이미 등록돼 있으면 무시
    /// (리스폰 후 재생성된 적이 중복 등록되는 것을 막음).
    /// </summary>
    public static void TryRegister(Vector3 position, Continent weakness)
    {
        foreach (var t in _templates)
            if (Vector3.Distance(t.Position, position) < 0.5f) return;
        _templates.Add(new SpawnSpec { Position = position, Weakness = weakness });
    }

    /// <summary>현재 살아있는 적을 모두 제거하고 등록된 템플릿에서 재생성한다.</summary>
    public static void RespawnAll()
    {
        foreach (var e in Enemy.All.ToArray())
            if (e != null) Object.Destroy(e.gameObject);

        foreach (var t in _templates)
        {
            var go = new GameObject($"Enemy_{t.Weakness}");
            go.transform.position = t.Position;
            var enemy = go.AddComponent<Enemy>();
            enemy.SetWeakness(t.Weakness); // Start 전에 weakness 주입
        }
    }
}
