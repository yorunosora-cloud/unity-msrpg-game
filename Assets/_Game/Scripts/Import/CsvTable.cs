using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

/// <summary>
/// CSV 시트 1장 (설계도 18 §8, 18-B 공통 규칙).
/// UTF-8(BOM 허용), 쉼표 구분, "로 감싸면 쉼표·줄바꿈 허용(""=따옴표), 첫 칸이 #인 줄은 주석, 빈 줄 무시,
/// 첫 비주석 줄 = 헤더. 셀 값은 앞뒤 공백을 제거한다.
/// </summary>
public class CsvTable
{
    public string        Sheet   { get; private set; }
    public List<string>  Headers { get; } = new();
    public List<CsvRow>  Rows    { get; } = new();

    readonly Dictionary<string, int> _col = new();

    public int ColumnIndex(string name) => _col.TryGetValue(name, out int i) ? i : -1;
    public bool HasColumn(string name)  => _col.ContainsKey(name);

    public static CsvTable Parse(string sheet, string text, ImportReport report)
    {
        var table = new CsvTable { Sheet = sheet };
        if (string.IsNullOrEmpty(text)) return table;

        foreach (var rec in Tokenize(sheet, text, report))
        {
            if (rec.cells.Count > 0 && rec.cells[0].StartsWith("#")) continue;     // 주석
            if (rec.cells.TrueForAll(string.IsNullOrEmpty)) continue;                // 빈 줄

            if (table.Headers.Count == 0)
            {
                foreach (var h in rec.cells)
                {
                    if (string.IsNullOrEmpty(h)) continue;
                    if (table._col.ContainsKey(h))
                    {
                        report.Error(sheet, rec.line, null, $"헤더 열 '{h}'가 중복되었습니다");
                        continue;
                    }
                    table._col[h] = table.Headers.Count;
                    table.Headers.Add(h);
                }
                continue;
            }

            // 헤더보다 많은 칸에 값이 있으면 오류, 비어 있으면 허용(끝의 쉼표)
            for (int i = table.Headers.Count; i < rec.cells.Count; i++)
                if (!string.IsNullOrEmpty(rec.cells[i]))
                {
                    report.Error(sheet, rec.line, null, $"열 개수({rec.cells.Count})가 헤더({table.Headers.Count})보다 많습니다");
                    break;
                }

            var cells = new string[table.Headers.Count];
            for (int i = 0; i < cells.Length; i++) cells[i] = i < rec.cells.Count ? rec.cells[i] : "";
            table.Rows.Add(new CsvRow(table, report, rec.line, cells));
        }
        return table;
    }

    /// <summary>필수 열이 모두 있는지 확인. 없는 열마다 오류 1건.</summary>
    public bool RequireColumns(ImportReport report, params string[] columns)
    {
        bool ok = true;
        foreach (var c in columns)
            if (!_col.ContainsKey(c)) { report.Error(Sheet, 0, null, $"필수 열 '{c}'가 없습니다"); ok = false; }
        return ok;
    }

    /// <summary>알려지지 않은 열이 있으면 경고 1건.</summary>
    public void WarnUnknownColumns(ImportReport report, params string[] known)
    {
        var set = new HashSet<string>(known);
        var extra = new List<string>();
        foreach (var h in Headers) if (!set.Contains(h)) extra.Add(h);
        if (extra.Count > 0) report.Warn(Sheet, 0, null, $"알 수 없는 열이 무시됩니다: {string.Join(", ", extra)}");
    }

    // ── 토큰화 ────────────────────────────────────────────────────────────

    struct Record { public int line; public List<string> cells; }

    static List<Record> Tokenize(string sheet, string text, ImportReport report)
    {
        var records = new List<Record>();
        if (text.Length > 0 && text[0] == '﻿') text = text.Substring(1);

        var cells = new List<string>();
        var sb    = new StringBuilder();
        int line = 1, recStart = 1;
        bool inQuotes = false, quoted = false;

        void EndCell() { cells.Add(sb.ToString().Trim()); sb.Clear(); quoted = false; }
        void EndRecord()
        {
            EndCell();
            records.Add(new Record { line = recStart, cells = cells });
            cells = new List<string>();
        }

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"') { sb.Append('"'); i++; }
                    else inQuotes = false;
                }
                else if (c == '\r') { if (i + 1 < text.Length && text[i + 1] == '\n') continue; sb.Append('\n'); line++; }
                else { if (c == '\n') line++; sb.Append(c); }
                continue;
            }

            if (c == '"' && sb.ToString().Trim().Length == 0 && !quoted) { inQuotes = true; quoted = true; sb.Clear(); }
            else if (c == ',') EndCell();
            else if (c == '\r' || c == '\n')
            {
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                EndRecord();
                line++;
                recStart = line;
            }
            else sb.Append(c);
        }

        if (inQuotes) report.Error(sheet, recStart, null, "닫히지 않은 따옴표가 있습니다");
        if (sb.Length > 0 || cells.Count > 0) EndRecord();
        return records;
    }
}

/// <summary>CsvTable의 한 행. 열 이름으로 값을 읽으며 형식 오류는 시트·행 번호와 함께 보고서에 쌓는다.</summary>
public class CsvRow
{
    readonly CsvTable     _table;
    readonly ImportReport _report;
    readonly string[]     _cells;

    public int    Line    { get; }
    public string Sheet   => _table.Sheet;

    /// <summary>오류 메시지 앞에 붙는 맥락(예: "skillId=forte_s3"). 빌더가 설정한다.</summary>
    public string Context { get; set; }

    internal CsvRow(CsvTable table, ImportReport report, int line, string[] cells)
    {
        _table = table; _report = report; Line = line; _cells = cells;
    }

    string Raw(string col)
    {
        int i = _table.ColumnIndex(col);
        return i >= 0 && i < _cells.Length ? _cells[i] : "";
    }

    public bool Has(string col) => !string.IsNullOrEmpty(Raw(col));

    public void Error(string message) => _report.Error(Sheet, Line, Context, message);
    public void Warn (string message) => _report.Warn (Sheet, Line, Context, message);

    bool Missing(string col, bool required)
    {
        if (!string.IsNullOrEmpty(Raw(col))) return false;
        if (required && _table.HasColumn(col)) Error($"{col}은(는) 필수입니다");
        return true;
    }

    public string Str(string col, string def = "", bool required = false)
        => Missing(col, required) ? def : Raw(col);

    public int Int(string col, int def = 0, bool required = false)
    {
        if (Missing(col, required)) return def;
        string v = Raw(col);
        if (int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n)) return n;
        Error($"{col} '{v}'는 정수가 아닙니다");
        return def;
    }

    public float Float(string col, float def = 0f, bool required = false)
    {
        if (Missing(col, required)) return def;
        string v = Raw(col);
        if (float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out float f)) return f;
        Error($"{col} '{v}'는 숫자가 아닙니다");
        return def;
    }

    public bool Bool(string col, bool def = false, bool required = false)
    {
        if (Missing(col, required)) return def;
        string v = Raw(col).ToLowerInvariant();
        if (v == "true" || v == "1") return true;
        if (v == "false" || v == "0") return false;
        Error($"{col} '{Raw(col)}'는 true/false(또는 1/0)가 아닙니다");
        return def;
    }

    /// <summary>';'로 구분한 리스트. 앞뒤 공백 제거, 빈 항목 제외. 열이 없거나 비면 빈 배열.</summary>
    public string[] List(string col)
    {
        string v = Raw(col);
        if (string.IsNullOrEmpty(v)) return new string[0];
        var list = new List<string>();
        foreach (var part in v.Split(';'))
        {
            string p = part.Trim();
            if (p.Length > 0) list.Add(p);
        }
        return list.ToArray();
    }

    /// <summary>C# enum 이름과 정확히 일치해야 한다(대소문자 구분, 숫자 불가).</summary>
    public T Enum<T>(string col, T def, bool required = false) where T : struct, System.Enum
    {
        if (Missing(col, required)) return def;
        string v = Raw(col);
        foreach (var name in System.Enum.GetNames(typeof(T)))
            if (name == v) return (T)System.Enum.Parse(typeof(T), v);

        var names = System.Enum.GetNames(typeof(T));
        Array.Sort(names, StringComparer.Ordinal);
        Error($"{col} '{v}'는 알 수 없는 값입니다 (허용: {string.Join(", ", names)})");
        return def;
    }

    public UnityEngine.Color Color(string col, UnityEngine.Color def)
    {
        if (Missing(col, false)) return def;
        string v = Raw(col);
        if (ColorUtility.TryParseHtmlString(v, out var c)) return c;
        Error($"{col} '{v}'는 #RRGGBB 색이 아닙니다");
        return def;
    }
}
