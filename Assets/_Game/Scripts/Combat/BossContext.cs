using UnityEngine;

public class BossContext
{
    public Boss      Boss           { get; }
    public Transform BossTransform  { get; }
    public Transform Player         { get; }

    public BossContext(Boss boss, Transform bossTransform, Transform player)
    {
        Boss          = boss;
        BossTransform = bossTransform;
        Player        = player;
    }
}
