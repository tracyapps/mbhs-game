using System;
using System.Collections.Generic;

namespace MBHS.Data.Models
{
    [Serializable]
    public class ShowScore
    {
        public float OverallScore;     // 0-100
        public float MusicScore;       // 0-100
        public float FormationScore;   // 0-100
        public float ShowmanshipScore; // 0-100
        public float DifficultyBonus;  // 0-20
        public string Grade;
        public List<ScoringNote> Notes = new();

        public static string CalculateGrade(float score)
        {
            return score switch
            {
                >= 97f => "A+",
                >= 93f => "A",
                >= 90f => "A-",
                >= 87f => "B+",
                >= 83f => "B",
                >= 80f => "B-",
                >= 77f => "C+",
                >= 73f => "C",
                >= 70f => "C-",
                >= 67f => "D+",
                >= 63f => "D",
                >= 60f => "D-",
                _ => "F"
            };
        }
    }

    [Serializable]
    public class ScoringNote
    {
        public float AtBeat;
        public string Category;    // "Formation", "Music", "Showmanship"
        public string Description;
        public float Impact;       // positive = good, negative = deduction
    }

    [Serializable]
    public class ScoringFrame
    {
        public float Beat;
        public List<MemberPerformanceSnapshot> MemberSnapshots = new();
    }

    [Serializable]
    public class MemberPerformanceSnapshot
    {
        public string MemberId;
        public float ActualX;
        public float ActualY;
        public float TargetX;
        public float TargetY;
        public float PositionError;
        public float FacingError;
        public float PlayingQuality; // 0-1, derived from skill + randomness
    }
}
