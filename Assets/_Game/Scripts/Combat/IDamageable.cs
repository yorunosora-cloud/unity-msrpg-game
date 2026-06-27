using UnityEngine;

public interface IDamageable
{
    void      ReceiveHit(Continent attackerElement, int atk);
    Transform Transform { get; }
    bool      IsDead { get; }
}
