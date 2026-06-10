using UnityEngine;

/// <summary>
/// MSRPG 디자인 시스템의 단일 출처 (§B-3, §D-2, §E-1).
/// 모든 패널·HUD·버튼의 색·폰트 크기·반경이 여기서 정의된다.
/// 런타임 HUD도 참조하므로 Editor 폴더가 아닌 런타임 어셈블리에 둔다.
/// 대륙색 → ContinentColors.Of(), 등급색 → RarityExtensions.DisplayColor() 재사용.
/// </summary>
public static class UITheme
{
    // ── §B-3  중립 UI 색 ──────────────────────────────────────────────────

    /// <summary>패널 배경 (어두움) #0D1120</summary>
    public static readonly Color PanelBgDark  = Hex(0x0D, 0x11, 0x20);

    /// <summary>패널 배경 (중간) #1A2240  — 반투명 오버레이 기본</summary>
    public static readonly Color PanelBgMid   = Hex(0x1A, 0x22, 0x40);

    /// <summary>패널 테두리·구분선 #2E3A5E</summary>
    public static readonly Color Border       = Hex(0x2E, 0x3A, 0x5E);

    /// <summary>주 텍스트 #E8ECFF — 거의 흰색, 약간 차갑게</summary>
    public static readonly Color TextPrimary  = Hex(0xE8, 0xEC, 0xFF);

    /// <summary>보조 텍스트 #8A9BB5 — 설명·메타 정보</summary>
    public static readonly Color TextSecondary= Hex(0x8A, 0x9B, 0xB5);

    /// <summary>비활성 텍스트 #4A5570 — 잠금·불활성</summary>
    public static readonly Color TextDisabled = Hex(0x4A, 0x55, 0x70);

    /// <summary>강조(흰 글로우) #FFFFFF — 호버·선택</summary>
    public static readonly Color Highlight    = Color.white;

    // ── 버튼 의미색 ──────────────────────────────────────────────────────

    /// <summary>기본 액션 — 파랑 (저장·확인·이동)</summary>
    public static readonly Color BtnPrimary = new Color(0.18f, 0.45f, 0.90f);

    /// <summary>위험 액션 — 빨강 (초기화·삭제·로그아웃)</summary>
    public static readonly Color BtnDanger  = new Color(0.75f, 0.15f, 0.15f);

    /// <summary>중립 액션 — 회청 (닫기·취소·뒤로)</summary>
    public static readonly Color BtnNeutral = new Color(0.22f, 0.26f, 0.38f);

    /// <summary>긍정 액션 — 초록 (레벨업·연구·해금)</summary>
    public static readonly Color BtnSuccess = new Color(0.14f, 0.52f, 0.22f);

    // ── §D-2  폰트 크기 위계 ─────────────────────────────────────────────

    /// <summary>가챠 등급 발표·화면 타이틀 (56px Bold)</summary>
    public const int FontDisplay = 56;

    /// <summary>패널 제목 (32px Bold)</summary>
    public const int FontH1 = 32;

    /// <summary>캐릭터 이름·섹션 소제목 (22px SemiBold)</summary>
    public const int FontH2 = 22;

    /// <summary>스킬 설명·설정 텍스트 (15px Regular)</summary>
    public const int FontBody = 15;

    /// <summary>쿨다운·메타 정보 (11px Regular)</summary>
    public const int FontCaption = 11;

    /// <summary>HP·MP·데미지 수치 (18px Bold)</summary>
    public const int FontStat = 18;

    // ── §E-1  형태 언어 ───────────────────────────────────────────────────

    /// <summary>패널 모서리 반경 8px</summary>
    public const int RadiusPanel  = 8;

    /// <summary>버튼 모서리 반경 6px</summary>
    public const int RadiusButton = 6;

    /// <summary>캐릭터 카드 모서리 반경 10px</summary>
    public const int RadiusCard   = 10;

    /// <summary>반투명 패널 알파 (0.92)</summary>
    public const float PanelAlpha = 0.92f;

    /// <summary>내부 여백 기본값 12px</summary>
    public const int PadInner    = 12;

    /// <summary>중요 요소 주변 여백 16px</summary>
    public const int PadEmphasis = 16;

    // ── 헬퍼 ──────────────────────────────────────────────────────────────

    /// <summary>PanelBgDark에 PanelAlpha를 적용한 반투명 버전.</summary>
    public static Color PanelBgDarkA => new Color(PanelBgDark.r, PanelBgDark.g, PanelBgDark.b, PanelAlpha);

    /// <summary>PanelBgMid에 PanelAlpha를 적용한 반투명 버전.</summary>
    public static Color PanelBgMidA  => new Color(PanelBgMid.r,  PanelBgMid.g,  PanelBgMid.b,  PanelAlpha);

    static Color Hex(byte r, byte g, byte b) =>
        new Color(r / 255f, g / 255f, b / 255f, 1f);
}
