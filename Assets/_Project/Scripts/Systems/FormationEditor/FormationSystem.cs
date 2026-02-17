using System;
using System.Collections.Generic;
using System.Linq;
using MBHS.Data.Enums;
using MBHS.Data.Models;
using UnityEngine;

namespace MBHS.Systems.FormationEditor
{
    public class FormationSystem : IFormationSystem
    {
        private DrillChart _activeChart;
        private int _currentFormationIndex;

        public DrillChart ActiveChart => _activeChart;
        public Formation CurrentFormation =>
            _activeChart?.Formations != null && _currentFormationIndex >= 0 &&
            _currentFormationIndex < _activeChart.Formations.Count
                ? _activeChart.Formations[_currentFormationIndex]
                : null;
        public int CurrentFormationIndex => _currentFormationIndex;

        public event Action<Formation> OnFormationAdded;
        public event Action<Formation> OnFormationChanged;
        public event Action<string> OnFormationRemoved;
        public event Action<DrillChart> OnChartChanged;
        public event Action<int> OnCurrentFormationChanged;

        // Chart Management
        public void CreateNewChart(string name, string songId)
        {
            _activeChart = new DrillChart
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                SongId = songId,
                Formations = new List<Formation>(),
                CreatedDate = DateTime.UtcNow.ToString("o"),
                LastModifiedDate = DateTime.UtcNow.ToString("o")
            };
            _currentFormationIndex = -1;

            OnChartChanged?.Invoke(_activeChart);
        }

        public void LoadChart(DrillChart chart)
        {
            _activeChart = chart;
            _currentFormationIndex = chart.Formations.Count > 0 ? 0 : -1;

            OnChartChanged?.Invoke(_activeChart);
            if (_currentFormationIndex >= 0)
                OnCurrentFormationChanged?.Invoke(_currentFormationIndex);
        }

        public void CloseChart()
        {
            _activeChart = null;
            _currentFormationIndex = -1;
            OnChartChanged?.Invoke(null);
        }

        // Formation CRUD
        public Formation AddFormation(float startBeat, float durationBeats, string label)
        {
            if (_activeChart == null)
            {
                Debug.LogError("FormationSystem: No active chart.");
                return null;
            }

            var formation = new Formation
            {
                Id = Guid.NewGuid().ToString(),
                Label = label,
                StartBeat = startBeat,
                DurationBeats = durationBeats,
                TransitionIn = TransitionType.LinearMarch,
                Positions = new List<MemberPosition>()
            };

            // Insert in sorted order by start beat
            int insertIndex = _activeChart.Formations
                .FindIndex(f => f.StartBeat > startBeat);
            if (insertIndex < 0)
                _activeChart.Formations.Add(formation);
            else
                _activeChart.Formations.Insert(insertIndex, formation);

            MarkModified();
            OnFormationAdded?.Invoke(formation);
            OnChartChanged?.Invoke(_activeChart);

            return formation;
        }

        public void RemoveFormation(string formationId)
        {
            if (_activeChart == null) return;

            var index = _activeChart.Formations.FindIndex(f => f.Id == formationId);
            if (index < 0) return;

            _activeChart.Formations.RemoveAt(index);

            if (_currentFormationIndex >= _activeChart.Formations.Count)
                _currentFormationIndex = _activeChart.Formations.Count - 1;

            MarkModified();
            OnFormationRemoved?.Invoke(formationId);
            OnChartChanged?.Invoke(_activeChart);
        }

        public void UpdateFormation(string formationId, float? startBeat = null,
                                   float? durationBeats = null, string label = null)
        {
            var formation = FindFormation(formationId);
            if (formation == null) return;

            if (startBeat.HasValue) formation.StartBeat = startBeat.Value;
            if (durationBeats.HasValue) formation.DurationBeats = durationBeats.Value;
            if (label != null) formation.Label = label;

            // Re-sort if start beat changed
            if (startBeat.HasValue)
            {
                _activeChart.Formations.Sort((a, b) => a.StartBeat.CompareTo(b.StartBeat));
            }

            MarkModified();
            OnFormationChanged?.Invoke(formation);
        }

        public void ReorderFormation(string formationId, int newIndex)
        {
            if (_activeChart == null) return;

            var formation = FindFormation(formationId);
            if (formation == null) return;

            _activeChart.Formations.Remove(formation);
            newIndex = Mathf.Clamp(newIndex, 0, _activeChart.Formations.Count);
            _activeChart.Formations.Insert(newIndex, formation);

            MarkModified();
            OnChartChanged?.Invoke(_activeChart);
        }

        public void SetCurrentFormation(int index)
        {
            if (_activeChart == null) return;
            if (index < 0 || index >= _activeChart.Formations.Count) return;

            _currentFormationIndex = index;
            OnCurrentFormationChanged?.Invoke(_currentFormationIndex);
        }

        // Member Positioning
        public void SetMemberPosition(string formationId, string memberId,
                                     Vector2 fieldPosition, float facingAngle)
        {
            var formation = FindFormation(formationId);
            if (formation == null) return;

            // Clamp to field bounds
            fieldPosition.x = Mathf.Clamp(fieldPosition.x, 0f, 100f);
            fieldPosition.y = Mathf.Clamp(fieldPosition.y, 0f, 53.33f);

            var existing = formation.Positions.FirstOrDefault(p => p.MemberId == memberId);
            if (existing != null)
            {
                existing.FieldPosition = fieldPosition;
                existing.FacingAngle = facingAngle;
            }
            else
            {
                formation.Positions.Add(new MemberPosition
                {
                    MemberId = memberId,
                    FieldX = fieldPosition.x,
                    FieldY = fieldPosition.y,
                    FacingAngle = facingAngle
                });
            }

            MarkModified();
            OnFormationChanged?.Invoke(formation);
        }

        public void SetMemberPositionsBatch(string formationId,
                                            List<MemberPosition> positions)
        {
            var formation = FindFormation(formationId);
            if (formation == null) return;

            foreach (var pos in positions)
            {
                var existing = formation.Positions
                    .FirstOrDefault(p => p.MemberId == pos.MemberId);
                if (existing != null)
                {
                    existing.FieldPosition = pos.FieldPosition;
                    existing.FacingAngle = pos.FacingAngle;
                }
                else
                {
                    formation.Positions.Add(pos);
                }
            }

            MarkModified();
            OnFormationChanged?.Invoke(formation);
        }

        public void RemoveMemberFromFormation(string formationId, string memberId)
        {
            var formation = FindFormation(formationId);
            if (formation == null) return;

            formation.Positions.RemoveAll(p => p.MemberId == memberId);

            MarkModified();
            OnFormationChanged?.Invoke(formation);
        }

        // Template Operations
        public Formation ApplyTemplate(string formationId, FormationTemplate template,
                                      Dictionary<int, string> slotToMemberMapping)
        {
            var formation = FindFormation(formationId);
            if (formation == null) return null;

            formation.Positions.Clear();

            foreach (var slot in template.Slots)
            {
                if (slotToMemberMapping.TryGetValue(slot.SlotIndex, out var memberId))
                {
                    formation.Positions.Add(new MemberPosition
                    {
                        MemberId = memberId,
                        FieldX = slot.FieldX,
                        FieldY = slot.FieldY,
                        FacingAngle = slot.FacingAngle
                    });
                }
            }

            MarkModified();
            OnFormationChanged?.Invoke(formation);
            return formation;
        }

        // Import/Export
        public string ExportChartToJson()
        {
            if (_activeChart == null) return null;
            return JsonUtility.ToJson(_activeChart, true);
        }

        public DrillChart ImportChartFromJson(string json)
        {
            return JsonUtility.FromJson<DrillChart>(json);
        }

        // Interpolation
        public List<MemberPosition> GetInterpolatedPositions(float beat)
        {
            if (_activeChart == null || _activeChart.Formations.Count == 0)
                return new List<MemberPosition>();

            int currentIndex = _activeChart.GetFormationIndexAtBeat(beat);
            var currentFormation = _activeChart.Formations[currentIndex];

            // If this is the last formation or we're within it, return as-is
            if (currentIndex >= _activeChart.Formations.Count - 1)
                return new List<MemberPosition>(currentFormation.Positions);

            var nextFormation = _activeChart.Formations[currentIndex + 1];
            float transitionStart = currentFormation.StartBeat + currentFormation.DurationBeats;
            float transitionEnd = nextFormation.StartBeat;

            // If we're still in the hold period, return current positions
            if (beat < transitionStart)
                return new List<MemberPosition>(currentFormation.Positions);

            // If we're past the next formation start, return next positions
            if (beat >= transitionEnd || transitionEnd <= transitionStart)
                return new List<MemberPosition>(nextFormation.Positions);

            // Interpolate between formations
            float t = (beat - transitionStart) / (transitionEnd - transitionStart);
            t = Mathf.Clamp01(t);

            // Smooth step for more natural movement
            t = t * t * (3f - 2f * t);

            return InterpolateFormations(currentFormation, nextFormation, t);
        }

        private List<MemberPosition> InterpolateFormations(Formation from, Formation to, float t)
        {
            var result = new List<MemberPosition>();

            // Get all unique member IDs from both formations
            var allMemberIds = from.Positions.Select(p => p.MemberId)
                .Union(to.Positions.Select(p => p.MemberId))
                .Distinct();

            foreach (var memberId in allMemberIds)
            {
                var fromPos = from.GetPositionForMember(memberId);
                var toPos = to.GetPositionForMember(memberId);

                if (fromPos != null && toPos != null)
                {
                    result.Add(new MemberPosition
                    {
                        MemberId = memberId,
                        FieldX = Mathf.Lerp(fromPos.FieldX, toPos.FieldX, t),
                        FieldY = Mathf.Lerp(fromPos.FieldY, toPos.FieldY, t),
                        FacingAngle = Mathf.LerpAngle(fromPos.FacingAngle, toPos.FacingAngle, t)
                    });
                }
                else if (fromPos != null)
                {
                    result.Add(fromPos);
                }
                else if (toPos != null)
                {
                    result.Add(toPos);
                }
            }

            return result;
        }

        // Helpers
        private Formation FindFormation(string formationId)
        {
            if (_activeChart == null)
            {
                Debug.LogError("FormationSystem: No active chart.");
                return null;
            }

            var formation = _activeChart.Formations.FirstOrDefault(f => f.Id == formationId);
            if (formation == null)
                Debug.LogWarning($"FormationSystem: Formation not found: {formationId}");

            return formation;
        }

        private void MarkModified()
        {
            if (_activeChart != null)
                _activeChart.LastModifiedDate = DateTime.UtcNow.ToString("o");
        }
    }
}
