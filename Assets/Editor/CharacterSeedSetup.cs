using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// MSRPG > Seed Placeholder Characters 메뉴.
/// 13종 CharacterDef + 캐릭터별 스킬 3종 + 스킬 해금 문제 + 레벨업 문제를 생성합니다.
/// </summary>
public static class CharacterSeedSetup
{
    const string CHAR_DIR        = "Assets/_Game/Data/Characters";
    const string SKILL_DIR       = "Assets/_Game/Data/Skills";
    const string PROBLEM_DIR     = "Assets/_Game/Data/Problems";
    const string DB_PATH         = "Assets/_Game/Resources/CharacterDatabase.asset";
    const string PROBLEM_DB_PATH = "Assets/_Game/Resources/ProblemDatabase.asset";

    // ── 캐릭터 시드 ─────────────────────────────────────────────────────────
    struct Seed
    {
        public string id, nameKo, nameEn, country;
        public Rarity rarity;
        public CharacterRole role;
        public Continent continent;
        public int hp, atk, def, spd, mp;
        public Color color;

        // 새로 추가
        public int dexNumber;
        public Gender gender;
        public string concept;
        public string appearance;
        public string weapon;
        public string loreKo;
        public SynergyKind synergyKind;
        public string[] synergyMarkedBy;   // null이면 빈 배열
        public string synergyComboName;
        public bool gachaObtainable;       // 기본 true
        public string acquireCondition;    // null이면 ""
    }

    // ── dexNumber 체계 ─────────────────────────────────────────────────────────
    // 물리(Physics)   :   1– 99
    // 화학(Chemistry) : 101–199
    // 생명(Biology)   : 201–299
    // 지구과학(Earth) : 301–399
    // 수학(Math)      : 401–499
    // 정보(Info)      : 501–599
    // 비배정(0)       : 도감 정렬 시 맨 뒤로 처리됨
    // inertia_r(관성) : 비물리 플레이스홀더. 향후 물리 7종과 별개 엔트리로 유지
    // ──────────────────────────────────────────────────────────────────────────
    static readonly Seed[] Seeds =
    {
        // ── 비물리 캐릭터 (기본값 그대로) ───────────────────────────────────
        new Seed { id="cell_n",     nameKo="세포",   nameEn="Cell",    rarity=Rarity.N,   role=CharacterRole.Tanker,     continent=Continent.Biology,  country="life-basics",       hp=400,  atk=30,  def=40,  spd=80,  mp=60,  color=new Color(0.239f,0.769f,0.153f) },
        new Seed { id="number_n",   nameKo="자연수", nameEn="Number",  rarity=Rarity.N,   role=CharacterRole.Dealer,     continent=Continent.Math,     country="arithmetic",        hp=350,  atk=45,  def=25,  spd=90,  mp=50,  color=new Color(0.784f,0.824f,0.000f) },
        // 레거시 플레이스홀더 — 물리 7종(mass_sr)과 별개 엔트리. dexNumber 미배정(0).
        new Seed { id="inertia_r",  nameKo="관성",   nameEn="Inertia", rarity=Rarity.R,   role=CharacterRole.Tanker,     continent=Continent.Physics,  country="newton-empire",     hp=700,  atk=60,  def=90,  spd=80,  mp=80,  color=new Color(0.169f,0.498f,1.000f) },
        new Seed { id="ion_r",      nameKo="이온",   nameEn="Ion",     rarity=Rarity.R,   role=CharacterRole.Dealer,     continent=Continent.Chemistry,country="bonding",           hp=600,  atk=80,  def=50,  spd=95,  mp=100, color=new Color(1.000f,0.090f,0.267f) },
        new Seed { id="dna_ssr",    nameKo="DNA",    nameEn="DNA",     rarity=Rarity.SSR, role=CharacterRole.AllRounder, continent=Continent.Biology,  country="genetics",          hp=1200, atk=110, def=90,  spd=95,  mp=140, color=new Color(0.239f,0.769f,0.153f) },
        new Seed { id="riemann_ur", nameKo="리만",   nameEn="Riemann", rarity=Rarity.UR,  role=CharacterRole.Dealer,     continent=Continent.Math,     country="analysis",          hp=1200, atk=180, def=80,  spd=130, mp=150, color=new Color(0.784f,0.824f,0.000f) },

        // ── 물리 7종 ─────────────────────────────────────────────────────────

        // dexNumber=1 — 자기장
        new Seed
        {
            id="magfield_r", nameKo="자기장", nameEn="Magnetic Field",
            rarity=Rarity.R, role=CharacterRole.Supporter,
            continent=Continent.Physics, country="maxwell-duchy",
            hp=700, atk=65, def=70, spd=95, mp=120,
            color=new Color(0.169f, 0.498f, 1.000f),
            dexNumber=1, gender=Gender.Male,
            concept="다프트 펑크 스타일 헬멧의 DJ. 자기장으로 금속 물질을 지배한다.",
            appearance="투명 강화유리+금속 프레임 헬멧, 내부에 자기장 선이 흐름. 슬림한 DJ 재킷 소매와 등판에 자기장 회로 패턴. 가슴 중앙에 원형 자기장 코어. 손가락에 자기 컨트롤 링.",
            weapon="DJ 도구 (턴테이블·패드)",
            loreKo="다프트 펑크 스타일의 헬멧을 착용한 DJ. 자기장(B, 테슬라 단위)은 운동하는 전하 주위에 형성되는 힘의 장으로, 금속에 강한 인력·척력을 미친다. 그는 디제잉으로 자기장 공간을 형성해 금속 속성 공격 피해를 대폭 줄이고, '전자기력' 버프로 전기 속성 아군의 능력을 강화한다. 전기장과 자기장이 서로 유도하는 패러데이·맥스웰 방정식은 그의 연주에 담겨 있다.",
            synergyKind=SynergyKind.PartyPassive, synergyMarkedBy=null,
            synergyComboName="전자기력 (전기 속성 아군 버프)",
            gachaObtainable=true, acquireCondition=null,
        },

        // dexNumber=2 — 길이·공간
        new Seed
        {
            id="length_sr", nameKo="길이·공간", nameEn="Length·Space",
            rarity=Rarity.SR, role=CharacterRole.Tanker,
            continent=Continent.Physics, country="newton-empire",
            hp=1000, atk=70, def=130, spd=85, mp=100,
            color=new Color(0.169f, 0.498f, 1.000f),
            dexNumber=2, gender=Gender.Female,
            concept="n차원 공간 관리자. 3차원 세계의 보수·유지를 위해 파견된 직책.",
            appearance="반쯤 걸친 실험복과 오피스룩, 다크서클, 푸석한 장발 흑발, 초점 없는 눈동자. 정장 앞주머니에 컴퍼스가 꽂혀있다.",
            weapon="자 / 컴퍼스",
            loreKo="n차원 공간의 점검·보수를 담당하는 차원 관리자. 길이는 두 점 사이의 거리—m(미터) 단위의 기본 물리량—이며, 그녀는 임의의 두 지점을 잇는 포탈을 설치해 거리를 '수축'시킬 수 있다. 공간을 조작하는 '길이 수축' 능력은 물리학에서 상대론적 고속 이동 시 공간이 줄어드는 현상을 반영한다. 만사가 귀찮다는 표정이지만, 상대의 공격이 닿는 거리를 늘려 무효화하는 '고유 공간' 능력은 파티 최강의 방어력을 자랑한다.",
            synergyKind=SynergyKind.PartyPassive, synergyMarkedBy=null,
            synergyComboName="길이 수축 (파티 이동 지원)",
            gachaObtainable=true, acquireCondition=null,
        },

        // dexNumber=3 — 시간
        new Seed
        {
            id="time_sr", nameKo="시간", nameEn="Time",
            rarity=Rarity.SR, role=CharacterRole.Supporter,
            continent=Continent.Physics, country="newton-empire",
            hp=900, atk=70, def=80, spd=100, mp=130,
            color=new Color(0.169f, 0.498f, 1.000f),
            dexNumber=3, gender=Gender.Male,
            concept="시간 관리국(時間管理局)에서 파견된 현장 직원.",
            appearance="태엽 시계 색상의 정장과 원통형 모자를 쓴 미소년. 녹색 단발 머리, 한쪽에만 달린 줄 달린 외안경, 회중시계를 항상 지니고 다닌다.",
            weapon="시계 바늘 (초침·분침·시침)",
            loreKo="시간 관리국(時間管理局)에서 파견된 현장 직원. '시간'이란 사건이 일어나는 순서를 구분하고 그 간격을 측정하는 물리량—초(s) 단위로 정의된다. 그는 우주 곳곳의 시간 흐름이 고르게 유지되도록 점검하며, 파티에 합류하면 자신의 '고유 시간' 감각으로 아군의 스킬 재사용 대기 시간을 단축시킨다. 이동하는 적에게는 상대론적 '시간 지연'이 적용되어 더 큰 피해를 입힌다고 주장한다.",
            synergyKind=SynergyKind.PartyPassive, synergyMarkedBy=null,
            synergyComboName="고유 시간 (아군 쿨타임 감소)",
            gachaObtainable=true, acquireCondition=null,
        },

        // dexNumber=4 — 전하량·전기장
        new Seed
        {
            id="charge_r", nameKo="전하량·전기장", nameEn="Charge·E-Field",
            rarity=Rarity.R, role=CharacterRole.Dealer,
            continent=Continent.Physics, country="maxwell-duchy",
            hp=650, atk=90, def=50, spd=100, mp=110,
            color=new Color(0.169f, 0.498f, 1.000f),
            dexNumber=4, gender=Gender.Female,
            concept="전하를 에너지원으로 삼는 일렉트릭 로커.",
            appearance="하얀 베이스 머리에 한쪽은 붉은색, 다른 쪽은 푸른색 할리퀸 헤어스타일 장발 양갈래. 양 볼에 붉은 + 기호와 푸른 - 기호 타투. 화려한 일렉 기타와 휴대용 스피커를 들고 다닌다.",
            weapon="일렉 기타 / 스피커",
            loreKo="전하를 에너지원으로 삼는 일렉트릭 로커. 전하(q, 쿨롱 단위)는 물질이 전기장 안에서 힘을 받는 성질이며, 양(+)/음(-) 부호에 따라 인력과 척력이 생긴다. 그녀는 이동할수록 전하 에너지를 누적하고 기타 연주로 폭발시키며, '교류(AC)'로 도트 데미지 스택을 쌓은 후 '직류(DC)'로 한꺼번에 방전하는 ACDC 기술이 주특기다. 전기장(E)과 전하(q)의 관계 F=qE는 빛 캐릭터와의 연계기 이름이기도 하다.",
            synergyKind=SynergyKind.Mark, synergyMarkedBy=null,
            synergyComboName="전기력 (F=qE)",
            gachaObtainable=true, acquireCondition=null,
        },

        // dexNumber=5 — 빛
        new Seed
        {
            id="light_ssr", nameKo="빛", nameEn="Light",
            rarity=Rarity.SSR, role=CharacterRole.Dealer,
            continent=Continent.Physics, country="maxwell-duchy",
            hp=1000, atk=130, def=70, spd=110, mp=120,
            color=new Color(0.169f, 0.498f, 1.000f),
            dexNumber=5, gender=Gender.Male,
            concept="빛의 이중성(wave-particle duality)을 몸으로 구현하는 변신형 캐릭터.",
            appearance="입자성: 노란 금발의 야구 투수, 광자(노란 공)을 들고 있다. 파동성: 금발 치어리더, 치어리딩 도구를 양손에 들고 있다.",
            weapon="광자 (입자형) / 치어리딩 도구 (파동형)",
            loreKo="빛의 이중성(wave-particle duality)을 몸으로 구현하는 변신형 캐릭터. 빛은 전자기파(파동)이면서 동시에 광자(입자)로도 행동한다—두 성질은 관측 방법에 따라 드러난다. 입자성 형태에선 광자를 투구처럼 던지는 야구 선수, 파동성 형태에선 치어리더로 변신하며, '간섭'으로 방어와 공격을 동시에 강화하거나 '광전효과'로 전기 피해를 입힌다.",
            synergyKind=SynergyKind.Mark, synergyMarkedBy=null,
            synergyComboName="광전효과 · 전기력",
            gachaObtainable=true, acquireCondition=null,
        },

        // dexNumber=6 — 쿼크
        new Seed
        {
            id="quark_ur", nameKo="쿼크", nameEn="Quark",
            rarity=Rarity.UR, role=CharacterRole.AllRounder,
            continent=Continent.Physics, country="quantum-rebellion",
            hp=1400, atk=160, def=100, spd=120, mp=160,
            color=new Color(0.169f, 0.498f, 1.000f),
            dexNumber=6, gender=Gender.Neutral,
            concept="업·다운·스트레인지·참·탑·바텀 6종 쿼크를 저글링 볼로 다루는 올라운더 조커.",
            appearance="트럼프 카드의 조커 복장. 삼원색 그라데이션 중단발, 빛의 삼원색과 흑백이 합쳐진 특수한 이중 동공.",
            weapon="저글링 볼 (쿼크)",
            loreKo="업·다운·스트레인지·참·탑·바텀—6종 쿼크를 저글링 볼로 자유자재로 다루는 조커. 쿼크는 강한 핵력으로 '가둠(confinement)'되어 홀로 존재하지 못하고, 반드시 반쿼크나 다른 쿼크와 결합된 상태로 관측된다. 각 쿼크는 분수 전하(+2/3 또는 -1/3)와 색전하(적·녹·청)를 가지며, '불확정성 원리'로 공격을 회피하거나 '바텀 쿼크' 패시브로 한 번의 죽음을 무효화한다.",
            synergyKind=SynergyKind.Mark, synergyMarkedBy=null,
            synergyComboName="색 가둠 (쿼크-반쿼크 결합)",
            gachaObtainable=true, acquireCondition=null,
        },

        // dexNumber=7 — 질량·관성
        new Seed
        {
            id="mass_sr", nameKo="질량", nameEn="Mass",
            rarity=Rarity.SR, role=CharacterRole.Tanker,
            continent=Continent.Physics, country="newton-empire",
            hp=1100, atk=80, def=120, spd=80, mp=110,
            color=new Color(0.169f, 0.498f, 1.000f),
            dexNumber=7, gender=Gender.Male,
            concept="엄숙한 기사의 외면 아래 다혈질 광인을 숨긴 이중인격 탱커.",
            appearance="엄숙한 갑옷의 기사. 전투 중 충분한 피해를 받으면 갑옷에 균열이 생기며 다른 인격이 드러난다.",
            weapon="방패 / 대검",
            loreKo="엄숙한 기사의 외면 아래 다혈질 광인을 숨긴 이중인격 탱커. 질량(m, 킬로그램)은 물체가 가진 물질의 양이자 관성의 크기를 나타낸다—질량이 클수록 상태 변화에 저항한다. 평소엔 '관성'처럼 쉽게 움직이지 않는 엄숙한 기사지만, 전투 중 충분한 타격을 받으면 '관성이 깨지듯' 다른 인격이 폭발한다. '질량 표식'을 가속도 캐릭터에게 전달해 F=ma 연계를 발동시키는 것이 이 파티의 핵심 전략이다.",
            synergyKind=SynergyKind.Mark, synergyMarkedBy=null,
            synergyComboName="F=ma · 힘",
            gachaObtainable=true, acquireCondition=null,
        },
    };

    // ── 역할별 스킬 템플릿 (슬롯 0 = 기본 해금, 슬롯 1·2 = 잠금) ────────────
    struct SkillTemplate
    {
        public SkillEffectKind effectKind;
        public int   mpCost;
        public float cooldown, range, halfAngle, damageMultiplier;
        public float healPercent, buffAtkMultiplier, buffDuration;
    }

    static SkillTemplate[] TemplatesFor(CharacterRole role)
    {
        switch (role)
        {
            case CharacterRole.Dealer:
                return new SkillTemplate[] {
                    new SkillTemplate { effectKind=SkillEffectKind.Strike,   mpCost=20, cooldown=1.0f, range=2.5f, halfAngle=60f, damageMultiplier=1.5f },
                    new SkillTemplate { effectKind=SkillEffectKind.Aoe,      mpCost=35, cooldown=3.0f, range=3.0f, halfAngle=80f, damageMultiplier=1.8f },
                    new SkillTemplate { effectKind=SkillEffectKind.Mark,     mpCost=25, cooldown=4.0f, range=4.5f, halfAngle=90f },
                };
            case CharacterRole.Tanker:
                return new SkillTemplate[] {
                    new SkillTemplate { effectKind=SkillEffectKind.Strike,   mpCost=20, cooldown=1.5f, range=2.2f, halfAngle=45f, damageMultiplier=1.2f },
                    new SkillTemplate { effectKind=SkillEffectKind.Strike,   mpCost=45, cooldown=4.0f, range=2.2f, halfAngle=45f, damageMultiplier=2.5f },
                    new SkillTemplate { effectKind=SkillEffectKind.HealBuff, mpCost=30, cooldown=8.0f, healPercent=0.15f, buffAtkMultiplier=1.2f, buffDuration=5f },
                };
            case CharacterRole.Supporter:
                return new SkillTemplate[] {
                    new SkillTemplate { effectKind=SkillEffectKind.HealBuff, mpCost=25, cooldown=6.0f, healPercent=0.15f, buffAtkMultiplier=1.2f, buffDuration=4f },
                    new SkillTemplate { effectKind=SkillEffectKind.Aoe,      mpCost=30, cooldown=3.0f, range=3.0f, halfAngle=80f, damageMultiplier=1.2f },
                    new SkillTemplate { effectKind=SkillEffectKind.HealBuff, mpCost=50, cooldown=10f,  healPercent=0.30f, buffAtkMultiplier=1.4f, buffDuration=8f },
                };
            default: // AllRounder + fallback
                return new SkillTemplate[] {
                    new SkillTemplate { effectKind=SkillEffectKind.Strike,   mpCost=20, cooldown=1.0f, range=2.5f, halfAngle=60f, damageMultiplier=1.4f },
                    new SkillTemplate { effectKind=SkillEffectKind.Aoe,      mpCost=35, cooldown=3.5f, range=3.0f, halfAngle=80f, damageMultiplier=1.6f },
                    new SkillTemplate { effectKind=SkillEffectKind.HealBuff, mpCost=35, cooldown=8.0f, healPercent=0.20f, buffAtkMultiplier=1.3f, buffDuration=6f },
                };
        }
    }

    static string EffectDescKo(SkillEffectKind k, int slot) => k switch
    {
        SkillEffectKind.Strike   => slot == 0 ? "대상을 강타하여 피해를 입힙니다." : "강력한 타격으로 큰 피해를 입힙니다.",
        SkillEffectKind.Aoe      => "전방 범위 내 적들에게 피해를 입힙니다.",
        SkillEffectKind.HealBuff => slot == 0 ? "체력을 회복합니다." : "체력을 크게 회복하고 공격력을 높입니다.",
        SkillEffectKind.Mark     => "적에게 속성 표식을 부여합니다.",
        _                         => "",
    };

    // ── 스킬 연구 문제 풀 (6종, 순환 할당) ─────────────────────────────────
    struct QuestionTemplate
    {
        public ProblemType type;
        public string prompt, explanation;
        public string[] choices;
        public int correctIndex;
        public string[] acceptedAnswers;
    }

    static readonly QuestionTemplate[] QuestionPool = new QuestionTemplate[]
    {
        new QuestionTemplate { type=ProblemType.MultipleChoice,
            prompt="뉴턴의 운동 제2법칙에 해당하는 공식은?",
            choices=new[]{"F = ma","E = mc²","PV = nRT","v = d/t"}, correctIndex=0,
            explanation="F = ma : 힘은 질량과 가속도의 곱입니다." },
        new QuestionTemplate { type=ProblemType.MultipleChoice,
            prompt="물체의 운동에너지를 올바르게 나타낸 것은?",
            choices=new[]{"Ek = ½mv²","Ek = mgh","Ek = Fd","Ek = mv"}, correctIndex=0,
            explanation="운동에너지 Ek = ½mv²" },
        new QuestionTemplate { type=ProblemType.MultipleChoice,
            prompt="파동의 속력(v), 진동수(f), 파장(λ)의 관계식은?",
            choices=new[]{"v = fλ","v = f/λ","v = λ/f","v = f+λ"}, correctIndex=0,
            explanation="파동 속력 v = fλ" },
        new QuestionTemplate { type=ProblemType.MultipleChoice,
            prompt="다음 중 금(Gold)의 원소 기호는?",
            choices=new[]{"Au","Ag","Fe","Cu"}, correctIndex=0,
            explanation="금의 원소 기호는 Au(Aurum)입니다." },
        new QuestionTemplate { type=ProblemType.FreeInput,
            prompt="세포 내에서 ATP를 합성해 에너지를 공급하는 소기관의 이름을 쓰시오.",
            acceptedAnswers=new[]{"미토콘드리아","mitochondria"},
            explanation="미토콘드리아는 세포호흡을 통해 ATP를 생산합니다." },
        new QuestionTemplate { type=ProblemType.FreeInput,
            prompt="SI 단위계에서 속도의 단위를 쓰시오. (예: m/s 형식)",
            acceptedAnswers=new[]{"m/s","m s-1"},
            explanation="속도의 SI 단위는 m/s입니다." },
    };

    // ── 레벨업 전용 문제 시드 (skillId 없음) ────────────────────────────────
    struct LvSeed
    {
        public string id;
        public ProblemType type;
        public ProblemDifficulty difficulty;
        public string prompt, explanation;
        public string[] choices;
        public int correctIndex;
        public string[] acceptedAnswers;
    }

    static readonly LvSeed[] LvSeeds = new LvSeed[]
    {
        new LvSeed { id="lv_low_mc", type=ProblemType.MultipleChoice, difficulty=ProblemDifficulty.Low,
            prompt="[하 / 객관식] 보기 중 올바른 것은?",
            choices=new[]{"보기 1 (정답)","보기 2","보기 3","보기 4"}, correctIndex=0,
            explanation="[하 난이도] 해설." },
        new LvSeed { id="lv_low_fi", type=ProblemType.FreeInput, difficulty=ProblemDifficulty.Low,
            prompt="[하 / 주관식] 답을 입력하세요.",
            acceptedAnswers=new[]{"정답","answer"}, explanation="[하 난이도] 해설." },
        new LvSeed { id="lv_mid_mc", type=ProblemType.MultipleChoice, difficulty=ProblemDifficulty.Mid,
            prompt="[중 / 객관식] 보기 중 올바른 것은?",
            choices=new[]{"보기 1 (정답)","보기 2","보기 3","보기 4"}, correctIndex=0,
            explanation="[중 난이도] 해설." },
        new LvSeed { id="lv_mid_fi", type=ProblemType.FreeInput, difficulty=ProblemDifficulty.Mid,
            prompt="[중 / 주관식] 답을 입력하세요.",
            acceptedAnswers=new[]{"정답","answer"}, explanation="[중 난이도] 해설." },
        new LvSeed { id="lv_high_mc", type=ProblemType.MultipleChoice, difficulty=ProblemDifficulty.High,
            prompt="[상 / 객관식] 보기 중 올바른 것은?",
            choices=new[]{"보기 1 (정답)","보기 2","보기 3","보기 4"}, correctIndex=0,
            explanation="[상 난이도] 해설." },
        new LvSeed { id="lv_high_fi", type=ProblemType.FreeInput, difficulty=ProblemDifficulty.High,
            prompt="[상 / 주관식] 답을 입력하세요.",
            acceptedAnswers=new[]{"정답","answer"}, explanation="[상 난이도] 해설." },
    };

    // ── 메인 ─────────────────────────────────────────────────────────────────
    [MenuItem("MSRPG/Seed Placeholder Characters")]
    public static void Run()
    {
        foreach (var dir in new[] { CHAR_DIR, SKILL_DIR, PROBLEM_DIR, "Assets/_Game/Resources" })
            Directory.CreateDirectory(dir.Replace("Assets", Application.dataPath));
        AssetDatabase.Refresh();

        var defs        = new List<CharacterDef>();
        var allProblems = new List<ProblemDef>();

        for (int ci = 0; ci < Seeds.Length; ci++)
        {
            var s = Seeds[ci];

            // 1. CharacterDef 생성·갱신
            var def = SeedCharDef(s);
            defs.Add(def);

            // 2. 캐릭터별 스킬 3종 생성 + 할당
            var templates = TemplatesFor(s.role);
            var skillDefs = new SkillDef[templates.Length];
            for (int si = 0; si < templates.Length; si++)
                skillDefs[si] = SeedSkillDef(s, si, templates[si]);
            AssignSkills(def, skillDefs);

            // 3. 잠금 스킬(슬롯 1, 2)에 해금 문제 생성
            for (int slot = 1; slot < templates.Length; slot++)
            {
                int qIdx = (ci * (templates.Length - 1) + (slot - 1)) % QuestionPool.Length;
                allProblems.Add(SeedSkillProblem(s, slot, QuestionPool[qIdx]));
            }
        }

        // 4. 레벨업 전용 문제
        foreach (var ls in LvSeeds)
            allProblems.Add(SeedLevelProblem(ls));

        // 5. 데이터베이스 빌드
        BuildCharDatabase(defs);
        BuildProblemDatabase(allProblems);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[MSRPG] ✅ {defs.Count}종 캐릭터  |  {defs.Count * 3}종 스킬  |  {allProblems.Count}종 문제 시드 완료\n" +
                  $"  캐릭터: {CHAR_DIR}/  스킬: {SKILL_DIR}/  문제: {PROBLEM_DIR}/");
    }

    // ── CharacterDef 생성·갱신 ───────────────────────────────────────────────
    static CharacterDef SeedCharDef(Seed s)
    {
        string path = $"{CHAR_DIR}/{s.id}.asset";
        var def = AssetDatabase.LoadAssetAtPath<CharacterDef>(path);
        if (def == null)
        {
            def = ScriptableObject.CreateInstance<CharacterDef>();
            AssetDatabase.CreateAsset(def, path);
        }

        var so = new SerializedObject(def);
        so.FindProperty("id").stringValue           = s.id;
        so.FindProperty("nameKo").stringValue       = s.nameKo;
        so.FindProperty("nameEn").stringValue       = s.nameEn;
        so.FindProperty("rarity").enumValueIndex    = (int)s.rarity;
        so.FindProperty("role").enumValueIndex      = (int)s.role;
        so.FindProperty("continent").enumValueIndex = (int)s.continent;
        so.FindProperty("country").stringValue      = s.country;
        so.FindProperty("portraitColor").colorValue = s.color;
        var stats = so.FindProperty("baseStats");
        stats.FindPropertyRelative("hp").intValue   = s.hp;
        stats.FindPropertyRelative("atk").intValue  = s.atk;
        stats.FindPropertyRelative("def").intValue  = s.def;
        stats.FindPropertyRelative("spd").intValue  = s.spd;
        stats.FindPropertyRelative("mp").intValue   = s.mp;

        // 도감 번호
        so.FindProperty("dexNumber").intValue = s.dexNumber;

        // 프로필
        so.FindProperty("gender").enumValueIndex  = (int)s.gender;
        so.FindProperty("concept").stringValue    = s.concept ?? "";
        so.FindProperty("appearance").stringValue = s.appearance ?? "";
        so.FindProperty("weapon").stringValue     = s.weapon ?? "";

        // 로어
        so.FindProperty("loreKo").stringValue = s.loreKo ?? "";

        // 시너지
        so.FindProperty("synergyKind").enumValueIndex   = (int)s.synergyKind;
        so.FindProperty("synergyComboName").stringValue = s.synergyComboName ?? "";
        var markedByProp = so.FindProperty("synergyMarkedBy");
        var markedByArr  = s.synergyMarkedBy;
        if (markedByArr == null || markedByArr.Length == 0)
        {
            markedByProp.arraySize = 0;
        }
        else
        {
            markedByProp.arraySize = markedByArr.Length;
            for (int i = 0; i < markedByArr.Length; i++)
                markedByProp.GetArrayElementAtIndex(i).stringValue = markedByArr[i];
        }

        // 획득
        so.FindProperty("gachaObtainable").boolValue      = s.gachaObtainable;
        so.FindProperty("acquireCondition").stringValue   = s.acquireCondition ?? "";

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(def);
        return def;
    }

    // ── SkillDef 생성·갱신 ───────────────────────────────────────────────────
    static SkillDef SeedSkillDef(Seed s, int slot, SkillTemplate t)
    {
        string skillId = $"{s.id}_s{slot}";
        string path    = $"{SKILL_DIR}/{skillId}.asset";
        var def = AssetDatabase.LoadAssetAtPath<SkillDef>(path);
        if (def == null)
        {
            def = ScriptableObject.CreateInstance<SkillDef>();
            AssetDatabase.CreateAsset(def, path);
        }

        var so = new SerializedObject(def);
        so.FindProperty("id").stringValue               = skillId;
        so.FindProperty("nameKo").stringValue           = $"{s.nameKo} 스킬 {slot + 1}";
        so.FindProperty("descKo").stringValue           = EffectDescKo(t.effectKind, slot);
        so.FindProperty("effectKind").enumValueIndex    = (int)t.effectKind;
        so.FindProperty("mpCost").intValue              = t.mpCost;
        so.FindProperty("cooldown").floatValue          = t.cooldown;
        so.FindProperty("range").floatValue             = t.range;
        so.FindProperty("halfAngle").floatValue         = t.halfAngle;
        so.FindProperty("damageMultiplier").floatValue  = t.damageMultiplier;
        so.FindProperty("healPercent").floatValue       = t.healPercent;
        so.FindProperty("buffAtkMultiplier").floatValue = t.buffAtkMultiplier;
        so.FindProperty("buffDuration").floatValue      = t.buffDuration;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(def);
        return def;
    }

    static void AssignSkills(CharacterDef def, SkillDef[] skillDefs)
    {
        var so  = new SerializedObject(def);
        var arr = so.FindProperty("skills");
        arr.arraySize = skillDefs.Length;
        for (int i = 0; i < skillDefs.Length; i++)
            arr.GetArrayElementAtIndex(i).objectReferenceValue = skillDefs[i];
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(def);
    }

    // ── 스킬 해금 문제 생성·갱신 ────────────────────────────────────────────
    static ProblemDef SeedSkillProblem(Seed s, int slot, QuestionTemplate q)
    {
        string skillId = $"{s.id}_s{slot}";
        string probId  = $"{skillId}_prob";
        string path    = $"{PROBLEM_DIR}/{probId}.asset";
        var def = AssetDatabase.LoadAssetAtPath<ProblemDef>(path);
        if (def == null)
        {
            def = ScriptableObject.CreateInstance<ProblemDef>();
            AssetDatabase.CreateAsset(def, path);
        }

        var so = new SerializedObject(def);
        so.FindProperty("id").stringValue            = probId;
        so.FindProperty("skillId").stringValue       = skillId;
        so.FindProperty("difficulty").enumValueIndex = 0;
        so.FindProperty("type").enumValueIndex       = (int)q.type;
        so.FindProperty("prompt").stringValue        = q.prompt;
        so.FindProperty("explanation").stringValue   = q.explanation ?? "";
        if (q.type == ProblemType.MultipleChoice)
        {
            var cp = so.FindProperty("choices");
            cp.arraySize = q.choices.Length;
            for (int i = 0; i < q.choices.Length; i++) cp.GetArrayElementAtIndex(i).stringValue = q.choices[i];
            so.FindProperty("correctIndex").intValue = q.correctIndex;
        }
        else
        {
            var ap = so.FindProperty("acceptedAnswers");
            ap.arraySize = q.acceptedAnswers.Length;
            for (int i = 0; i < q.acceptedAnswers.Length; i++) ap.GetArrayElementAtIndex(i).stringValue = q.acceptedAnswers[i];
        }
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(def);
        return def;
    }

    // ── 레벨업 문제 생성·갱신 ───────────────────────────────────────────────
    static ProblemDef SeedLevelProblem(LvSeed ls)
    {
        string path = $"{PROBLEM_DIR}/{ls.id}.asset";
        var def = AssetDatabase.LoadAssetAtPath<ProblemDef>(path);
        if (def == null)
        {
            def = ScriptableObject.CreateInstance<ProblemDef>();
            AssetDatabase.CreateAsset(def, path);
        }

        var so = new SerializedObject(def);
        so.FindProperty("id").stringValue            = ls.id;
        so.FindProperty("skillId").stringValue       = "";
        so.FindProperty("difficulty").enumValueIndex = (int)ls.difficulty;
        so.FindProperty("type").enumValueIndex       = (int)ls.type;
        so.FindProperty("prompt").stringValue        = ls.prompt;
        so.FindProperty("explanation").stringValue   = ls.explanation ?? "";
        if (ls.type == ProblemType.MultipleChoice)
        {
            var cp = so.FindProperty("choices");
            cp.arraySize = ls.choices.Length;
            for (int i = 0; i < ls.choices.Length; i++) cp.GetArrayElementAtIndex(i).stringValue = ls.choices[i];
            so.FindProperty("correctIndex").intValue = ls.correctIndex;
        }
        else
        {
            var ap = so.FindProperty("acceptedAnswers");
            ap.arraySize = ls.acceptedAnswers.Length;
            for (int i = 0; i < ls.acceptedAnswers.Length; i++) ap.GetArrayElementAtIndex(i).stringValue = ls.acceptedAnswers[i];
        }
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(def);
        return def;
    }

    // ── 데이터베이스 빌드 ─────────────────────────────────────────────────────
    static void BuildCharDatabase(List<CharacterDef> defs)
    {
        var db = AssetDatabase.LoadAssetAtPath<CharacterDatabase>(DB_PATH);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<CharacterDatabase>();
            AssetDatabase.CreateAsset(db, DB_PATH);
        }

        var so  = new SerializedObject(db);
        var arr = so.FindProperty("characters");
        arr.arraySize = defs.Count;
        for (int i = 0; i < defs.Count; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = defs[i];
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(db);
    }

    static void BuildProblemDatabase(List<ProblemDef> problems)
    {
        var db = AssetDatabase.LoadAssetAtPath<ProblemDatabase>(PROBLEM_DB_PATH);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<ProblemDatabase>();
            AssetDatabase.CreateAsset(db, PROBLEM_DB_PATH);
        }

        var so  = new SerializedObject(db);
        var arr = so.FindProperty("problems");
        arr.arraySize = problems.Count;
        for (int i = 0; i < problems.Count; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = problems[i];
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(db);
    }
}
