using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace MBHS.Systems.FormationEditor
{
    public class TimelineRuler : VisualElement
    {
        private readonly TimelineState _state;

        private bool _isDragging;
        private int _capturedPointerId = -1;

        public event Action<float> OnScrub;

        public TimelineRuler(TimelineState state)
        {
            _state = state;
            AddToClassList("timeline-ruler");

            generateVisualContent += OnGenerateVisualContent;

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);

            _state.OnZoomChanged += _ => MarkDirtyRepaint();
            _state.OnScrollChanged += _ => MarkDirtyRepaint();
            _state.OnSongDataChanged += () => MarkDirtyRepaint();
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0) return;

            evt.StopPropagation();
            _isDragging = true;
            _capturedPointerId = evt.pointerId;
            this.CapturePointer(evt.pointerId);

            float beat = _state.PixelToBeat(evt.localPosition.x);
            beat = Mathf.Clamp(beat, 0f, _state.TotalBeats);
            _state.PlayheadBeat = beat;
            OnScrub?.Invoke(beat);
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!_isDragging) return;

            float beat = _state.PixelToBeat(evt.localPosition.x);
            beat = Mathf.Clamp(beat, 0f, _state.TotalBeats);
            _state.PlayheadBeat = beat;
            OnScrub?.Invoke(beat);
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!_isDragging) return;

            _isDragging = false;
            this.ReleasePointer(_capturedPointerId);
            _capturedPointerId = -1;

            float beat = _state.PixelToBeat(evt.localPosition.x);
            beat = Mathf.Clamp(beat, 0f, _state.TotalBeats);
            OnScrub?.Invoke(beat);
        }

        private void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            var painter = ctx.painter2D;
            float width = resolvedStyle.width;
            float height = resolvedStyle.height;

            if (width <= 0 || height <= 0) return;

            // Determine tick interval based on zoom level
            int tickInterval = GetTickInterval();
            int measureInterval = _state.BeatsPerMeasure;

            float startBeat = _state.ScrollOffsetBeats;
            float endBeat = _state.PixelToBeat(width);

            int firstBeat = Mathf.FloorToInt(startBeat / tickInterval) * tickInterval;
            if (firstBeat < 0) firstBeat = 0;

            // Draw ticks and labels
            for (int beat = firstBeat; beat <= endBeat; beat += tickInterval)
            {
                float x = _state.BeatToPixel(beat);
                if (x < -10 || x > width + 10) continue;

                bool isMeasure = measureInterval > 0 && beat % measureInterval == 0;
                float tickHeight = isMeasure ? height * 0.6f : height * 0.3f;
                float tickAlpha = isMeasure ? 0.7f : 0.35f;

                // Draw tick line
                painter.strokeColor = new Color(1f, 1f, 1f, tickAlpha);
                painter.lineWidth = isMeasure ? 1.5f : 0.75f;
                painter.BeginPath();
                painter.MoveTo(new Vector2(x, height - tickHeight));
                painter.LineTo(new Vector2(x, height));
                painter.Stroke();

                // Draw label for measure lines (or every N beats at low zoom)
                if (isMeasure || (tickInterval >= 4 && beat % tickInterval == 0))
                {
                    int measureNumber = measureInterval > 0 ? beat / measureInterval + 1 : beat;
                    string label = isMeasure ? $"M{measureNumber}" : $"{beat}";

                    // We can't draw text with painter2D directly in UI Toolkit,
                    // so we use VisualElements for labels. This is handled in RefreshLabels.
                }
            }

            // Draw bottom border line
            painter.strokeColor = new Color(1f, 1f, 1f, 0.15f);
            painter.lineWidth = 1f;
            painter.BeginPath();
            painter.MoveTo(new Vector2(0, height - 0.5f));
            painter.LineTo(new Vector2(width, height - 0.5f));
            painter.Stroke();
        }

        public void RefreshLabels()
        {
            // Remove existing labels
            for (int i = childCount - 1; i >= 0; i--)
                RemoveAt(i);

            float width = resolvedStyle.width;
            if (width <= 0) return;

            int tickInterval = GetTickInterval();
            int measureInterval = _state.BeatsPerMeasure;

            float endBeat = _state.PixelToBeat(width);
            int firstBeat = Mathf.FloorToInt(_state.ScrollOffsetBeats / tickInterval) * tickInterval;
            if (firstBeat < 0) firstBeat = 0;

            for (int beat = firstBeat; beat <= endBeat; beat += tickInterval)
            {
                float x = _state.BeatToPixel(beat);
                if (x < -20 || x > width + 20) continue;

                bool isMeasure = measureInterval > 0 && beat % measureInterval == 0;
                if (!isMeasure && tickInterval < 4) continue;

                int measureNumber = measureInterval > 0 ? beat / measureInterval + 1 : beat;
                string text = isMeasure ? $"M{measureNumber}" : $"{beat}";

                var lbl = new Label(text);
                lbl.style.position = Position.Absolute;
                lbl.style.left = x - 12;
                lbl.style.top = 2;
                lbl.style.width = 24;
                lbl.style.fontSize = 9;
                lbl.style.color = new Color(0.7f, 0.7f, 0.7f, isMeasure ? 0.9f : 0.5f);
                lbl.style.unityTextAlign = TextAnchor.UpperCenter;
                Add(lbl);
            }
        }

        private int GetTickInterval()
        {
            float ppb = _state.PixelsPerBeat;

            // At high zoom (large ppb), show every beat
            // At low zoom (small ppb), show every 4, 8, or 16 beats
            if (ppb >= 30f) return 1;
            if (ppb >= 15f) return 2;
            if (ppb >= 8f) return 4;
            if (ppb >= 4f) return 8;
            return 16;
        }
    }
}
