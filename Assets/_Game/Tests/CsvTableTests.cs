using NUnit.Framework;
using UnityEngine;

/// <summary>설계도 18-B — Stage A: CsvTable 파서 · CsvRow 접근자 · ImportReport.</summary>
public class CsvTableTests
{
    enum Fruit { Apple, Banana }

    static CsvTable Parse(string text, out ImportReport report, string sheet = "t.csv")
    {
        report = new ImportReport();
        return CsvTable.Parse(sheet, text, report);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 파서
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Parse_HeaderAndRows_TrimmedValues()
    {
        var t = Parse("id, name\n a ,  b \nc,d\n", out var r);
        Assert.IsFalse(r.HasErrors);
        CollectionAssert.AreEqual(new[] { "id", "name" }, t.Headers);
        Assert.AreEqual(2, t.Rows.Count);
        Assert.AreEqual("a", t.Rows[0].Str("id"));
        Assert.AreEqual("b", t.Rows[0].Str("name"));
    }

    [Test]
    public void Parse_BomCrlfAndNoTrailingNewline()
    {
        var t = Parse("﻿id,v\r\nx,1\r\ny,2", out var r);
        Assert.IsFalse(r.HasErrors);
        Assert.AreEqual("id", t.Headers[0], "BOM 제거");
        Assert.AreEqual(2, t.Rows.Count);
        Assert.AreEqual("2", t.Rows[1].Str("v"));
    }

    [Test]
    public void Parse_QuotedCommasQuotesAndNewlines_AndLineNumbers()
    {
        var t = Parse("id,text\nA,\"hello, \"\"world\"\"\"\nB,\"line1\nline2\"\nC,z\n", out var r);
        Assert.IsFalse(r.HasErrors);
        Assert.AreEqual("hello, \"world\"", t.Rows[0].Str("text"));
        Assert.AreEqual("line1\nline2", t.Rows[1].Str("text"));
        Assert.AreEqual(2, t.Rows[0].Line);
        Assert.AreEqual(3, t.Rows[1].Line);
        Assert.AreEqual(5, t.Rows[2].Line, "여러 줄 셀 뒤의 행 번호는 실제 파일 줄 기준");
    }

    [Test]
    public void Parse_SkipsCommentsAndBlankLines_HeaderIsFirstNonComment()
    {
        var t = Parse("# 범례\n# 더\n\nid,v\n\n# 중간 주석\nx,1\n,\n", out var r);
        Assert.IsFalse(r.HasErrors);
        CollectionAssert.AreEqual(new[] { "id", "v" }, t.Headers);
        Assert.AreEqual(1, t.Rows.Count, "주석·빈 줄·전부 빈 칸인 줄은 무시");
        Assert.AreEqual(7, t.Rows[0].Line);
    }

    [Test]
    public void Parse_ShortRowsArePadded_ExtraNonEmptyCellsAreAnError()
    {
        var t = Parse("a,b,c\n1\n1,2,3,4\n", out var r);
        Assert.AreEqual("", t.Rows[0].Str("c"), "짧은 행은 빈 칸으로 채움");
        Assert.IsTrue(r.HasErrors);
        StringAssert.Contains("t.csv:3", r.Errors[0]);
    }

    [Test]
    public void Parse_TrailingEmptyCells_AreAllowed()
    {
        Parse("a,b\n1,2,,\n", out var r);
        Assert.IsFalse(r.HasErrors);
    }

    [Test]
    public void Parse_DuplicateHeader_IsAnError()
    {
        Parse("a,b,a\n1,2,3\n", out var r);
        Assert.IsTrue(r.HasErrors);
        StringAssert.Contains("a", r.Errors[0]);
    }

    [Test]
    public void Parse_UnterminatedQuote_IsAnError()
    {
        Parse("a,b\n1,\"oops\n", out var r);
        Assert.IsTrue(r.HasErrors);
    }

    [Test]
    public void Parse_EmptyText_GivesEmptyTable_NoError()
    {
        var t = Parse("", out var r);
        Assert.IsFalse(r.HasErrors);
        Assert.AreEqual(0, t.Rows.Count);
        Assert.AreEqual(0, t.Headers.Count);
    }

    // ════════════════════════════════════════════════════════════════════════
    // 열 검사
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void RequireColumns_ReportsEachMissingColumnOnce()
    {
        var t = Parse("id,name\nx,y\n", out var r);
        Assert.IsFalse(t.RequireColumns(r, "id", "hp", "atk"));
        Assert.AreEqual(2, r.Errors.Count);
        StringAssert.Contains("hp", r.Errors[0]);
        StringAssert.Contains("t.csv", r.Errors[0]);
        Assert.IsTrue(t.RequireColumns(new ImportReport(), "id", "name"));
    }

    [Test]
    public void WarnUnknownColumns_ListsExtras()
    {
        var t = Parse("id,name,oops\nx,y,z\n", out var r);
        t.WarnUnknownColumns(r, "id", "name");
        Assert.AreEqual(1, r.Warnings.Count);
        StringAssert.Contains("oops", r.Warnings[0]);
        Assert.IsFalse(r.HasErrors);
    }

    // ════════════════════════════════════════════════════════════════════════
    // CsvRow 접근자
    // ════════════════════════════════════════════════════════════════════════

    static CsvRow Row(string header, string values, out ImportReport r)
    {
        var t = Parse(header + "\n" + values + "\n", out r);
        var row = t.Rows[0];
        row.Context = "id=x";
        return row;
    }

    [Test]
    public void Int_Float_Parse_AndDefaults()
    {
        var row = Row("i,f,e", "7,0.25,", out var r);
        Assert.AreEqual(7, row.Int("i"));
        Assert.AreEqual(0.25f, row.Float("f"), 0.0001f);
        Assert.AreEqual(5, row.Int("e", 5), "빈 칸 = 기본값");
        Assert.AreEqual(9f, row.Float("missingColumn", 9f), "없는 열 = 기본값");
        Assert.IsFalse(r.HasErrors);
    }

    [Test]
    public void Int_Invalid_ReportsSheetLineContextAndColumn()
    {
        var row = Row("i", "abc", out var r);
        Assert.AreEqual(0, row.Int("i"));
        Assert.IsTrue(r.HasErrors);
        StringAssert.Contains("[t.csv:2]", r.Errors[0]);
        StringAssert.Contains("id=x", r.Errors[0]);
        StringAssert.Contains("i", r.Errors[0]);
        StringAssert.Contains("abc", r.Errors[0]);
    }

    [Test]
    public void Float_UsesInvariantCulture()
    {
        var row = Row("f", "1.5", out var r);
        Assert.AreEqual(1.5f, row.Float("f"), 0.0001f);
        Assert.IsFalse(r.HasErrors);
    }

    [Test]
    public void Required_EmptyCell_IsAnError()
    {
        var row = Row("k,i,s", "x,,", out var r);
        row.Int("i", 0, required: true);
        row.Str("s", "", required: true);
        Assert.AreEqual(2, r.Errors.Count);
    }

    [Test]
    public void Bool_AcceptsCommonForms()
    {
        var row = Row("a,b,c,d,e", "true,FALSE,1,0,", out var r);
        Assert.IsTrue(row.Bool("a"));
        Assert.IsFalse(row.Bool("b"));
        Assert.IsTrue(row.Bool("c"));
        Assert.IsFalse(row.Bool("d"));
        Assert.IsTrue(row.Bool("e", true), "빈 칸 = 기본값");
        Assert.IsFalse(r.HasErrors);
    }

    [Test]
    public void Bool_Invalid_IsAnError()
    {
        var row = Row("a", "maybe", out var r);
        row.Bool("a");
        Assert.IsTrue(r.HasErrors);
    }

    [Test]
    public void List_SplitsOnSemicolon_TrimsAndDropsEmpty()
    {
        var row = Row("l,e", "a; b ;;c,", out _);
        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, row.List("l"));
        Assert.AreEqual(0, row.List("e").Length);
        Assert.AreEqual(0, row.List("nope").Length);
    }

    [Test]
    public void Enum_ParsesExactNames_AndReportsAllowedValues()
    {
        var row = Row("f,g,h", "Banana,apple,", out var r);
        Assert.AreEqual(Fruit.Banana, row.Enum("f", Fruit.Apple));
        Assert.AreEqual(Fruit.Apple, row.Enum("g", Fruit.Apple), "대소문자 다르면 오류 → 기본값 반환");
        Assert.AreEqual(1, r.Errors.Count);
        StringAssert.Contains("Apple, Banana", r.Errors[0]);
        Assert.AreEqual(Fruit.Banana, row.Enum("h", Fruit.Banana), "빈 칸 = 기본값");
    }

    [Test]
    public void Enum_NumericStrings_AreRejected()
    {
        var row = Row("f", "1", out var r);
        row.Enum("f", Fruit.Apple);
        Assert.IsTrue(r.HasErrors, "숫자로 enum을 지정하지 못하게 한다");
    }

    [Test]
    public void Color_ParsesHex_AndRejectsGarbage()
    {
        var row = Row("k,c,d,e", "x,#FF8000,zzz,", out var r);
        var c = row.Color("c", Color.white);
        Assert.AreEqual(1f, c.r, 0.01f);
        Assert.AreEqual(0.5f, c.g, 0.01f);
        Assert.AreEqual(0f, c.b, 0.01f);
        row.Color("d", Color.white);
        Assert.AreEqual(1, r.Errors.Count);
        Assert.AreEqual(Color.gray, row.Color("e", Color.gray), "빈 칸 = 기본값");
    }

    // ════════════════════════════════════════════════════════════════════════
    // ImportReport
    // ════════════════════════════════════════════════════════════════════════

    [Test]
    public void Report_FormatsErrorsAndWarnings()
    {
        var r = new ImportReport();
        r.Error("a.csv", 12, "skillId=s1", "문제");
        r.Error("a.csv", 0, null, "시트 전체 문제");
        r.Warn("b.csv", 3, null, "경고");
        Assert.AreEqual("[a.csv:12] skillId=s1: 문제", r.Errors[0]);
        Assert.AreEqual("[a.csv] 시트 전체 문제", r.Errors[1]);
        Assert.AreEqual("[b.csv:3] 경고", r.Warnings[0]);
        Assert.IsTrue(r.HasErrors);
    }
}
