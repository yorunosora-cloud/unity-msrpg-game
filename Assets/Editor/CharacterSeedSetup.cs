using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// MSRPG > Seed Placeholder Characters 메뉴.
/// 등급별 2종(총 10종) CharacterDef 에셋과 CharacterDatabase를 자동 생성합니다.
/// </summary>
public static class CharacterSeedSetup
{
    const string CHAR_DIR = "Assets/_Game/Data/Characters";
    const string DB_PATH  = "Assets/_Game/Resources/CharacterDatabase.asset";

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

    [MenuItem("MSRPG/Seed Placeholder Characters")]
    public static void Run()
    {
        Directory.CreateDirectory(CHAR_DIR.Replace("Assets", Application.dataPath));
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

        Debug.Log($"[MSRPG] ✅ {defs.Count}종 캐릭터 시드 완료!\n" +
                  $"  캐릭터: {CHAR_DIR}/\n  DB: {DB_PATH}");
    }
}
