using System;
using System.Collections.Generic;
using System.Linq;
using MBHS.Core;
using MBHS.Data.Enums;
using MBHS.Data.Models;
using MBHS.Systems.BandManagement;
using MBHS.Systems.SaveLoad;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace MBHS.UI.Screens
{
    [RequireComponent(typeof(UIDocument))]
    public class BandManagementScreen : MonoBehaviour
    {
        public static bool IsNewGame;

        private const int TrainingCost = 150;
        private const float TrainingAmount = 0.05f;
        private const int ScoutCount = 6;

        private UIDocument _document;
        private IBandManager _bandManager;

        // Top bar
        private Label _lblSchoolName;
        private Label _lblBudget;
        private Label _lblReputation;
        private Label _lblMemberCount;

        // Roster
        private ListView _rosterList;
        private Label _lblRosterCount;
        private readonly List<Button> _filterButtons = new();
        private InstrumentFamily? _activeFilter;
        private List<BandMemberData> _filteredMembers = new();

        // Detail
        private VisualElement _detailEmpty;
        private VisualElement _detailView;
        private VisualElement _detailActions;
        private Label _lblDetailName;
        private Label _lblDetailYear;
        private Label _lblDetailStatus;
        private Label _lblDetailInstrument;
        private VisualElement _barMusicianship;
        private VisualElement _barMarching;
        private VisualElement _barStamina;
        private VisualElement _barShowmanship;
        private Label _valMusicianship;
        private Label _valMarching;
        private Label _valStamina;
        private Label _valShowmanship;
        private Label _lblOverallRating;
        private Label _lblMorale;
        private Label _lblExperience;
        private DropdownField _ddInstrument;
        private BandMemberData _selectedMember;

        // Recruiting
        private ListView _recruitList;
        private List<RecruitCandidate> _currentRecruits = new();

        // Composition
        private VisualElement _compositionContent;

        // Status
        private Label _lblStatus;

        private static readonly string[] YearNames = { "", "Freshman", "Sophomore", "Junior", "Senior" };
        private static readonly string[] YearShort = { "", "Fr", "So", "Jr", "Sr" };

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void Start()
        {
            if (!ServiceLocator.TryGet(out _bandManager))
            {
                Debug.LogError("BandManagementScreen: IBandManager not registered.");
                return;
            }

            // Initialize roster based on how we got here
            if (IsNewGame)
            {
                CreateStarterBand();
                IsNewGame = false;
            }
            else if (_bandManager.Roster == null)
            {
                LoadSavedRoster();

                // If still null after load attempt, create a new one
                if (_bandManager.Roster == null)
                    CreateStarterBand();
            }

            BindUI();
            SetupRosterList();
            SetupRecruitList();
            RefreshAll();

            _bandManager.OnRosterChanged += OnRosterChanged;
            _bandManager.OnMemberRecruited += OnMemberRecruited;
            _bandManager.OnMemberUpdated += OnMemberUpdated;
            _bandManager.OnMemberDismissed += OnMemberDismissed;
        }

        private void OnRosterChanged(BandRosterData _) => RefreshAll();
        private void OnMemberRecruited(BandMemberData _) => RefreshAll();
        private void OnMemberUpdated(BandMemberData _) => RefreshDetailPanel();
        private void OnMemberDismissed(string _)
        {
            _selectedMember = null;
            RefreshAll();
        }

        private void CreateStarterBand()
        {
            _bandManager.CreateNewRoster("default_school");

            // Generate a starter set of recruits and auto-recruit them
            var starters = _bandManager.GetAvailableRecruits(12);
            foreach (var starter in starters)
            {
                starter.RecruitCost = 0; // Starters are free
                _bandManager.RecruitMember(starter);
            }

            SetStatus("New band created with 12 starter members");
        }

        private async void LoadSavedRoster()
        {
            if (ServiceLocator.TryGet<ISaveSystem>(out var saveSystem))
            {
                var roster = await saveSystem.LoadBandRoster();
                if (roster != null)
                {
                    _bandManager.LoadRoster(roster);
                    SetStatus("Roster loaded");
                }
            }
        }

        // =================================================================
        // UI Binding
        // =================================================================

        private void BindUI()
        {
            var root = _document.rootVisualElement;

            // Top bar
            _lblSchoolName = root.Q<Label>("lbl-school-name");
            _lblBudget = root.Q<Label>("lbl-budget");
            _lblReputation = root.Q<Label>("lbl-reputation");
            _lblMemberCount = root.Q<Label>("lbl-member-count");

            root.Q<Button>("btn-back").clicked += OnBack;
            root.Q<Button>("btn-proceed").clicked += OnProceed;

            // Roster panel
            _rosterList = root.Q<ListView>("roster-list");
            _lblRosterCount = root.Q<Label>("lbl-roster-count");

            // Filter tabs
            SetupFilterTabs(root);

            // Detail panel
            _detailEmpty = root.Q("detail-empty");
            _detailView = root.Q("detail-view");
            _detailActions = root.Q("detail-actions");

            _lblDetailName = root.Q<Label>("lbl-detail-name");
            _lblDetailYear = root.Q<Label>("lbl-detail-year");
            _lblDetailStatus = root.Q<Label>("lbl-detail-status");
            _lblDetailInstrument = root.Q<Label>("lbl-detail-instrument");

            _barMusicianship = root.Q("bar-musicianship");
            _barMarching = root.Q("bar-marching");
            _barStamina = root.Q("bar-stamina");
            _barShowmanship = root.Q("bar-showmanship");

            _valMusicianship = root.Q<Label>("val-musicianship");
            _valMarching = root.Q<Label>("val-marching");
            _valStamina = root.Q<Label>("val-stamina");
            _valShowmanship = root.Q<Label>("val-showmanship");

            _lblOverallRating = root.Q<Label>("lbl-overall-rating");
            _lblMorale = root.Q<Label>("lbl-morale");
            _lblExperience = root.Q<Label>("lbl-experience");

            // Instrument dropdown
            _ddInstrument = root.Q<DropdownField>("dd-instrument");
            var instrumentNames = Enum.GetValues(typeof(InstrumentType))
                .Cast<InstrumentType>()
                .Select(FormatInstrumentName)
                .ToList();
            _ddInstrument.choices = instrumentNames;

            root.Q<Button>("btn-assign").clicked += OnAssignInstrument;
            root.Q<Button>("btn-dismiss").clicked += OnDismissMember;

            // Training buttons
            root.Q<Button>("btn-train-musicianship").clicked +=
                () => OnTrainSkill(SkillType.Musicianship);
            root.Q<Button>("btn-train-marching").clicked +=
                () => OnTrainSkill(SkillType.Marching);
            root.Q<Button>("btn-train-stamina").clicked +=
                () => OnTrainSkill(SkillType.Stamina);
            root.Q<Button>("btn-train-showmanship").clicked +=
                () => OnTrainSkill(SkillType.Showmanship);

            // Recruiting
            _recruitList = root.Q<ListView>("recruit-list");
            root.Q<Button>("btn-scout").clicked += OnScoutRecruits;

            // Composition
            _compositionContent = root.Q("composition-content");

            // Status
            _lblStatus = root.Q<Label>("lbl-status");
        }

        private void SetupFilterTabs(VisualElement root)
        {
            var tabMap = new (string name, InstrumentFamily? family)[]
            {
                ("filter-all", null),
                ("filter-brass", InstrumentFamily.Brass),
                ("filter-woodwind", InstrumentFamily.Woodwind),
                ("filter-percussion", InstrumentFamily.BatteryPercussion),
                ("filter-guard", InstrumentFamily.ColorGuard),
                ("filter-pit", InstrumentFamily.FrontEnsemble),
                ("filter-leadership", InstrumentFamily.Leadership)
            };

            foreach (var (name, family) in tabMap)
            {
                var btn = root.Q<Button>(name);
                if (btn == null) continue;

                _filterButtons.Add(btn);
                var capturedFamily = family;
                btn.clicked += () => SetFilter(capturedFamily);
            }
        }

        // =================================================================
        // Roster ListView
        // =================================================================

        private void SetupRosterList()
        {
            _rosterList.makeItem = () =>
            {
                var row = new VisualElement();
                row.AddToClassList("member-row");

                var name = new Label { name = "name" };
                name.AddToClassList("member-name");

                var year = new Label { name = "year" };
                year.AddToClassList("member-year");

                var instrument = new Label { name = "instrument" };
                instrument.AddToClassList("member-instrument");

                var rating = new Label { name = "rating" };
                rating.AddToClassList("member-rating");

                row.Add(name);
                row.Add(year);
                row.Add(instrument);
                row.Add(rating);

                return row;
            };

            _rosterList.bindItem = (element, index) =>
            {
                if (index >= _filteredMembers.Count) return;
                var member = _filteredMembers[index];

                element.Q<Label>("name").text = member.DisplayName;
                element.Q<Label>("year").text = member.YearInSchool is >= 1 and <= 4
                    ? YearShort[member.YearInSchool]
                    : "?";

                element.Q<Label>("instrument").text = FormatInstrumentName(member.AssignedInstrument);

                int ratingPct = Mathf.RoundToInt(member.OverallRating * 100);
                var ratingLabel = element.Q<Label>("rating");
                ratingLabel.text = ratingPct.ToString();

                ratingLabel.RemoveFromClassList("rating-high");
                ratingLabel.RemoveFromClassList("rating-mid");
                ratingLabel.RemoveFromClassList("rating-low");
                ratingLabel.AddToClassList(ratingPct >= 70 ? "rating-high" :
                    ratingPct >= 40 ? "rating-mid" : "rating-low");
            };

            _rosterList.selectionChanged += OnRosterSelectionChanged;
            _rosterList.fixedItemHeight = 32;
            _rosterList.selectionType = SelectionType.Single;
        }

        // =================================================================
        // Recruit ListView
        // =================================================================

        private void SetupRecruitList()
        {
            _recruitList.makeItem = () =>
            {
                var row = new VisualElement();
                row.AddToClassList("recruit-row");

                var topRow = new VisualElement();
                topRow.AddToClassList("recruit-top-row");

                var name = new Label { name = "name" };
                name.AddToClassList("recruit-name");

                var cost = new Label { name = "cost" };
                cost.AddToClassList("recruit-cost");

                topRow.Add(name);
                topRow.Add(cost);

                var bottomRow = new VisualElement();
                bottomRow.AddToClassList("recruit-bottom-row");

                var info = new Label { name = "info" };
                info.AddToClassList("recruit-info");

                var recruitBtn = new Button { text = "Recruit", name = "btn-recruit" };
                recruitBtn.AddToClassList("recruit-btn");

                bottomRow.Add(info);
                bottomRow.Add(recruitBtn);

                row.Add(topRow);
                row.Add(bottomRow);

                return row;
            };

            _recruitList.bindItem = (element, index) =>
            {
                if (index >= _currentRecruits.Count) return;
                var candidate = _currentRecruits[index];
                var member = candidate.MemberData;

                element.Q<Label>("name").text = member.DisplayName;
                element.Q<Label>("cost").text = $"${candidate.RecruitCost:N0}";

                int rating = Mathf.RoundToInt(member.OverallRating * 100);
                element.Q<Label>("info").text =
                    $"{FormatInstrumentName(member.AssignedInstrument)} | {rating} OVR | " +
                    $"{(member.YearInSchool is >= 1 and <= 4 ? YearNames[member.YearInSchool] : "?")}";

                var btn = element.Q<Button>("btn-recruit");
                bool canAfford = _bandManager.Roster != null &&
                                 candidate.RecruitCost <= _bandManager.Roster.Budget;
                btn.SetEnabled(canAfford);

                btn.clicked -= null; // Clear isn't available, re-register
                var capturedIndex = index;
                btn.clickable = new Clickable(() => OnRecruitCandidate(capturedIndex));
            };

            _recruitList.fixedItemHeight = 54;
            _recruitList.selectionType = SelectionType.None;
        }

        // =================================================================
        // Refresh
        // =================================================================

        private void RefreshAll()
        {
            RefreshTopBar();
            RefreshRosterList();
            RefreshDetailPanel();
            RefreshRecruitList();
            RefreshComposition();
        }

        private void RefreshTopBar()
        {
            var roster = _bandManager.Roster;
            if (roster == null) return;

            _lblSchoolName.text = string.IsNullOrEmpty(roster.SchoolId)
                ? "My Band"
                : FormatSchoolName(roster.SchoolId);
            _lblBudget.text = $"${roster.Budget:N0}";
            _lblReputation.text = roster.Reputation.ToString();
            _lblMemberCount.text = roster.ActiveMemberCount.ToString();
        }

        private void RefreshRosterList()
        {
            var roster = _bandManager.Roster;
            if (roster == null)
            {
                _filteredMembers.Clear();
                _rosterList.itemsSource = _filteredMembers;
                _rosterList.Rebuild();
                return;
            }

            if (_activeFilter.HasValue)
            {
                _filteredMembers = roster.GetMembersByFamily(_activeFilter.Value);
            }
            else
            {
                _filteredMembers = roster.Members
                    .Where(m => m.Status == MemberStatus.Active)
                    .ToList();
            }

            _filteredMembers = _filteredMembers
                .OrderByDescending(m => m.OverallRating)
                .ToList();

            _lblRosterCount.text = $"{_filteredMembers.Count} members";

            _rosterList.itemsSource = _filteredMembers;
            _rosterList.Rebuild();
        }

        private void RefreshDetailPanel()
        {
            if (_selectedMember == null)
            {
                _detailEmpty.style.display = DisplayStyle.Flex;
                _detailView.style.display = DisplayStyle.None;
                _detailActions.style.display = DisplayStyle.None;
                return;
            }

            _detailEmpty.style.display = DisplayStyle.None;
            _detailView.style.display = DisplayStyle.Flex;
            _detailActions.style.display = DisplayStyle.Flex;

            var m = _selectedMember;
            _lblDetailName.text = m.DisplayName;
            _lblDetailYear.text = m.YearInSchool is >= 1 and <= 4
                ? YearNames[m.YearInSchool]
                : "Unknown";
            _lblDetailStatus.text = m.Status.ToString();
            _lblDetailInstrument.text = FormatInstrumentName(m.AssignedInstrument);

            SetSkillBar(_barMusicianship, _valMusicianship, m.Musicianship);
            SetSkillBar(_barMarching, _valMarching, m.Marching);
            SetSkillBar(_barStamina, _valStamina, m.Stamina);
            SetSkillBar(_barShowmanship, _valShowmanship, m.Showmanship);

            _lblOverallRating.text = Mathf.RoundToInt(m.OverallRating * 100).ToString();
            _lblMorale.text = m.Morale.ToString();
            _lblExperience.text = m.Experience.ToString();

            // Set dropdown to current instrument
            _ddInstrument.value = FormatInstrumentName(m.AssignedInstrument);

            // Training button state
            bool canTrain = _bandManager.Roster != null &&
                            _bandManager.Roster.Budget >= TrainingCost;
            var root = _document.rootVisualElement;
            root.Q<Button>("btn-train-musicianship").SetEnabled(canTrain);
            root.Q<Button>("btn-train-marching").SetEnabled(canTrain);
            root.Q<Button>("btn-train-stamina").SetEnabled(canTrain);
            root.Q<Button>("btn-train-showmanship").SetEnabled(canTrain);
        }

        private void RefreshRecruitList()
        {
            _recruitList.itemsSource = _currentRecruits;
            _recruitList.Rebuild();
        }

        private void RefreshComposition()
        {
            _compositionContent.Clear();

            var composition = _bandManager.GetCurrentComposition();

            var familyInfo = new (InstrumentFamily family, string label, string cssClass)[]
            {
                (InstrumentFamily.Brass, "Brass", "brass"),
                (InstrumentFamily.Woodwind, "Woodwind", "woodwind"),
                (InstrumentFamily.BatteryPercussion, "Percussion", "percussion"),
                (InstrumentFamily.FrontEnsemble, "Front Ensemble", "front-ensemble"),
                (InstrumentFamily.ColorGuard, "Color Guard", "color-guard"),
                (InstrumentFamily.Leadership, "Leadership", "leadership")
            };

            int maxCount = composition.FamilyCounts.Values.DefaultIfEmpty(1).Max();
            maxCount = Mathf.Max(maxCount, 1);

            foreach (var (family, label, cssClass) in familyInfo)
            {
                int count = composition.FamilyCounts.GetValueOrDefault(family, 0);
                float pct = count / (float)maxCount;

                var row = new VisualElement();
                row.AddToClassList("section-row");

                var lblName = new Label(label);
                lblName.AddToClassList("section-label");

                var barBg = new VisualElement();
                barBg.AddToClassList("section-bar-bg");

                var barFill = new VisualElement();
                barFill.AddToClassList("section-bar-fill");
                barFill.AddToClassList(cssClass);
                barFill.style.width = new StyleLength(new Length(pct * 100f, LengthUnit.Percent));

                barBg.Add(barFill);

                var lblCount = new Label(count.ToString());
                lblCount.AddToClassList("section-count");

                row.Add(lblName);
                row.Add(barBg);
                row.Add(lblCount);

                _compositionContent.Add(row);
            }

            // Balance score
            var balanceRow = new VisualElement();
            balanceRow.AddToClassList("balance-row");

            var balanceLabel = new Label("Balance:");
            balanceLabel.AddToClassList("balance-label");

            int balancePct = Mathf.RoundToInt(composition.BalanceScore * 100);
            var balanceValue = new Label($"{balancePct}%");
            balanceValue.AddToClassList("balance-value");
            balanceValue.AddToClassList(balancePct >= 70 ? "rating-high" :
                balancePct >= 40 ? "rating-mid" : "rating-low");

            balanceRow.Add(balanceLabel);
            balanceRow.Add(balanceValue);
            _compositionContent.Add(balanceRow);

            // Warnings
            if (composition.Warnings.Count > 0)
            {
                var warningsContainer = new VisualElement();
                warningsContainer.AddToClassList("warnings-container");

                foreach (var warning in composition.Warnings)
                {
                    var wLabel = new Label($"! {warning}");
                    wLabel.AddToClassList("warning-text");
                    warningsContainer.Add(wLabel);
                }

                _compositionContent.Add(warningsContainer);
            }
        }

        // =================================================================
        // Event Handlers
        // =================================================================

        private void SetFilter(InstrumentFamily? family)
        {
            _activeFilter = family;

            foreach (var btn in _filterButtons)
                btn.RemoveFromClassList("active");

            // Find and highlight the active tab
            string targetName = family switch
            {
                null => "filter-all",
                InstrumentFamily.Brass => "filter-brass",
                InstrumentFamily.Woodwind => "filter-woodwind",
                InstrumentFamily.BatteryPercussion => "filter-percussion",
                InstrumentFamily.ColorGuard => "filter-guard",
                InstrumentFamily.FrontEnsemble => "filter-pit",
                InstrumentFamily.Leadership => "filter-leadership",
                _ => "filter-all"
            };

            var activeBtn = _filterButtons.FirstOrDefault(b => b.name == targetName);
            activeBtn?.AddToClassList("active");

            _selectedMember = null;
            RefreshRosterList();
            RefreshDetailPanel();
        }

        private void OnRosterSelectionChanged(IEnumerable<object> selection)
        {
            var selected = selection.FirstOrDefault();
            if (selected is BandMemberData member)
            {
                _selectedMember = member;
                RefreshDetailPanel();
            }
        }

        private void OnScoutRecruits()
        {
            _currentRecruits = _bandManager.GetAvailableRecruits(ScoutCount);
            RefreshRecruitList();
            SetStatus($"Scouted {ScoutCount} potential recruits");
        }

        private void OnRecruitCandidate(int index)
        {
            if (index < 0 || index >= _currentRecruits.Count) return;

            var candidate = _currentRecruits[index];
            var result = _bandManager.RecruitMember(candidate);

            if (result != null)
            {
                _currentRecruits.RemoveAt(index);
                RefreshRecruitList();
                SetStatus($"Recruited {result.DisplayName} ({FormatInstrumentName(result.AssignedInstrument)})");
            }
            else
            {
                SetStatus("Cannot recruit: insufficient budget");
            }
        }

        private void OnAssignInstrument()
        {
            if (_selectedMember == null || _ddInstrument.value == null) return;

            var instrumentType = ParseInstrumentName(_ddInstrument.value);
            _bandManager.AssignInstrument(_selectedMember.Id, instrumentType);
            SetStatus($"Assigned {_selectedMember.DisplayName} to {FormatInstrumentName(instrumentType)}");
        }

        private void OnDismissMember()
        {
            if (_selectedMember == null) return;

            string name = _selectedMember.DisplayName;
            _bandManager.DismissMember(_selectedMember.Id);
            _selectedMember = null;
            SetStatus($"Dismissed {name}");
        }

        private void OnTrainSkill(SkillType skill)
        {
            if (_selectedMember == null) return;
            if (_bandManager.Roster == null || _bandManager.Roster.Budget < TrainingCost) return;

            _bandManager.Roster.Budget -= TrainingCost;

            // Diminishing returns: less improvement at higher skill levels
            float currentSkill = _selectedMember.GetSkill(skill);
            float effectiveAmount = TrainingAmount * (1f - currentSkill * 0.5f);

            _bandManager.TrainMember(_selectedMember.Id, skill, effectiveAmount);
            RefreshTopBar();
            SetStatus($"Trained {_selectedMember.DisplayName}'s {skill} (+{effectiveAmount:P0})");
        }

        private void OnBack()
        {
            SaveRoster();
            SceneManager.LoadScene("MainMenu");
        }

        private void OnProceed()
        {
            SaveRoster();
            SongLibraryScreen.ReturnScene = "BandManagement";
            SceneManager.LoadScene("SongLibrary");
        }

        private async void SaveRoster()
        {
            if (_bandManager.Roster == null) return;

            if (ServiceLocator.TryGet<ISaveSystem>(out var saveSystem))
            {
                await saveSystem.SaveBandRoster(_bandManager.Roster);
                Debug.Log("BandManagementScreen: Roster saved.");
            }
        }

        // =================================================================
        // Helpers
        // =================================================================

        private static void SetSkillBar(VisualElement bar, Label valueLabel, float value)
        {
            int pct = Mathf.RoundToInt(value * 100);
            bar.style.width = new StyleLength(new Length(pct, LengthUnit.Percent));
            valueLabel.text = pct.ToString();
        }

        private void SetStatus(string message)
        {
            if (_lblStatus != null)
                _lblStatus.text = message;
            Debug.Log($"BandManagement: {message}");
        }

        private static string FormatSchoolName(string schoolId)
        {
            if (string.IsNullOrEmpty(schoolId)) return "My Band";

            // Convert IDs like "default_school" to "Default School"
            return string.Join(" ", schoolId.Split('_')
                .Select(w => w.Length > 0
                    ? char.ToUpper(w[0]) + w[1..]
                    : w));
        }

        private static string FormatInstrumentName(InstrumentType type)
        {
            return type switch
            {
                InstrumentType.FrenchHorn => "French Horn",
                InstrumentType.SnareDrum => "Snare Drum",
                InstrumentType.BassDrum => "Bass Drum",
                InstrumentType.TenorDrums => "Tenor Drums",
                InstrumentType.DrumMajor => "Drum Major",
                _ => type.ToString()
            };
        }

        private static InstrumentType ParseInstrumentName(string name)
        {
            // Reverse the formatting
            string normalized = name.Replace(" ", "");

            if (Enum.TryParse<InstrumentType>(normalized, out var result))
                return result;

            return InstrumentType.Trumpet;
        }

        private void OnDestroy()
        {
            if (_bandManager != null)
            {
                _bandManager.OnRosterChanged -= OnRosterChanged;
                _bandManager.OnMemberRecruited -= OnMemberRecruited;
                _bandManager.OnMemberUpdated -= OnMemberUpdated;
                _bandManager.OnMemberDismissed -= OnMemberDismissed;
            }
        }
    }
}
