using System;
using System.Collections.Generic;
using System.Linq;
using MBHS.Data.Enums;
using MBHS.Data.Models;
using MBHS.Systems.BandManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace MBHS.Systems.FormationEditor
{
    public class BandMemberPanelController
    {
        private readonly FormationEditorController _editorController;
        private readonly IBandManager _bandManager;
        private readonly IFormationSystem _formationSystem;
        private readonly VisualElement _root;

        private ScrollView _memberScroll;
        private readonly Dictionary<string, VisualElement> _memberRows = new();
        private readonly Dictionary<string, MemberAvatarElement> _avatars = new();

        private string _activeFilter = "All";
        private string _editingNicknameId;
        private bool _ignoreSelectionCallback;

        private static readonly (string Label, InstrumentFamily? Family)[] Filters =
        {
            ("All", null),
            ("Brass", InstrumentFamily.Brass),
            ("Wood", InstrumentFamily.Woodwind),
            ("Perc", InstrumentFamily.BatteryPercussion),
            ("Guard", InstrumentFamily.ColorGuard),
            ("Pit", InstrumentFamily.FrontEnsemble),
            ("Lead", InstrumentFamily.Leadership)
        };

        public BandMemberPanelController(
            FormationEditorController editorController,
            IBandManager bandManager,
            VisualElement membersTabContent)
        {
            _editorController = editorController;
            _bandManager = bandManager;
            _formationSystem = editorController.FormationSystem;
            _root = membersTabContent;

            BuildUI();
            BindEvents();
            RefreshMemberList();
        }

        private void BuildUI()
        {
            // Filter tabs
            var filterRow = new VisualElement();
            filterRow.AddToClassList("member-filter-tabs");

            foreach (var (label, _) in Filters)
            {
                var btn = new Button(() => SetFilter(label)) { text = label };
                btn.AddToClassList("member-filter-tab");
                btn.name = $"filter-{label.ToLowerInvariant()}";
                if (label == "All")
                    btn.AddToClassList("active");
                filterRow.Add(btn);
            }

            _root.Add(filterRow);

            // Member scroll view
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("member-scroll");
            _root.Add(scroll);
            _memberScroll = scroll;

            // Add member button
            var addBtn = new Button(OnAddMemberClicked) { text = "+ Add Member" };
            addBtn.AddToClassList("member-add-btn");
            _root.Add(addBtn);
        }

        private void BindEvents()
        {
            _editorController.OnSelectionChanged += OnFieldSelectionChanged;
            _bandManager.OnMemberRecruited += _ => RefreshMemberList();
            _bandManager.OnMemberDismissed += _ => RefreshMemberList();
            _bandManager.OnRosterChanged += _ => RefreshMemberList();
            _bandManager.OnMemberUpdated += _ => RefreshMemberList();
            _formationSystem.OnCurrentFormationChanged += _ => RefreshPlacedIndicators();
            _formationSystem.OnFormationChanged += _ => RefreshPlacedIndicators();
        }

        private void SetFilter(string filterLabel)
        {
            _activeFilter = filterLabel;

            // Update active state on filter buttons
            var filterRow = _root.Q<VisualElement>(className: "member-filter-tabs");
            if (filterRow != null)
            {
                foreach (var btn in filterRow.Children())
                {
                    btn.EnableInClassList("active",
                        btn.name == $"filter-{filterLabel.ToLowerInvariant()}");
                }
            }

            RefreshMemberList();
        }

        public void RefreshMemberList()
        {
            _memberScroll.Clear();
            _memberRows.Clear();
            _avatars.Clear();

            var roster = _bandManager.Roster;
            if (roster == null || roster.Members.Count == 0) return;

            var formation = _formationSystem.CurrentFormation;
            var placedIds = formation != null
                ? new HashSet<string>(formation.Positions.Select(p => p.MemberId))
                : new HashSet<string>();

            var selectedIds = _editorController.SelectedMemberIds;

            // Get the matching filter family
            InstrumentFamily? filterFamily = null;
            foreach (var (label, family) in Filters)
            {
                if (label == _activeFilter)
                {
                    filterFamily = family;
                    break;
                }
            }

            foreach (var member in roster.Members)
            {
                if (member.Status != MemberStatus.Active) continue;

                // Apply filter
                if (filterFamily.HasValue && GetFamily(member.AssignedInstrument) != filterFamily.Value)
                    continue;

                bool isPlaced = placedIds.Contains(member.Id);
                bool isSelected = selectedIds.Contains(member.Id);
                var row = CreateMemberRow(member, isPlaced, isSelected);
                _memberRows[member.Id] = row;
                _memberScroll.Add(row);
            }
        }

        private VisualElement CreateMemberRow(BandMemberData member, bool isPlaced, bool isSelected)
        {
            var row = new VisualElement();
            row.AddToClassList("member-row");
            row.userData = member.Id;

            if (!isPlaced)
                row.AddToClassList("unplaced");
            if (isSelected)
                row.AddToClassList("selected");

            // Avatar
            var avatar = new MemberAvatarElement();
            avatar.SetMember(member.DisplayName, member.AssignedInstrument);
            avatar.UpdateInitialLabel();
            _avatars[member.Id] = avatar;
            row.Add(avatar);

            // Info column
            var info = new VisualElement();
            info.AddToClassList("member-info");

            var nameLabel = new Label(member.DisplayName);
            nameLabel.AddToClassList("member-name");
            nameLabel.name = $"name-{member.Id}";
            info.Add(nameLabel);

            // Double-click name to edit nickname
            nameLabel.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.clickCount == 2)
                    StartNicknameEdit(member.Id, info, nameLabel);
            });

            var instrLabel = new Label(FormatInstrumentName(member.AssignedInstrument));
            instrLabel.AddToClassList("member-instrument");
            info.Add(instrLabel);

            row.Add(info);

            // Placed indicator
            var placed = new Label(isPlaced ? "\u2713" : "\u2022");
            placed.AddToClassList("member-placed-indicator");
            if (!isPlaced)
                placed.AddToClassList("not-placed");
            placed.name = $"placed-{member.Id}";
            row.Add(placed);

            // Action buttons
            var actions = new VisualElement();
            actions.AddToClassList("member-actions");

            var focusBtn = new Button(() => OnFocusClicked(member.Id)) { text = "F" };
            focusBtn.AddToClassList("member-action-btn");
            focusBtn.tooltip = "Focus on member";
            actions.Add(focusBtn);

            var soloBtn = new Button(() => OnSoloClicked(member.Id)) { text = "S" };
            soloBtn.AddToClassList("member-action-btn");
            soloBtn.name = $"solo-{member.Id}";
            soloBtn.tooltip = "Solo view";
            if (_editorController.IsSoloActive && _editorController.SoloMemberId == member.Id)
                soloBtn.AddToClassList("solo-active");
            actions.Add(soloBtn);

            var editBtn = new Button(() => OnEditClicked(member.Id)) { text = "E" };
            editBtn.AddToClassList("member-action-btn");
            editBtn.tooltip = "Edit member profile";
            actions.Add(editBtn);

            row.Add(actions);

            // Click row to select
            row.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0) return;
                evt.StopPropagation();
                OnMemberRowClicked(member.Id);
            });

            return row;
        }

        private void OnMemberRowClicked(string memberId)
        {
            _ignoreSelectionCallback = true;
            _editorController.SelectMember(memberId);
            _ignoreSelectionCallback = false;

            // Highlight the clicked row
            foreach (var kvp in _memberRows)
                kvp.Value.EnableInClassList("selected", kvp.Key == memberId);
        }

        private void OnFocusClicked(string memberId)
        {
            _editorController.SelectMember(memberId);
            _editorController.CenterOnMember(memberId);
        }

        private void OnSoloClicked(string memberId)
        {
            if (_editorController.IsSoloActive && _editorController.SoloMemberId == memberId)
                _editorController.ClearSoloMember();
            else
                _editorController.SetSoloMember(memberId);

            // Update solo button states
            foreach (var kvp in _memberRows)
            {
                var soloBtn = kvp.Value.Q<Button>($"solo-{kvp.Key}");
                soloBtn?.EnableInClassList("solo-active",
                    _editorController.IsSoloActive && _editorController.SoloMemberId == kvp.Key);
            }
        }

        private void OnEditClicked(string memberId)
        {
            // Set static fields for the MemberProfile scene
            var profileType = Type.GetType("MBHS.UI.Screens.MemberProfileScreen, MBHS.UI");
            if (profileType != null)
            {
                var incomingField = profileType.GetField("IncomingMemberId",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                incomingField?.SetValue(null, memberId);

                var returnField = profileType.GetField("ReturnScene",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                returnField?.SetValue(null, "FormationEditor");
            }

            SceneManager.LoadScene("MemberProfile");
        }

        private void OnAddMemberClicked()
        {
            var recruits = _bandManager.GetAvailableRecruits(1);
            if (recruits.Count == 0) return;

            var recruit = recruits[0];
            // Ensure budget
            if (_bandManager.Roster.Budget < recruit.RecruitCost)
                _bandManager.Roster.Budget += recruit.RecruitCost;

            _bandManager.RecruitMember(recruit);
        }

        private void StartNicknameEdit(string memberId, VisualElement container, Label nameLabel)
        {
            if (_editingNicknameId != null) return;
            _editingNicknameId = memberId;

            var member = _bandManager.Roster?.GetMemberById(memberId);
            if (member == null) return;

            nameLabel.style.display = DisplayStyle.None;

            var textField = new TextField();
            textField.AddToClassList("nickname-edit-field");
            textField.value = member.Nickname ?? member.DisplayName;
            textField.name = $"nickname-edit-{memberId}";
            container.Insert(0, textField);

            // Focus the text field
            textField.schedule.Execute(() => textField.Focus()).StartingIn(50);

            // Commit on Enter or blur
            textField.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    CommitNicknameEdit(memberId, textField, container, nameLabel);
                else if (evt.keyCode == KeyCode.Escape)
                    CancelNicknameEdit(textField, container, nameLabel);
            });

            textField.RegisterCallback<FocusOutEvent>(_ =>
                CommitNicknameEdit(memberId, textField, container, nameLabel));
        }

        private void CommitNicknameEdit(string memberId, TextField textField,
            VisualElement container, Label nameLabel)
        {
            if (_editingNicknameId != memberId) return;
            _editingNicknameId = null;

            string newNickname = textField.value?.Trim();
            var member = _bandManager.Roster?.GetMemberById(memberId);

            if (member != null && !string.IsNullOrEmpty(newNickname))
            {
                // If nickname matches full name, clear it
                string fullName = $"{member.FirstName} {member.LastName}";
                if (newNickname == fullName)
                    _bandManager.SetNickname(memberId, null);
                else
                    _bandManager.SetNickname(memberId, newNickname);

                nameLabel.text = member.DisplayName;
            }

            container.Remove(textField);
            nameLabel.style.display = DisplayStyle.Flex;
        }

        private void CancelNicknameEdit(TextField textField, VisualElement container, Label nameLabel)
        {
            _editingNicknameId = null;
            container.Remove(textField);
            nameLabel.style.display = DisplayStyle.Flex;
        }

        private void OnFieldSelectionChanged(HashSet<string> selectedIds)
        {
            if (_ignoreSelectionCallback) return;

            // Sync row highlighting
            foreach (var kvp in _memberRows)
                kvp.Value.EnableInClassList("selected", selectedIds.Contains(kvp.Key));

            // Scroll to first selected member
            if (selectedIds.Count > 0)
            {
                string firstId = selectedIds.First();
                ScrollToMember(firstId);
            }
        }

        public void ScrollToMember(string memberId)
        {
            if (_memberRows.TryGetValue(memberId, out var row))
                _memberScroll.ScrollTo(row);
        }

        private void RefreshPlacedIndicators()
        {
            var formation = _formationSystem.CurrentFormation;
            var placedIds = formation != null
                ? new HashSet<string>(formation.Positions.Select(p => p.MemberId))
                : new HashSet<string>();

            foreach (var kvp in _memberRows)
            {
                string memberId = kvp.Key;
                var row = kvp.Value;
                bool isPlaced = placedIds.Contains(memberId);

                row.EnableInClassList("unplaced", !isPlaced);

                var indicator = row.Q<Label>($"placed-{memberId}");
                if (indicator != null)
                {
                    indicator.text = isPlaced ? "\u2713" : "\u2022";
                    indicator.EnableInClassList("not-placed", !isPlaced);
                }
            }
        }

        private static string FormatInstrumentName(InstrumentType type)
        {
            // Convert PascalCase enum to display name
            string name = type.ToString();
            var result = new System.Text.StringBuilder();
            foreach (char c in name)
            {
                if (char.IsUpper(c) && result.Length > 0)
                    result.Append(' ');
                result.Append(c);
            }
            return result.ToString();
        }

        private static InstrumentFamily GetFamily(InstrumentType type)
        {
            return type switch
            {
                InstrumentType.Trumpet or InstrumentType.Trombone or
                InstrumentType.FrenchHorn or InstrumentType.Tuba or
                InstrumentType.Sousaphone or InstrumentType.Baritone or
                InstrumentType.Mellophone => InstrumentFamily.Brass,

                InstrumentType.Flute or InstrumentType.Piccolo or
                InstrumentType.Clarinet or InstrumentType.Saxophone
                    => InstrumentFamily.Woodwind,

                InstrumentType.SnareDrum or InstrumentType.BassDrum or
                InstrumentType.TenorDrums or InstrumentType.Cymbals
                    => InstrumentFamily.BatteryPercussion,

                InstrumentType.Xylophone or InstrumentType.Marimba or
                InstrumentType.Vibraphone or InstrumentType.Timpani
                    => InstrumentFamily.FrontEnsemble,

                InstrumentType.Flag or InstrumentType.Rifle or
                InstrumentType.Saber => InstrumentFamily.ColorGuard,

                InstrumentType.DrumMajor => InstrumentFamily.Leadership,

                _ => InstrumentFamily.Brass
            };
        }
    }
}
