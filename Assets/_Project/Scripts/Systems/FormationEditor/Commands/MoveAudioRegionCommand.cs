using System.Linq;
using MBHS.Data.Models;

namespace MBHS.Systems.FormationEditor.Commands
{
    public class MoveAudioRegionCommand : IEditorCommand
    {
        private readonly IFormationSystem _formationSystem;
        private readonly TimelineState _state;
        private readonly string _trackId;
        private readonly string _regionId;
        private readonly float _oldStartBeat;
        private readonly float _newStartBeat;

        public string Description => $"Move audio region to beat {_newStartBeat:F1}";

        public MoveAudioRegionCommand(
            IFormationSystem formationSystem,
            TimelineState state,
            string trackId,
            string regionId,
            float oldStartBeat,
            float newStartBeat)
        {
            _formationSystem = formationSystem;
            _state = state;
            _trackId = trackId;
            _regionId = regionId;
            _oldStartBeat = oldStartBeat;
            _newStartBeat = newStartBeat;
        }

        public void Execute()
        {
            SetStartBeat(_newStartBeat);
        }

        public void Undo()
        {
            SetStartBeat(_oldStartBeat);
        }

        private void SetStartBeat(float beat)
        {
            var region = FindRegion();
            if (region == null) return;
            region.StartBeat = beat;
            _state.NotifyAudioTimelineChanged();
        }

        private AudioRegionData FindRegion()
        {
            var track = _formationSystem.ActiveChart?.AudioTimeline.SfxTracks
                .FirstOrDefault(t => t.Id == _trackId);
            return track?.Regions.FirstOrDefault(r => r.Id == _regionId);
        }
    }
}
