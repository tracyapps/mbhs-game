using System.Linq;
using MBHS.Data.Models;

namespace MBHS.Systems.FormationEditor.Commands
{
    public class RemoveAudioRegionCommand : IEditorCommand
    {
        private readonly IFormationSystem _formationSystem;
        private readonly TimelineState _state;
        private readonly string _trackId;
        private readonly AudioRegionData _region;
        private int _removedIndex;

        public string Description => $"Remove audio region '{_region.Label}'";

        public RemoveAudioRegionCommand(
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
            _removedIndex = track.Regions.IndexOf(_region);
            track.Regions.Remove(_region);
            _state.NotifyAudioTimelineChanged();
        }

        public void Undo()
        {
            var track = FindTrack();
            if (track == null) return;
            if (_removedIndex >= 0 && _removedIndex <= track.Regions.Count)
                track.Regions.Insert(_removedIndex, _region);
            else
                track.Regions.Add(_region);
            _state.NotifyAudioTimelineChanged();
        }

        private AudioTrackData FindTrack()
        {
            return _formationSystem.ActiveChart?.AudioTimeline.SfxTracks
                .FirstOrDefault(t => t.Id == _trackId);
        }
    }
}
