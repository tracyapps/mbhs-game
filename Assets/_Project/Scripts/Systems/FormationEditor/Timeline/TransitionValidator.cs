using System.Collections.Generic;
using System.Linq;
using MBHS.Data.Models;
using UnityEngine;

namespace MBHS.Systems.FormationEditor
{
    public enum TransitionSeverity
    {
        Normal,     // <= 2.5 yds/s - comfortable march
        Fast,       // <= 4.0 yds/s - quick march
        Hard,       // <= 5.0 yds/s - running
        Impossible  // > 5.0 yds/s  - physically not feasible
    }

    public struct TransitionResult
    {
        public float MaxSpeed;           // yards per second
        public float AverageSpeed;       // yards per second
        public string FastestMemberId;
        public float GapBeats;
        public float GapSeconds;
        public TransitionSeverity Severity;
    }

    public static class TransitionValidator
    {
        public const float NormalSpeedLimit = 2.5f;   // yds/s (~120 steps/min at 8-to-5)
        public const float FastSpeedLimit = 4.0f;     // yds/s (~180 steps/min at 6-to-5)
        public const float HardSpeedLimit = 5.0f;     // yds/s (running)

        public static TransitionResult ValidateTransition(Formation from, Formation to, float bpm)
        {
            var result = new TransitionResult();

            float gapBeats = to.StartBeat - from.StartBeat - from.DurationBeats;
            result.GapBeats = Mathf.Max(0f, gapBeats);
            result.GapSeconds = bpm > 0 ? result.GapBeats * 60f / bpm : 0f;

            if (result.GapSeconds <= 0f)
            {
                result.Severity = TransitionSeverity.Impossible;
                result.MaxSpeed = float.MaxValue;
                return result;
            }

            float maxSpeed = 0f;
            float totalSpeed = 0f;
            int memberCount = 0;
            string fastestId = null;

            // Check each member that exists in both formations
            foreach (var fromPos in from.Positions)
            {
                var toPos = to.GetPositionForMember(fromPos.MemberId);
                if (toPos == null) continue;

                float distance = Vector2.Distance(fromPos.FieldPosition, toPos.FieldPosition);
                float speed = distance / result.GapSeconds;

                totalSpeed += speed;
                memberCount++;

                if (speed > maxSpeed)
                {
                    maxSpeed = speed;
                    fastestId = fromPos.MemberId;
                }
            }

            result.MaxSpeed = maxSpeed;
            result.AverageSpeed = memberCount > 0 ? totalSpeed / memberCount : 0f;
            result.FastestMemberId = fastestId;
            result.Severity = GetSeverity(maxSpeed);

            return result;
        }

        public static TransitionSeverity GetSeverity(float speedYardsPerSecond)
        {
            if (speedYardsPerSecond <= NormalSpeedLimit) return TransitionSeverity.Normal;
            if (speedYardsPerSecond <= FastSpeedLimit) return TransitionSeverity.Fast;
            if (speedYardsPerSecond <= HardSpeedLimit) return TransitionSeverity.Hard;
            return TransitionSeverity.Impossible;
        }

        public static string GetSeverityLabel(TransitionSeverity severity)
        {
            return severity switch
            {
                TransitionSeverity.Normal => "Normal",
                TransitionSeverity.Fast => "Fast",
                TransitionSeverity.Hard => "Hard",
                TransitionSeverity.Impossible => "Impossible",
                _ => "Unknown"
            };
        }
    }
}
