using System;
using System.Linq;
using MBHS.Core;
using MBHS.Data.Enums;
using MBHS.Data.Models;
using MBHS.Systems.BandManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace MBHS.UI.Screens
{
    [RequireComponent(typeof(UIDocument))]
    public class MemberProfileScreen : MonoBehaviour
    {
        public static string IncomingMemberId;
        public static string ReturnScene = "FormationEditor";

        [SerializeField] private UIDocument _uiDocument;

        private IBandManager _bandManager;
        private BandMemberData _member;

        private void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();
        }

        private void Start()
        {
            _bandManager = ServiceLocator.Get<IBandManager>();

            if (string.IsNullOrEmpty(IncomingMemberId))
            {
                Debug.LogWarning("MemberProfileScreen: No IncomingMemberId set.");
                return;
            }

            _member = _bandManager.Roster?.GetMemberById(IncomingMemberId);
            if (_member == null)
            {
                Debug.LogWarning($"MemberProfileScreen: Member '{IncomingMemberId}' not found.");
                return;
            }

            IncomingMemberId = null;
            BindUI();
        }

        private void BindUI()
        {
            var root = _uiDocument.rootVisualElement;

            // Back button
            var backBtn = root.Q<Button>("btn-back");
            backBtn?.RegisterCallback<ClickEvent>(_ => SceneManager.LoadScene(ReturnScene));

            // Avatar (large)
            var avatarHost = root.Q<VisualElement>("profile-avatar");
            if (avatarHost != null)
            {
                var avatar = new MBHS.Systems.FormationEditor.MemberAvatarElement();
                avatar.style.width = 80;
                avatar.style.height = 80;
                avatar.style.position = Position.Absolute;
                avatar.style.left = 0;
                avatar.style.top = 0;
                avatar.SetMember(_member.DisplayName, _member.AssignedInstrument);
                avatar.UpdateInitialLabel();

                // Override initial label size for large avatar
                var initial = avatar.Q<Label>("avatar-initial");
                if (initial != null)
                    initial.style.fontSize = 32;

                avatarHost.Add(avatar);
            }

            // Name
            var fullName = root.Q<Label>("profile-full-name");
            if (fullName != null)
                fullName.text = $"{_member.FirstName} {_member.LastName}";

            // Nickname
            var nicknameField = root.Q<TextField>("profile-nickname");
            if (nicknameField != null)
            {
                nicknameField.value = _member.Nickname ?? "";
                nicknameField.RegisterValueChangedCallback(evt =>
                {
                    string nickname = evt.newValue?.Trim();
                    _bandManager.SetNickname(_member.Id,
                        string.IsNullOrEmpty(nickname) ? null : nickname);
                });
            }

            // Year
            var yearLabel = root.Q<Label>("profile-year");
            if (yearLabel != null)
            {
                yearLabel.text = _member.YearInSchool switch
                {
                    1 => "Freshman",
                    2 => "Sophomore",
                    3 => "Junior",
                    4 => "Senior",
                    _ => $"Year {_member.YearInSchool}"
                };
            }

            // Status
            var statusLabel = root.Q<Label>("profile-status");
            if (statusLabel != null)
                statusLabel.text = _member.Status.ToString();

            // Instrument
            var instrLabel = root.Q<Label>("profile-instrument-label");
            if (instrLabel != null)
                instrLabel.text = FormatInstrumentName(_member.AssignedInstrument);

            // Instrument dropdown
            var instrDropdown = root.Q<DropdownField>("profile-instrument-dropdown");
            if (instrDropdown != null)
            {
                var instrumentNames = Enum.GetValues(typeof(InstrumentType))
                    .Cast<InstrumentType>()
                    .Select(i => FormatInstrumentName(i))
                    .ToList();

                instrDropdown.choices = instrumentNames;
                instrDropdown.value = FormatInstrumentName(_member.AssignedInstrument);

                instrDropdown.RegisterValueChangedCallback(evt =>
                {
                    var allInstruments = Enum.GetValues(typeof(InstrumentType))
                        .Cast<InstrumentType>().ToArray();
                    int idx = instrumentNames.IndexOf(evt.newValue);
                    if (idx >= 0 && idx < allInstruments.Length)
                    {
                        _bandManager.AssignInstrument(_member.Id, allInstruments[idx]);
                        instrLabel.text = evt.newValue;
                    }
                });
            }

            // Skills
            SetSkillBar(root, "bar-musicianship", "val-musicianship", _member.Musicianship);
            SetSkillBar(root, "bar-marching", "val-marching", _member.Marching);
            SetSkillBar(root, "bar-stamina", "val-stamina", _member.Stamina);
            SetSkillBar(root, "bar-showmanship", "val-showmanship", _member.Showmanship);

            // Stats
            var overallLabel = root.Q<Label>("stat-overall");
            if (overallLabel != null)
                overallLabel.text = Mathf.RoundToInt(_member.OverallRating * 100).ToString();

            var moraleLabel = root.Q<Label>("stat-morale");
            if (moraleLabel != null)
                moraleLabel.text = _member.Morale.ToString();

            var xpLabel = root.Q<Label>("stat-xp");
            if (xpLabel != null)
                xpLabel.text = _member.Experience.ToString();
        }

        private static void SetSkillBar(VisualElement root, string barName, string valName, float value)
        {
            var bar = root.Q<VisualElement>(barName);
            if (bar != null)
            {
                float pct = Mathf.Clamp01(value) * 100f;
                bar.style.width = new StyleLength(new Length(pct, LengthUnit.Percent));
            }

            var val = root.Q<Label>(valName);
            if (val != null)
                val.text = Mathf.RoundToInt(value * 100).ToString();
        }

        private static string FormatInstrumentName(InstrumentType type)
        {
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
    }
}
