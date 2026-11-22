/// <summary>속성에서 파생되는 물리량(실험실이 소비할 순수 함수, 설계도 18 §3-6).</summary>
public static class DerivedQuantity
{
    /// <summary>운동량 p = mv.</summary>
    public static float Momentum(AttributeProfile a) => a.mass * a.velocity;

    /// <summary>운동에너지 Ek = ½mv².</summary>
    public static float KineticEnergy(AttributeProfile a) => 0.5f * a.mass * a.velocity * a.velocity;
}
