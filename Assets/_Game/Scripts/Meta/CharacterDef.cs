using UnityEngine;

/// <summary>
/// 캐릭터 1종의 정적 정의 (설계 §9 스키마).
/// MSRPG > Seed Placeholder Characters 에디터 메뉴로 시드 에셋을 생성하세요.
/// </summary>
[CreateAssetMenu(menuName = "MSRPG/Character Definition", fileName = "New Character")]
public class CharacterDef : ScriptableObject
{
    [Header("기본 정보")]
    public string id;
    public string nameKo;
    public string nameEn;

    [Header("분류 (설계 §2)")]
    public Continent    continent;
    public string       country;   // 세부 국가 ID (예: "newton-empire")
    public Rarity       rarity;
    public CharacterRole role;

    [Header("스탯 (레벨 1 기준, 설계 §3)")]
    public CharacterStats baseStats;

    [Header("비주얼 (플레이스홀더)")]
    public Color portraitColor = Color.white;

    [Header("액티브 스킬 (설계 §5) — 인덱스 순서대로 E/R/T/F/V/G 키에 매핑")]
    public SkillDef[] skills;  // 보유 수만큼 키 활성. 장착/해제 없이 전부 상시 발동 가능.

    // TODO: synergy   — 시너지 시스템 (설계 §6) 구현 시 추가
    // TODO: acquisition — 조건 해금 (설계 §7-2) 구현 시 추가
}
