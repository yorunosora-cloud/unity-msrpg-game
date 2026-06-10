using UnityEngine;

/// <summary>
/// 액티브 스킬 1종의 정적 정의 (설계 §5).
/// MSRPG > Seed Skills 에디터 메뉴로 플레이스홀더 에셋을 생성하세요.
/// CharacterDef.skills[] 배열에서 참조한다.
/// </summary>
[CreateAssetMenu(menuName = "MSRPG/Skill Definition", fileName = "New Skill")]
public class SkillDef : ScriptableObject
{
    [Header("기본 정보")]
    public string         id;
    public string         nameKo;
    [TextArea(2, 5)]
    public string         descKo;   // 한국어 설명 (스킬 정보 패널에 표시)

    [Header("발동 조건")]
    public int   mpCost;     // 소비 MP
    public float cooldown;   // 쿨타임 (초)

    [Header("효과 범주")]
    public SkillEffectKind effectKind;

    [Header("타겟 범위 (Strike / Aoe / Mark 공통)")]
    public float range     = 3.0f;  // 최대 사거리
    public float halfAngle = 70f;   // 전방 기준 ±각도

    [Header("Strike / Aoe — 데미지 배율")]
    [Tooltip("활성 캐릭터 Atk × 배율 → 최종 공격력")]
    public float damageMultiplier = 1.5f;

    [Header("HealBuff — 회복 · 버프")]
    [Tooltip("회복량 = MaxHp × healPercent")]
    public float healPercent       = 0.20f;
    [Tooltip("공격력 배율 (1.0 = 변화 없음)")]
    public float buffAtkMultiplier = 1.30f;
    [Tooltip("버프 지속 시간 (초)")]
    public float buffDuration      = 5f;
}
