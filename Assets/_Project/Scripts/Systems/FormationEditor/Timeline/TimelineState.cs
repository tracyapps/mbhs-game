using System;
using MBHS.Data.Models;
using UnityEngine;

namespace MBHS.Systems.FormationEditor
{
    public class TimelineState
    {
        private float _pixelsPerBeat = 20f;
        private float _scrollOffsetBeats;
        private float _playheadBeat;
        private float _totalBeats = 64f;
        private float _bpm = 120f;
        private int _beatsPerMeasure = 4;
        private bool _isPlaying;

        private string _currentSongId = "";
        private string _currentSongTitle = "";
        private AudioTimelineData _audioTimeline;

        public const float MinPixelsPerBeat = 5f;
        public const float MaxPixelsPerBeat = 100f;
        public const float ZoomStep = 1.2f;

        public float PixelsPerBeat
        {
            get => _pixelsPerBeat;
            set
            {
                float clamped = Mathf.Clamp(value, MinPixelsPerBeat, MaxPixelsPerBeat);
                if (Mathf.Approximately(clamped, _pixelsPerBeat)) return;
                _pixelsPerBeat = clamped;
                OnZoomChanged?.Invoke(_pixelsPerBeat);
            }
        }

        public float ScrollOffsetBeats
        {
            get => _scrollOffsetBeats;
            set
            {
                float clamped = Mathf.Max(0f, value);
                if (Mathf.Approximately(clamped, _scrollOffsetBeats)) return;
                _scrollOffsetBeats = clamped;
                OnScrollChanged?.Invoke(_scrollOffsetBeats);
            }
        }

        public float PlayheadBeat
        {
            get => _playheadBeat;
            set
            {
                float clamped = Mathf.Clamp(value, 0f, _totalBeats);
                _playheadBeat = clamped;
                OnPlayheadChanged?.Invoke(_playheadBeat);
            }
        }

        public float TotalBeats
        {
            get => _totalBeats;
            set => _totalBeats = Mathf.Max(1f, value);
        }

        public float BPM
        {
            get => _bpm;
            set => _bpm = Mathf.Max(1f, value);
        }

        public int BeatsPerMeasure
        {
            get => _beatsPerMeasure;
            set => _beatsPerMeasure = Mathf.Max(1, value);
        }

        public bool IsPlaying
        {
            get => _isPlaying;
            set => _isPlaying = value;
        }

        public string CurrentSongId
        {
            get => _currentSongId;
            set => _currentSongId = value ?? "";
        }

        public string CurrentSongTitle
        {
            get => _currentSongTitle;
            set => _currentSongTitle = value ?? "";
        }

        public AudioTimelineData AudioTimeline
        {
            get => _audioTimeline;
            set => _audioTimeline = value;
        }

        // Events
        public event Action<float> OnZoomChanged;
        public event Action<float> OnScrollChanged;
        public event Action<float> OnPlayheadChanged;
        public event Action OnSongDataChanged;
        public event Action OnAudioTimelineChanged;

        // Zoom methods
        public void ZoomIn() => PixelsPerBeat *= ZoomStep;
        public void ZoomOut() => PixelsPerBeat /= ZoomStep;

        public void SetZoom(float pixelsPerBeat)
        {
            PixelsPerBeat = pixelsPerBeat;
        }

        public void ZoomToFit(float viewportWidth)
        {
            if (_totalBeats > 0 && viewportWidth > 0)
                PixelsPerBeat = viewportWidth / _totalBeats;
        }

        // Coordinate conversion
        public float BeatToPixel(float beat)
        {
            return (beat - _scrollOffsetBeats) * _pixelsPerBeat;
        }

        public float PixelToBeat(float pixel)
        {
            return pixel / _pixelsPerBeat + _scrollOffsetBeats;
        }

        public float TotalWidthPixels => _totalBeats * _pixelsPerBeat;

        // Scroll helpers
        public void ScrollTo(float beat)
        {
            ScrollOffsetBeats = beat;
        }

        public void EnsureBeatVisible(float beat, float viewportWidth)
        {
            float viewportBeats = viewportWidth / _pixelsPerBeat;
            if (beat < _scrollOffsetBeats)
                ScrollOffsetBeats = beat;
            else if (beat > _scrollOffsetBeats + viewportBeats)
                ScrollOffsetBeats = beat - viewportBeats;
        }

        // Song data update
        public void SetSongData(float bpm, int beatsPerMeasure, float totalBeats)
        {
            _bpm = Mathf.Max(1f, bpm);
            _beatsPerMeasure = Mathf.Max(1, beatsPerMeasure);
            _totalBeats = Mathf.Max(1f, totalBeats);
            _scrollOffsetBeats = 0f;
            _playheadBeat = 0f;
            OnSongDataChanged?.Invoke();
        }

        public void NotifyAudioTimelineChanged()
        {
            OnAudioTimelineChanged?.Invoke();
        }

        // Beat-to-time conversion
        public float BeatsToSeconds(float beats)
        {
            return _bpm > 0 ? beats * 60f / _bpm : 0f;
        }

        public float SecondsToBeat(float seconds)
        {
            return _bpm > 0 ? seconds * _bpm / 60f : 0f;
        }
    }
}
