using System;
using MBHS.Data.Enums;
using MBHS.Data.Models;

namespace MBHS.Systems.MusicConductor
{
    public interface IMusicConductor
    {
        // State
        bool IsPlaying { get; }
        bool IsPaused { get; }
        float CurrentBeat { get; }
        int CurrentMeasure { get; }
        float CurrentBPM { get; }
        float SongProgress { get; } // 0.0 to 1.0

        // Control
        void LoadSong(SongData song);
        void UnloadSong();
        void Play();
        void Pause();
        void Resume();
        void Stop();
        void SeekToBeat(float beat);

        // Stem mixing
        void SetStemVolume(InstrumentFamily family, float volume);
        void SetStemPitch(InstrumentFamily family, float pitchMultiplier);
        void MuteStem(InstrumentFamily family);
        void UnmuteStem(InstrumentFamily family);

        // Events
        event Action<float> OnBeat;
        event Action<int> OnMeasure;
        event Action<float> OnBeatUpdate; // fires every frame with fractional beat
        event Action OnSongComplete;
        event Action OnSongLoaded;
    }
}
