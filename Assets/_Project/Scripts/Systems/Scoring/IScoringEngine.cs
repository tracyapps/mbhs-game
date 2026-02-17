using System;
using MBHS.Data.Models;

namespace MBHS.Systems.Scoring
{
    public interface IScoringEngine
    {
        bool IsEvaluating { get; }
        float RunningScore { get; }

        void BeginEvaluation(DrillChart chart, BandRosterData roster);
        void RecordFrame(ScoringFrame frame);
        ShowScore FinalizeEvaluation();
        void CancelEvaluation();

        event Action<float> OnRunningScoreUpdated;
        event Action<ScoringNote> OnNotableEvent;
    }
}
