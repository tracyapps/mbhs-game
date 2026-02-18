using System;
using System.Collections.Generic;

namespace MBHS.Data.Models
{
    public enum AudioTrackType
    {
        Music,
        Sfx
    }

    [Serializable]
    public class AudioTimelineData
    {
        public string SongId = "";
        public float SongStartBeat;
        public float SongEndBeat;
        public float SongVolume = 1f;
        public List<AudioTrackData> SfxTracks = new();
    }

    [Serializable]
    public class AudioTrackData
    {
        public string Id = "";
        public string Label = "";
        public float Volume = 1f;
        public bool IsMuted;
        public List<AudioRegionData> Regions = new();
    }

    [Serializable]
    public class AudioRegionData
    {
        public string Id = "";
        public string SfxId = "";
        public string Label = "";
        public float StartBeat;
        public float DurationBeats = 4f;
        public float Volume = 1f;
        public float FadeInBeats;
        public float FadeOutBeats;
    }
}
