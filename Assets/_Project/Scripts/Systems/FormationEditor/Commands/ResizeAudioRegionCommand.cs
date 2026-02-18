using System.Linq;
using MBHS.Data.Models;

namespace MBHS.Systems.FormationEditor.Commands
{
    public class ResizeAudioRegionCommand : IEditorCommand
    {
        private readonly IFormationSystem _formationSystem;
        private readonly TimelineState _state;
        private readonly string _trackId;
        private readonly string _regionId;
        private readonly float _oldStartBeat;
        private readonly float _oldDuration;
        private readonly float _newStartBeat;
        private readonly float _newDuration;

        public string Description => $"Resize audio region to {_newDuration:F1} beats";

        public ResizeAudioRegionCommand(
            IFormationSystem formationSystem,
            TimelineState state,
            string trackId,
            string regionId,
            float oldStartBeat,
            float oldDuration,
            float newStartBeat,
            float newDuration)
        {
            _formationSystem = formationSystem;
            _state = state;
            _trackId = trackId;
            _regionId = regionId;
            _oldStartBeat = oldStartBeat;
            _oldDuration = oldDuration;
            _newStartBeat = newStartBeat;
            _newDuration = newDuration;
        }

        public void Execute()
        {
            Apply(_newStartBeat, _newDuration);
        }

        public void Undo()
        {
            Apply(_oldStartBeat, _oldDuration);
        }

        private void Apply(float startBeat, float duration)
        {
            var region = FindRegion();
            if (region == null) return;
            region.StartBeat = startBeat;
            region.DurationBeats = duration;
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
