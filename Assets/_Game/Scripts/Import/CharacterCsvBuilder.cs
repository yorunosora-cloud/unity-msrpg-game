using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>CSV 임포트 결과: 메모리 ScriptableObject(에셋이 아님)와 보고서.</summary>
public class ImportResult
{
    public readonly List<CharacterDef> characters = new();
    public readonly List<SkillDef>     skills     = new();
    public TagRule[]                   tagRules   = new TagRule[0];
    public readonly List<SynergyComboDef> combos  = new();
    public readonly ImportReport       report     = new();
}

/// <summary>
/// 관계형 CSV 시트 → CharacterDef/SkillDef/TagRule 매핑·검증 (설계도 18 §8-3). 순수 로직이라 PlayMode 테스트가 가능하다.
/// 입력: {시트 파일명 → CSV 텍스트}. 없는 시트는 빈 시트로 취급. 오류는 모두 모아 보고하며, 오류가 있으면 호출자는 에셋을 쓰지 않는다.
/// </summary>
public static class CharacterCsvBuilder
{
    public const string TagRules         = "tag_rules.csv";
    public const string Catalog          = "character_catalog.csv";
    public const string Forms            = "character_forms.csv";
    public const string AutoRules        = "character_auto_rules.csv";
    public const string Skills           = "character_skills.csv";
    public const string Effects          = "skill_effects.csv";
    public const string FormChange       = "skill_formchange.csv";
    public const string FormChangeStates = "skill_formchange_states.csv";
    public const string Passives         = "skill_passives.csv";
    public const string Modifiers        = "skill_modifiers.csv";
    public const string Codex            = "character_codex.csv";
    public const string Counters         = "skill_counters.csv";
    public const string Combos           = "skill_combos.csv";

    public static readonly string[] AllSheets =
        { TagRules, Catalog, Forms, AutoRules, Skills, Effects, FormChange, FormChangeStates, Passives, Modifiers, Codex, Counters, Combos };

    sealed class SkillEntry { public SkillDef def; public int line; public int order; public string[] forms; public string owner; }

    /// <summary>빌드 중 시트 간에 공유하는 상태(캐릭터별 스킬 목록).</summary>
    sealed class BuildCtx { public readonly Dictionary<string, List<SkillEntry>> owned = new(); }

    public static ImportResult Build(IDictionary<string, string> sheets)
    {
        var res = new ImportResult();
        var rep = res.report;
        var ctx = new BuildCtx();
        CsvTable Sheet(string name)
        {
            string text = null;
            if (sheets != null) sheets.TryGetValue(name, out text);
            return CsvTable.Parse(name, text, rep);
        }

        BuildTagRules(Sheet(TagRules), res);

        var chars = BuildCatalog(Sheet(Catalog), res);
        var forms = BuildForms(Sheet(Forms), chars, res);
        BuildAutoRules(Sheet(AutoRules), chars, forms, res);

        var skills = BuildSkills(Sheet(Skills), chars, forms, ctx, res);
        var skillOwner = new Dictionary<string, string>();
        foreach (var kv in ctx.owned) foreach (var se in kv.Value) skillOwner[se.def.id] = kv.Key;
        var counterIds = BuildCounters(Sheet(Counters), chars, skills, res);
        BuildEffects(Sheet(Effects), skills, skillOwner, counterIds, res);
        BuildFormChange(Sheet(FormChange), Sheet(FormChangeStates), skills, forms, ctx, res);
        BuildPassives(Sheet(Passives), Sheet(Modifiers), skills, skillOwner, counterIds, res);
        BuildCodex(Sheet(Codex), chars, res);
        BuildCombos(Sheet(Combos), chars, skills, res);

        CheckSlotConflicts(ctx, res);
        return res;
    }

    // ════════════════════════════════════════════════════════════════════════
    // tag_rules
    // ════════════════════════════════════════════════════════════════════════

    static readonly string[] ValidAttrs = { "mass", "velocity", "temp", "charge" };

    static void BuildTagRules(CsvTable t, ImportResult res)
    {
        if (t.Rows.Count == 0) return;
        if (!t.RequireColumns(res.report, "tagId", "targetAttr", "direction")) return;
        t.WarnUnknownColumns(res.report, "tagId", "displayKo", "iconId", "targetAttr", "direction", "baseMult", "coeffPerUnit", "refValue", "capMult");

        var list = new List<TagRule>();
        var seen = new HashSet<string>();
        foreach (var row in t.Rows)
        {
            string id = row.Str("tagId", "", required: true);
            row.Context = $"tagId={id}";
            if (id.Length > 0 && !seen.Add(id)) row.Error("tagId가 중복되었습니다");

            string attr = row.Str("targetAttr", "", required: true);
            if (attr.Length > 0 && Array.IndexOf(ValidAttrs, attr) < 0)
                row.Error($"targetAttr '{attr}'는 알 수 없는 값입니다 (허용: {string.Join(", ", ValidAttrs)})");

            string dir = row.Str("direction", "", required: true);
            if (dir.Length > 0 && dir != "high" && dir != "low")
                row.Error($"direction '{dir}'는 high 또는 low여야 합니다");

            list.Add(new TagRule
            {
                tagId = id, displayKo = row.Str("displayKo"), iconId = row.Str("iconId"),
                targetAttr = attr, directionHigh = dir != "low",
                baseMult = row.Float("baseMult", 1f), coeffPerUnit = row.Float("coeffPerUnit"),
                refValue = row.Float("refValue"), capMult = row.Float("capMult", 1f),
            });
        }
        res.tagRules = list.ToArray();
    }

    // ════════════════════════════════════════════════════════════════════════
    // character_catalog
    // ════════════════════════════════════════════════════════════════════════

    static Dictionary<string, CharacterDef> BuildCatalog(CsvTable t, ImportResult res)
    {
        var chars = new Dictionary<string, CharacterDef>();
        if (t.Rows.Count == 0 && t.Headers.Count == 0) return chars;
        if (!t.RequireColumns(res.report, "id", "nameKo", "continent", "rarity")) return chars;
        t.WarnUnknownColumns(res.report, "id", "nameKo", "nameEn", "continent", "country", "rarity", "role", "gender", "dexNumber",
            "hp", "atk", "def", "spd", "mp", "mass", "velocity", "temp", "charge", "personality", "weapon", "concept",
            "basicAttackMotion", "appearance", "loreKo", "portraitColor", "gachaObtainable", "acquireCondition");

        foreach (var row in t.Rows)
        {
            string id = row.Str("id", "", required: true);
            row.Context = $"id={id}";
            if (id.Length == 0) continue;
            if (chars.ContainsKey(id)) { row.Error("id가 중복되었습니다"); continue; }

            var d = ScriptableObject.CreateInstance<CharacterDef>();
            d.name = id; d.id = id;
            d.nameKo  = row.Str("nameKo", "", required: true);
            d.nameEn  = row.Str("nameEn");
            d.continent = row.Enum("continent", Continent.Physics, required: true);
            d.country = row.Str("country");
            d.rarity  = row.Enum("rarity", Rarity.N, required: true);
            d.role    = row.Enum("role", CharacterRole.Dealer);
            d.gender  = row.Enum("gender", Gender.Neutral);
            d.dexNumber = row.Int("dexNumber");
            d.baseStats = new CharacterStats
            {
                hp = row.Int("hp"), atk = row.Int("atk"), def = row.Int("def"), spd = row.Int("spd"), mp = row.Int("mp"),
            };
            d.attributes = new AttributeProfile
            {
                mass = row.Int("mass"), velocity = row.Int("velocity"), temp = row.Int("temp"), charge = row.Int("charge"),
            };
            d.personality = row.Str("personality");
            d.weapon      = row.Str("weapon");
            d.concept     = row.Str("concept");
            d.basicAttackMotion = row.Str("basicAttackMotion");
            d.appearance  = row.Str("appearance");
            d.loreKo      = row.Str("loreKo");
            d.portraitColor = row.Color("portraitColor", Color.white);
            d.gachaObtainable = row.Bool("gachaObtainable", true);
            d.acquireCondition = row.Str("acquireCondition");
            d.skills = new SkillDef[0];

            chars[id] = d;
            res.characters.Add(d);
        }
        return chars;
    }

    // ════════════════════════════════════════════════════════════════════════
    // character_forms / character_auto_rules
    // ════════════════════════════════════════════════════════════════════════

    static Dictionary<string, HashSet<string>> BuildForms(CsvTable t, Dictionary<string, CharacterDef> chars, ImportResult res)
    {
        var formIds = new Dictionary<string, HashSet<string>>();
        if (t.Rows.Count == 0) return formIds;
        if (!t.RequireColumns(res.report, "ownerId", "formId")) return formIds;
        t.WarnUnknownColumns(res.report, "ownerId", "formId", "nameKo", "role", "gender", "isDefault", "hp", "atk", "def", "spd", "mp", "tint");

        var lists = new Dictionary<string, List<CharacterForm>>();
        foreach (var row in t.Rows)
        {
            string owner = row.Str("ownerId", "", required: true);
            string fid   = row.Str("formId", "", required: true);
            row.Context = $"ownerId={owner} formId={fid}";
            if (owner.Length == 0 || fid.Length == 0) continue;
            if (!chars.TryGetValue(owner, out var c)) { row.Error($"ownerId '{owner}'가 character_catalog.csv에 없습니다"); continue; }

            if (!lists.TryGetValue(owner, out var list)) lists[owner] = list = new List<CharacterForm>();
            if (list.Any(f => f.formId == fid)) { row.Error("formId가 중복되었습니다"); continue; }

            list.Add(new CharacterForm
            {
                formId = fid, nameKo = row.Str("nameKo", fid),
                role = row.Enum("role", c.role), gender = row.Enum("gender", c.gender),
                isDefault = row.Bool("isDefault"),
                baseStats = new CharacterStats
                {
                    hp = row.Int("hp", c.baseStats.hp), atk = row.Int("atk", c.baseStats.atk), def = row.Int("def", c.baseStats.def),
                    spd = row.Int("spd", c.baseStats.spd), mp = row.Int("mp", c.baseStats.mp),
                },
                tint = row.Color("tint", Color.white),
            });
        }

        foreach (var kv in lists)
        {
            var c = chars[kv.Key];
            int defaults = kv.Value.Count(f => f.isDefault);
            if (defaults != 1)
            {
                res.report.Error(Forms, 0, $"ownerId={kv.Key}", defaults == 0
                    ? "기본 폼(isDefault=true)이 없습니다 — 정확히 1개여야 합니다"
                    : $"기본 폼이 {defaults}개입니다 — 정확히 1개여야 합니다");
            }
            c.forms = kv.Value.ToArray();
            var def = kv.Value.FirstOrDefault(f => f.isDefault) ?? kv.Value[0];
            c.baseStats = def.baseStats;                // 기본 폼 스탯이 캐릭터 baseStats
            c.role = def.role; c.gender = def.gender;
            formIds[kv.Key] = new HashSet<string>(kv.Value.Select(f => f.formId));
        }
        return formIds;
    }

    static void BuildAutoRules(CsvTable t, Dictionary<string, CharacterDef> chars, Dictionary<string, HashSet<string>> forms, ImportResult res)
    {
        if (t.Rows.Count == 0) return;
        if (!t.RequireColumns(res.report, "ownerId", "cond", "toForm")) return;
        t.WarnUnknownColumns(res.report, "ownerId", "order", "cond", "param", "statKey", "statCompare", "statThreshold", "consecutive", "toForm", "priority");

        var byOwner = new Dictionary<string, List<(int order, int idx, FormAutoRule rule)>>();
        int idx = 0;
        foreach (var row in t.Rows)
        {
            string owner = row.Str("ownerId", "", required: true);
            row.Context = $"ownerId={owner}";
            var rule = new FormAutoRule
            {
                cond = row.Enum("cond", FormAutoCond.SpeedRatioAtLeast, required: true),
                param = row.Float("param"), statKey = row.Str("statKey"),
                statCompare = row.Enum("statCompare", FormCompare.AtLeast),
                statThreshold = row.Float("statThreshold"), consecutive = row.Int("consecutive"),
                toForm = row.Str("toForm", "", required: true), priority = row.Int("priority"),
            };
            if (owner.Length == 0) continue;
            if (!chars.ContainsKey(owner)) { row.Error($"ownerId '{owner}'가 character_catalog.csv에 없습니다"); continue; }
            if (!forms.TryGetValue(owner, out var set)) { row.Error("폼이 없는 캐릭터에는 자동 전환 규칙을 둘 수 없습니다(character_forms.csv에 폼을 먼저 정의)"); continue; }
            if (rule.toForm.Length > 0 && !set.Contains(rule.toForm)) row.Error($"toForm '{rule.toForm}'가 {owner}의 폼에 없습니다");

            if (!byOwner.TryGetValue(owner, out var list)) byOwner[owner] = list = new();
            list.Add((row.Int("order"), idx++, rule));
        }
        foreach (var kv in byOwner)
            chars[kv.Key].autoRules = kv.Value.OrderBy(x => x.order).ThenBy(x => x.idx).Select(x => x.rule).ToArray();
    }

    // ════════════════════════════════════════════════════════════════════════
    // character_skills
    // ════════════════════════════════════════════════════════════════════════

    static Dictionary<string, SkillEntry> BuildSkills(CsvTable t, Dictionary<string, CharacterDef> chars,
                                                      Dictionary<string, HashSet<string>> forms, BuildCtx ctx, ImportResult res)
    {
        var skills = new Dictionary<string, SkillEntry>();
        if (t.Rows.Count == 0) return skills;
        if (!t.RequireColumns(res.report, "skillId", "ownerId", "slot")) return skills;
        t.WarnUnknownColumns(res.report, "skillId", "ownerId", "slot", "nameKo", "descKo", "skillType", "startUnlocked", "unlockCond",
            "effectKind", "mpCost", "cooldown", "shape", "range", "halfAngle", "lineWidth", "radius", "damageMultiplier", "healPercent",
            "buffAtkMultiplier", "buffDuration", "strengthTags", "formReq", "skillMotion", "factNoteKo", "gameNoteKo", "milestoneLevel",
            "milestoneDescKo", "milestoneExtraTargets", "milestoneRangeBonus", "milestoneHalfAngleBonus", "milestoneHealPercentBonus",
            "milestoneBuffDurationBonus", "milestoneDamageBonus", "milestoneCooldownBonus", "dashDistance", "dashTime",
            "autoSwitchDistance", "altEffectKind", "altRange", "altHalfAngle", "altDamageMultiplier");

        var owned = ctx.owned;
        int order = 0;
        foreach (var row in t.Rows)
        {
            string id    = row.Str("skillId", "", required: true);
            string owner = row.Str("ownerId", "", required: true);
            row.Context = $"skillId={id}";

            var s = ScriptableObject.CreateInstance<SkillDef>();
            s.name = id; s.id = id;
            s.slot = row.Int("slot", -1, required: true);
            if (s.slot < 0 || s.slot >= SkillSlots.Count)
                row.Error($"slot {s.slot}는 범위(0~{SkillSlots.Count - 1}) 밖입니다");
            s.nameKo = row.Str("nameKo", id);
            s.descKo = row.Str("descKo");
            s.skillType = row.Enum("skillType", SkillKind.Active);
            s.startUnlocked = row.Bool("startUnlocked");
            s.unlockCond = row.Str("unlockCond");
            s.effectKind = row.Enum("effectKind", SkillEffectKind.Strike);
            s.mpCost = row.Int("mpCost");
            s.cooldown = row.Float("cooldown");
            s.shape = row.Enum("shape", SkillShape.Cone);
            s.range = row.Float("range", s.range);
            s.halfAngle = row.Float("halfAngle", s.halfAngle);
            s.lineWidth = row.Float("lineWidth", s.lineWidth);
            s.radius = row.Float("radius", s.radius);
            s.damageMultiplier = row.Float("damageMultiplier", s.damageMultiplier);
            s.healPercent = row.Float("healPercent", s.healPercent);
            s.buffAtkMultiplier = row.Float("buffAtkMultiplier", s.buffAtkMultiplier);
            s.buffDuration = row.Float("buffDuration", s.buffDuration);
            s.strengthTags = row.List("strengthTags");
            s.skillMotion = row.Str("skillMotion");
            s.factNoteKo = row.Str("factNoteKo");
            s.gameNoteKo = row.Str("gameNoteKo");
            s.milestoneLevel = row.Int("milestoneLevel", s.milestoneLevel);
            s.milestoneDescKo = row.Str("milestoneDescKo");
            s.milestoneExtraTargets = row.Int("milestoneExtraTargets");
            s.milestoneRangeBonus = row.Float("milestoneRangeBonus");
            s.milestoneHalfAngleBonus = row.Float("milestoneHalfAngleBonus");
            s.milestoneHealPercentBonus = row.Float("milestoneHealPercentBonus");
            s.milestoneBuffDurationBonus = row.Float("milestoneBuffDurationBonus");
            s.milestoneCooldownBonus = row.Float("milestoneCooldownBonus");
            s.milestoneDamageBonus = row.Float("milestoneDamageBonus");
            s.dashDistance = row.Float("dashDistance");
            s.dashTime = row.Float("dashTime", s.dashTime);
            s.autoSwitchDistance = row.Float("autoSwitchDistance");
            s.altEffectKind = row.Enum("altEffectKind", s.altEffectKind);
            s.altRange = row.Float("altRange");
            s.altHalfAngle = row.Float("altHalfAngle");
            s.altDamageMultiplier = row.Float("altDamageMultiplier", s.altDamageMultiplier);

            if (id.Length == 0 || owner.Length == 0) continue;
            if (skills.ContainsKey(id)) { row.Error("skillId가 중복되었습니다"); continue; }
            if (!chars.ContainsKey(owner)) { row.Error($"ownerId '{owner}'가 character_catalog.csv에 없습니다"); continue; }

            var formList = row.List("formReq");
            if (formList.Length > 0)
            {
                if (!forms.TryGetValue(owner, out var set)) row.Error($"{owner}는 폼이 없는 캐릭터라 formReq를 둘 수 없습니다");
                else foreach (var f in formList)
                    if (!set.Contains(f)) row.Error($"formReq '{f}'가 {owner}의 폼에 없습니다");
                s.formReq = string.Join(",", formList);
            }

            var entry = new SkillEntry { def = s, line = row.Line, order = order++, forms = formList, owner = owner };
            skills[id] = entry;
            if (!owned.TryGetValue(owner, out var list)) owned[owner] = list = new();
            list.Add(entry);
            res.skills.Add(s);
        }

        foreach (var kv in owned)
            chars[kv.Key].skills = kv.Value.OrderBy(e => e.def.slot).ThenBy(e => e.order).Select(e => e.def).ToArray();
        return skills;
    }

    // ════════════════════════════════════════════════════════════════════════
    // skill_effects
    // ════════════════════════════════════════════════════════════════════════

    // ════════════════════════════════════════════════════════════════════════
    // skill_counters (스킬 카운터, 구현계획 23-A)
    // ════════════════════════════════════════════════════════════════════════

    static CounterTrigger ParseTriggers(CsvRow row, string col)
    {
        var f = CounterTrigger.None;
        foreach (var name in row.List(col))
        {
            bool ok = false;
            foreach (CounterTrigger v in System.Enum.GetValues(typeof(CounterTrigger)))
                if (v != CounterTrigger.None && v.ToString() == name) { f |= v; ok = true; break; }
            if (!ok) row.Error($"{col} '{name}'는 알 수 없는 값입니다 (허용: {string.Join(", ", System.Enum.GetNames(typeof(CounterTrigger)).Where(n => n != "None"))})");
        }
        return f;
    }

    /// <summary>카운터 id 참조 검증: "@recent" / "target:상태id" / 해당 캐릭터가 정의한 카운터.</summary>
    static bool CounterRefOk(string counterId, string sid, Dictionary<string, string> skillOwner,
                             Dictionary<string, HashSet<string>> counterIds, CsvRow row)
    {
        if (counterId == "@recent") return true;
        if (counterId.StartsWith("target:"))
        {
            if (counterId.Length == "target:".Length) { row.Error("counterId 'target:' 뒤에 상태 id가 필요합니다"); return false; }
            return true;
        }
        if (skillOwner.TryGetValue(sid, out var owner) && counterIds.TryGetValue(owner, out var set) && set.Contains(counterId)) return true;
        row.Error($"counterId '{counterId}'가 {(skillOwner.TryGetValue(sid, out var o2) ? o2 : "이 스킬의 소유자")}의 skill_counters.csv에 없습니다");
        return false;
    }

    static Dictionary<string, HashSet<string>> BuildCounters(CsvTable t, Dictionary<string, CharacterDef> chars,
                                                             Dictionary<string, SkillEntry> skills, ImportResult res)
    {
        var ids = new Dictionary<string, HashSet<string>>();
        if (t.Rows.Count == 0) return ids;
        if (!t.RequireColumns(res.report, "counterId", "ownerId")) return ids;
        t.WarnUnknownColumns(res.report, "counterId", "ownerId", "maxStacks", "milestoneMaxStacks", "decay", "decaySeconds",
            "gainOn", "resetOn", "milestoneSkillId", "gainSkillId");

        var by = new Dictionary<string, List<CounterSpec>>();
        foreach (var row in t.Rows)
        {
            string id = row.Str("counterId", "", required: true);
            string owner = row.Str("ownerId", "", required: true);
            row.Context = $"counterId={id}";
            var spec = new CounterSpec
            {
                id = id, maxStacks = row.Int("maxStacks"), milestoneMaxStacks = row.Int("milestoneMaxStacks"),
                decay = row.Enum("decay", CounterDecay.None), decaySeconds = row.Float("decaySeconds"),
                gainOn = ParseTriggers(row, "gainOn"), resetOn = ParseTriggers(row, "resetOn"),
                milestoneSkillId = row.Str("milestoneSkillId"), gainSkillId = row.Str("gainSkillId"),
            };
            if (id.Length == 0 || owner.Length == 0) continue;
            if (id == "@recent" || id.StartsWith("target:")) { row.Error($"counterId '{id}'는 예약된 형식입니다(@recent, target:…)"); continue; }
            if (!chars.ContainsKey(owner)) { row.Error($"ownerId '{owner}'가 character_catalog.csv에 없습니다"); continue; }
            if (spec.maxStacks <= 0) { row.Error("maxStacks는 1 이상이어야 합니다"); continue; }
            if (spec.milestoneMaxStacks > 0 && string.IsNullOrEmpty(spec.milestoneSkillId)) { row.Error("milestoneMaxStacks를 쓰려면 milestoneSkillId가 필요합니다"); continue; }
            if (!string.IsNullOrEmpty(spec.milestoneSkillId) &&
                !(skills.TryGetValue(spec.milestoneSkillId, out var ms) && ms.owner == owner))
            { row.Error($"milestoneSkillId '{spec.milestoneSkillId}'가 {owner}의 스킬이 아닙니다"); continue; }
            if (!string.IsNullOrEmpty(spec.gainSkillId) && !(skills.TryGetValue(spec.gainSkillId, out var gs) && gs.owner == owner))
            { row.Error($"gainSkillId '{spec.gainSkillId}'가 {owner}의 스킬이 아닙니다"); continue; }
            if (!ids.TryGetValue(owner, out var set)) ids[owner] = set = new HashSet<string>();
            if (!set.Add(id)) { row.Error($"counterId '{id}'가 {owner}에 중복되었습니다"); continue; }
            if (!by.TryGetValue(owner, out var list)) by[owner] = list = new();
            list.Add(spec);
        }
        foreach (var kv in by) chars[kv.Key].counters = kv.Value.ToArray();
        return ids;
    }

    static void BuildEffects(CsvTable t, Dictionary<string, SkillEntry> skills, Dictionary<string, string> skillOwner,
                             Dictionary<string, HashSet<string>> counterIds, ImportResult res)
    {
        if (t.Rows.Count == 0) return;
        if (!t.RequireColumns(res.report, "skillId", "type")) return;
        t.WarnUnknownColumns(res.report, "skillId", "order", "type", "target", "stat", "value", "duration", "maxStacks", "targetCap",
            "areaRadius", "milestoneValue", "milestoneDuration", "whenForm", "consumeOnHit",
            "counterId", "counterMode", "counterCoeff", "milestoneCounterCoeff", "counterConsume", "requireCounterFull", "counterWindow", "markId");

        var by = new Dictionary<string, List<(int order, int idx, SkillEffectEntry e)>>();
        int idx = 0;
        foreach (var row in t.Rows)
        {
            string sid = row.Str("skillId", "", required: true);
            row.Context = $"skillId={sid}";
            var e = new SkillEffectEntry
            {
                type = row.Enum("type", SkillEffectType.Damage, required: true),
                target = row.Enum("target", TargetScope.EnemyArea),
                stat = row.Enum("stat", StatKind.Atk),
                value = row.Float("value"), duration = row.Float("duration"),
                maxStacks = row.Int("maxStacks"), targetCap = row.Int("targetCap"),
                areaRadius = row.Float("areaRadius"),
                milestoneValue = row.Float("milestoneValue"), milestoneDuration = row.Float("milestoneDuration"),
                whenForm = row.Str("whenForm"), consumeOnHit = row.Bool("consumeOnHit"),
                counterId = row.Str("counterId"), counterMode = row.Enum("counterMode", CounterMode.None),
                counterCoeff = row.Float("counterCoeff"), milestoneCounterCoeff = row.Float("milestoneCounterCoeff"),
                counterConsume = row.Bool("counterConsume"), requireCounterFull = row.Bool("requireCounterFull"),
                counterWindow = row.Float("counterWindow"), markId = row.Str("markId"),
            };
            if (sid.Length == 0) continue;
            if (!skills.ContainsKey(sid)) { row.Error($"skillId '{sid}'가 character_skills.csv에 없습니다"); continue; }
            if (!string.IsNullOrEmpty(e.markId) && e.type != SkillEffectType.Mark) { row.Error("markId는 type=Mark 효과에서만 쓸 수 있습니다"); continue; }
            if (string.IsNullOrEmpty(e.counterId))
            {
                if (e.counterMode != CounterMode.None || e.counterConsume || e.requireCounterFull)
                { row.Error("counterMode·counterConsume·requireCounterFull을 쓰려면 counterId가 필요합니다"); continue; }
            }
            else if (!CounterRefOk(e.counterId, sid, skillOwner, counterIds, row)) continue;
            if (!by.TryGetValue(sid, out var list)) by[sid] = list = new();
            list.Add((row.Int("order"), idx++, e));
        }

        foreach (var kv in by)
        {
            var arr = kv.Value.OrderBy(x => x.order).ThenBy(x => x.idx).Select(x => x.e).ToArray();
            var def = skills[kv.Key].def;
            if (def.skillType == SkillKind.Passive)
            {
                var p = def.passive; p.effects = arr; def.passive = p;
            }
            else def.effects = arr;
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // skill_formchange / skill_formchange_states
    // ════════════════════════════════════════════════════════════════════════

    static void BuildFormChange(CsvTable t, CsvTable states, Dictionary<string, SkillEntry> skills,
                                Dictionary<string, HashSet<string>> forms, BuildCtx ctx, ImportResult res)
    {
        var specs = new Dictionary<string, FormChangeSpec>();

        if (t.Rows.Count > 0 && t.RequireColumns(res.report, "skillId", "mode"))
        {
            t.WarnUnknownColumns(res.report, "skillId", "keyIndex", "mode", "fromForms", "toForm", "sequence", "revertAfterSeconds",
                "revertToForm", "autoRevertForms", "pressAgainReverts");
            foreach (var row in t.Rows)
            {
                string sid = row.Str("skillId", "", required: true);
                row.Context = $"skillId={sid}";
                if (sid.Length == 0) continue;
                if (!skills.TryGetValue(sid, out var entry)) { row.Error($"skillId '{sid}'가 character_skills.csv에 없습니다"); continue; }
                if (entry.def.skillType != SkillKind.FormChange) { row.Error("skillType이 FormChange인 스킬만 폼 전환 규칙을 가질 수 있습니다"); continue; }
                if (specs.ContainsKey(sid)) { row.Error("같은 skillId의 폼 전환 행이 중복되었습니다"); continue; }

                var spec = new FormChangeSpec
                {
                    keyIndex = row.Int("keyIndex"), mode = row.Enum("mode", FormChangeMode.Toggle, required: true),
                    fromForms = row.List("fromForms"), toForm = row.Str("toForm"), sequence = row.List("sequence"),
                    revertAfterSeconds = row.Float("revertAfterSeconds"), revertToForm = row.Str("revertToForm"),
                    autoRevertForms = row.List("autoRevertForms"), pressAgainReverts = row.Bool("pressAgainReverts"),
                };
                if (spec.keyIndex < 0 || spec.keyIndex > 1) row.Error($"keyIndex {spec.keyIndex}는 0(Z) 또는 1(X)이어야 합니다");

                string owner = OwnerOf(ctx, entry);
                forms.TryGetValue(owner ?? "", out var set);
                void Check(string label, IEnumerable<string> ids)
                {
                    foreach (var f in ids)
                        if (string.IsNullOrEmpty(f)) continue;
                        else if (set == null || !set.Contains(f)) row.Error($"{label} '{f}'가 {owner}의 폼에 없습니다");
                }
                Check("fromForms", spec.fromForms); Check("toForm", new[] { spec.toForm });
                Check("sequence", spec.sequence); Check("revertToForm", new[] { spec.revertToForm });
                Check("autoRevertForms", spec.autoRevertForms);

                specs[sid] = spec;
            }
        }

        if (states.Rows.Count > 0 && states.RequireColumns(res.report, "skillId", "cond", "toForm"))
        {
            states.WarnUnknownColumns(res.report, "skillId", "order", "cond", "param", "toForm", "priority");
            var by = new Dictionary<string, List<(int order, int idx, FormStateRule r)>>();
            int idx = 0;
            foreach (var row in states.Rows)
            {
                string sid = row.Str("skillId", "", required: true);
                row.Context = $"skillId={sid}";
                var r = new FormStateRule
                {
                    cond = row.Enum("cond", FormStateCond.Always, required: true), param = row.Float("param"),
                    toForm = row.Str("toForm", "", required: true), priority = row.Int("priority"),
                };
                if (sid.Length == 0) continue;
                if (!specs.ContainsKey(sid)) { row.Error($"skillId '{sid}'에 skill_formchange.csv 행이 없습니다"); continue; }
                string owner = OwnerOf(ctx, skills[sid]);
                if (forms.TryGetValue(owner ?? "", out var set) && r.toForm.Length > 0 && !set.Contains(r.toForm))
                    row.Error($"toForm '{r.toForm}'가 {owner}의 폼에 없습니다");
                if (!by.TryGetValue(sid, out var list)) by[sid] = list = new();
                list.Add((row.Int("order"), idx++, r));
            }
            foreach (var kv in by)
                specs[kv.Key].stateRules = kv.Value.OrderBy(x => x.order).ThenBy(x => x.idx).Select(x => x.r).ToArray();
        }

        foreach (var kv in skills)
        {
            if (kv.Value.def.skillType != SkillKind.FormChange) continue;
            if (specs.TryGetValue(kv.Key, out var spec)) kv.Value.def.formChange = spec;
            else res.report.Error(FormChange, 0, $"skillId={kv.Key}", "FormChange 스킬에는 skill_formchange.csv 행이 필요합니다");
        }
    }

    static string OwnerOf(BuildCtx ctx, SkillEntry entry)
    {
        foreach (var kv in ctx.owned) if (kv.Value.Contains(entry)) return kv.Key;
        return null;
    }

    // ════════════════════════════════════════════════════════════════════════
    // skill_passives / skill_modifiers
    // ════════════════════════════════════════════════════════════════════════

    static void BuildPassives(CsvTable t, CsvTable mods, Dictionary<string, SkillEntry> skills, Dictionary<string, string> skillOwner,
                              Dictionary<string, HashSet<string>> counterIds, ImportResult res)
    {
        var seen = new HashSet<string>();

        if (t.Rows.Count > 0 && t.RequireColumns(res.report, "skillId", "trigger"))
        {
            t.WarnUnknownColumns(res.report, "skillId", "trigger", "value", "hitCount", "internalCooldown", "formula", "milestoneValue",
                "stackId", "maxStacks", "perStackValue", "decay", "decaySeconds", "stackStat", "damageRatio", "cap",
                "milestonePerStackValue", "milestoneMaxStacks");
            foreach (var row in t.Rows)
            {
                string sid = row.Str("skillId", "", required: true);
                row.Context = $"skillId={sid}";
                if (sid.Length == 0) continue;
                if (!skills.TryGetValue(sid, out var entry)) { row.Error($"skillId '{sid}'가 character_skills.csv에 없습니다"); continue; }
                if (entry.def.skillType != SkillKind.Passive) { row.Error("skillType이 Passive인 스킬만 패시브 규칙을 가질 수 있습니다"); continue; }
                if (!seen.Add(sid)) { row.Error("같은 skillId의 패시브 행이 중복되었습니다"); continue; }

                var p = entry.def.passive;                          // effects는 skill_effects에서 이미 채워졌을 수 있다
                p.trigger = row.Enum("trigger", PassiveTriggerType.None, required: true);
                p.value = row.Float("value");
                p.hitCount = row.Enum("hitCount", HitCountMode.PerAction);
                p.internalCooldown = row.Float("internalCooldown");
                p.formula = row.Enum("formula", PassiveFormulaId.None);
                p.milestoneValue = row.Float("milestoneValue");
                p.stack = new StackSpec
                {
                    stackId = row.Str("stackId"), maxStacks = row.Int("maxStacks"), perStackValue = row.Float("perStackValue"),
                    decay = row.Enum("decay", StackDecay.None), decaySeconds = row.Float("decaySeconds"),
                    stat = row.Enum("stackStat", StatKind.Atk), damageRatio = row.Float("damageRatio"), cap = row.Float("cap"),
                    milestonePerStackValue = row.Float("milestonePerStackValue"), milestoneMaxStacks = row.Int("milestoneMaxStacks"),
                };
                entry.def.passive = p;
            }
        }

        if (mods.Rows.Count > 0 && mods.RequireColumns(res.report, "skillId", "source", "stat"))
        {
            mods.WarnUnknownColumns(res.report, "skillId", "order", "source", "stat", "value", "cap", "decayPerSecond", "partyWide",
                "enemyAuraRadius", "milestoneValue", "milestoneCap", "immuneToSlow", "counterId");
            var by = new Dictionary<string, List<(int order, int idx, PassiveModifierSpec m)>>();
            int idx = 0;
            foreach (var row in mods.Rows)
            {
                string sid = row.Str("skillId", "", required: true);
                row.Context = $"skillId={sid}";
                var m = new PassiveModifierSpec
                {
                    source = row.Enum("source", ModifierSource.Constant, required: true), stat = row.Enum("stat", StatKind.Atk, required: true),
                    value = row.Float("value"), cap = row.Float("cap"), decayPerSecond = row.Float("decayPerSecond"),
                    partyWide = row.Bool("partyWide"), enemyAuraRadius = row.Float("enemyAuraRadius"),
                    milestoneValue = row.Float("milestoneValue"), milestoneCap = row.Float("milestoneCap"),
                    immuneToSlow = row.Bool("immuneToSlow"), counterId = row.Str("counterId"),
                };
                if (sid.Length == 0) continue;
                if (!skills.TryGetValue(sid, out var entry)) { row.Error($"skillId '{sid}'가 character_skills.csv에 없습니다"); continue; }
                if (m.source == ModifierSource.Counter)
                {
                    if (string.IsNullOrEmpty(m.counterId)) { row.Error("source=Counter에는 counterId가 필요합니다"); continue; }
                    if (!CounterRefOk(m.counterId, sid, skillOwner, counterIds, row)) continue;
                }
                if (entry.def.skillType != SkillKind.Passive) { row.Error("skillType이 Passive인 스킬만 상시 수정자를 가질 수 있습니다"); continue; }
                if (!by.TryGetValue(sid, out var list)) by[sid] = list = new();
                list.Add((row.Int("order"), idx++, m));
            }
            foreach (var kv in by)
            {
                var def = skills[kv.Key].def;
                var p = def.passive;
                p.modifiers = kv.Value.OrderBy(x => x.order).ThenBy(x => x.idx).Select(x => x.m).ToArray();
                def.passive = p;
            }
        }

        foreach (var kv in skills)
            if (kv.Value.def.skillType == SkillKind.Passive && !seen.Contains(kv.Key))
                res.report.Error(Passives, 0, $"skillId={kv.Key}", "Passive 스킬에는 skill_passives.csv 행이 필요합니다");
    }

    // ════════════════════════════════════════════════════════════════════════
    // character_codex (교육 레이어, 설계도 18-C)
    // ════════════════════════════════════════════════════════════════════════

    // ════════════════════════════════════════════════════════════════════════
    // skill_combos (표식 연계, 구현계획 23-B)
    // ════════════════════════════════════════════════════════════════════════

    static void BuildCombos(CsvTable t, Dictionary<string, CharacterDef> chars, Dictionary<string, SkillEntry> skills, ImportResult res)
    {
        if (t.Rows.Count == 0) return;
        if (!t.RequireColumns(res.report, "comboId", "markId", "consumerId", "triggerSkillId", "comboSkillId")) return;
        t.WarnUnknownColumns(res.report, "comboId", "comboName", "formula", "markId", "consumerId", "triggerSkillId", "comboSkillId");

        var appliedMarks = new HashSet<string>();
        foreach (var sk in res.skills)
        {
            if (sk.effects != null) foreach (var e in sk.effects) if (!string.IsNullOrEmpty(e.markId)) appliedMarks.Add(e.markId);
            if (sk.passive.effects != null) foreach (var e in sk.passive.effects) if (!string.IsNullOrEmpty(e.markId)) appliedMarks.Add(e.markId);
        }

        var ids = new HashSet<string>(); var pairs = new HashSet<string>();
        foreach (var row in t.Rows)
        {
            string id = row.Str("comboId", "", required: true);
            row.Context = $"comboId={id}";
            string mark = row.Str("markId", "", required: true), consumer = row.Str("consumerId", "", required: true);
            string trig = row.Str("triggerSkillId", "", required: true), comboId = row.Str("comboSkillId", "", required: true);
            if (id.Length == 0 || mark.Length == 0 || consumer.Length == 0 || trig.Length == 0 || comboId.Length == 0) continue;
            if (!chars.ContainsKey(consumer)) { row.Error($"consumerId '{consumer}'가 character_catalog.csv에 없습니다"); continue; }
            if (!skills.TryGetValue(trig, out var te)) { row.Error($"triggerSkillId '{trig}'가 character_skills.csv에 없습니다"); continue; }
            if (te.owner != consumer) { row.Error($"triggerSkillId '{trig}'는 {consumer}의 스킬이 아닙니다"); continue; }
            if (!skills.TryGetValue(comboId, out var ce)) { row.Error($"comboSkillId '{comboId}'가 character_skills.csv에 없습니다"); continue; }
            if (ce.owner != consumer) { row.Error($"comboSkillId '{comboId}'는 {consumer}의 스킬이 아닙니다"); continue; }
            if (ce.def.skillType != SkillKind.Combo) { row.Error($"comboSkillId '{comboId}'의 skillType이 Combo여야 합니다"); continue; }
            if (!ids.Add(id)) { row.Error($"comboId '{id}'가 중복되었습니다"); continue; }
            if (!pairs.Add(consumer + "|" + trig)) { row.Error($"소모자 {consumer}의 트리거 스킬 '{trig}'에 연계가 중복되었습니다"); continue; }
            if (!appliedMarks.Contains(mark)) row.Warn($"표식 '{mark}'를 거는 스킬(skill_effects.csv markId)이 없습니다 — 표식 id 오타인지 확인하세요");
            res.combos.Add(new SynergyComboDef
            {
                id = id, comboName = row.Str("comboName"), formula = row.Str("formula"), markId = mark,
                consumerId = consumer, triggerSkillId = trig, comboSkill = ce.def,
            });
        }
    }

    static void BuildCodex(CsvTable t, Dictionary<string, CharacterDef> chars, ImportResult res)
    {
        if (t.Rows.Count == 0) return;
        if (!t.RequireColumns(res.report, "ownerId")) return;
        t.WarnUnknownColumns(res.report, "ownerId", "basicExplanation", "advancedExplanation", "realPrinciple", "sourceRef",
            "conceptQ", "conceptChoices", "conceptAnswer", "affinityQ", "affinityChoices", "affinityConceptPick");

        var concepts = new Dictionary<string, List<ConceptQuestion>>();
        var affinities = new Dictionary<string, List<AffinityQuestion>>();

        foreach (var row in t.Rows)
        {
            string owner = row.Str("ownerId", "", required: true);
            row.Context = $"ownerId={owner}";
            if (owner.Length == 0) continue;
            if (!chars.TryGetValue(owner, out var c)) { row.Error($"ownerId '{owner}'가 character_catalog.csv에 없습니다"); continue; }

            // 교육 문구는 캐릭터당 한 번만 정의
            void Text(string col, string current, System.Action<string> set)
            {
                string v = row.Str(col);
                if (v.Length == 0) return;
                if (!string.IsNullOrEmpty(current)) { row.Error($"{col}가 이미 정의되어 있습니다(한 행에만 적으세요)"); return; }
                set(v);
            }
            Text("basicExplanation",    c.basicExplanation,    v => c.basicExplanation = v);
            Text("advancedExplanation", c.advancedExplanation, v => c.advancedExplanation = v);
            Text("realPrinciple",       c.realPrinciple,       v => c.realPrinciple = v);
            Text("sourceRef",           c.sourceRef,           v => c.sourceRef = v);

            // 개념 문제 (4지선다 권장, 선택지 2개 이상, 정답 번호는 1부터)
            string cq = row.Str("conceptQ");
            var cch = row.List("conceptChoices");
            int cans = row.Int("conceptAnswer");
            if (cq.Length == 0)
            {
                if (cch.Length > 0 || cans != 0) row.Error("conceptChoices/conceptAnswer가 있는데 conceptQ(문제)가 없습니다");
            }
            else if (cch.Length < 2) row.Error($"개념 문제의 선택지는 2개 이상이어야 합니다(현재 {cch.Length}개)");
            else if (cans < 1 || cans > cch.Length) row.Error($"conceptAnswer {cans}는 1~{cch.Length} 범위여야 합니다");
            else
            {
                if (!concepts.TryGetValue(owner, out var l)) concepts[owner] = l = new();
                l.Add(new ConceptQuestion { question = cq, choices = cch, answerIndex = cans - 1 });
            }

            // 호감도 개념 2지선다
            string aq = row.Str("affinityQ");
            var ach = row.List("affinityChoices");
            int apick = row.Int("affinityConceptPick");
            if (aq.Length == 0)
            {
                if (ach.Length > 0 || apick != 0) row.Error("affinityChoices/affinityConceptPick이 있는데 affinityQ(질문)가 없습니다");
            }
            else if (ach.Length != 2) row.Error($"호감도 질문은 2지선다여야 합니다(현재 선택지 {ach.Length}개)");
            else if (apick < 1 || apick > 2) row.Error($"affinityConceptPick {apick}는 1 또는 2여야 합니다");
            else
            {
                if (!affinities.TryGetValue(owner, out var l)) affinities[owner] = l = new();
                l.Add(new AffinityQuestion { question = aq, choices = ach, conceptPick = apick - 1 });
            }
        }

        foreach (var kv in concepts)   chars[kv.Key].conceptQuestions  = kv.Value.ToArray();
        foreach (var kv in affinities) chars[kv.Key].affinityQuestions = kv.Value.ToArray();
    }

    // ════════════════════════════════════════════════════════════════════════
    // 교차 검증
    // ════════════════════════════════════════════════════════════════════════

    static void CheckSlotConflicts(BuildCtx ctx, ImportResult res)
    {
        foreach (var kv in ctx.owned)
        {
            // 같은 슬롯의 액티브 스킬은 서로 다른 폼 전용이어야 한다(폼 제한이 없으면 모든 폼과 겹친다)
            var actives = kv.Value.Where(e => e.def.skillType == SkillKind.Active).ToList();
            for (int i = 0; i < actives.Count; i++)
                for (int j = i + 1; j < actives.Count; j++)
                {
                    var a = actives[i]; var b = actives[j];
                    if (a.def.slot != b.def.slot) continue;
                    bool overlap = a.forms.Length == 0 || b.forms.Length == 0 || a.forms.Intersect(b.forms).Any();
                    if (overlap)
                        res.report.Error(Skills, b.line, $"skillId={b.def.id}",
                            $"슬롯 {a.def.slot}이(가) {a.def.id}와 겹칩니다 — 같은 슬롯은 서로 다른 폼 전용(formReq)이어야 합니다");
                }
        }
    }
}
