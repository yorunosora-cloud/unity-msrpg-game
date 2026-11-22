using System;

/// <summary>
/// 도감 심화 해설을 여는 개념 문제(4지선다, 설계도 18 §3-5·18-C 결정 1).
/// answerIndex는 0부터 시작(CSV에서는 1부터 적고 임포터가 변환한다).
/// </summary>
[Serializable]
public struct ConceptQuestion
{
    public string   question;
    public string[] choices;
    public int      answerIndex;
}

/// <summary>
/// 호감도 개념 2지선다 데이터(설계도 18 §3-14). conceptPick = 그 캐릭터의 개념 성향에 맞는 선택지(0부터).
/// 호감도 가산(AffinityService)은 10번 호감도 구현 때 이 데이터를 읽어 처리한다.
/// </summary>
[Serializable]
public struct AffinityQuestion
{
    public string   question;
    public string[] choices;
    public int      conceptPick;

    public bool IsConceptPick(int choice) => choice >= 0 && choice == conceptPick;
}
