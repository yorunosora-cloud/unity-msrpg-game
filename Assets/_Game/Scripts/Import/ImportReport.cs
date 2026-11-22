using System.Collections.Generic;

/// <summary>
/// CSV 임포트 결과 보고서 (설계도 18 §8). 오류·경고를 "[시트:행] 맥락: 메시지" 형식으로 모은다.
/// 오류가 하나라도 있으면 임포터는 에셋을 쓰지 않는다.
/// </summary>
public class ImportReport
{
    public readonly List<string> Errors   = new();
    public readonly List<string> Warnings = new();

    public bool HasErrors => Errors.Count > 0;

    /// <param name="line">0 이하이면 시트 전체에 대한 메시지("[시트] 메시지")</param>
    public void Error(string sheet, int line, string context, string message) => Errors.Add(Format(sheet, line, context, message));
    public void Warn (string sheet, int line, string context, string message) => Warnings.Add(Format(sheet, line, context, message));

    static string Format(string sheet, int line, string context, string message)
    {
        string head = line > 0 ? $"[{sheet}:{line}]" : $"[{sheet}]";
        return string.IsNullOrEmpty(context) ? $"{head} {message}" : $"{head} {context}: {message}";
    }
}
