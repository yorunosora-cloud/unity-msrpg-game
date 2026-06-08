using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// MSRPG > Seed Placeholder Characters 메뉴.
/// 등급별 2종(총 10종) CharacterDef 에셋과 CharacterDatabase를 자동 생성합니다.
/// 실행 시 스킬 에셋(6종)도 함께 생성하고 각 캐릭터에 연결합니다.
/// </summary>
public static class CharacterSeedSetup
{
    const string CHAR_DIR    = "Assets/_Game/Data/Characters";
    const string SKILL_DIR   = "Assets/_Game/Data/Skills";
    const string PROBLEM_DIR = "Assets/_Game/Data/Problems";
    const string DB_PATH     = "Assets/_Game/Resources/CharacterDatabase.asset";
    const string PROBLEM_DB_PATH = "Assets/_Game/Resources/ProblemDatabase.asset";

    // ── 플레이스홀더 데이터 (등급별 2종) ────────────────────────────────

    struct Seed
    {
        public string id, nameKo, nameEn, country;
        public Rarity rarity; public CharacterRole role; public Continent continent;
        public int hp, atk, def, spd, mp;
        public Color color;
    }

    static readonly Seed[] Seeds =
    {
        // N — 회색
        new Seed { id="cell_n",    nameKo="세포",   nameEn="Cell",    rarity=Rarity.N,   role=CharacterRole.Tanker,     continent=Continent.Biology,  country="life-basics",       hp=400,  atk=30,  def=40,  spd=80,  mp=60,  color=new Color(0.75f,0.75f,0.75f) },
        new Seed { id="number_n",  nameKo="자연수", nameEn="Number",  rarity=Rarity.N,   role=CharacterRole.Dealer,     continent=Continent.Math,     country="arithmetic",        hp=350,  atk=45,  def=25,  spd=90,  mp=50,  color=new Color(0.80f,0.80f,0.80f) },
        // R — 파랑
        new Seed { id="inertia_r", nameKo="관성",   nameEn="Inertia", rarity=Rarity.R,   role=CharacterRole.Tanker,     continent=Continent.Physics,  country="newton-empire",     hp=700,  atk=60,  def=90,  spd=80,  mp=80,  color=new Color(0.20f,0.60f,1.00f) },
        new Seed { id="ion_r",     nameKo="이온",   nameEn="Ion",     rarity=Rarity.R,   role=CharacterRole.Dealer,     continent=Continent.Chemistry,country="bonding",           hp=600,  atk=80,  def=50,  spd=95,  mp=100, color=new Color(0.30f,0.50f,1.00f) },
        // SR — 보라
        new Seed { id="time_sr",   nameKo="시간",   nameEn="Time",    rarity=Rarity.SR,  role=CharacterRole.Supporter,  continent=Continent.Physics,  country="newton-empire",     hp=900,  atk=70,  def=80,  spd=100, mp=130, color=new Color(0.60f,0.20f,1.00f) },
        new Seed { id="mass_sr",   nameKo="질량",   nameEn="Mass",    rarity=Rarity.SR,  role=CharacterRole.Tanker,     continent=Continent.Physics,  country="newton-empire",     hp=1100, atk=80,  def=120, spd=80,  mp=110, color=new Color(0.50f,0.10f,0.90f) },
        // SSR — 금
        new Seed { id="light_ssr", nameKo="빛",     nameEn="Light",   rarity=Rarity.SSR, role=CharacterRole.Dealer,     continent=Continent.Physics,  country="maxwell-duchy",     hp=1000, atk=130, def=70,  spd=110, mp=120, color=new Color(1.00f,0.75f,0.00f) },
        new Seed { id="dna_ssr",   nameKo="DNA",    nameEn="DNA",     rarity=Rarity.SSR, role=CharacterRole.AllRounder, continent=Continent.Biology,  country="genetics",          hp=1200, atk=110, def=90,  spd=95,  mp=140, color=new Color(1.00f,0.85f,0.10f) },
        // UR — 빨강
        new Seed { id="quark_ur",  nameKo="쿼크",   nameEn="Quark",   rarity=Rarity.UR,  role=CharacterRole.AllRounder, continent=Continent.Physics,  country="quantum-rebellion", hp=1400, atk=160, def=100, spd=120, mp=160, color=new Color(1.00f,0.30f,0.10f) },
        new Seed { id="riemann_ur",nameKo="리만",   nameEn="Riemann", rarity=Rarity.UR,  role=CharacterRole.Dealer,     continent=Continent.Math,     country="analysis",          hp=1200, atk=180, def=80,  spd=130, mp=150, color=new Color(1.00f,0.20f,0.20f) },
    };

    // ── 스킬 플레이스홀더 데이터 (6종 — 모든 테스트 캐릭터 공유) ──────────

    struct SkillSeed
    {
        public string         id, nameKo;
        public SkillEffectKind effectKind;
        public int   mpCost;
        public float cooldown, range, halfAngle, damageMultiplier;
        public float healPercent, buffAtkMultiplier, buffDuration;
    }

    static readonly SkillSeed[] SkillSeeds =
    {
        // E — 기본 강타 (Strike, 가볍고 빠름)
        new SkillSeed { id="strike_basic",  nameKo="기본 강타",   effectKind=SkillEffectKind.Strike,
                        mpCost=20, cooldown=1.0f, range=2.5f, halfAngle=60f, damageMultiplier=1.5f },
        // R — 중격 (Strike, 무겁고 느림)
        new SkillSeed { id="strike_heavy",  nameKo="중격",        effectKind=SkillEffectKind.Strike,
                        mpCost=40, cooldown=3.5f, range=2.2f, halfAngle=45f, damageMultiplier=3.0f },
        // T — 범위 휩쓸기 (Aoe, 넓고 약함)
        new SkillSeed { id="aoe_sweep",     nameKo="범위 휩쓸기", effectKind=SkillEffectKind.Aoe,
                        mpCost=30, cooldown=2.5f, range=3.0f, halfAngle=80f, damageMultiplier=1.2f },
        // F — 폭발 (Aoe, 넓고 강함)
        new SkillSeed { id="aoe_blast",     nameKo="폭발",        effectKind=SkillEffectKind.Aoe,
                        mpCost=55, cooldown=6.0f, range=4.0f, halfAngle=90f, damageMultiplier=2.2f },
        // V — 회복·강화 (HealBuff)
        new SkillSeed { id="heal_buff",     nameKo="회복·강화",   effectKind=SkillEffectKind.HealBuff,
                        mpCost=35, cooldown=8.0f,
                        healPercent=0.20f, buffAtkMultiplier=1.30f, buffDuration=6f },
        // G — 표식 부여 (Mark)
        new SkillSeed { id="mark",          nameKo="표식 부여",   effectKind=SkillEffectKind.Mark,
                        mpCost=25, cooldown=4.0f, range=4.5f, halfAngle=90f },
    };

    // ── 문제 플레이스홀더 데이터 (스킬 1개당 1문제) ──────────────────────────

    struct ProblemSeed
    {
        public string      id, skillId, prompt, explanation;
        public ProblemType type;
        // 객관식
        public string[] choices;
        public int      correctIndex;
        // 주관식
        public string[] acceptedAnswers;
    }

    static readonly ProblemSeed[] ProblemSeeds =
    {
        // strike_basic — 뉴턴의 제2법칙 (객관식)
        new ProblemSeed {
            id="prob_strike_basic", skillId="strike_basic", type=ProblemType.MultipleChoice,
            prompt="뉴턴의 운동 제2법칙에 해당하는 공식은 무엇인가요?",
            choices=new[]{"F = ma","E = mc²","PV = nRT","v = d/t"},
            correctIndex=0,
            explanation="F = ma : 힘(F)은 질량(m)과 가속도(a)의 곱입니다."
        },
        // strike_heavy — 운동에너지 (객관식)
        new ProblemSeed {
            id="prob_strike_heavy", skillId="strike_heavy", type=ProblemType.MultipleChoice,
            prompt="물체의 운동에너지를 올바르게 나타낸 것은?",
            choices=new[]{"Ek = ½mv²","Ek = mgh","Ek = Fd","Ek = mv"},
            correctIndex=0,
            explanation="운동에너지 Ek = ½mv² (m: 질량, v: 속력)."
        },
        // aoe_sweep — 파동 (객관식)
        new ProblemSeed {
            id="prob_aoe_sweep", skillId="aoe_sweep", type=ProblemType.MultipleChoice,
            prompt="파동의 속력(v), 진동수(f), 파장(λ)의 관계식은?",
            choices=new[]{"v = fλ","v = f/λ","v = λ/f","v = f+λ"},
            correctIndex=0,
            explanation="파동 속력 v = fλ (진동수 × 파장)."
        },
        // aoe_blast — 원소 기호 (객관식)
        new ProblemSeed {
            id="prob_aoe_blast", skillId="aoe_blast", type=ProblemType.MultipleChoice,
            prompt="다음 중 금(Gold)의 원소 기호는?",
            choices=new[]{"Au","Ag","Fe","Cu"},
            correctIndex=0,
            explanation="금의 원소 기호는 Au(Aurum)입니다."
        },
        // heal_buff — 세포 소기관 (주관식)
        new ProblemSeed {
            id="prob_heal_buff", skillId="heal_buff", type=ProblemType.FreeInput,
            prompt="세포 내에서 ATP를 합성해 에너지를 공급하는 소기관의 이름을 쓰시오.",
            acceptedAnswers=new[]{"미토콘드리아","mitochondria"},
            explanation="미토콘드리아는 세포호흡을 통해 ATP를 생산하는 에너지 공장입니다."
        },
        // mark — 속도 단위 (주관식)
        new ProblemSeed {
            id="prob_mark", skillId="mark", type=ProblemType.FreeInput,
            prompt="SI 단위계에서 속도의 단위를 쓰시오. (예: m/s 형식)",
            acceptedAnswers=new[]{"m/s","m s-1","ms-1"},
            explanation="속도의 SI 단위는 m/s (미터 퍼 세컨드)입니다."
        },
    };

    [MenuItem("MSRPG/Seed Placeholder Characters")]
    public static void Run()
    {
        Directory.CreateDirectory(CHAR_DIR.Replace("Assets", Application.dataPath));
        Directory.CreateDirectory(SKILL_DIR.Replace("Assets", Application.dataPath));
        Directory.CreateDirectory(PROBLEM_DIR.Replace("Assets", Application.dataPath));
        Directory.CreateDirectory((Path.GetDirectoryName(DB_PATH))!.Replace("Assets", Application.dataPath));
        AssetDatabase.Refresh();

        var defs = new List<CharacterDef>();

        foreach (var s in Seeds)
        {
            string path = $"{CHAR_DIR}/{s.id}.asset";

            var def = AssetDatabase.LoadAssetAtPath<CharacterDef>(path);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<CharacterDef>();
                AssetDatabase.CreateAsset(def, path);
            }

            var so = new SerializedObject(def);
            so.FindProperty("id").stringValue      = s.id;
            so.FindProperty("nameKo").stringValue  = s.nameKo;
            so.FindProperty("nameEn").stringValue  = s.nameEn;
            so.FindProperty("rarity").enumValueIndex     = (int)s.rarity;
            so.FindProperty("role").enumValueIndex       = (int)s.role;
            so.FindProperty("continent").enumValueIndex  = (int)s.continent;
            so.FindProperty("country").stringValue       = s.country;
            so.FindProperty("portraitColor").colorValue  = s.color;

            var stats = so.FindProperty("baseStats");
            stats.FindPropertyRelative("hp").intValue    = s.hp;
            stats.FindPropertyRelative("atk").intValue   = s.atk;
            stats.FindPropertyRelative("def").intValue   = s.def;
            stats.FindPropertyRelative("spd").intValue   = s.spd;
            stats.FindPropertyRelative("mp").intValue    = s.mp;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(def);
            defs.Add(def);
        }

        // CharacterDatabase 에셋 생성/갱신
        var db = AssetDatabase.LoadAssetAtPath<CharacterDatabase>(DB_PATH);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<CharacterDatabase>();
            AssetDatabase.CreateAsset(db, DB_PATH);
        }

        var dbSo   = new SerializedObject(db);
        var arrProp = dbSo.FindProperty("characters");
        arrProp.arraySize = defs.Count;
        for (int i = 0; i < defs.Count; i++)
            arrProp.GetArrayElementAtIndex(i).objectReferenceValue = defs[i];
        dbSo.ApplyModifiedProperties();
        EditorUtility.SetDirty(db);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // ── 스킬 에셋 생성 + 각 CharacterDef에 연결 ──────────────────────
        var skillDefs = SeedSkillAssets();
        AssignSkillsToDefs(defs, skillDefs);

        // ── 문제 에셋 생성 + ProblemDatabase 구성 ─────────────────────────
        var problemDefs = SeedProblemAssets();
        BuildProblemDatabase(problemDefs);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[MSRPG] ✅ {defs.Count}종 캐릭터 + {skillDefs.Length}종 스킬 + {problemDefs.Length}종 문제 시드 완료!\n" +
                  $"  캐릭터: {CHAR_DIR}/\n  스킬: {SKILL_DIR}/\n  문제: {PROBLEM_DIR}/\n  DB: {DB_PATH}");
    }

    // ── 스킬 에셋 생성 ─────────────────────────────────────────────────────

    static SkillDef[] SeedSkillAssets()
    {
        var result = new SkillDef[SkillSeeds.Length];

        for (int i = 0; i < SkillSeeds.Length; i++)
        {
            var ss   = SkillSeeds[i];
            string p = $"{SKILL_DIR}/{ss.id}.asset";

            var def = AssetDatabase.LoadAssetAtPath<SkillDef>(p);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<SkillDef>();
                AssetDatabase.CreateAsset(def, p);
            }

            var so = new SerializedObject(def);
            so.FindProperty("id").stringValue                  = ss.id;
            so.FindProperty("nameKo").stringValue              = ss.nameKo;
            so.FindProperty("effectKind").enumValueIndex       = (int)ss.effectKind;
            so.FindProperty("mpCost").intValue                 = ss.mpCost;
            so.FindProperty("cooldown").floatValue             = ss.cooldown;
            so.FindProperty("range").floatValue                = ss.range;
            so.FindProperty("halfAngle").floatValue            = ss.halfAngle;
            so.FindProperty("damageMultiplier").floatValue     = ss.damageMultiplier;
            so.FindProperty("healPercent").floatValue          = ss.healPercent;
            so.FindProperty("buffAtkMultiplier").floatValue    = ss.buffAtkMultiplier;
            so.FindProperty("buffDuration").floatValue         = ss.buffDuration;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(def);

            result[i] = def;
        }

        return result;
    }

    /// <summary>모든 CharacterDef에 6종 스킬 에셋을 전부 할당한다.</summary>
    static void AssignSkillsToDefs(List<CharacterDef> defs, SkillDef[] skillDefs)
    {
        foreach (var def in defs)
        {
            if (def == null) continue;
            var so  = new SerializedObject(def);
            var arr = so.FindProperty("skills");
            if (arr == null) { Debug.LogWarning($"[MSRPG] CharacterDef '{def.id}' 에 skills 프로퍼티 없음"); continue; }
            arr.arraySize = skillDefs.Length;
            for (int i = 0; i < skillDefs.Length; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = skillDefs[i];
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(def);
        }
    }

    // ── 문제 에셋 생성 ─────────────────────────────────────────────────────

    static ProblemDef[] SeedProblemAssets()
    {
        var result = new ProblemDef[ProblemSeeds.Length];

        for (int i = 0; i < ProblemSeeds.Length; i++)
        {
            var ps = ProblemSeeds[i];
            string p = $"{PROBLEM_DIR}/{ps.id}.asset";

            var def = AssetDatabase.LoadAssetAtPath<ProblemDef>(p);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<ProblemDef>();
                AssetDatabase.CreateAsset(def, p);
            }

            var so = new SerializedObject(def);
            so.FindProperty("id").stringValue          = ps.id;
            so.FindProperty("skillId").stringValue     = ps.skillId;
            so.FindProperty("prompt").stringValue      = ps.prompt;
            so.FindProperty("explanation").stringValue = ps.explanation ?? "";
            so.FindProperty("type").enumValueIndex     = (int)ps.type;

            if (ps.type == ProblemType.MultipleChoice)
            {
                var choicesProp = so.FindProperty("choices");
                choicesProp.arraySize = ps.choices?.Length ?? 0;
                if (ps.choices != null)
                    for (int j = 0; j < ps.choices.Length; j++)
                        choicesProp.GetArrayElementAtIndex(j).stringValue = ps.choices[j];
                so.FindProperty("correctIndex").intValue = ps.correctIndex;
            }
            else
            {
                var ansProp = so.FindProperty("acceptedAnswers");
                ansProp.arraySize = ps.acceptedAnswers?.Length ?? 0;
                if (ps.acceptedAnswers != null)
                    for (int j = 0; j < ps.acceptedAnswers.Length; j++)
                        ansProp.GetArrayElementAtIndex(j).stringValue = ps.acceptedAnswers[j];
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(def);
            result[i] = def;
        }

        return result;
    }

    static void BuildProblemDatabase(ProblemDef[] problemDefs)
    {
        var db = AssetDatabase.LoadAssetAtPath<ProblemDatabase>(PROBLEM_DB_PATH);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<ProblemDatabase>();
            AssetDatabase.CreateAsset(db, PROBLEM_DB_PATH);
        }

        var so  = new SerializedObject(db);
        var arr = so.FindProperty("problems");
        arr.arraySize = problemDefs.Length;
        for (int i = 0; i < problemDefs.Length; i++)
            arr.GetArrayElementAtIndex(i).objectReferenceValue = problemDefs[i];
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(db);
    }
}
