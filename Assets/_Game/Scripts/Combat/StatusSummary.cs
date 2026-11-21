using System;
using System.Text;

/// <summary>
/// 파티 HUD 슬롯용 상태 효과 요약 문자열 (순수 정적, 설계도 22 §4-5-1 UI 요구).
/// ▲N 남은초 = 활성 이로운 효과(가장 짧은 남은 시간, 올림) / ○N = 예약(대기 중 건 버프) / ▼N = 해로운 효과.
/// </summary>
public static class StatusSummary
{
    public static string Format(StatusHolder h)
    {
        if (h == null || h.Count == 0) return string.Empty;

        int   active = 0, pending = 0, harmful = 0;
        float shortest = float.PositiveInfinity;

        foreach (var e in h.Effects)
        {
            if (e.Pending) { pending++; continue; }
            if (e.IsBeneficial)
            {
                active++;
                if (e.Duration < shortest) shortest = e.Duration;
            }
            else harmful++;
        }

        var sb = new StringBuilder();
        if (active > 0)
        {
            sb.Append('▲').Append(active);
            if (!float.IsInfinity(shortest)) sb.Append(' ').Append((int)Math.Ceiling(shortest)).Append('s');
        }
        if (pending > 0) { if (sb.Length > 0) sb.Append(' '); sb.Append('○').Append(pending); }
        if (harmful > 0) { if (sb.Length > 0) sb.Append(' '); sb.Append('▼').Append(harmful); }
        return sb.ToString();
    }
}
