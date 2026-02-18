using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace MBHS.Systems.FormationEditor
{
    public class TimelinePlayhead : VisualElement
    {
        private readonly TimelineState _state;
        private readonly VisualElement _handle;
        private readonly VisualElement _hitZone;

        private bool _isDragging;
        private float _pointerStartX;
        private float _dragStartBeat;

        public event Action<float> OnScrub;

        public TimelinePlayhead(TimelineState state)
        {
            _state = state;
            AddToClassList("timeline-playhead");

            // Wide invisible hit zone for easier grabbing (20px wide)
            _hitZone = new VisualElement();
            _hitZone.AddToClassList("timeline-playhead-hitzone");
            Add(_hitZone);

            // Triangle handle at top
            _handle = new VisualElement();
            _handle.AddToClassList("timeline-playhead-handle");
            Add(_handle);

            _hitZone.RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);

            _state.OnPlayheadChanged += OnPlayheadChanged;
            _state.OnZoomChanged += _ => UpdatePosition();
            _state.OnScrollChanged += _ => UpdatePosition();

            pickingMode = PickingMode.Position;
            UpdatePosition();
        }

        private void OnPlayheadChanged(float beat)
        {
            if (!_isDragging)
                UpdatePosition();
        }

        public void UpdatePosition()
        {
            float x = _state.BeatToPixel(_state.PlayheadBeat);
            style.left = x - 1; // center the 2px line
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0) return;

            evt.StopPropagation();
            _isDragging = true;
            _pointerStartX = evt.position.x;
            _dragStartBeat = _state.PlayheadBeat;
            this.CapturePointer(evt.pointerId);
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_isDragging) return;

            float deltaPixels = evt.position.x - _pointerStartX;
            float deltaBeats = deltaPixels / _state.PixelsPerBeat;
            float newBeat = Mathf.Clamp(_dragStartBeat + deltaBeats, 0f, _state.TotalBeats);

            _state.PlayheadBeat = newBeat;
            UpdatePosition();
            OnScrub?.Invoke(newBeat);
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!_isDragging) return;

            this.ReleasePointer(evt.pointerId);
            _isDragging = false;

            // Final scrub position
            float deltaPixels = evt.position.x - _pointerStartX;
            float deltaBeats = deltaPixels / _state.PixelsPerBeat;
            float newBeat = Mathf.Clamp(_dragStartBeat + deltaBeats, 0f, _state.TotalBeats);
            OnScrub?.Invoke(newBeat);
        }
    }
}
