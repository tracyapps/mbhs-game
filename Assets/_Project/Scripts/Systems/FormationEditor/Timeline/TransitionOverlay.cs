using MBHS.Data.Models;
using UnityEngine;
using UnityEngine.UIElements;

namespace MBHS.Systems.FormationEditor
{
    public class TransitionOverlay : VisualElement
    {
        private readonly TimelineState _state;
        private readonly Label _speedLabel;

        private Formation _fromFormation;
        private Formation _toFormation;
        private TransitionResult _result;

        public TransitionOverlay(TimelineState state)
        {
            _state = state;
            AddToClassList("transition-overlay");

            _speedLabel = new Label();
            _speedLabel.AddToClassList("transition-speed-label");
            Add(_speedLabel);

            _state.OnZoomChanged += _ => UpdatePosition();
            _state.OnScrollChanged += _ => UpdatePosition();
        }

        public void SetTransition(Formation from, Formation to, float bpm)
        {
            _fromFormation = from;
            _toFormation = to;

            _result = TransitionValidator.ValidateTransition(from, to, bpm);

            // Update severity class
            RemoveFromClassList("normal");
            RemoveFromClassList("fast");
            RemoveFromClassList("hard");
            RemoveFromClassList("impossible");

            string severityClass = _result.Severity switch
            {
                TransitionSeverity.Normal => "normal",
                TransitionSeverity.Fast => "fast",
                TransitionSeverity.Hard => "hard",
                TransitionSeverity.Impossible => "impossible",
                _ => "normal"
            };
            AddToClassList(severityClass);

            // Update label
            if (_result.GapBeats > 0 && _result.MaxSpeed < float.MaxValue)
            {
                _speedLabel.text = $"{_result.MaxSpeed:F1} yds/s";
                tooltip = $"{_result.GapBeats:F1} beats, {_result.GapSeconds:F1}s | " +
                          $"Max speed: {_result.MaxSpeed:F1} yds/s | " +
                          TransitionValidator.GetSeverityLabel(_result.Severity);
            }
            else if (_result.GapBeats <= 0)
            {
                _speedLabel.text = "overlap";
                tooltip = "Formations overlap - no transition gap";
            }
            else
            {
                _speedLabel.text = "!!";
                tooltip = "Impossible transition speed";
            }

            UpdatePosition();
        }

        public void UpdatePosition()
        {
            if (_fromFormation == null || _toFormation == null) return;

            float gapStart = _fromFormation.StartBeat + _fromFormation.DurationBeats;
            float gapEnd = _toFormation.StartBeat;

            float x = _state.BeatToPixel(gapStart);
            float w = (gapEnd - gapStart) * _state.PixelsPerBeat;

            style.left = x;
            style.width = Mathf.Max(w, 2f);

            // Hide label if too narrow
            _speedLabel.style.display = w > 30 ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
