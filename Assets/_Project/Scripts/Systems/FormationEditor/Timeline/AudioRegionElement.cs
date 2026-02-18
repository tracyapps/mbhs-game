using System;
using MBHS.Data.Models;
using MBHS.Systems.FormationEditor.Commands;
using UnityEngine;
using UnityEngine.UIElements;

namespace MBHS.Systems.FormationEditor
{
    public class AudioRegionElement : VisualElement
    {
        private readonly AudioRegionData _region;
        private readonly AudioTrackData _track;
        private readonly TimelineState _state;
        private readonly IFormationSystem _formationSystem;
        private readonly CommandHistory _commandHistory;
        private readonly AudioTrackType _trackType;

        private readonly Label _label;
        private readonly VisualElement _resizeLeft;
        private readonly VisualElement _resizeRight;
        private readonly VisualElement _fadeInIndicator;
        private readonly VisualElement _fadeOutIndicator;

        private bool _isDragging;
        private bool _isResizingLeft;
        private bool _isResizingRight;
        private float _dragStartBeat;
        private float _dragStartDuration;
        private float _pointerStartX;
        private bool _isSelected;

        private const float MinDurationBeats = 0.5f;
        private const float ResizeHandleWidth = 6f;

        public event Action<AudioRegionElement> OnRegionClicked;
        public event Action<AudioRegionElement> OnRegionDeleteRequested;

        public AudioRegionData Region => _region;

        public AudioRegionElement(
            AudioRegionData region,
            AudioTrackData track,
            TimelineState state,
            IFormationSystem formationSystem,
            CommandHistory commandHistory,
            AudioTrackType trackType)
        {
            _region = region;
            _track = track;
            _state = state;
            _formationSystem = formationSystem;
            _commandHistory = commandHistory;
            _trackType = trackType;

            AddToClassList("audio-region");
            AddToClassList(trackType == AudioTrackType.Music ? "music" : "sfx");

            // Label
            _label = new Label(region.Label ?? "Clip");
            _label.AddToClassList("audio-region-label");
            _label.pickingMode = PickingMode.Ignore;
            Add(_label);

            // Fade indicators
            _fadeInIndicator = new VisualElement();
            _fadeInIndicator.AddToClassList("audio-region-fade-handle");
            _fadeInIndicator.AddToClassList("fade-in");
            _fadeInIndicator.pickingMode = PickingMode.Ignore;
            Add(_fadeInIndicator);

            _fadeOutIndicator = new VisualElement();
            _fadeOutIndicator.AddToClassList("audio-region-fade-handle");
            _fadeOutIndicator.AddToClassList("fade-out");
            _fadeOutIndicator.pickingMode = PickingMode.Ignore;
            Add(_fadeOutIndicator);

            // Resize handles
            _resizeLeft = new VisualElement();
            _resizeLeft.AddToClassList("audio-region-resize-handle");
            _resizeLeft.AddToClassList("audio-region-resize-left");
            Add(_resizeLeft);

            _resizeRight = new VisualElement();
            _resizeRight.AddToClassList("audio-region-resize-handle");
            _resizeRight.AddToClassList("audio-region-resize-right");
            Add(_resizeRight);

            // Events
            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<KeyDownEvent>(OnKeyDown);

            _state.OnZoomChanged += _ => UpdatePosition();
            _state.OnScrollChanged += _ => UpdatePosition();

            UpdatePosition();
            UpdateFadeIndicators();
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            if (selected)
                AddToClassList("selected");
            else
                RemoveFromClassList("selected");
        }

        public void UpdateFromRegion()
        {
            _label.text = _region.Label ?? "Clip";
            UpdatePosition();
            UpdateFadeIndicators();
        }

        public void UpdatePosition()
        {
            float x = _state.BeatToPixel(_region.StartBeat);
            float w = _region.DurationBeats * _state.PixelsPerBeat;

            style.left = x;
            style.width = Mathf.Max(w, _state.PixelsPerBeat * MinDurationBeats);
        }

        private void UpdateFadeIndicators()
        {
            float fadeInWidth = _region.FadeInBeats * _state.PixelsPerBeat;
            float fadeOutWidth = _region.FadeOutBeats * _state.PixelsPerBeat;

            _fadeInIndicator.style.width = Mathf.Max(0f, fadeInWidth);
            _fadeInIndicator.style.display =
                _region.FadeInBeats > 0 ? DisplayStyle.Flex : DisplayStyle.None;

            _fadeOutIndicator.style.width = Mathf.Max(0f, fadeOutWidth);
            _fadeOutIndicator.style.display =
                _region.FadeOutBeats > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button == 1) // Right-click
            {
                evt.StopPropagation();
                OnRegionDeleteRequested?.Invoke(this);
                return;
            }

            if (evt.button != 0) return;

            evt.StopPropagation();
            OnRegionClicked?.Invoke(this);

            float localX = evt.localPosition.x;
            _pointerStartX = evt.position.x;

            if (localX <= ResizeHandleWidth)
            {
                _isResizingLeft = true;
                _dragStartBeat = _region.StartBeat;
                _dragStartDuration = _region.DurationBeats;
            }
            else if (localX >= resolvedStyle.width - ResizeHandleWidth)
            {
                _isResizingRight = true;
                _dragStartBeat = _region.StartBeat;
                _dragStartDuration = _region.DurationBeats;
            }
            else
            {
                _isDragging = true;
                _dragStartBeat = _region.StartBeat;
            }

            this.CapturePointer(evt.pointerId);
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_isDragging && !_isResizingLeft && !_isResizingRight) return;

            float deltaPixels = evt.position.x - _pointerStartX;
            float deltaBeats = deltaPixels / _state.PixelsPerBeat;

            if (_isDragging)
            {
                float newStart = Mathf.Max(0f, _dragStartBeat + deltaBeats);
                newStart = Mathf.Round(newStart * 2f) / 2f;
                style.left = _state.BeatToPixel(newStart);
            }
            else if (_isResizingLeft)
            {
                float newStart = _dragStartBeat + deltaBeats;
                float maxStart = _dragStartBeat + _dragStartDuration - MinDurationBeats;
                newStart = Mathf.Clamp(newStart, 0f, maxStart);
                newStart = Mathf.Round(newStart * 2f) / 2f;

                float newDuration = _dragStartDuration - (newStart - _dragStartBeat);
                style.left = _state.BeatToPixel(newStart);
                style.width = newDuration * _state.PixelsPerBeat;
            }
            else if (_isResizingRight)
            {
                float newDuration = _dragStartDuration + deltaBeats;
                newDuration = Mathf.Max(MinDurationBeats, newDuration);
                newDuration = Mathf.Round(newDuration * 2f) / 2f;
                style.width = newDuration * _state.PixelsPerBeat;
            }
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            this.ReleasePointer(evt.pointerId);

            float deltaPixels = evt.position.x - _pointerStartX;
            float deltaBeats = deltaPixels / _state.PixelsPerBeat;

            if (_isDragging)
            {
                float newStart = Mathf.Max(0f, _dragStartBeat + deltaBeats);
                newStart = Mathf.Round(newStart * 2f) / 2f;

                if (!Mathf.Approximately(newStart, _dragStartBeat))
                {
                    var cmd = new MoveAudioRegionCommand(
                        _formationSystem, _state, _track.Id, _region.Id,
                        _dragStartBeat, newStart);
                    _commandHistory.Execute(cmd);
                }
                else
                {
                    UpdatePosition();
                }
            }
            else if (_isResizingLeft)
            {
                float newStart = _dragStartBeat + deltaBeats;
                float maxStart = _dragStartBeat + _dragStartDuration - MinDurationBeats;
                newStart = Mathf.Clamp(newStart, 0f, maxStart);
                newStart = Mathf.Round(newStart * 2f) / 2f;
                float newDuration = _dragStartDuration - (newStart - _dragStartBeat);

                if (!Mathf.Approximately(newStart, _dragStartBeat) ||
                    !Mathf.Approximately(newDuration, _dragStartDuration))
                {
                    var cmd = new ResizeAudioRegionCommand(
                        _formationSystem, _state, _track.Id, _region.Id,
                        _dragStartBeat, _dragStartDuration,
                        newStart, newDuration);
                    _commandHistory.Execute(cmd);
                }
                else
                {
                    UpdatePosition();
                }
            }
            else if (_isResizingRight)
            {
                float newDuration = _dragStartDuration + deltaBeats;
                newDuration = Mathf.Max(MinDurationBeats, newDuration);
                newDuration = Mathf.Round(newDuration * 2f) / 2f;

                if (!Mathf.Approximately(newDuration, _dragStartDuration))
                {
                    var cmd = new ResizeAudioRegionCommand(
                        _formationSystem, _state, _track.Id, _region.Id,
                        _dragStartBeat, _dragStartDuration,
                        _dragStartBeat, newDuration);
                    _commandHistory.Execute(cmd);
                }
                else
                {
                    UpdatePosition();
                }
            }

            _isDragging = false;
            _isResizingLeft = false;
            _isResizingRight = false;
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (_isSelected && evt.keyCode == KeyCode.Delete)
            {
                OnRegionDeleteRequested?.Invoke(this);
                evt.StopPropagation();
            }
        }
    }
}
