using System;
using MBHS.Data.Enums;
using MBHS.Data.Models;

namespace MBHS.Systems.ShowSimulation
{
    public interface IShowSimulator
    {
        ShowState CurrentState { get; }
        float CurrentBeat { get; }

        void PrepareShow(DrillChart chart, BandRosterData roster, SongData song);
        void StartShow();
        void PauseShow();
        void ResumeShow();
        void StopShow();

        // Preview (used by FormationEditor)
        void PreviewFormation(Formation formation, BandRosterData roster);
        void PreviewTransition(Formation from, Formation to,
                              float progress, BandRosterData roster);

        event Action<ShowState> OnShowStateChanged;
        event Action<ScoringFrame> OnFrameRecorded;
        event Action<ShowScore> OnShowComplete;
    }
}
