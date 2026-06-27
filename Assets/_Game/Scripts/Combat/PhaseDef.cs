using System;

[Serializable]
public class PhaseDef
{
    public float               hpThreshold;
    public BossPatternModule[] patterns;
    public float               patternInterval = 3f;
    public bool                exposeWeakPointOnEnter;
    public float               weakPointExposeDuration = 5f;
    public string              bannerLine;
}
