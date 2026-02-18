using System;
using System.Collections.Generic;
using MBHS.Data.Models;
using MBHS.Systems.FormationEditor.Commands;
using UnityEngine;
using UnityEngine.UIElements;

namespace MBHS.Systems.FormationEditor
{
    public class TimelineTrack : VisualElement
    {
        private readonly TimelineState _state;
        private readonly IFormationSystem _formationSystem;
        private readonly CommandHistory _commandHistory;

        private readonly VisualElement _gridLayer;
        private readonly VisualElement _blockLayer;
        private readonly TimelinePlayhead _playhead;

        private readonly List<FormationBlockElement> _blocks = new();
        private readonly List<TransitionOverlay> _overlays = new();

        private bool _isPanning;
        private bool _isSeeking;
        private float _panStartX;
        private float _panStartScroll;

        public TimelinePlayhead Playhead => _playhead;

        public event Action<float> OnScrub;

        public TimelineTrack(
            TimelineState state,
            IFormationSystem formationSystem,
            CommandHistory commandHistory)
        {
            _state = state;
            _formationSystem = formationSystem;
            _commandHistory = commandHistory;

            AddToClassList("timeline-track");
            style.position = Position.Relative;

            // Grid layer (behind blocks)
            _gridLayer = new VisualElement();
            _gridLayer.style.position = Position.Absolute;
            _gridLayer.style.left = 0;
            _gridLayer.style.right = 0;
            _gridLayer.style.top = 0;
            _gridLayer.style.bottom = 0;
            _gridLayer.generateVisualContent += DrawGrid;
            _gridLayer.pickingMode = PickingMode.Ignore;
            Add(_gridLayer);

            // Block layer (contains formation blocks and overlays)
            _blockLayer = new VisualElement();
            _blockLayer.style.position = Position.Absolute;
            _blockLayer.style.left = 0;
            _blockLayer.style.right = 0;
            _blockLayer.style.top = 0;
            _blockLayer.style.bottom = 0;
            _blockLayer.pickingMode = PickingMode.Ignore;
            Add(_blockLayer);

            // Playhead (on top of everything)
            _playhead = new TimelinePlayhead(state);
            Add(_playhead);

            // Events
            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<WheelEvent>(OnWheel);

            _state.OnZoomChanged += _ => { _gridLayer.MarkDirtyRepaint(); UpdateTrackWidth(); };
            _state.OnScrollChanged += _ => _gridLayer.MarkDirtyRepaint();
            _state.OnSongDataChanged += () => { RebuildBlocks(); _gridLayer.MarkDirtyRepaint(); };

            // Formation system events
            _formationSystem.OnFormationAdded += _ => RebuildBlocks();
            _formationSystem.OnFormationRemoved += _ => RebuildBlocks();
            _formationSystem.OnFormationChanged += _ => RefreshBlocks();
            _formationSystem.OnChartChanged += _ => RebuildBlocks();
        }

        private void UpdateTrackWidth()
        {
            float totalWidth = _state.TotalWidthPixels;
            style.minWidth = totalWidth;
        }

        // =====================================================================
        // Grid Drawing
        // =====================================================================

        private void DrawGrid(MeshGenerationContext ctx)
        {
            var painter = ctx.painter2D;
            float width = resolvedStyle.width;
            float height = resolvedStyle.height;

            if (width <= 0 || height <= 0) return;

            float startBeat = _state.ScrollOffsetBeats;
            float endBeat = _state.PixelToBeat(width);
            int measureSize = _state.BeatsPerMeasure;

            // Determine grid density
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
                    ? new Color(1f, 1f, 1f, 0.15f)
                    : new Color(1f, 1f, 1f, 0.06f);
                painter.lineWidth = isMeasure ? 1f : 0.5f;

                painter.BeginPath();
                painter.MoveTo(new Vector2(x, 0));
                painter.LineTo(new Vector2(x, height));
                painter.Stroke();
            }
        }

        // =====================================================================
        // Block Management
        // =====================================================================

        public void RebuildBlocks()
        {
            // Clear existing
            foreach (var block in _blocks)
                _blockLayer.Remove(block);
            _blocks.Clear();

            foreach (var overlay in _overlays)
                _blockLayer.Remove(overlay);
            _overlays.Clear();

            var chart = _formationSystem.ActiveChart;
            if (chart == null) return;

            // Create blocks
            for (int i = 0; i < chart.Formations.Count; i++)
            {
                var formation = chart.Formations[i];
                var block = new FormationBlockElement(
                    formation, _state, _formationSystem, _commandHistory);

                block.OnBlockClicked += OnBlockClicked;

                // Highlight current formation
                if (i == _formationSystem.CurrentFormationIndex)
                    block.SetSelected(true);

                _blocks.Add(block);
                _blockLayer.Add(block);
            }

            // Create transition overlays between adjacent blocks
            for (int i = 0; i < chart.Formations.Count - 1; i++)
            {
                var overlay = new TransitionOverlay(_state);
                overlay.SetTransition(
                    chart.Formations[i],
                    chart.Formations[i + 1],
                    _state.BPM);

                _overlays.Add(overlay);
                _blockLayer.Add(overlay);
            }

            UpdateTrackWidth();
        }

        public void RefreshBlocks()
        {
            var chart = _formationSystem.ActiveChart;
            if (chart == null) return;

            // Update positions of existing blocks
            foreach (var block in _blocks)
                block.UpdateFromFormation();

            // Update overlays
            for (int i = 0; i < _overlays.Count && i < chart.Formations.Count - 1; i++)
            {
                _overlays[i].SetTransition(
                    chart.Formations[i],
                    chart.Formations[i + 1],
                    _state.BPM);
            }
        }

        public void SetSelectedFormation(int index)
        {
            for (int i = 0; i < _blocks.Count; i++)
                _blocks[i].SetSelected(i == index);
        }

        private void OnBlockClicked(Formation formation)
        {
            var chart = _formationSystem.ActiveChart;
            if (chart == null) return;

            int index = chart.Formations.IndexOf(formation);
            if (index >= 0)
            {
                _formationSystem.SetCurrentFormation(index);
                SetSelectedFormation(index);
            }
        }

        // =====================================================================
        // Pointer Events (panning + click-to-seek)
        // =====================================================================

        private void OnPointerDown(PointerDownEvent evt)
        {
            // Middle click or Shift+Left for panning
            if (evt.button == 2 || (evt.button == 0 && evt.shiftKey))
            {
                _isPanning = true;
                _panStartX = evt.position.x;
                _panStartScroll = _state.ScrollOffsetBeats;
                this.CapturePointer(evt.pointerId);
                evt.StopPropagation();
            }
            // Left click on empty space = seek (with drag-to-scrub)
            else if (evt.button == 0)
            {
                _isSeeking = true;
                this.CapturePointer(evt.pointerId);
                float beat = _state.PixelToBeat(evt.localPosition.x);
                beat = Mathf.Clamp(beat, 0f, _state.TotalBeats);
                _state.PlayheadBeat = beat;
                OnScrub?.Invoke(beat);
            }
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (_isPanning)
            {
                float deltaPixels = evt.position.x - _panStartX;
                float deltaBeats = deltaPixels / _state.PixelsPerBeat;
                _state.ScrollOffsetBeats = _panStartScroll - deltaBeats;
            }
            else if (_isSeeking)
            {
                float beat = _state.PixelToBeat(evt.localPosition.x);
                beat = Mathf.Clamp(beat, 0f, _state.TotalBeats);
                _state.PlayheadBeat = beat;
                OnScrub?.Invoke(beat);
            }
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (_isPanning)
            {
                _isPanning = false;
                this.ReleasePointer(evt.pointerId);
            }
            else if (_isSeeking)
            {
                _isSeeking = false;
                this.ReleasePointer(evt.pointerId);
                float beat = _state.PixelToBeat(evt.localPosition.x);
                beat = Mathf.Clamp(beat, 0f, _state.TotalBeats);
                OnScrub?.Invoke(beat);
            }
        }

        private void OnWheel(WheelEvent evt)
        {
            if (evt.ctrlKey)
            {
                // Zoom
                float zoomCenter = _state.PixelToBeat(evt.localMousePosition.x);
                if (evt.delta.y > 0)
                    _state.ZoomOut();
                else
                    _state.ZoomIn();

                // Keep the beat under the mouse cursor at the same pixel position
                float newPixel = _state.BeatToPixel(zoomCenter);
                float pixelDelta = evt.localMousePosition.x - newPixel;
                _state.ScrollOffsetBeats -= pixelDelta / _state.PixelsPerBeat;

                evt.StopPropagation();
            }
            else if (evt.shiftKey)
            {
                // Horizontal scroll
                float scrollDelta = evt.delta.y * 2f / _state.PixelsPerBeat;
                _state.ScrollOffsetBeats += scrollDelta;
                evt.StopPropagation();
            }
        }
    }
}
