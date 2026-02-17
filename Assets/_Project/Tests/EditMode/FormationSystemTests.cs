using NUnit.Framework;
using MBHS.Systems.FormationEditor;
using MBHS.Data.Models;
using MBHS.Data.Enums;
using UnityEngine;

namespace MBHS.Tests.EditMode
{
    public class FormationSystemTests
    {
        private FormationSystem _system;

        [SetUp]
        public void SetUp()
        {
            _system = new FormationSystem();
        }

        [Test]
        public void CreateNewChart_SetsActiveChart()
        {
            _system.CreateNewChart("Test Chart", "song_001");

            Assert.IsNotNull(_system.ActiveChart);
            Assert.AreEqual("Test Chart", _system.ActiveChart.Name);
            Assert.AreEqual("song_001", _system.ActiveChart.SongId);
            Assert.IsNotNull(_system.ActiveChart.Id);
        }

        [Test]
        public void AddFormation_AddsToChart()
        {
            _system.CreateNewChart("Test", "song_001");

            var formation = _system.AddFormation(0f, 8f, "Opening Set");

            Assert.IsNotNull(formation);
            Assert.AreEqual(1, _system.ActiveChart.Formations.Count);
            Assert.AreEqual("Opening Set", formation.Label);
            Assert.AreEqual(0f, formation.StartBeat);
            Assert.AreEqual(8f, formation.DurationBeats);
        }

        [Test]
        public void AddFormation_SortsByStartBeat()
        {
            _system.CreateNewChart("Test", "song_001");

            _system.AddFormation(16f, 8f, "Move 2");
            _system.AddFormation(0f, 8f, "Opening");
            _system.AddFormation(8f, 8f, "Move 1");

            Assert.AreEqual("Opening", _system.ActiveChart.Formations[0].Label);
            Assert.AreEqual("Move 1", _system.ActiveChart.Formations[1].Label);
            Assert.AreEqual("Move 2", _system.ActiveChart.Formations[2].Label);
        }

        [Test]
        public void RemoveFormation_RemovesFromChart()
        {
            _system.CreateNewChart("Test", "song_001");
            var formation = _system.AddFormation(0f, 8f, "Opening");

            _system.RemoveFormation(formation.Id);

            Assert.AreEqual(0, _system.ActiveChart.Formations.Count);
        }

        [Test]
        public void SetMemberPosition_AddsNewPosition()
        {
            _system.CreateNewChart("Test", "song_001");
            var formation = _system.AddFormation(0f, 8f, "Opening");

            _system.SetMemberPosition(formation.Id, "member_1",
                new Vector2(50f, 26.67f), 0f);

            Assert.AreEqual(1, formation.Positions.Count);
            Assert.AreEqual("member_1", formation.Positions[0].MemberId);
            Assert.AreEqual(50f, formation.Positions[0].FieldX);
        }

        [Test]
        public void SetMemberPosition_UpdatesExistingPosition()
        {
            _system.CreateNewChart("Test", "song_001");
            var formation = _system.AddFormation(0f, 8f, "Opening");

            _system.SetMemberPosition(formation.Id, "member_1",
                new Vector2(50f, 26.67f), 0f);
            _system.SetMemberPosition(formation.Id, "member_1",
                new Vector2(60f, 30f), 90f);

            Assert.AreEqual(1, formation.Positions.Count);
            Assert.AreEqual(60f, formation.Positions[0].FieldX);
            Assert.AreEqual(30f, formation.Positions[0].FieldY);
            Assert.AreEqual(90f, formation.Positions[0].FacingAngle);
        }

        [Test]
        public void SetMemberPosition_ClampsToFieldBounds()
        {
            _system.CreateNewChart("Test", "song_001");
            var formation = _system.AddFormation(0f, 8f, "Opening");

            _system.SetMemberPosition(formation.Id, "member_1",
                new Vector2(-10f, 200f), 0f);

            Assert.AreEqual(0f, formation.Positions[0].FieldX);
            Assert.AreEqual(53.33f, formation.Positions[0].FieldY);
        }

        [Test]
        public void RemoveMemberFromFormation_RemovesMember()
        {
            _system.CreateNewChart("Test", "song_001");
            var formation = _system.AddFormation(0f, 8f, "Opening");
            _system.SetMemberPosition(formation.Id, "member_1",
                new Vector2(50f, 26.67f), 0f);

            _system.RemoveMemberFromFormation(formation.Id, "member_1");

            Assert.AreEqual(0, formation.Positions.Count);
        }

        [Test]
        public void GetInterpolatedPositions_ReturnsCurrentDuringHold()
        {
            _system.CreateNewChart("Test", "song_001");
            var f1 = _system.AddFormation(0f, 8f, "Opening");
            _system.AddFormation(16f, 8f, "Move 1");

            _system.SetMemberPosition(f1.Id, "member_1",
                new Vector2(50f, 26.67f), 0f);

            // Beat 4 is within the hold period of formation 1
            var positions = _system.GetInterpolatedPositions(4f);

            Assert.AreEqual(1, positions.Count);
            Assert.AreEqual(50f, positions[0].FieldX, 0.01f);
        }

        [Test]
        public void GetInterpolatedPositions_InterpolatesBetweenFormations()
        {
            _system.CreateNewChart("Test", "song_001");
            var f1 = _system.AddFormation(0f, 4f, "Opening"); // hold until beat 4
            var f2 = _system.AddFormation(8f, 4f, "Move 1");  // starts at beat 8

            _system.SetMemberPosition(f1.Id, "member_1",
                new Vector2(40f, 26.67f), 0f);
            _system.SetMemberPosition(f2.Id, "member_1",
                new Vector2(60f, 26.67f), 0f);

            // Beat 6 is halfway through the transition (beats 4-8)
            var positions = _system.GetInterpolatedPositions(6f);

            Assert.AreEqual(1, positions.Count);
            // Should be approximately 50 (midpoint between 40 and 60)
            // Note: smoothstep makes this not exactly 50
            Assert.AreEqual(50f, positions[0].FieldX, 2f);
        }

        [Test]
        public void ExportImportChart_RoundTrips()
        {
            _system.CreateNewChart("Round Trip Test", "song_001");
            var f1 = _system.AddFormation(0f, 8f, "Opening");
            _system.SetMemberPosition(f1.Id, "member_1",
                new Vector2(50f, 26.67f), 45f);

            string json = _system.ExportChartToJson();
            Assert.IsNotNull(json);

            var imported = _system.ImportChartFromJson(json);
            Assert.IsNotNull(imported);
            Assert.AreEqual("Round Trip Test", imported.Name);
            Assert.AreEqual(1, imported.Formations.Count);
            Assert.AreEqual(1, imported.Formations[0].Positions.Count);
            Assert.AreEqual(50f, imported.Formations[0].Positions[0].FieldX, 0.01f);
        }
    }
}
