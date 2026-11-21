using UnityEngine;

[CreateAssetMenu(menuName = "MSRPG/Boss/BossDef", fileName = "NewBoss")]
public class BossDef : ScriptableObject
{
    public string     displayName;
    public Continent  continent;
    public Continent  weakness;
    public int        maxHp  = 1500;
    public int        def    = 20;
    [Tooltip("true면 스턴·넉백·견인 면역 (둔화·도트·받는피해배율·표식은 받는다). 설계도 22 §4-9 잠정안")]
    public bool       ccImmune = true;
    public PhaseDef[] phases;
    public string     introLine;
    public string     defeatLine;
}
