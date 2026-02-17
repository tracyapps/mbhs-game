using System;
using System.Collections.Generic;
using System.Linq;
using MBHS.Core;
using MBHS.Core.Utilities;
using MBHS.Data.Enums;
using MBHS.Data.Models;
using MBHS.Systems.FormationEditor.Commands;
using MBHS.Systems.BandManagement;
using MBHS.Systems.SaveLoad;
using UnityEngine;
using UnityEngine.UIElements;

namespace MBHS.Systems.FormationEditor
{
    public enum EditorTool
    {
        Select,
        Place,
        Delete
    }

    public class FormationEditorController
    {
        private readonly IFormationSystem _formationSystem;
        private readonly IBandManager _bandManager;
        private readonly CommandHistory _commandHistory;

        // UI elements
        private readonly VisualElement _fieldContainer;
        private readonly VisualElement _fieldViewport;
        private readonly ScrollView _formationList;
        private readonly Label _statusLabel;
        private readonly Label _coordsLabel;
        private readonly Label _membersPlacedLabel;

        // Property panel elements
        private readonly VisualElement _memberProperties;
        private readonly VisualElement _formationProperties;
        private readonly Label _noSelectionMsg;
        private readonly Label _propMemberName;
        private readonly Label _propMemberInstrument;
        private readonly FloatField _propFieldX;
        private readonly FloatField _propFieldY;
        private readonly Slider _propFacing;
        private readonly TextField _propFormationLabel;
        private readonly FloatField _propStartBeat;
        private readonly FloatField _propDuration;
        private readonly Label _propMemberCount;

        // Toolbar
        private readonly Button _btnUndo;
        private readonly Button _btnRedo;

        // State
        private EditorTool _activeTool = EditorTool.Select;
        private readonly HashSet<string> _selectedMemberIds = new();
        private string _dragMemberId;
        private Vector2 _dragStartFieldPos;
        private bool _isDragging;
        private bool _isBoxSelecting;
        private Vector2 _boxSelectStart;
        private VisualElement _selectionBox;
        private bool _snapEnabled = true;

        // Field rendering
        private float _fieldScale = 1f; // pixels per yard
        private Vector2 _fieldOffset;   // pan offset
        private readonly Dictionary<string, VisualElement> _memberDots = new();
        private int _placementIndex; // which roster member to place next

        // Events for the 3D preview
        public event Action OnFormationDisplayChanged;

        public FormationEditorController(
            IFormationSystem formationSystem,
            IBandManager bandManager,
            UIDocument document)
        {
            _formationSystem = formationSystem;
            _bandManager = bandManager;
            _commandHistory = new CommandHistory();

            var root = document.rootVisualElement;

            // Query UI elements
            _fieldContainer = root.Q<VisualElement>("field-container");
            _fieldViewport = root.Q<VisualElement>("field-viewport");
            _formationList = root.Q<ScrollView>("formation-list");
            _statusLabel = root.Q<Label>("lbl-status");
            _coordsLabel = root.Q<Label>("lbl-coords");
            _membersPlacedLabel = root.Q<Label>("lbl-members-placed");

            // Property panel
            _memberProperties = root.Q<VisualElement>("member-properties");
            _formationProperties = root.Q<VisualElement>("formation-properties");
            _noSelectionMsg = root.Q<Label>("no-selection-msg");
            _propMemberName = root.Q<Label>("prop-member-name");
            _propMemberInstrument = root.Q<Label>("prop-member-instrument");
            _propFieldX = root.Q<FloatField>("prop-field-x");
            _propFieldY = root.Q<FloatField>("prop-field-y");
            _propFacing = root.Q<Slider>("prop-facing");
            _propFormationLabel = root.Q<TextField>("prop-formation-label");
            _propStartBeat = root.Q<FloatField>("prop-start-beat");
            _propDuration = root.Q<FloatField>("prop-duration");
            _propMemberCount = root.Q<Label>("prop-member-count");

            // Toolbar
            _btnUndo = root.Q<Button>("btn-undo");
            _btnRedo = root.Q<Button>("btn-redo");

            BindToolbarEvents(root);
            BindFieldEvents();
            BindPropertyEvents();
            BindFormationSystemEvents();

            // Initial state
            UpdatePropertyPanelVisibility();
            _commandHistory.OnHistoryChanged += UpdateUndoRedoButtons;
            UpdateUndoRedoButtons();
        }

        private void BindToolbarEvents(VisualElement root)
        {
            _btnUndo.clicked += () => _commandHistory.Undo();
            _btnRedo.clicked += () => _commandHistory.Redo();

            root.Q<Button>("btn-tool-select").clicked += () => SetTool(EditorTool.Select);
            root.Q<Button>("btn-tool-place").clicked += () => SetTool(EditorTool.Place);
            root.Q<Button>("btn-tool-delete").clicked += () => SetTool(EditorTool.Delete);

            root.Q<Toggle>("toggle-snap").RegisterValueChangedCallback(evt =>
                _snapEnabled = evt.newValue);

            root.Q<Button>("btn-zoom-fit").clicked += ZoomToFit;
            root.Q<Button>("btn-add-formation").clicked += AddNewFormation;
            root.Q<Button>("btn-save").clicked += SaveChart;
            root.Q<Button>("btn-load").clicked += LoadChart;
        }

        private void BindFieldEvents()
        {
            _fieldContainer.RegisterCallback<GeometryChangedEvent>(_ => LayoutField());
            _fieldContainer.RegisterCallback<PointerDownEvent>(OnFieldPointerDown);
            _fieldContainer.RegisterCallback<PointerMoveEvent>(OnFieldPointerMove);
            _fieldContainer.RegisterCallback<PointerUpEvent>(OnFieldPointerUp);
            _fieldContainer.RegisterCallback<WheelEvent>(OnFieldScroll);

            // Create selection box element (hidden by default)
            _selectionBox = new VisualElement();
            _selectionBox.AddToClassList("selection-box");
            _selectionBox.style.display = DisplayStyle.None;
            _fieldViewport.Add(_selectionBox);
        }

        private void BindPropertyEvents()
        {
            _propFieldX?.RegisterValueChangedCallback(evt =>
            {
                if (_selectedMemberIds.Count != 1) return;
                var memberId = _selectedMemberIds.First();
                var formation = _formationSystem.CurrentFormation;
                if (formation == null) return;
                var pos = formation.GetPositionForMember(memberId);
                if (pos == null) return;
                _formationSystem.SetMemberPosition(formation.Id, memberId,
                    new Vector2(evt.newValue, pos.FieldY), pos.FacingAngle);
            });

            _propFieldY?.RegisterValueChangedCallback(evt =>
            {
                if (_selectedMemberIds.Count != 1) return;
                var memberId = _selectedMemberIds.First();
                var formation = _formationSystem.CurrentFormation;
                if (formation == null) return;
                var pos = formation.GetPositionForMember(memberId);
                if (pos == null) return;
                _formationSystem.SetMemberPosition(formation.Id, memberId,
                    new Vector2(pos.FieldX, evt.newValue), pos.FacingAngle);
            });

            _propFacing?.RegisterValueChangedCallback(evt =>
            {
                if (_selectedMemberIds.Count != 1) return;
                var memberId = _selectedMemberIds.First();
                var formation = _formationSystem.CurrentFormation;
                if (formation == null) return;
                var pos = formation.GetPositionForMember(memberId);
                if (pos == null) return;
                _formationSystem.SetMemberPosition(formation.Id, memberId,
                    pos.FieldPosition, evt.newValue);
            });

            _propFormationLabel?.RegisterValueChangedCallback(evt =>
            {
                var formation = _formationSystem.CurrentFormation;
                if (formation != null)
                    _formationSystem.UpdateFormation(formation.Id, label: evt.newValue);
            });

            _propStartBeat?.RegisterValueChangedCallback(evt =>
            {
                var formation = _formationSystem.CurrentFormation;
                if (formation != null)
                    _formationSystem.UpdateFormation(formation.Id, startBeat: evt.newValue);
            });

            _propDuration?.RegisterValueChangedCallback(evt =>
            {
                var formation = _formationSystem.CurrentFormation;
                if (formation != null)
                    _formationSystem.UpdateFormation(formation.Id, durationBeats: evt.newValue);
            });
        }

        private void BindFormationSystemEvents()
        {
            _formationSystem.OnFormationChanged += _ => RefreshFieldDisplay();
            _formationSystem.OnFormationAdded += _ => RefreshFormationList();
            _formationSystem.OnFormationRemoved += _ => RefreshFormationList();
            _formationSystem.OnChartChanged += _ =>
            {
                RefreshFormationList();
                RefreshFieldDisplay();
            };
            _formationSystem.OnCurrentFormationChanged += _ =>
            {
                RefreshFormationList();
                RefreshFieldDisplay();
                UpdateFormationProperties();
            };
        }

        // =====================================================================
        // Field Layout & Rendering
        // =====================================================================

        public void LayoutField()
        {
            if (_fieldContainer == null) return;

            float containerWidth = _fieldContainer.resolvedStyle.width;
            float containerHeight = _fieldContainer.resolvedStyle.height;

            if (containerWidth <= 0 || containerHeight <= 0) return;

            // Calculate scale to fit field in container with padding
            float padding = 40f;
            float availableWidth = containerWidth - padding * 2;
            float availableHeight = containerHeight - padding * 2;

            float scaleX = availableWidth / FieldCoordinates.FieldLengthYards;
            float scaleY = availableHeight / FieldCoordinates.FieldWidthYards;
            _fieldScale = Mathf.Min(scaleX, scaleY);

            float fieldPixelWidth = FieldCoordinates.FieldLengthYards * _fieldScale;
            float fieldPixelHeight = FieldCoordinates.FieldWidthYards * _fieldScale;

            _fieldOffset = new Vector2(
                (containerWidth - fieldPixelWidth) / 2f,
                (containerHeight - fieldPixelHeight) / 2f
            );

            // Position the field viewport
            _fieldViewport.style.left = _fieldOffset.x;
            _fieldViewport.style.top = _fieldOffset.y;
            _fieldViewport.style.width = fieldPixelWidth;
            _fieldViewport.style.height = fieldPixelHeight;

            DrawFieldMarkings();
            RefreshFieldDisplay();
        }

        private void DrawFieldMarkings()
        {
            // Clear previous markings (but keep member dots and selection box)
            var toRemove = _fieldViewport.Children()
                .Where(c => c.ClassListContains("yard-line") ||
                           c.ClassListContains("yard-line-major") ||
                           c.ClassListContains("yard-line-label") ||
                           c.ClassListContains("hash-mark"))
                .ToList();
            foreach (var el in toRemove)
                _fieldViewport.Remove(el);

            float fieldPixelHeight = FieldCoordinates.FieldWidthYards * _fieldScale;

            // Draw yard lines every 5 yards
            for (int yard = 0; yard <= 100; yard += 5)
            {
                float x = yard * _fieldScale;
                bool isMajor = yard % 10 == 0;

                var line = new VisualElement();
                line.AddToClassList(isMajor ? "yard-line-major" : "yard-line");
                line.style.left = x;
                line.style.top = 0;
                line.style.height = fieldPixelHeight;
                _fieldViewport.Add(line);

                // Yard number labels for major lines (10-yard intervals)
                if (isMajor && yard > 0 && yard < 100)
                {
                    int displayNumber = yard <= 50 ? yard : 100 - yard;

                    // Top label
                    var topLabel = new VisualElement();
                    topLabel.AddToClassList("yard-line-label");
                    var topText = new Label(displayNumber.ToString());
                    topText.style.fontSize = 9;
                    topText.style.color = new Color(1, 1, 1, 0.6f);
                    topText.style.position = Position.Absolute;
                    topText.style.left = x - 10;
                    topText.style.top = 4;
                    topText.style.width = 20;
                    topText.style.unityTextAlign = TextAnchor.MiddleCenter;
                    _fieldViewport.Add(topText);

                    // Bottom label
                    var bottomText = new Label(displayNumber.ToString());
                    bottomText.style.fontSize = 9;
                    bottomText.style.color = new Color(1, 1, 1, 0.6f);
                    bottomText.style.position = Position.Absolute;
                    bottomText.style.left = x - 10;
                    bottomText.style.bottom = 4;
                    bottomText.style.width = 20;
                    bottomText.style.unityTextAlign = TextAnchor.MiddleCenter;
                    _fieldViewport.Add(bottomText);
                }
            }

            // Draw hash marks
            float homeHash = FieldCoordinates.HomeHashYards * _fieldScale;
            float visitorHash = FieldCoordinates.VisitorHashYards * _fieldScale;

            for (int yard = 0; yard <= 100; yard++)
            {
                float x = yard * _fieldScale;

                // Home hash
                var hHome = new VisualElement();
                hHome.AddToClassList("hash-mark");
                hHome.style.left = x - 4;
                hHome.style.top = homeHash;
                _fieldViewport.Add(hHome);

                // Visitor hash
                var hVisitor = new VisualElement();
                hVisitor.AddToClassList("hash-mark");
                hVisitor.style.left = x - 4;
                hVisitor.style.top = visitorHash;
                _fieldViewport.Add(hVisitor);
            }
        }

        public void RefreshFieldDisplay()
        {
            var formation = _formationSystem.CurrentFormation;

            // Clear existing member dots
            foreach (var dot in _memberDots.Values)
            {
                if (_fieldViewport.Contains(dot))
                    _fieldViewport.Remove(dot);
            }
            _memberDots.Clear();

            if (formation == null)
            {
                UpdateMembersPlacedLabel(0);
                OnFormationDisplayChanged?.Invoke();
                return;
            }

            // Create dots for each positioned member
            foreach (var pos in formation.Positions)
            {
                var dot = CreateMemberDot(pos);
                _memberDots[pos.MemberId] = dot;
                _fieldViewport.Add(dot);
            }

            UpdateMembersPlacedLabel(formation.Positions.Count);
            UpdatePropertyPanelVisibility();
            OnFormationDisplayChanged?.Invoke();
        }

        private VisualElement CreateMemberDot(MemberPosition pos)
        {
            var dot = new VisualElement();
            dot.AddToClassList("member-dot");
            dot.userData = pos.MemberId;

            // Color by instrument family
            var member = _bandManager?.Roster?.GetMemberById(pos.MemberId);
            if (member != null)
            {
                string familyClass = GetFamilyClass(member.AssignedInstrument);
                dot.AddToClassList(familyClass);
            }
            else
            {
                dot.AddToClassList("brass"); // fallback
            }

            // Position on field
            float pixelX = pos.FieldX * _fieldScale;
            float pixelY = pos.FieldY * _fieldScale;
            dot.style.left = pixelX;
            dot.style.top = pixelY;

            // Selection state
            if (_selectedMemberIds.Contains(pos.MemberId))
                dot.AddToClassList("selected");

            // Facing indicator
            var facing = new VisualElement();
            facing.AddToClassList("facing-indicator");
            facing.style.rotate = new Rotate(Angle.Degrees(pos.FacingAngle));
            dot.Add(facing);

            // Pointer events for individual dots
            dot.RegisterCallback<PointerDownEvent>(evt =>
            {
                evt.StopPropagation();
                OnMemberDotPointerDown(pos.MemberId, evt);
            });

            dot.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (_isDragging)
                {
                    evt.StopPropagation();
                    OnMemberDotDrag(evt);
                }
            });

            dot.RegisterCallback<PointerUpEvent>(evt =>
            {
                evt.StopPropagation();
                OnMemberDotPointerUp(evt);
            });

            return dot;
        }

        // =====================================================================
        // Tool Management
        // =====================================================================

        public void SetTool(EditorTool tool)
        {
            _activeTool = tool;
            _statusLabel.text = $"Tool: {tool}";

            // Reset placement index when switching to place tool
            if (tool == EditorTool.Place)
                _placementIndex = 0;
        }

        // =====================================================================
        // Field Pointer Events
        // =====================================================================

        private void OnFieldPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0) return; // left click only

            Vector2 fieldPos = ScreenToFieldPos(evt.localPosition);

            switch (_activeTool)
            {
                case EditorTool.Select:
                    // Start box selection if clicking on empty space
                    if (!IsOverMemberDot(evt.localPosition))
                    {
                        ClearSelection();
                        _isBoxSelecting = true;
                        _boxSelectStart = evt.localPosition;
                        _selectionBox.style.display = DisplayStyle.Flex;
                        _fieldContainer.CapturePointer(evt.pointerId);
                    }
                    break;

                case EditorTool.Place:
                    PlaceMemberAtPosition(fieldPos);
                    break;

                case EditorTool.Delete:
                    // Find member near click and remove
                    string nearestId = FindNearestMember(fieldPos, 2f);
                    if (nearestId != null)
                    {
                        var formation = _formationSystem.CurrentFormation;
                        if (formation != null)
                        {
                            var memberPos = formation.GetPositionForMember(nearestId);
                            if (memberPos != null)
                            {
                                var cmd = new RemoveMemberCommand(
                                    _formationSystem, formation.Id, nearestId,
                                    memberPos.FieldPosition, memberPos.FacingAngle);
                                _commandHistory.Execute(cmd);
                            }
                        }
                    }
                    break;
            }
        }

        private void OnFieldPointerMove(PointerMoveEvent evt)
        {
            // Update coordinate display
            Vector2 fieldPos = ScreenToFieldPos(evt.localPosition);
            fieldPos = FieldCoordinates.ClampToField(fieldPos);
            string desc = FieldCoordinates.GetFieldPositionDescription(fieldPos);
            _coordsLabel.text = $"({fieldPos.x:F1}, {fieldPos.y:F1}) {desc}";

            // Box selection
            if (_isBoxSelecting)
            {
                UpdateSelectionBox(evt.localPosition);
            }
        }

        private void OnFieldPointerUp(PointerUpEvent evt)
        {
            if (_isBoxSelecting)
            {
                _isBoxSelecting = false;
                _selectionBox.style.display = DisplayStyle.None;
                _fieldContainer.ReleasePointer(evt.pointerId);
                FinishBoxSelection(evt.localPosition);
            }
        }

        private void OnFieldScroll(WheelEvent evt)
        {
            // Zoom in/out
            float delta = evt.delta.y > 0 ? 0.9f : 1.1f;
            _fieldScale *= delta;
            _fieldScale = Mathf.Clamp(_fieldScale, 2f, 20f);
            LayoutField();
            evt.StopPropagation();
        }

        // =====================================================================
        // Member Dot Events
        // =====================================================================

        private void OnMemberDotPointerDown(string memberId, PointerDownEvent evt)
        {
            if (_activeTool == EditorTool.Delete)
            {
                var formation = _formationSystem.CurrentFormation;
                if (formation != null)
                {
                    var pos = formation.GetPositionForMember(memberId);
                    if (pos != null)
                    {
                        var cmd = new RemoveMemberCommand(
                            _formationSystem, formation.Id, memberId,
                            pos.FieldPosition, pos.FacingAngle);
                        _commandHistory.Execute(cmd);
                    }
                }
                return;
            }

            // Select/toggle selection
            bool isMultiSelect = evt.shiftKey || evt.ctrlKey;
            if (!isMultiSelect)
            {
                if (!_selectedMemberIds.Contains(memberId))
                    ClearSelection();
            }

            if (_selectedMemberIds.Contains(memberId) && isMultiSelect)
                _selectedMemberIds.Remove(memberId);
            else
                _selectedMemberIds.Add(memberId);

            // Start drag
            _dragMemberId = memberId;
            _isDragging = true;
            var formation2 = _formationSystem.CurrentFormation;
            var memberPos = formation2?.GetPositionForMember(memberId);
            if (memberPos != null)
                _dragStartFieldPos = memberPos.FieldPosition;

            RefreshFieldDisplay();
            UpdateSelectedMemberProperties();

            // Capture pointer for drag
            if (_memberDots.TryGetValue(memberId, out var dot))
                dot.CapturePointer(evt.pointerId);
        }

        private void OnMemberDotDrag(PointerMoveEvent evt)
        {
            if (!_isDragging || _dragMemberId == null) return;

            var formation = _formationSystem.CurrentFormation;
            if (formation == null) return;

            Vector2 fieldPos = ScreenToFieldPos(evt.position);
            fieldPos = FieldCoordinates.ClampToField(fieldPos);

            if (_snapEnabled)
                fieldPos = FieldCoordinates.SnapToGrid(fieldPos);

            // Update position directly (command created on pointer up)
            var pos = formation.GetPositionForMember(_dragMemberId);
            if (pos != null)
            {
                pos.FieldPosition = fieldPos;
            }

            RefreshFieldDisplay();
        }

        private void OnMemberDotPointerUp(PointerUpEvent evt)
        {
            if (_isDragging && _dragMemberId != null)
            {
                var formation = _formationSystem.CurrentFormation;
                var pos = formation?.GetPositionForMember(_dragMemberId);

                if (pos != null && _dragStartFieldPos != pos.FieldPosition)
                {
                    // Create undo command for the completed drag
                    // First revert, then execute through command
                    var endPos = pos.FieldPosition;
                    pos.FieldPosition = _dragStartFieldPos;

                    var cmd = new MoveMemberCommand(
                        _formationSystem, formation.Id, _dragMemberId,
                        _dragStartFieldPos, pos.FacingAngle,
                        endPos, pos.FacingAngle);
                    _commandHistory.Execute(cmd);
                }

                if (_memberDots.TryGetValue(_dragMemberId, out var dot))
                    dot.ReleasePointer(evt.pointerId);
            }

            _isDragging = false;
            _dragMemberId = null;
        }

        // =====================================================================
        // Selection
        // =====================================================================

        private void ClearSelection()
        {
            _selectedMemberIds.Clear();
            RefreshFieldDisplay();
            UpdatePropertyPanelVisibility();
        }

        private void UpdateSelectionBox(Vector2 currentPos)
        {
            float x = Mathf.Min(_boxSelectStart.x, currentPos.x) - _fieldOffset.x;
            float y = Mathf.Min(_boxSelectStart.y, currentPos.y) - _fieldOffset.y;
            float w = Mathf.Abs(currentPos.x - _boxSelectStart.x);
            float h = Mathf.Abs(currentPos.y - _boxSelectStart.y);

            _selectionBox.style.left = x;
            _selectionBox.style.top = y;
            _selectionBox.style.width = w;
            _selectionBox.style.height = h;
        }

        private void FinishBoxSelection(Vector2 endPos)
        {
            float minX = Mathf.Min(_boxSelectStart.x, endPos.x);
            float maxX = Mathf.Max(_boxSelectStart.x, endPos.x);
            float minY = Mathf.Min(_boxSelectStart.y, endPos.y);
            float maxY = Mathf.Max(_boxSelectStart.y, endPos.y);

            // Convert screen rect to field rect
            Vector2 fieldMin = ScreenToFieldPos(new Vector2(minX, minY));
            Vector2 fieldMax = ScreenToFieldPos(new Vector2(maxX, maxY));

            var formation = _formationSystem.CurrentFormation;
            if (formation == null) return;

            _selectedMemberIds.Clear();

            foreach (var pos in formation.Positions)
            {
                if (pos.FieldX >= fieldMin.x && pos.FieldX <= fieldMax.x &&
                    pos.FieldY >= fieldMin.y && pos.FieldY <= fieldMax.y)
                {
                    _selectedMemberIds.Add(pos.MemberId);
                }
            }

            RefreshFieldDisplay();
            UpdatePropertyPanelVisibility();
        }

        // =====================================================================
        // Placement
        // =====================================================================

        private void PlaceMemberAtPosition(Vector2 fieldPos)
        {
            var formation = _formationSystem.CurrentFormation;
            if (formation == null)
            {
                _statusLabel.text = "Create a formation first";
                return;
            }

            var roster = _bandManager?.Roster;
            if (roster == null || roster.Members.Count == 0)
            {
                _statusLabel.text = "No band members available";
                return;
            }

            // Find the next unplaced member
            var placedIds = new HashSet<string>(formation.Positions.Select(p => p.MemberId));
            var unplacedMembers = roster.Members
                .Where(m => m.Status == MemberStatus.Active && !placedIds.Contains(m.Id))
                .ToList();

            if (unplacedMembers.Count == 0)
            {
                _statusLabel.text = "All members placed";
                return;
            }

            var memberToPlace = unplacedMembers[0];

            if (_snapEnabled)
                fieldPos = FieldCoordinates.SnapToGrid(fieldPos);

            fieldPos = FieldCoordinates.ClampToField(fieldPos);

            var cmd = new PlaceMemberCommand(
                _formationSystem, formation.Id, memberToPlace.Id,
                fieldPos, 0f);
            _commandHistory.Execute(cmd);

            _statusLabel.text = $"Placed {memberToPlace.DisplayName}";
        }

        // =====================================================================
        // Formation List
        // =====================================================================

        public void RefreshFormationList()
        {
            _formationList.Clear();

            var chart = _formationSystem.ActiveChart;
            if (chart == null) return;

            for (int i = 0; i < chart.Formations.Count; i++)
            {
                var formation = chart.Formations[i];
                int index = i;

                var item = new VisualElement();
                item.AddToClassList("formation-item");

                if (i == _formationSystem.CurrentFormationIndex)
                    item.AddToClassList("selected");

                var label = new Label(formation.Label ?? $"Formation {i + 1}");
                label.AddToClassList("formation-item-label");
                item.Add(label);

                var info = new Label(
                    $"Beat {formation.StartBeat:F0} | {formation.Positions.Count} members");
                info.AddToClassList("formation-item-info");
                item.Add(info);

                item.RegisterCallback<ClickEvent>(_ =>
                {
                    _formationSystem.SetCurrentFormation(index);
                    ClearSelection();
                });

                _formationList.Add(item);
            }
        }

        private void AddNewFormation()
        {
            var chart = _formationSystem.ActiveChart;
            if (chart == null)
            {
                // Create a new chart first
                _formationSystem.CreateNewChart("New Show", "");
            }

            float startBeat = 0f;
            if (_formationSystem.ActiveChart.Formations.Count > 0)
            {
                var last = _formationSystem.ActiveChart.Formations[^1];
                startBeat = last.StartBeat + last.DurationBeats + 8f; // 8 beats transition
            }

            int formationNumber = _formationSystem.ActiveChart.Formations.Count + 1;
            var formation = _formationSystem.AddFormation(startBeat, 8f, $"Set {formationNumber}");

            if (formation != null)
            {
                _formationSystem.SetCurrentFormation(
                    _formationSystem.ActiveChart.Formations.Count - 1);
                _statusLabel.text = $"Added {formation.Label}";
            }
        }

        // =====================================================================
        // Properties Panel
        // =====================================================================

        private void UpdatePropertyPanelVisibility()
        {
            bool hasMemberSelection = _selectedMemberIds.Count > 0;
            bool hasFormation = _formationSystem.CurrentFormation != null;

            if (_memberProperties != null)
                _memberProperties.style.display =
                    hasMemberSelection ? DisplayStyle.Flex : DisplayStyle.None;

            if (_formationProperties != null)
                _formationProperties.style.display =
                    hasFormation ? DisplayStyle.Flex : DisplayStyle.None;

            if (_noSelectionMsg != null)
                _noSelectionMsg.style.display =
                    (!hasMemberSelection && !hasFormation)
                        ? DisplayStyle.Flex
                        : DisplayStyle.None;

            if (hasMemberSelection)
                UpdateSelectedMemberProperties();

            if (hasFormation)
                UpdateFormationProperties();
        }

        private void UpdateSelectedMemberProperties()
        {
            if (_selectedMemberIds.Count != 1) return;

            var memberId = _selectedMemberIds.First();
            var member = _bandManager?.Roster?.GetMemberById(memberId);
            var formation = _formationSystem.CurrentFormation;
            var pos = formation?.GetPositionForMember(memberId);

            if (_propMemberName != null)
                _propMemberName.text = member?.DisplayName ?? memberId;

            if (_propMemberInstrument != null)
                _propMemberInstrument.text = member?.AssignedInstrument.ToString() ?? "--";

            if (pos != null)
            {
                if (_propFieldX != null)
                    _propFieldX.SetValueWithoutNotify(pos.FieldX);
                if (_propFieldY != null)
                    _propFieldY.SetValueWithoutNotify(pos.FieldY);
                if (_propFacing != null)
                    _propFacing.SetValueWithoutNotify(pos.FacingAngle);
            }
        }

        private void UpdateFormationProperties()
        {
            var formation = _formationSystem.CurrentFormation;
            if (formation == null) return;

            if (_propFormationLabel != null)
                _propFormationLabel.SetValueWithoutNotify(formation.Label ?? "");
            if (_propStartBeat != null)
                _propStartBeat.SetValueWithoutNotify(formation.StartBeat);
            if (_propDuration != null)
                _propDuration.SetValueWithoutNotify(formation.DurationBeats);
            if (_propMemberCount != null)
                _propMemberCount.text = formation.Positions.Count.ToString();
        }

        // =====================================================================
        // Save/Load
        // =====================================================================

        private async void SaveChart()
        {
            var chart = _formationSystem.ActiveChart;
            if (chart == null)
            {
                _statusLabel.text = "No chart to save";
                return;
            }

            var saveSystem = ServiceLocator.Get<ISaveSystem>();
            await saveSystem.SaveDrillChart(chart);
            _statusLabel.text = $"Saved: {chart.Name}";
        }

        private async void LoadChart()
        {
            var saveSystem = ServiceLocator.Get<ISaveSystem>();
            var charts = await saveSystem.ListDrillCharts();

            if (charts.Count == 0)
            {
                _statusLabel.text = "No saved charts found";
                return;
            }

            // Load most recent chart
            var mostRecent = charts[0];
            var chart = await saveSystem.LoadDrillChart(mostRecent.Id);

            if (chart != null)
            {
                _formationSystem.LoadChart(chart);
                _statusLabel.text = $"Loaded: {chart.Name}";
            }
        }

        // =====================================================================
        // Helpers
        // =====================================================================

        private Vector2 ScreenToFieldPos(Vector2 screenPos)
        {
            // screenPos is relative to _fieldContainer
            float fieldX = (screenPos.x - _fieldOffset.x) / _fieldScale;
            float fieldY = (screenPos.y - _fieldOffset.y) / _fieldScale;
            return new Vector2(fieldX, fieldY);
        }

        private Vector2 FieldToScreenPos(Vector2 fieldPos)
        {
            float screenX = fieldPos.x * _fieldScale + _fieldOffset.x;
            float screenY = fieldPos.y * _fieldScale + _fieldOffset.y;
            return new Vector2(screenX, screenY);
        }

        private bool IsOverMemberDot(Vector2 screenPos)
        {
            Vector2 fieldPos = ScreenToFieldPos(screenPos);
            return FindNearestMember(fieldPos, 1.5f) != null;
        }

        private string FindNearestMember(Vector2 fieldPos, float maxDistance)
        {
            var formation = _formationSystem.CurrentFormation;
            if (formation == null) return null;

            string nearestId = null;
            float nearestDist = maxDistance;

            foreach (var pos in formation.Positions)
            {
                float dist = Vector2.Distance(fieldPos, pos.FieldPosition);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestId = pos.MemberId;
                }
            }

            return nearestId;
        }

        private void ZoomToFit()
        {
            LayoutField(); // recalculate to fit
        }

        private void UpdateUndoRedoButtons()
        {
            if (_btnUndo != null)
                _btnUndo.SetEnabled(_commandHistory.CanUndo);
            if (_btnRedo != null)
                _btnRedo.SetEnabled(_commandHistory.CanRedo);
        }

        private void UpdateMembersPlacedLabel(int count)
        {
            if (_membersPlacedLabel != null)
                _membersPlacedLabel.text = $"{count} members placed";
        }

        private static string GetFamilyClass(InstrumentType type)
        {
            return type switch
            {
                InstrumentType.Trumpet or InstrumentType.Trombone or
                InstrumentType.FrenchHorn or InstrumentType.Tuba or
                InstrumentType.Sousaphone or InstrumentType.Baritone or
                InstrumentType.Mellophone => "brass",

                InstrumentType.Flute or InstrumentType.Piccolo or
                InstrumentType.Clarinet or InstrumentType.Saxophone
                    => "woodwind",

                InstrumentType.SnareDrum or InstrumentType.BassDrum or
                InstrumentType.TenorDrums or InstrumentType.Cymbals
                    => "battery-percussion",

                InstrumentType.Xylophone or InstrumentType.Marimba or
                InstrumentType.Vibraphone or InstrumentType.Timpani
                    => "front-ensemble",

                InstrumentType.Flag or InstrumentType.Rifle or
                InstrumentType.Saber => "color-guard",

                InstrumentType.DrumMajor => "leadership",

                _ => "brass"
            };
        }

        // Public accessors for the view
        public IFormationSystem FormationSystem => _formationSystem;
        public HashSet<string> SelectedMemberIds => _selectedMemberIds;
        public CommandHistory CommandHistory => _commandHistory;

        public List<MemberPosition> GetCurrentPositions()
        {
            return _formationSystem.CurrentFormation?.Positions
                ?? new List<MemberPosition>();
        }
    }
}
