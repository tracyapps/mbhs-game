using System;
using System.Collections.Generic;
using MBHS.Data.Models;
using UnityEngine;

namespace MBHS.Systems.FormationEditor
{
    public interface IFormationSystem
    {
        DrillChart ActiveChart { get; }
        Formation CurrentFormation { get; }
        int CurrentFormationIndex { get; }

        // Chart management
        void CreateNewChart(string name, string songId);
        void LoadChart(DrillChart chart);
        void CloseChart();

        // Formation CRUD
        Formation AddFormation(float startBeat, float durationBeats, string label);
        void RemoveFormation(string formationId);
        void UpdateFormation(string formationId, float? startBeat = null,
                            float? durationBeats = null, string label = null);
        void ReorderFormation(string formationId, int newIndex);
        void SetCurrentFormation(int index);

        // Member positioning
        void SetMemberPosition(string formationId, string memberId,
                              Vector2 fieldPosition, float facingAngle);
        void SetMemberPositionsBatch(string formationId,
                                     List<MemberPosition> positions);
        void RemoveMemberFromFormation(string formationId, string memberId);

        // Template operations
        Formation ApplyTemplate(string formationId, FormationTemplate template,
                               Dictionary<int, string> slotToMemberMapping);

        // Import/Export
        string ExportChartToJson();
        DrillChart ImportChartFromJson(string json);

        // Interpolation (for preview and simulation)
        List<MemberPosition> GetInterpolatedPositions(float beat);

        // Events
        event Action<Formation> OnFormationAdded;
        event Action<Formation> OnFormationChanged;
        event Action<string> OnFormationRemoved;
        event Action<DrillChart> OnChartChanged;
        event Action<int> OnCurrentFormationChanged;
    }
}
