using System;
using System.Globalization;
using UnityEngine;

/// <summary>
/// 강점 태그 1개의 계산 규칙(설계도 18 §3-4). 플레이어 노출층(displayKo·iconId)과 계산층(나머지)을 가진다.
/// 배율 = clamp(baseMult + coeffPerUnit × delta, baseMult, capMult), delta = high ? (값 − ref) : (ref − 값).
/// </summary>
[Serializable]
public class TagRule
{
    public string tagId, displayKo, iconId;
    public string targetAttr;       // "mass" | "velocity" | "temp" | "charge"
    public bool   directionHigh;    // true=값이 클수록 유리, false=작을수록 유리
    public float  baseMult = 1f, coeffPerUnit, refValue, capMult = 1f;

    public float Evaluate(AttributeProfile target)
    {
        int v = ReadAttr(target);
        float delta = directionHigh ? (v - refValue) : (refValue - v);
        return Mathf.Clamp(baseMult + coeffPerUnit * delta, baseMult, capMult);
    }

    int ReadAttr(AttributeProfile a)
    {
        switch (targetAttr)
        {
            case "mass":     return a.mass;
            case "velocity": return a.velocity;
            case "temp":     return a.temp;
            case "charge":   return a.charge;
            default:         return 0;
        }
    }

    public static string LabelKo(string attr)
    {
        switch (attr)
        {
            case "mass":     return "질량";
            case "velocity": return "속도";
            case "temp":     return "온도";
            case "charge":   return "전하";
            default:         return attr ?? "";
        }
    }

    static string N(float f) => f.ToString("0.###", CultureInfo.InvariantCulture);

    /// <summary>"자세히 보기"용 실제 계산식 문자열(구조화 계수로 조립 — 파싱 불필요).</summary>
    public string FormulaKo()
    {
        string label = LabelKo(targetAttr);
        string diff  = directionHigh ? $"대상 {label} − {N(refValue)}" : $"{N(refValue)} − 대상 {label}";
        return $"배율 = {N(baseMult)} + {N(coeffPerUnit)} × ({diff}), 최대 {N(capMult)}배";
    }
}

/// <summary>
/// 강점 태그 사전 (tag_rules.csv 임포트 결과, 에셋 이름 TagRuleTable).
/// 에셋이 없으면 v1 물리 8태그 기본 세트를 쓴다(임포터 이전에도 동작).
/// </summary>
[CreateAssetMenu(menuName = "MSRPG/Tag Rule Table", fileName = "TagRuleTable")]
public class TagRuleTable : ScriptableObject
{
    public TagRule[] rules;

    public TagRule Find(string tagId)
        => string.IsNullOrEmpty(tagId) || rules == null ? null : Array.Find(rules, r => r != null && r.tagId == tagId);

    static TagRuleTable _current;

    public static TagRuleTable Current
    {
        get
        {
            if (_current != null) return _current;
            _current = Resources.Load<TagRuleTable>("TagRuleTable");
            if (_current == null)
            {
                _current = CreateInstance<TagRuleTable>();
                _current.rules = DefaultPhysicsRules();
                _current.hideFlags = HideFlags.HideAndDontSave;
            }
            return _current;
        }
    }

    public static void SetForTests(TagRule[] rules)
    {
        var t = CreateInstance<TagRuleTable>();
        t.rules = rules;
        t.hideFlags = HideFlags.HideAndDontSave;
        _current = t;
    }

    public static void ResetForTests() => _current = null;

    /// <summary>태그들의 배율을 곱한다. 없거나 모르는 태그는 무시(배율 1).</summary>
    public static float Multiplier(string[] tagIds, AttributeProfile target)
    {
        if (tagIds == null) return 1f;
        float m = 1f;
        foreach (var id in tagIds)
        {
            var rule = Current.Find(id);
            if (rule != null) m *= rule.Evaluate(target);
        }
        return m;
    }

    /// <summary>설계도 18 §3-4 v1 물리 태그 초안 8종.</summary>
    public static TagRule[] DefaultPhysicsRules() => new[]
    {
        R("고온",   "열에 취약",       "temp",     true,  0.001f, 300f, 2.0f),
        R("저온",   "냉기에 취약",     "temp",     false, 0.002f, 300f, 1.8f),
        R("고속",   "빠른 적 특효",    "velocity", true,  0.01f,  50f,  1.8f),
        R("저속",   "둔한 적 특효",    "velocity", false, 0.01f,  50f,  1.8f),
        R("고질량", "무거운 적 특효",  "mass",     true,  0.005f, 100f, 1.8f),
        R("저질량", "가벼운 적 특효",  "mass",     false, 0.005f, 100f, 1.8f),
        R("양전하", "양전하 반응",     "charge",   true,  0.01f,  0f,   1.8f),
        R("음전하", "음전하 반응",     "charge",   false, 0.01f,  0f,   1.8f),
    };

    static TagRule R(string id, string label, string attr, bool high, float coeff, float refV, float cap)
        => new TagRule { tagId = id, displayKo = label, targetAttr = attr, directionHigh = high,
                         baseMult = 1f, coeffPerUnit = coeff, refValue = refV, capMult = cap };
}
