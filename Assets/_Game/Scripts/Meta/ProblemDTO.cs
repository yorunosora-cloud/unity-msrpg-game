using System;

/// <summary>
/// ProblemDef(ScriptableObject)를 JSON 직렬화 가능한 플레인 데이터로 미러링한다.
/// TitleData/CloudScript 왕복에 사용. enum은 int로 저장.
/// </summary>
[Serializable]
public class ProblemDTO
{
    public string id;
    public string skillId;
    public int    difficulty;
    public int    subject;
    public string country;
    public int    type;
    public string prompt;
    public string[] choices;
    public int    correctIndex;
    public string[] acceptedAnswers;
    public string explanation;

    public static ProblemDTO FromDef(ProblemDef def)
    {
        return new ProblemDTO
        {
            id              = def.id,
            skillId         = def.skillId,
            difficulty      = (int)def.difficulty,
            subject         = (int)def.subject,
            country         = def.country,
            type            = (int)def.type,
            prompt          = def.prompt,
            choices         = def.choices,
            correctIndex    = def.correctIndex,
            acceptedAnswers = def.acceptedAnswers,
            explanation     = def.explanation,
        };
    }

    public void ApplyTo(ProblemDef def)
    {
        def.id              = id;
        def.skillId         = skillId;
        def.difficulty      = (ProblemDifficulty)difficulty;
        def.subject         = (Continent)subject;
        def.country         = country;
        def.type            = (ProblemType)type;
        def.prompt          = prompt;
        def.choices         = choices;
        def.correctIndex    = correctIndex;
        def.acceptedAnswers = acceptedAnswers;
        def.explanation     = explanation;
    }
}

/// <summary>TitleData "problems" 키에 저장되는 JSON 최상위 구조.</summary>
[Serializable]
public class ProblemStore
{
    public ProblemDTO[] items      = new ProblemDTO[0];
    public string[]     deletedIds = new string[0];
}
