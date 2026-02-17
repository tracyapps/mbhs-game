using NUnit.Framework;
using MBHS.Systems.Scoring;
using MBHS.Data.Models;
using MBHS.Data.Enums;
using System.Collections.Generic;

namespace MBHS.Tests.EditMode
{
    public class ScoringEngineTests
    {
        private ScoringEngine _engine;
        private DrillChart _testChart;
        private BandRosterData _testRoster;

        [SetUp]
        public void SetUp()
        {
            _engine = new ScoringEngine();

            _testChart = new DrillChart
            {
                Id = "test",
                Name = "Test Chart",
                Formations = new List<Formation>
                {
                    new Formation
                    {
                        Id = "f1",
                        StartBeat = 0f,
                        DurationBeats = 8f,
                        Positions = new List<MemberPosition>
                        {
                            new MemberPosition { MemberId = "m1", FieldX = 50f, FieldY = 26.67f }
                        }
                    }
                }
            };

            _testRoster = new BandRosterData
            {
                SchoolId = "school_1",
                Members = new List<BandMemberData>
                {
                    new BandMemberData
                    {
                        Id = "m1",
                        FirstName = "Test",
                        LastName = "Member",
                        Status = MemberStatus.Active,
                        Musicianship = 0.8f,
                        Marching = 0.8f,
                        Stamina = 0.8f,
                        Showmanship = 0.8f,
                        AssignedInstrument = InstrumentType.Trumpet
                    }
                }
            };
        }

        [Test]
        public void BeginEvaluation_SetsIsEvaluating()
        {
            _engine.BeginEvaluation(_testChart, _testRoster);

            Assert.IsTrue(_engine.IsEvaluating);
        }

        [Test]
        public void FinalizeEvaluation_ReturnsScore()
        {
            _engine.BeginEvaluation(_testChart, _testRoster);

            // Record a perfect frame
            _engine.RecordFrame(new ScoringFrame
            {
                Beat = 0f,
                MemberSnapshots = new List<MemberPerformanceSnapshot>
                {
                    new MemberPerformanceSnapshot
                    {
                        MemberId = "m1",
                        ActualX = 50f, ActualY = 26.67f,
                        TargetX = 50f, TargetY = 26.67f,
                        PositionError = 0f,
                        FacingError = 0f,
                        PlayingQuality = 0.9f
                    }
                }
            });

            var score = _engine.FinalizeEvaluation();

            Assert.IsNotNull(score);
            Assert.Greater(score.OverallScore, 0f);
            Assert.IsNotNull(score.Grade);
            Assert.IsFalse(_engine.IsEvaluating);
        }

        [Test]
        public void PerfectPerformance_ScoresHigh()
        {
            _engine.BeginEvaluation(_testChart, _testRoster);

            for (int i = 0; i < 8; i++)
            {
                _engine.RecordFrame(new ScoringFrame
                {
                    Beat = i,
                    MemberSnapshots = new List<MemberPerformanceSnapshot>
                    {
                        new MemberPerformanceSnapshot
                        {
                            MemberId = "m1",
                            PositionError = 0f,
                            FacingError = 0f,
                            PlayingQuality = 1.0f
                        }
                    }
                });
            }

            var score = _engine.FinalizeEvaluation();
            Assert.Greater(score.FormationScore, 90f);
        }

        [Test]
        public void PoorPerformance_ScoresLow()
        {
            _engine.BeginEvaluation(_testChart, _testRoster);

            for (int i = 0; i < 8; i++)
            {
                _engine.RecordFrame(new ScoringFrame
                {
                    Beat = i,
                    MemberSnapshots = new List<MemberPerformanceSnapshot>
                    {
                        new MemberPerformanceSnapshot
                        {
                            MemberId = "m1",
                            PositionError = 5f, // 5 yards off
                            FacingError = 45f,
                            PlayingQuality = 0.3f
                        }
                    }
                });
            }

            var score = _engine.FinalizeEvaluation();
            Assert.Less(score.FormationScore, 50f);
        }

        [Test]
        public void CalculateGrade_ReturnsCorrectGrades()
        {
            Assert.AreEqual("A+", ShowScore.CalculateGrade(97f));
            Assert.AreEqual("A", ShowScore.CalculateGrade(95f));
            Assert.AreEqual("B+", ShowScore.CalculateGrade(88f));
            Assert.AreEqual("C", ShowScore.CalculateGrade(75f));
            Assert.AreEqual("F", ShowScore.CalculateGrade(50f));
        }

        [Test]
        public void CancelEvaluation_StopsEvaluating()
        {
            _engine.BeginEvaluation(_testChart, _testRoster);
            _engine.CancelEvaluation();

            Assert.IsFalse(_engine.IsEvaluating);
        }
    }
}
