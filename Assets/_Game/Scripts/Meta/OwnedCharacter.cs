using System;
using System.Collections.Generic;

/// <summary>스킬별 성장 정보.</summary>
[Serializable]
public class SkillProgress
{
    public string skillId;
    public int    level       = 1;  // 1~5
    public int    proficiency = 0;  // 현재 레벨에서 누적된 숙련도
}

/// <summary>플레이어가 보유 중인 캐릭터 1개의 인스턴스 데이터.</summary>
[Serializable]
public class OwnedCharacter
{
    public string id;
    public int    level = 1;
    public int    exp   = 0; // 누적 EXP (레벨업 후 잔여 보존)
    public int    dupes = 0; // 중복 획득 횟수

    // 해금된 스킬 ID 목록. 빈 목록이면 CombatCharacter 가 index 0을 기본 해금으로 간주한다.
    public List<string> unlockedSkillIds = new List<string>();

    // 스킬별 성장 데이터
    public List<SkillProgress> skillProgress = new List<SkillProgress>();

    /// <summary>주어진 skillId에 해당하는 SkillProgress를 반환한다. 없으면 새로 생성해서 반환한다.</summary>
    public SkillProgress GetSkillProgress(string skillId)
    {
        // 기존 엔트리 찾기
        foreach (var progress in skillProgress)
        {
            if (progress.skillId == skillId)
                return progress;
        }

        // 없으면 새로 생성
        var newProgress = new SkillProgress { skillId = skillId };
        skillProgress.Add(newProgress);
        return newProgress;
    }

    /// <summary>주어진 skillId의 현재 스킬 레벨을 반환한다. 없으면 1을 반환한다.</summary>
    public int SkillLevel(string skillId)
    {
        return GetSkillProgress(skillId).level;
    }
}

