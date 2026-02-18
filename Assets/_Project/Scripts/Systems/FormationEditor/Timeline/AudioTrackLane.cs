using System;
using System.Collections.Generic;
using MBHS.Data.Models;
using MBHS.Systems.FormationEditor.Commands;
using UnityEngine;
using UnityEngine.UIElements;

namespace MBHS.Systems.FormationEditor
{
    public class AudioTrackLane : VisualElement
    {
        private readonly TimelineState _state;
        private readonly AudioTrackData _trackData;
        private readonly AudioTrackType _trackType;
        private readonly IFormationSystem _formationSystem;
        private readonly CommandHistory _commandHistory;

        private readonly VisualElement _contentArea;
        private readonly List<AudioRegionElement> _regions = new();

        private AudioRegionElement _selectedRegion;

        public event Action<AudioTrackLane> OnAddRegionRequested;
        public event Action<AudioTrackLane, AudioRegionData> OnRegionDeleted;

        public AudioTrackData TrackData => _trackData;
        public AudioTrackType TrackType => _trackType;

        public AudioTrackLane(
            TimelineState state,
            AudioTrackData trackData,
            AudioTrackType trackType,
            IFormationSystem formationSystem,
            CommandHistory commandHistory)
        {
            _state = state;
            _trackData = trackData;
            _trackType = trackType;
            _formationSystem = formationSystem;
            _commandHistory = commandHistory;

            AddToClassList("audio-track-lane");
            if (trackType == AudioTrackType.Music)
                AddToClassList("music-lane");
            else
                AddToClassList("sfx-lane");

            // Content area for regions (fills the lane)
            _contentArea = new VisualElement();
            _contentArea.AddToClassList("audio-track-content");
            _contentArea.style.position = Position.Relative;
            _contentArea.style.flexGrow = 1;
            _contentArea.generateVisualContent += DrawGrid;
            Add(_contentArea);

            // Listen for zoom/scroll to redraw grid
            _state.OnZoomChanged += _ => { _contentArea.MarkDirtyRepaint(); UpdateTrackWidth(); };
            _state.OnScrollChanged += _ => _contentArea.MarkDirtyRepaint();
            _state.OnAudioTimelineChanged += RebuildRegions;

            // Click on empty space in SFX lane = trigger add region
            if (trackType == AudioTrackType.Sfx)
            {
                _contentArea.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button == 0 && evt.target == _contentArea)
                    {
                        OnAddRegionRequested?.Invoke(this);
                        evt.StopPropagation();
                    }
                });
            }

            UpdateTrackWidth();
            RebuildRegions();
        }

        private void UpdateTrackWidth()
        {
            float totalWidth = _state.TotalWidthPixels;
            _contentArea.style.minWidth = totalWidth;
        }

        private void DrawGrid(MeshGenerationContext ctx)
        {
            var painter = ctx.painter2D;
            float width = _contentArea.resolvedStyle.width;
            float height = _contentArea.resolvedStyle.height;

            if (width <= 0 || height <= 0) return;

            float startBeat = _state.ScrollOffsetBeats;
            float endBeat = _state.PixelToBeat(width);
            int measureSize = _state.BeatsPerMeasure;

            int beatStep = 1;
            if (_state.PixelsPerBeat < 8f) beatStep = 4;
            else if (_state.PixelsPerBeat < 15f) beatStep = 2;

            int firstBeat = Mathf.FloorToInt(startBeat / beatStep) * beatStep;
            if (firstBeat < 0) firstBeat = 0;

            for (int beat = firstBeat; beat <= endBeat; beat += beatStep)
            {
                float x = _state.BeatToPixel(beat);
                if (x < -1 || x > width + 1) continue;

                bool isMeasure = measureSize > 0 && beat % measureSize == 0;

                painter.strokeColor = isMeasure
                    ? new Color(1f, 1f, 1f, 0.12f)
                    : new Color(1f, 1f, 1f, 0.04f);
                painter.lineWidth = isMeasure ? 1f : 0.5f;

                painter.BeginPath();
                painter.MoveTo(new Vector2(x, 0));
                painter.LineTo(new Vector2(x, height));
                painter.Stroke();
            }
        }

        public void RebuildRegions()
        {
            foreach (var region in _regions)
                _contentArea.Remove(region);
            _regions.Clear();
            _selectedRegion = null;

            foreach (var regionData in _trackData.Regions)
            {
                var element = new AudioRegionElement(
                    regionData, _trackData, _state,
                    _formationSystem, _commandHistory, _trackType);

                element.OnRegionClicked += OnRegionClicked;
                element.OnRegionDeleteRequested += OnRegionDeleteRequest;

                _regions.Add(element);
                _contentArea.Add(element);
            }

            UpdateTrackWidth();
        }

        public void RefreshRegions()
        {
            foreach (var region in _regions)
                region.UpdateFromRegion();
        }

        private void OnRegionClicked(AudioRegionElement element)
        {
            if (_selectedRegion != null)
                _selectedRegion.SetSelected(false);

            _selectedRegion = element;
            element.SetSelected(true);
        }

        private void OnRegionDeleteRequest(AudioRegionElement element)
        {
            var cmd = new RemoveAudioRegionCommand(
                _formationSystem, _state, _trackData.Id, element.Region);
            _commandHistory.Execute(cmd);
            OnRegionDeleted?.Invoke(this, element.Region);
        }

        public void SetMusicRegion(float startBeat, float endBeat, string songTitle)
        {
            // For music lane: ensure there's exactly one region representing the song
            _trackData.Regions.Clear();
            var region = new AudioRegionData
            {
                Id = "music_main",
                SfxId = "",
                Label = songTitle,
                StartBeat = startBeat,
                DurationBeats = endBeat - startBeat,
                Volume = 1f
            };
            _trackData.Regions.Add(region);
            RebuildRegions();
        }
    }
}
