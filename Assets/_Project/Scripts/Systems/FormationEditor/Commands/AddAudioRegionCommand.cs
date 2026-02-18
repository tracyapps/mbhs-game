using System.Linq;
using MBHS.Data.Models;

namespace MBHS.Systems.FormationEditor.Commands
{
    public class AddAudioRegionCommand : IEditorCommand
    {
        private readonly IFormationSystem _formationSystem;
        private readonly TimelineState _state;
        private readonly string _trackId;
        private readonly AudioRegionData _region;

        public string Description => $"Add audio region '{_region.Label}'";

        public AddAudioRegionCommand(
            IFormationSystem formationSystem,
            TimelineState state,
            string trackId,
            AudioRegionData region)
        {
            _formationSystem = formationSystem;
            _state = state;
            _trackId = trackId;
            _region = region;
        }

        public void Execute()
        {
            var track = FindTrack();
            if (track == null) return;
            track.Regions.Add(_region);
            _state.NotifyAudioTimelineChanged();
        }

        public void Undo()
        {
            var track = FindTrack();
            if (track == null) return;
            track.Regions.Remove(_region);
            _state.NotifyAudioTimelineChanged();
        }

        private AudioTrackData FindTrack()
        {
            return _formationSystem.ActiveChart?.AudioTimeline.SfxTracks
                .FirstOrDefault(t => t.Id == _trackId);
        }
    }
}
