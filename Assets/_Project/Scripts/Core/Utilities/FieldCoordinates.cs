using UnityEngine;

namespace MBHS.Core.Utilities
{
    /// <summary>
    /// Converts between field coordinates (yards) and Unity world space.
    /// Field coordinates: X = 0-100 (end zone to end zone), Y = 0-53.33 (sideline to sideline).
    /// World space: Field centered at origin, 1 unit = 1 yard.
    /// </summary>
    public static class FieldCoordinates
    {
        public const float FieldLengthYards = 100f;
        public const float FieldWidthYards = 53.33f;
        public const float HalfFieldLength = FieldLengthYards / 2f;
        public const float HalfFieldWidth = FieldWidthYards / 2f;

        // Standard marching step size (22.5 inches = 0.625 yards)
        public const float StepSize = 0.625f;

        // Hash mark positions from home sideline (in yards)
        public const float HomeHashYards = 17.78f;  // college
        public const float VisitorHashYards = 35.56f; // college

        public static Vector3 FieldToWorld(Vector2 fieldPos)
        {
            return new Vector3(
                fieldPos.x - HalfFieldLength,
                0f,
                fieldPos.y - HalfFieldWidth
            );
        }

        public static Vector3 FieldToWorld(float fieldX, float fieldY)
        {
            return new Vector3(
                fieldX - HalfFieldLength,
                0f,
                fieldY - HalfFieldWidth
            );
        }

        public static Vector2 WorldToField(Vector3 worldPos)
        {
            return new Vector2(
                worldPos.x + HalfFieldLength,
                worldPos.z + HalfFieldWidth
            );
        }

        public static Vector2 SnapToGrid(Vector2 fieldPos, float gridSize = StepSize)
        {
            return new Vector2(
                Mathf.Round(fieldPos.x / gridSize) * gridSize,
                Mathf.Round(fieldPos.y / gridSize) * gridSize
            );
        }

        public static Vector2 SnapToYardLines(Vector2 fieldPos)
        {
            return new Vector2(
                Mathf.Round(fieldPos.x / 5f) * 5f,
                fieldPos.y
            );
        }

        public static Vector2 ClampToField(Vector2 fieldPos)
        {
            return new Vector2(
                Mathf.Clamp(fieldPos.x, 0f, FieldLengthYards),
                Mathf.Clamp(fieldPos.y, 0f, FieldWidthYards)
            );
        }

        public static string GetYardLineLabel(float fieldX)
        {
            float yardLine = fieldX <= 50f ? fieldX : 100f - fieldX;
            int rounded = Mathf.RoundToInt(yardLine);
            return rounded == 50 ? "50" : rounded.ToString();
        }

        public static string GetFieldPositionDescription(Vector2 fieldPos)
        {
            string yardLine = GetYardLineLabel(fieldPos.x);
            string side = fieldPos.x <= 50f ? "own" : "opp";
            string hashRef;

            if (fieldPos.y < HomeHashYards)
                hashRef = "home side";
            else if (fieldPos.y > VisitorHashYards)
                hashRef = "visitor side";
            else
                hashRef = "between hashes";

            return $"{side} {yardLine}, {hashRef}";
        }
    }
}
