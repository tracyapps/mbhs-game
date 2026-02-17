using System;
using System.Collections.Generic;
using MBHS.Data.Enums;
using MBHS.Data.Models;
using UnityEngine;

namespace MBHS.Systems.MusicConductor
{
    public class MusicConductorBehaviour : MonoBehaviour, IMusicConductor
    {
        private SongData _songData;
        private AudioSource _mainAudioSource;
        private Dictionary<InstrumentFamily, AudioSource> _stemSources = new();

        private double _startDspTime;
        private float _lastReportedBeat;
        private int _lastReportedMeasure;
        private bool _songLoaded;

        public bool IsPlaying { get; private set; }
        public bool IsPaused { get; private set; }
        public float CurrentBeat { get; private set; }
        public int CurrentMeasure { get; private set; }
        public float CurrentBPM { get; private set; }

        public float SongProgress =>
            _songData != null && _songData.TotalBeats > 0
                ? CurrentBeat / _songData.TotalBeats
                : 0f;

        public event Action<float> OnBeat;
        public event Action<int> OnMeasure;
        public event Action<float> OnBeatUpdate;
        public event Action OnSongComplete;
        public event Action OnSongLoaded;

        private void Awake()
        {
            _mainAudioSource = gameObject.AddComponent<AudioSource>();
            _mainAudioSource.playOnAwake = false;
        }

        public void LoadSong(SongData song)
        {
            _songData = song;
            CurrentBPM = song.BPM;
            CurrentBeat = 0f;
            CurrentMeasure = 0;
            _songLoaded = true;

            // In a full implementation, this would load audio clips via Addressables
            // For now, just mark as loaded
            Debug.Log($"MusicConductor: Song loaded - {song.Title} ({song.BPM} BPM)");

            OnSongLoaded?.Invoke();
        }

        public void UnloadSong()
        {
            Stop();
            _songData = null;
            _songLoaded = false;
        }

        public void Play()
        {
            if (!_songLoaded)
            {
                Debug.LogWarning("MusicConductor: No song loaded.");
                return;
            }

            _startDspTime = AudioSettings.dspTime;
            _lastReportedBeat = -1;
            _lastReportedMeasure = -1;
            IsPlaying = true;
            IsPaused = false;

            if (_mainAudioSource.clip != null)
                _mainAudioSource.Play();

            Debug.Log("MusicConductor: Playing");
        }

        public void Pause()
        {
            if (!IsPlaying) return;

            IsPlaying = false;
            IsPaused = true;

            if (_mainAudioSource.isPlaying)
                _mainAudioSource.Pause();
        }

        public void Resume()
        {
            if (!IsPaused) return;

            // Adjust start time to account for pause duration
            float elapsedBeats = CurrentBeat;
            float elapsedSeconds = BeatsToSeconds(elapsedBeats, CurrentBPM);
            _startDspTime = AudioSettings.dspTime - elapsedSeconds;

            IsPlaying = true;
            IsPaused = false;

            if (_mainAudioSource.clip != null)
                _mainAudioSource.UnPause();
        }

        public void Stop()
        {
            IsPlaying = false;
            IsPaused = false;
            CurrentBeat = 0f;
            CurrentMeasure = 0;

            if (_mainAudioSource.isPlaying)
                _mainAudioSource.Stop();
        }

        public void SeekToBeat(float beat)
        {
            if (_songData == null) return;

            CurrentBeat = Mathf.Clamp(beat, 0f, _songData.TotalBeats);
            CurrentBPM = _songData.GetBPMAtBeat(CurrentBeat);
            CurrentMeasure = _songData.BeatsPerMeasure > 0
                ? Mathf.FloorToInt(CurrentBeat / _songData.BeatsPerMeasure)
                : 0;

            if (IsPlaying)
            {
                float elapsedSeconds = BeatsToSeconds(CurrentBeat, CurrentBPM);
                _startDspTime = AudioSettings.dspTime - elapsedSeconds;

                if (_mainAudioSource.clip != null)
                    _mainAudioSource.time = elapsedSeconds;
            }

            OnBeatUpdate?.Invoke(CurrentBeat);
        }

        public void SetStemVolume(InstrumentFamily family, float volume)
        {
            if (_stemSources.TryGetValue(family, out var source))
                source.volume = Mathf.Clamp01(volume);
        }

        public void SetStemPitch(InstrumentFamily family, float pitchMultiplier)
        {
            if (_stemSources.TryGetValue(family, out var source))
                source.pitch = pitchMultiplier;
        }

        public void MuteStem(InstrumentFamily family)
        {
            if (_stemSources.TryGetValue(family, out var source))
                source.mute = true;
        }

        public void UnmuteStem(InstrumentFamily family)
        {
            if (_stemSources.TryGetValue(family, out var source))
                source.mute = false;
        }

        private void Update()
        {
            if (!IsPlaying || _songData == null) return;

            // Calculate current beat from DSP time
            double elapsed = AudioSettings.dspTime - _startDspTime;
            CurrentBPM = _songData.GetBPMAtBeat(CurrentBeat);
            CurrentBeat = SecondsToBeats((float)elapsed, CurrentBPM);

            // Fire beat update every frame
            OnBeatUpdate?.Invoke(CurrentBeat);

            // Check for whole beat crossings
            int wholeBeat = Mathf.FloorToInt(CurrentBeat);
            if (wholeBeat > Mathf.FloorToInt(_lastReportedBeat) && wholeBeat >= 0)
            {
                _lastReportedBeat = CurrentBeat;
                OnBeat?.Invoke(CurrentBeat);

                // Check for measure crossings
                if (_songData.BeatsPerMeasure > 0)
                {
                    int measure = wholeBeat / _songData.BeatsPerMeasure;
                    if (measure > _lastReportedMeasure)
                    {
                        _lastReportedMeasure = measure;
                        CurrentMeasure = measure;
                        OnMeasure?.Invoke(measure);
                    }
                }
            }

            // Check for song end
            if (CurrentBeat >= _songData.TotalBeats)
            {
                IsPlaying = false;
                OnSongComplete?.Invoke();
            }
        }

        private static float BeatsToSeconds(float beats, float bpm)
        {
            return bpm > 0 ? beats * 60f / bpm : 0f;
        }

        private static float SecondsToBeats(float seconds, float bpm)
        {
            return bpm > 0 ? seconds * bpm / 60f : 0f;
        }
    }
}
