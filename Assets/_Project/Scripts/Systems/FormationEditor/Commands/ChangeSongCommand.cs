using MBHS.Data.Models;
using UnityEngine;

namespace MBHS.Systems.FormationEditor.Commands
{
    public class ChangeSongCommand : IEditorCommand
    {
        private readonly IFormationSystem _formationSystem;
        private readonly TimelineState _state;
        private readonly string _oldSongId;
        private readonly string _newSongId;
        private readonly float _oldSongStartBeat;
        private readonly float _oldSongEndBeat;
        private readonly float _oldSongVolume;

        public string Description => "Change song";

        public ChangeSongCommand(
            IFormationSystem formationSystem,
            TimelineState state,
            string oldSongId,
            string newSongId)
        {
            _formationSystem = formationSystem;
            _state = state;
            _oldSongId = oldSongId;
            _newSongId = newSongId;

            var audio = _formationSystem.ActiveChart?.AudioTimeline;
            _oldSongStartBeat = audio?.SongStartBeat ?? 0f;
            _oldSongEndBeat = audio?.SongEndBeat ?? 0f;
            _oldSongVolume = audio?.SongVolume ?? 1f;
        }

        public void Execute()
        {
            ApplySong(_newSongId);
        }

        public void Undo()
        {
            ApplySong(_oldSongId);
            var audio = _formationSystem.ActiveChart?.AudioTimeline;
            if (audio != null)
            {
                audio.SongStartBeat = _oldSongStartBeat;
                audio.SongEndBeat = _oldSongEndBeat;
                audio.SongVolume = _oldSongVolume;
            }
            _state.NotifyAudioTimelineChanged();
        }

        private void ApplySong(string songId)
        {
            var chart = _formationSystem.ActiveChart;
            if (chart == null) return;

            chart.SongId = songId;
            chart.AudioTimeline.SongId = songId;
            chart.AudioTimeline.SongStartBeat = 0f;
            chart.AudioTimeline.SongEndBeat = _state.TotalBeats;
            chart.AudioTimeline.SongVolume = 1f;
            _state.CurrentSongId = songId;
            _state.NotifyAudioTimelineChanged();
        }
    }
}
