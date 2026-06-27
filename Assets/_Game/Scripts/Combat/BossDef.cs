using UnityEngine;

[CreateAssetMenu(menuName = "MSRPG/Boss/BossDef", fileName = "NewBoss")]
public class BossDef : ScriptableObject
{
    public string     displayName;
    public Continent  continent;
    public Continent  weakness;
    public int        maxHp  = 1500;
    public int        def    = 20;
    public PhaseDef[] phases;
    public string     introLine;
    public string     defeatLine;
}
