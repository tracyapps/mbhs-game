using System.Collections.Generic;
using System.Linq;
using MBHS.Core;
using MBHS.Data.Enums;
using MBHS.Data.Models;
using MBHS.Systems.ContentPipeline;
using MBHS.Systems.FormationEditor;
using MBHS.Systems.MusicConductor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace MBHS.UI.Screens
{
    [RequireComponent(typeof(UIDocument))]
    public class SongLibraryScreen : MonoBehaviour
    {
        public static string SelectedSongId;
        public static string ReturnScene = "MainMenu";

        private UIDocument _document;
        private IContentCatalog _contentCatalog;
        private IMusicConductor _musicConductor;

        // Song data
        private List<ContentManifestEntry> _allSongs = new();
        private List<ContentManifestEntry> _filteredSongs = new();
        private readonly Dictionary<string, SongData> _songDataCache = new();
        private ContentManifestEntry _selectedEntry;
        private SongData _selectedSongData;

        // Filtering
        private string _searchQuery = "";
        private string _activeTag;
        private readonly List<Button> _tagButtons = new();

        // Top bar
        private Button _btnSelect;

        // Song list
        private ListView _songList;
        private Label _lblSongCount;
        private TextField _txtSearch;

        // Category tabs
        private Button _tabBuiltin;
        private Button _tabMarketplace;

        // Preview panel
        private VisualElement _previewEmpty;
        private VisualElement _previewView;
        private Label _lblPreviewTitle;
        private Label _lblPreviewComposer;
        private Label _lblPreviewArranger;
        private VisualElement _barDifficulty;
        private Label _lblDifficulty;
        private Label _lblBpm;
        private Label _lblTimeSig;
        private Label _lblDuration;
        private VisualElement _tagsContainer;
        private Label _lblDescription;

        // Player
        private Button _btnPlay;
        private VisualElement _playerProgressFill;
        private Label _lblPlayerTime;
        private Label _lblPlayerStatus;
        private bool _isPreviewPlaying;

        // Stems
        private VisualElement _stemsContainer;

        // Status
        private Label _lblStatus;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private async void Start()
        {
            if (!ServiceLocator.TryGet(out _contentCatalog))
            {
                Debug.LogError("SongLibraryScreen: IContentCatalog not registered.");
                return;
            }

            ServiceLocator.TryGet(out _musicConductor);

            BindUI();
            SetupSongList();

            _allSongs = await _contentCatalog.GetSongs();

            // Pre-load all song metadata for instant list display
            foreach (var entry in _allSongs)
            {
                var songData = await _contentCatalog.LoadSongDataAsync(entry.Id);
                if (songData != null)
                    _songDataCache[entry.Id] = songData;
            }

            ApplyFilters();

            _btnSelect.SetEnabled(false);
            _tabMarketplace.SetEnabled(false);

            SetStatus($"Loaded {_allSongs.Count} songs");
        }

        private void Update()
        {
            if (_isPreviewPlaying && _musicConductor != null && _selectedSongData != null)
            {
                float progress = _musicConductor.SongProgress;
                _playerProgressFill.style.width =
                    new StyleLength(new Length(progress * 100f, LengthUnit.Percent));

                float elapsed = _selectedSongData.DurationSeconds * progress;
                _lblPlayerTime.text = FormatDuration(elapsed);

                if (!_musicConductor.IsPlaying && !_musicConductor.IsPaused)
                    OnPreviewComplete();
            }
        }

        // =================================================================
        // UI Binding
        // =================================================================

        private void BindUI()
        {
            var root = _document.rootVisualElement;

            // Top bar
            root.Q<Button>("btn-back").clicked += OnBack;
            _btnSelect = root.Q<Button>("btn-select");
            _btnSelect.clicked += OnSelectSong;

            // Category tabs
            _tabBuiltin = root.Q<Button>("tab-builtin");
            _tabMarketplace = root.Q<Button>("tab-marketplace");
            _tabBuiltin.clicked += () => OnCategoryTab("builtin");
            _tabMarketplace.clicked += () => OnCategoryTab("marketplace");

            // Search
            _txtSearch = root.Q<TextField>("txt-search");
            _txtSearch.RegisterValueChangedCallback(evt =>
            {
                _searchQuery = evt.newValue ?? "";
                ApplyFilters();
            });

            // Tag filters
            SetupTagFilters(root);

            // Song list
            _songList = root.Q<ListView>("song-list");
            _lblSongCount = root.Q<Label>("lbl-song-count");

            // Preview
            _previewEmpty = root.Q("preview-empty");
            _previewView = root.Q("preview-view");
            _lblPreviewTitle = root.Q<Label>("lbl-preview-title");
            _lblPreviewComposer = root.Q<Label>("lbl-preview-composer");
            _lblPreviewArranger = root.Q<Label>("lbl-preview-arranger");
            _barDifficulty = root.Q("bar-difficulty");
            _lblDifficulty = root.Q<Label>("lbl-difficulty");
            _lblBpm = root.Q<Label>("lbl-bpm");
            _lblTimeSig = root.Q<Label>("lbl-time-sig");
            _lblDuration = root.Q<Label>("lbl-duration");
            _tagsContainer = root.Q("tags-container");
            _lblDescription = root.Q<Label>("lbl-description");

            // Player
            _btnPlay = root.Q<Button>("btn-play");
            _btnPlay.clicked += OnTogglePreview;
            _playerProgressFill = root.Q("player-progress-fill");
            _lblPlayerTime = root.Q<Label>("lbl-player-time");
            _lblPlayerStatus = root.Q<Label>("lbl-player-status");

            // Stems
            _stemsContainer = root.Q("stems-container");

            // Status
            _lblStatus = root.Q<Label>("lbl-status");
        }

        private void SetupTagFilters(VisualElement root)
        {
            var tagMap = new (string name, string tag)[]
            {
                ("tag-all", null),
                ("tag-march", "march"),
                ("tag-patriotic", "patriotic"),
                ("tag-classic", "classic"),
                ("tag-military", "military")
            };

            foreach (var (name, tag) in tagMap)
            {
                var btn = root.Q<Button>(name);
                if (btn == null) continue;

                _tagButtons.Add(btn);
                var capturedTag = tag;
                btn.clicked += () => SetTagFilter(capturedTag);
            }
        }

        // =================================================================
        // Song ListView
        // =================================================================

        private void SetupSongList()
        {
            _songList.makeItem = () =>
            {
                var row = new VisualElement();
                row.AddToClassList("song-row");

                var info = new VisualElement();
                info.AddToClassList("song-row-info");

                var title = new Label { name = "title" };
                title.AddToClassList("song-row-title");

                var composer = new Label { name = "composer" };
                composer.AddToClassList("song-row-composer");

                info.Add(title);
                info.Add(composer);

                var meta = new VisualElement();
                meta.AddToClassList("song-row-meta");

                var difficulty = new Label { name = "difficulty" };
                difficulty.AddToClassList("song-row-difficulty");

                var duration = new Label { name = "duration" };
                duration.AddToClassList("song-row-duration");

                meta.Add(difficulty);
                meta.Add(duration);

                row.Add(info);
                row.Add(meta);

                return row;
            };

            _songList.bindItem = (element, index) =>
            {
                if (index >= _filteredSongs.Count) return;
                var entry = _filteredSongs[index];

                element.Q<Label>("title").text = entry.Title;
                element.Q<Label>("composer").text = entry.Author;

                if (_songDataCache.TryGetValue(entry.Id, out var songData))
                {
                    element.Q<Label>("difficulty").text = $"{songData.Difficulty}/10";
                    element.Q<Label>("duration").text = FormatDuration(songData.DurationSeconds);
                }
                else
                {
                    element.Q<Label>("difficulty").text = "";
                    element.Q<Label>("duration").text = "";
                }
            };

            _songList.selectionChanged += OnSongSelectionChanged;
            _songList.fixedItemHeight = 52;
            _songList.selectionType = SelectionType.Single;
        }

        // =================================================================
        // Filtering
        // =================================================================

        private void ApplyFilters()
        {
            var results = _allSongs.AsEnumerable();

            if (!string.IsNullOrEmpty(_searchQuery))
            {
                var query = _searchQuery.ToLowerInvariant();
                results = results.Where(e =>
                    e.Title.ToLowerInvariant().Contains(query) ||
                    e.Author.ToLowerInvariant().Contains(query) ||
                    (e.Description != null &&
                     e.Description.ToLowerInvariant().Contains(query)));
            }

            if (!string.IsNullOrEmpty(_activeTag))
            {
                results = results.Where(e =>
                    e.Tags != null && e.Tags.Contains(_activeTag));
            }

            _filteredSongs = results.ToList();
            _lblSongCount.text = $"{_filteredSongs.Count} songs";

            _songList.itemsSource = _filteredSongs;
            _songList.Rebuild();
        }

        private void SetTagFilter(string tag)
        {
            _activeTag = tag;

            foreach (var btn in _tagButtons)
                btn.RemoveFromClassList("active");

            string targetName = tag == null ? "tag-all" : $"tag-{tag}";
            var activeBtn = _tagButtons.FirstOrDefault(b => b.name == targetName);
            activeBtn?.AddToClassList("active");

            ApplyFilters();
        }

        // =================================================================
        // Song Selection & Preview
        // =================================================================

        private void OnSongSelectionChanged(IEnumerable<object> selection)
        {
            var selected = selection.FirstOrDefault();
            if (selected is ContentManifestEntry entry)
            {
                _selectedEntry = entry;
                _btnSelect.SetEnabled(true);
                LoadSongPreview(entry);
            }
        }

        private async void LoadSongPreview(ContentManifestEntry entry)
        {
            StopPreview();

            if (_songDataCache.TryGetValue(entry.Id, out var cached))
            {
                _selectedSongData = cached;
            }
            else
            {
                _selectedSongData = await _contentCatalog.LoadSongDataAsync(entry.Id);
            }

            if (_selectedSongData == null)
            {
                SetStatus($"Could not load song data for {entry.Title}");
                _previewEmpty.style.display = DisplayStyle.Flex;
                _previewView.style.display = DisplayStyle.None;
                return;
            }

            _previewEmpty.style.display = DisplayStyle.None;
            _previewView.style.display = DisplayStyle.Flex;

            RefreshPreviewPanel();
            SetStatus($"Selected: {entry.Title}");
        }

        private void RefreshPreviewPanel()
        {
            if (_selectedSongData == null) return;

            var s = _selectedSongData;

            _lblPreviewTitle.text = s.Title;
            _lblPreviewComposer.text = s.Composer;
            _lblPreviewArranger.text = !string.IsNullOrEmpty(s.Arranger)
                ? $"Arr. {s.Arranger}"
                : "";

            // Difficulty bar
            int diffPct = Mathf.RoundToInt(s.Difficulty / 10f * 100f);
            _barDifficulty.style.width =
                new StyleLength(new Length(diffPct, LengthUnit.Percent));
            _barDifficulty.style.backgroundColor =
                new StyleColor(GetDifficultyColor(s.Difficulty));
            _lblDifficulty.text = $"{s.Difficulty}/10";

            // Metadata
            _lblBpm.text = s.BPM.ToString("F0");
            _lblTimeSig.text = $"{s.BeatsPerMeasure}/{s.BeatUnit}";
            _lblDuration.text = FormatDuration(s.DurationSeconds);

            // Tags
            _tagsContainer.Clear();
            if (_selectedEntry?.Tags != null)
            {
                foreach (var tag in _selectedEntry.Tags)
                {
                    var tagLabel = new Label(tag);
                    tagLabel.AddToClassList("song-tag");
                    _tagsContainer.Add(tagLabel);
                }
            }

            // Description
            _lblDescription.text = _selectedEntry?.Description ?? "";

            // Stem toggles
            _stemsContainer.Clear();
            if (s.Stems != null && s.Stems.Count > 0)
            {
                foreach (var stem in s.Stems)
                {
                    var row = new VisualElement();
                    row.AddToClassList("stem-row");

                    var toggle = new Toggle { value = true };
                    toggle.AddToClassList("stem-toggle");

                    var capturedFamily = stem.Family;
                    toggle.RegisterValueChangedCallback(evt =>
                        OnStemToggle(capturedFamily, evt.newValue));

                    var label = new Label(FormatFamilyName(stem.Family));
                    label.AddToClassList("stem-label");

                    row.Add(toggle);
                    row.Add(label);
                    _stemsContainer.Add(row);
                }
            }

            // Player state
            _btnPlay.SetEnabled(_musicConductor != null);
            _lblPlayerStatus.text = _musicConductor != null
                ? "No audio files yet - beat simulation only"
                : "Music system unavailable";

            _playerProgressFill.style.width =
                new StyleLength(new Length(0, LengthUnit.Percent));
            _lblPlayerTime.text = "0:00";
            _btnPlay.text = "\u25B6";
        }

        // =================================================================
        // Preview Player
        // =================================================================

        private void OnTogglePreview()
        {
            if (_musicConductor == null || _selectedSongData == null) return;

            if (_isPreviewPlaying)
            {
                if (_musicConductor.IsPaused)
                {
                    _musicConductor.Resume();
                    _btnPlay.text = "\u23F8";
                }
                else
                {
                    _musicConductor.Pause();
                    _btnPlay.text = "\u25B6";
                }
            }
            else
            {
                StartPreview();
            }
        }

        private void StartPreview()
        {
            if (_musicConductor == null || _selectedSongData == null) return;

            _musicConductor.LoadSong(_selectedSongData);
            _musicConductor.Play();
            _isPreviewPlaying = true;
            _btnPlay.text = "\u23F8";
            _lblPlayerStatus.text = "Playing (beat simulation)";
        }

        private void StopPreview()
        {
            if (_musicConductor == null || !_isPreviewPlaying) return;

            _musicConductor.Stop();
            _musicConductor.UnloadSong();
            _isPreviewPlaying = false;
            _btnPlay.text = "\u25B6";
        }

        private void OnPreviewComplete()
        {
            _isPreviewPlaying = false;
            _btnPlay.text = "\u25B6";
            _lblPlayerStatus.text = "Preview complete";
        }

        private void OnStemToggle(InstrumentFamily family, bool enabled)
        {
            if (_musicConductor == null) return;

            if (enabled)
                _musicConductor.UnmuteStem(family);
            else
                _musicConductor.MuteStem(family);
        }

        // =================================================================
        // Navigation
        // =================================================================

        private void OnBack()
        {
            StopPreview();
            SceneManager.LoadScene(ReturnScene);
        }

        private void OnSelectSong()
        {
            if (_selectedEntry == null) return;

            SelectedSongId = _selectedEntry.Id;
            FormationEditorView.IncomingSongId = _selectedEntry.Id;
            StopPreview();

            Debug.Log($"SongLibrary: Selected '{_selectedEntry.Title}' (Id: {SelectedSongId})");
            SceneManager.LoadScene("FormationEditor");
        }

        private void OnCategoryTab(string category)
        {
            _tabBuiltin.RemoveFromClassList("active");
            _tabMarketplace.RemoveFromClassList("active");

            if (category == "builtin")
            {
                _tabBuiltin.AddToClassList("active");
                ApplyFilters();
            }
            else
            {
                _tabMarketplace.AddToClassList("active");
                SetStatus("Marketplace coming soon!");
            }
        }

        // =================================================================
        // Helpers
        // =================================================================

        private static string FormatDuration(float seconds)
        {
            int mins = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            return $"{mins}:{secs:D2}";
        }

        private static Color GetDifficultyColor(int difficulty)
        {
            float t = (difficulty - 1) / 9f;
            if (t < 0.5f)
                return Color.Lerp(
                    new Color(0.3f, 0.8f, 0.5f),
                    new Color(0.9f, 0.8f, 0.3f),
                    t * 2f);
            return Color.Lerp(
                new Color(0.9f, 0.8f, 0.3f),
                new Color(0.8f, 0.3f, 0.3f),
                (t - 0.5f) * 2f);
        }

        private static string FormatFamilyName(InstrumentFamily family)
        {
            return family switch
            {
                InstrumentFamily.BatteryPercussion => "Battery Percussion",
                InstrumentFamily.FrontEnsemble => "Front Ensemble",
                InstrumentFamily.ColorGuard => "Color Guard",
                _ => family.ToString()
            };
        }

        private void SetStatus(string message)
        {
            if (_lblStatus != null)
                _lblStatus.text = message;
            Debug.Log($"SongLibrary: {message}");
        }

        private void OnDestroy()
        {
            StopPreview();
        }
    }
}
