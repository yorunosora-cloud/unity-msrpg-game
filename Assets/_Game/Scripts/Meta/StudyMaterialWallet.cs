using System;

/// <summary>
/// 과목별 연구 자원. 적 처치 시 약점 과목의 자원이 쌓이고,
/// K 패널에서 캐릭터 레벨업 비용으로 소모한다.
/// </summary>
[Serializable]
public class StudyMaterialWallet
{
    int[] _amounts = new int[7]; // Continent 열거형 순서와 일치 (0=Physics … 6=Mesoria)

    public int Get(Continent c)
    {
        int idx = (int)c;
        return idx >= 0 && idx < _amounts.Length ? _amounts[idx] : 0;
    }

    public void Add(Continent c, int amount)
    {
        int idx = (int)c;
        if (idx >= 0 && idx < _amounts.Length)
            _amounts[idx] = Math.Max(0, _amounts[idx] + amount);
    }

    public bool CanSpend(Continent c, int amount) => Get(c) >= amount;

    public bool TrySpend(Continent c, int amount)
    {
        if (!CanSpend(c, amount)) return false;
        _amounts[(int)c] -= amount;
        return true;
    }

    /// <summary>currentLevel → currentLevel+1 레벨업 기본 비용 (하 난이도 기준).</summary>
    public static int LevelUpCost(int currentLevel) => currentLevel * 5;

    /// <summary>
    /// 난이도별 레벨업 자원 비용.
    /// 기본 비용(level×5)에 ProblemDifficultyInfo.CostMultiplier(d)를 곱한다.
    /// </summary>
    public static int LevelUpCost(int currentLevel, ProblemDifficulty d)
        => LevelUpCost(currentLevel) * ProblemDifficultyInfo.CostMultiplier(d);

    public StudyMaterialData Export() =>
        new StudyMaterialData { amounts = (int[])_amounts.Clone() };

    public void LoadState(StudyMaterialData data)
    {
        if (data?.amounts == null) return;
        for (int i = 0; i < Math.Min(data.amounts.Length, _amounts.Length); i++)
            _amounts[i] = data.amounts[i];
    }
}

[Serializable]
public class StudyMaterialData
{
    public int[] amounts = new int[7];
}
