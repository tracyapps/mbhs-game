using System;
using System.Collections.Generic;
using MBHS.Data.Enums;
using MBHS.Data.Models;
using UnityEngine;

namespace MBHS.Systems.Scoring
{
    public class ScoringEngine : IScoringEngine
    {
        // Weight defaults (can be overridden by ScoringRubricDefinition)
        private float _formationWeight = 0.4f;
        private float _musicWeight = 0.35f;
        private float _showmanshipWeight = 0.15f;
        private float _difficultyWeight = 0.1f;
        private float _positionErrorThreshold = 0.5f;

        private DrillChart _chart;
        private BandRosterData _roster;
        private List<ScoringFrame> _frames;
        private List<ScoringNote> _notes;

        public bool IsEvaluating { get; private set; }
        public float RunningScore { get; private set; }

        public event Action<float> OnRunningScoreUpdated;
        public event Action<ScoringNote> OnNotableEvent;

        public void BeginEvaluation(DrillChart chart, BandRosterData roster)
        {
            _chart = chart;
            _roster = roster;
            _frames = new List<ScoringFrame>();
            _notes = new List<ScoringNote>();
            RunningScore = 100f;
            IsEvaluating = true;
        }

        public void RecordFrame(ScoringFrame frame)
        {
            if (!IsEvaluating) return;

            _frames.Add(frame);

            // Calculate running score based on this frame
            float frameScore = EvaluateFrame(frame);
            RunningScore = CalculateRunningScore();

            OnRunningScoreUpdated?.Invoke(RunningScore);
        }

        public ShowScore FinalizeEvaluation()
        {
            if (!IsEvaluating)
                return new ShowScore();

            IsEvaluating = false;

            float formationScore = CalculateFormationScore();
            float musicScore = CalculateMusicScore();
            float showmanshipScore = CalculateShowmanshipScore();
            float difficultyBonus = CalculateDifficultyBonus();

            float overall = formationScore * _formationWeight +
                           musicScore * _musicWeight +
                           showmanshipScore * _showmanshipWeight +
                           difficultyBonus * _difficultyWeight;

            overall = Mathf.Clamp(overall + difficultyBonus * 0.2f, 0f, 100f);

            var score = new ShowScore
            {
                OverallScore = overall,
                MusicScore = musicScore,
                FormationScore = formationScore,
                ShowmanshipScore = showmanshipScore,
                DifficultyBonus = difficultyBonus,
                Notes = new List<ScoringNote>(_notes),
                Grade = ShowScore.CalculateGrade(overall)
            };

            return score;
        }

        public void CancelEvaluation()
        {
            IsEvaluating = false;
            _frames?.Clear();
            _notes?.Clear();
        }

        private float EvaluateFrame(ScoringFrame frame)
        {
            float totalError = 0f;
            int count = 0;

            foreach (var snapshot in frame.MemberSnapshots)
            {
                totalError += snapshot.PositionError;
                count++;

                // Generate notes for significant errors
                if (snapshot.PositionError > _positionErrorThreshold * 3f)
                {
                    var note = new ScoringNote
                    {
                        AtBeat = frame.Beat,
                        Category = "Formation",
                        Description = $"Member significantly out of position",
                        Impact = -snapshot.PositionError
                    };
                    _notes.Add(note);
                    OnNotableEvent?.Invoke(note);
                }
            }

            return count > 0 ? 100f - (totalError / count) * 10f : 100f;
        }

        private float CalculateRunningScore()
        {
            if (_frames.Count == 0) return 100f;

            float totalFormationError = 0f;
            int totalSnapshots = 0;

            foreach (var frame in _frames)
            {
                foreach (var snapshot in frame.MemberSnapshots)
                {
                    totalFormationError += snapshot.PositionError;
                    totalSnapshots++;
                }
            }

            float avgError = totalSnapshots > 0 ? totalFormationError / totalSnapshots : 0f;
            return Mathf.Clamp(100f - avgError * 20f, 0f, 100f);
        }

        private float CalculateFormationScore()
        {
            if (_frames.Count == 0) return 100f;

            float totalError = 0f;
            int count = 0;

            foreach (var frame in _frames)
            {
                foreach (var snapshot in frame.MemberSnapshots)
                {
                    // Score based on position error relative to threshold
                    float errorRatio = snapshot.PositionError / _positionErrorThreshold;
                    totalError += Mathf.Min(errorRatio, 5f); // cap at 5x threshold
                    count++;
                }
            }

            float avgErrorRatio = count > 0 ? totalError / count : 0f;
            return Mathf.Clamp(100f - avgErrorRatio * 20f, 0f, 100f);
        }

        private float CalculateMusicScore()
        {
            if (_roster == null) return 50f;

            // Base music score on average musicianship of active members
            float totalMusicianship = 0f;
            int count = 0;

            foreach (var member in _roster.Members)
            {
                if (member.Status == MemberStatus.Active)
                {
                    totalMusicianship += member.Musicianship;
                    count++;
                }
            }

            float avgMusicianship = count > 0 ? totalMusicianship / count : 0.5f;

            // Also factor in playing quality from frames
            float avgPlayingQuality = 0.5f;
            if (_frames.Count > 0)
            {
                float totalQuality = 0f;
                int qualityCount = 0;
                foreach (var frame in _frames)
                {
                    foreach (var snapshot in frame.MemberSnapshots)
                    {
                        totalQuality += snapshot.PlayingQuality;
                        qualityCount++;
                    }
                }
                if (qualityCount > 0)
                    avgPlayingQuality = totalQuality / qualityCount;
            }

            return (avgMusicianship * 0.6f + avgPlayingQuality * 0.4f) * 100f;
        }

        private float CalculateShowmanshipScore()
        {
            if (_roster == null) return 50f;

            // Based on average showmanship skill
            float totalShowmanship = 0f;
            int count = 0;

            foreach (var member in _roster.Members)
            {
                if (member.Status == MemberStatus.Active)
                {
                    totalShowmanship += member.Showmanship;
                    count++;
                }
            }

            float avgShowmanship = count > 0 ? totalShowmanship / count : 0.5f;

            // Bonus for formation complexity (number of formations)
            float complexityBonus = 0f;
            if (_chart != null)
            {
                complexityBonus = Mathf.Min(_chart.Formations.Count * 2f, 15f);
            }

            return Mathf.Clamp(avgShowmanship * 85f + complexityBonus, 0f, 100f);
        }

        private float CalculateDifficultyBonus()
        {
            float bonus = 0f;

            if (_chart != null)
            {
                // More formations = harder
                bonus += Mathf.Min(_chart.Formations.Count * 1.5f, 10f);

                // Shorter transition times = harder
                for (int i = 1; i < _chart.Formations.Count; i++)
                {
                    var prev = _chart.Formations[i - 1];
                    var curr = _chart.Formations[i];
                    float transitionBeats = curr.StartBeat - (prev.StartBeat + prev.DurationBeats);
                    if (transitionBeats < 8f)
                        bonus += 1f;
                }
            }

            return Mathf.Clamp(bonus, 0f, 20f);
        }
    }
}
