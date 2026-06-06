using UnityEngine;

/// <summary>
/// 적 1기의 전투 데이터 (순수 C# 클래스, MonoBehaviour 아님 — 테스트 가능).
/// Enemy MonoBehaviour가 인스턴스를 소유한다.
/// </summary>
public class EnemyUnit
{
    public int       MaxHp     { get; }
    public int       Hp        { get; private set; }
    public int       Def       { get; }
    public Continent Weakness  { get; }
    public int       ExpReward { get; }

    public bool  IsDead     => Hp <= 0;
    public float HpFraction => (float)Hp / MaxHp;

    public EnemyUnit(int maxHp, int def, Continent weakness, int expReward)
    {
        MaxHp     = Mathf.Max(1, maxHp);
        Hp        = MaxHp;
        Def       = Mathf.Max(0, def);
        Weakness  = weakness;
        ExpReward = Mathf.Max(0, expReward);
    }

    /// <summary>
    /// 지정된 양의 데미지를 적용합니다.
    /// HP를 0~MaxHp 범위로 클램프하고 실제 깎인 양을 반환합니다.
    /// </summary>
    public int TakeDamage(int amount)
    {
        int before = Hp;
        Hp = Mathf.Clamp(Hp - amount, 0, MaxHp);
        return before - Hp;
    }
}
