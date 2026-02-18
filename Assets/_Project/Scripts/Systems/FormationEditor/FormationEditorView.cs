using MBHS.Core;
using MBHS.Systems.BandManagement;
using MBHS.Systems.ContentPipeline;
using MBHS.Systems.MusicConductor;
using MBHS.Systems.SaveLoad;
using UnityEngine;
using UnityEngine.UIElements;

namespace MBHS.Systems.FormationEditor
{
    [RequireComponent(typeof(UIDocument))]
    public class FormationEditorView : MonoBehaviour
    {
        // Set by SongLibraryScreen before navigating to this scene
        public static string IncomingSongId;
        public static string ReturnScene = "FormationEditor";

        [Header("UI")]
        [SerializeField] private UIDocument _uiDocument;

        [Header("3D Preview")]
        [SerializeField] private Camera _previewCamera;
        [SerializeField] private RenderTexture _previewRenderTexture;
        [SerializeField] private FieldRenderer _fieldRenderer;

        [Header("Preview Camera Settings")]
        [SerializeField] private Vector3 _previewCameraPosition = new(0f, 60f, -30f);
        [SerializeField] private Vector3 _previewCameraRotation = new(60f, 0f, 0f);

        private FormationEditorController _controller;
        private BandMemberPanelController _memberPanel;
        private IFormationSystem _formationSystem;
        private IBandManager _bandManager;
        private IMusicConductor _musicConductor;
        private IContentCatalog _contentCatalog;
        private TimelinePanel _timelinePanel;

        private void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();
        }

        private void Start()
        {
            _formationSystem = ServiceLocator.Get<IFormationSystem>();
            _bandManager = ServiceLocator.Get<IBandManager>();
            ServiceLocator.TryGet(out _musicConductor);
            ServiceLocator.TryGet(out _contentCatalog);

            SetupPreviewCamera();
            SetupFieldRenderer();
            SetupController();
            CreateStarterContent();
            SetupTabSwitching();
            SetupMemberPanel();
            SetupTimeline();
        }

        private void SetupPreviewCamera()
        {
            if (_previewCamera == null)
            {
                // Create a preview camera if not assigned
                var camObj = new GameObject("FormationPreviewCamera");
                camObj.transform.SetParent(transform);
                _previewCamera = camObj.AddComponent<Camera>();
                _previewCamera.clearFlags = CameraClearFlags.SolidColor;
                _previewCamera.backgroundColor = new Color(0.12f, 0.12f, 0.12f);
            }

            _previewCamera.transform.localPosition = _previewCameraPosition;
            _previewCamera.transform.localEulerAngles = _previewCameraRotation;

            // Create render texture if not assigned
            if (_previewRenderTexture == null)
            {
                _previewRenderTexture = new RenderTexture(512, 256, 16);
                _previewRenderTexture.name = "FormationPreview_RT";
            }

            _previewCamera.targetTexture = _previewRenderTexture;

            // Assign render texture to the UI Image
            var root = _uiDocument.rootVisualElement;
            var previewImage = root.Q<VisualElement>("preview-image");
            if (previewImage != null)
            {
                previewImage.style.backgroundImage =
                    new StyleBackground(Background.FromRenderTexture(_previewRenderTexture));
            }
        }

        private void SetupFieldRenderer()
        {
            if (_fieldRenderer == null)
            {
                var rendererObj = new GameObject("FieldRenderer");
                rendererObj.transform.SetParent(transform);
                _fieldRenderer = rendererObj.AddComponent<FieldRenderer>();
            }

            _fieldRenderer.Initialize(_bandManager);
        }

        private void SetupController()
        {
            _controller = new FormationEditorController(
                _formationSystem,
                _bandManager,
                _uiDocument);

            // When the 2D editor updates, refresh the 3D preview
            _controller.OnFormationDisplayChanged += () =>
            {
                var positions = _controller.GetCurrentPositions();
                _fieldRenderer.UpdatePositions(positions);
            };

            // Set up overlays (templates, load chart, confirmation dialog)
            ServiceLocator.TryGet<ISaveSystem>(out var saveSystem);
            if (_contentCatalog != null && saveSystem != null)
            {
                var root = _uiDocument.rootVisualElement;
                _controller.SetupOverlays(root, _contentCatalog, saveSystem);
            }
        }

        private void SetupTabSwitching()
        {
            var root = _uiDocument.rootVisualElement;
            var tabFormations = root.Q<Button>("tab-formations");
            var tabMembers = root.Q<Button>("tab-members");
            var formationsContent = root.Q<VisualElement>("formations-tab-content");
            var membersContent = root.Q<VisualElement>("members-tab-content");

            if (tabFormations == null || tabMembers == null) return;

            tabFormations.clicked += () =>
            {
                tabFormations.AddToClassList("active");
                tabMembers.RemoveFromClassList("active");
                if (formationsContent != null)
                    formationsContent.style.display = DisplayStyle.Flex;
                if (membersContent != null)
                    membersContent.style.display = DisplayStyle.None;
            };

            tabMembers.clicked += () =>
            {
                tabMembers.AddToClassList("active");
                tabFormations.RemoveFromClassList("active");
                if (membersContent != null)
                    membersContent.style.display = DisplayStyle.Flex;
                if (formationsContent != null)
                    formationsContent.style.display = DisplayStyle.None;

                // Refresh member list when switching to Members tab
                _memberPanel?.RefreshMemberList();
            };
        }

        private void SetupMemberPanel()
        {
            var root = _uiDocument.rootVisualElement;
            var membersContent = root.Q<VisualElement>("members-tab-content");
            if (membersContent == null)
            {
                Debug.LogWarning("FormationEditorView: members-tab-content not found in UXML.");
                return;
            }

            _memberPanel = new BandMemberPanelController(_controller, _bandManager, membersContent);
        }

        private void SetupTimeline()
        {
            var root = _uiDocument.rootVisualElement;
            var timelineHost = root.Q<VisualElement>("timeline-host");
            if (timelineHost == null)
            {
                Debug.LogWarning("FormationEditorView: timeline-host not found in UXML.");
                return;
            }

            _timelinePanel = new TimelinePanel(
                _formationSystem, _musicConductor,
                _controller.CommandHistory, _contentCatalog);
            timelineHost.Add(_timelinePanel);

            // When the timeline changes the song, reload MusicConductor
            _timelinePanel.OnSongChanged += OnTimelineSongChanged;

            // When the playhead moves (scrub or playback), show interpolated positions
            _timelinePanel.OnPlayheadMoved += OnPlayheadMoved;

            // Load song data if arriving from SongLibrary
            if (!string.IsNullOrEmpty(IncomingSongId))
            {
                LoadSongForTimeline(IncomingSongId);
                IncomingSongId = null;
            }
        }

        private async void LoadSongForTimeline(string songId)
        {
            if (_contentCatalog == null) return;

            var songData = await _contentCatalog.LoadSongDataAsync(songId);
            if (songData != null)
            {
                _musicConductor?.LoadSong(songData);
                _timelinePanel?.LoadSongData(songData);
            }
        }

        private void OnPlayheadMoved(float beat)
        {
            var interpolated = _formationSystem.GetInterpolatedPositions(beat);
            _controller.ShowInterpolatedPositions(interpolated);

            // Also update the 3D preview with interpolated positions
            _fieldRenderer?.UpdatePositions(interpolated);
        }

        private async void OnTimelineSongChanged(string songId)
        {
            if (_contentCatalog == null || string.IsNullOrEmpty(songId)) return;

            var songData = await _contentCatalog.LoadSongDataAsync(songId);
            if (songData != null)
                _musicConductor?.LoadSong(songData);
        }

        private void Update()
        {
            _timelinePanel?.Update(Time.deltaTime);
        }

        private void CreateStarterContent()
        {
            // If no roster exists, create a starter band for testing
            if (_bandManager.Roster == null || _bandManager.Roster.Members.Count == 0)
            {
                _bandManager.CreateNewRoster("starter_school");

                // Auto-recruit some members for testing
                var recruits = _bandManager.GetAvailableRecruits(24);
                foreach (var recruit in recruits)
                {
                    // Give free budget for testing
                    if (_bandManager.Roster.Budget < recruit.RecruitCost)
                        _bandManager.Roster.Budget += recruit.RecruitCost;

                    _bandManager.RecruitMember(recruit);
                }

                Debug.Log($"FormationEditorView: Created starter band with " +
                          $"{_bandManager.Roster.Members.Count} members");
            }

            // If no chart exists, create one
            if (_formationSystem.ActiveChart == null)
            {
                _formationSystem.CreateNewChart("My First Show", "");
                _formationSystem.AddFormation(0f, 16f, "Opening Set");
                _formationSystem.SetCurrentFormation(0);

                Debug.Log("FormationEditorView: Created starter drill chart");
            }

            _controller.RefreshFormationList();
        }

        private void OnDestroy()
        {
            if (_previewRenderTexture != null && !_previewRenderTexture.IsCreated())
                return;

            // Clean up dynamically created render texture
            if (_previewCamera != null)
                _previewCamera.targetTexture = null;
        }
    }
}
