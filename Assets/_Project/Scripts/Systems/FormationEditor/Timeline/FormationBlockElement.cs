using System;
using MBHS.Data.Models;
using MBHS.Systems.FormationEditor.Commands;
using UnityEngine;
using UnityEngine.UIElements;

namespace MBHS.Systems.FormationEditor
{
    public class FormationBlockElement : VisualElement
    {
        private readonly Formation _formation;
        private readonly TimelineState _state;
        private readonly IFormationSystem _formationSystem;
        private readonly CommandHistory _commandHistory;

        private readonly Label _label;
        private readonly VisualElement _resizeLeft;
        private readonly VisualElement _resizeRight;

        private bool _isDragging;
        private bool _isResizingLeft;
        private bool _isResizingRight;
        private float _dragStartBeat;
        private float _dragStartDuration;
        private float _pointerStartX;
        private bool _isSelected;
        private bool _isRenaming;

        private const float MinDurationBeats = 1f;
        private const float ResizeHandleWidth = 6f;

        public event Action<Formation> OnBlockClicked;

        public Formation Formation => _formation;

        public FormationBlockElement(
            Formation formation,
            TimelineState state,
            IFormationSystem formationSystem,
            CommandHistory commandHistory)
        {
            _formation = formation;
            _state = state;
            _formationSystem = formationSystem;
            _commandHistory = commandHistory;

            AddToClassList("formation-block");

            // Label (double-click to rename)
            _label = new Label(formation.Label ?? "Set");
            _label.AddToClassList("formation-block-label");
            _label.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.clickCount == 2)
                {
                    evt.StopPropagation();
                    StartRename();
                }
            });
            Add(_label);

            // Resize handles
            _resizeLeft = new VisualElement();
            _resizeLeft.AddToClassList("formation-block-resize-handle");
            _resizeLeft.AddToClassList("formation-block-resize-left");
            Add(_resizeLeft);

            _resizeRight = new VisualElement();
            _resizeRight.AddToClassList("formation-block-resize-handle");
            _resizeRight.AddToClassList("formation-block-resize-right");
            Add(_resizeRight);

            // Events
            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerMoveEvent>(OnPointerMove);
            RegisterCallback<PointerUpEvent>(OnPointerUp);

            _state.OnZoomChanged += _ => UpdatePosition();
            _state.OnScrollChanged += _ => UpdatePosition();

            UpdatePosition();
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            if (selected)
                AddToClassList("selected");
            else
                RemoveFromClassList("selected");
        }

        public void UpdateFromFormation()
        {
            _label.text = _formation.Label ?? "Set";
            UpdatePosition();
        }

        public void UpdatePosition()
        {
            float x = _state.BeatToPixel(_formation.StartBeat);
            float w = _formation.DurationBeats * _state.PixelsPerBeat;

            style.left = x;
            style.width = Mathf.Max(w, _state.PixelsPerBeat); // at least 1 beat wide visually
        }

        private void StartRename()
        {
            if (_isRenaming) return;
            _isRenaming = true;

            _label.style.display = DisplayStyle.None;

            var textField = new TextField();
            textField.AddToClassList("formation-block-rename-field");
            textField.value = _formation.Label ?? "";
            textField.name = "rename-field";
            Insert(0, textField);

            textField.schedule.Execute(() => textField.Focus()).StartingIn(50);

            textField.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    evt.StopPropagation();
                    CommitRename(textField);
                }
                else if (evt.keyCode == KeyCode.Escape)
                {
                    evt.StopPropagation();
                    CancelRename(textField);
                }
            });

            textField.RegisterCallback<FocusOutEvent>(_ => CommitRename(textField));
        }

        private void CommitRename(TextField textField)
        {
            if (!_isRenaming) return;
            _isRenaming = false;

            string newLabel = textField.value?.Trim();
            if (!string.IsNullOrEmpty(newLabel))
            {
                _formationSystem.UpdateFormation(_formation.Id, label: newLabel);
                _label.text = newLabel;
            }

            if (Contains(textField))
                Remove(textField);
            _label.style.display = DisplayStyle.Flex;
        }

        private void CancelRename(TextField textField)
        {
            _isRenaming = false;
            if (Contains(textField))
                Remove(textField);
            _label.style.display = DisplayStyle.Flex;
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0 || _isRenaming) return;

            evt.StopPropagation();
            OnBlockClicked?.Invoke(_formation);

            float localX = evt.localPosition.x;
            _pointerStartX = evt.position.x;

            // Determine if resizing or dragging
            if (localX <= ResizeHandleWidth)
            {
                _isResizingLeft = true;
                _dragStartBeat = _formation.StartBeat;
                _dragStartDuration = _formation.DurationBeats;
            }
            else if (localX >= resolvedStyle.width - ResizeHandleWidth)
            {
                _isResizingRight = true;
                _dragStartBeat = _formation.StartBeat;
                _dragStartDuration = _formation.DurationBeats;
            }
            else
            {
                _isDragging = true;
                _dragStartBeat = _formation.StartBeat;
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
                // Snap to nearest beat
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

            if (_isDragging)
            {
                float deltaPixels = evt.position.x - _pointerStartX;
                float deltaBeats = deltaPixels / _state.PixelsPerBeat;
                float newStart = Mathf.Max(0f, _dragStartBeat + deltaBeats);
                newStart = Mathf.Round(newStart * 2f) / 2f;

                if (!Mathf.Approximately(newStart, _dragStartBeat))
                {
                    var cmd = new MoveFormationCommand(
                        _formationSystem, _formation.Id, _dragStartBeat, newStart);
                    _commandHistory.Execute(cmd);
                }
                else
                {
                    UpdatePosition();
                }
            }
            else if (_isResizingLeft)
            {
                float deltaPixels = evt.position.x - _pointerStartX;
                float deltaBeats = deltaPixels / _state.PixelsPerBeat;
                float newStart = _dragStartBeat + deltaBeats;
                float maxStart = _dragStartBeat + _dragStartDuration - MinDurationBeats;
                newStart = Mathf.Clamp(newStart, 0f, maxStart);
                newStart = Mathf.Round(newStart * 2f) / 2f;
                float newDuration = _dragStartDuration - (newStart - _dragStartBeat);

                if (!Mathf.Approximately(newStart, _dragStartBeat) ||
                    !Mathf.Approximately(newDuration, _dragStartDuration))
                {
                    var cmd = new ResizeFormationCommand(
                        _formationSystem, _formation.Id,
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
                float deltaPixels = evt.position.x - _pointerStartX;
                float deltaBeats = deltaPixels / _state.PixelsPerBeat;
                float newDuration = _dragStartDuration + deltaBeats;
                newDuration = Mathf.Max(MinDurationBeats, newDuration);
                newDuration = Mathf.Round(newDuration * 2f) / 2f;

                if (!Mathf.Approximately(newDuration, _dragStartDuration))
                {
                    var cmd = new ResizeFormationCommand(
                        _formationSystem, _formation.Id,
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
    }
}
