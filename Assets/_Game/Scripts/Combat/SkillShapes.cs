using UnityEngine;

/// <summary>스킬 판정 형태 (설계도 22 §4-2).</summary>
public enum SkillShape { Cone, Line, Circle }

/// <summary>스킬 판정 형태 수학 (순수 정적, 수직 차이 무시).</summary>
public static class SkillShapes
{
    /// <summary>
    /// target이 형태 안에 있는지 판정한다.
    /// Cone: range + halfAngle(전방 기준 ±도). Line: range(길이) + lineWidth(폭), 전방 직선.
    /// Circle: 시전자 중심 radius. dist = 수평 거리.
    /// </summary>
    public static bool Contains(SkillShape shape, Vector3 origin, Vector3 forward, Vector3 target,
                                float range, float halfAngle, float lineWidth, float radius,
                                out float dist)
    {
        Vector3 to = target - origin;
        to.y = 0f;
        dist = to.magnitude;

        Vector3 f = new Vector3(forward.x, 0f, forward.z);
        if (f.sqrMagnitude < 1e-6f) f = Vector3.forward;
        f.Normalize();

        switch (shape)
        {
            case SkillShape.Line:
            {
                float along   = Vector3.Dot(to, f);
                float lateral = Mathf.Abs(Vector3.Dot(to, new Vector3(f.z, 0f, -f.x)));
                return along >= 0f && along <= range && lateral <= lineWidth * 0.5f;
            }
            case SkillShape.Circle:
                return dist <= radius;
            default: // Cone
                if (dist > range) return false;
                return Vector3.Angle(f, to) <= halfAngle;
        }
    }
}
