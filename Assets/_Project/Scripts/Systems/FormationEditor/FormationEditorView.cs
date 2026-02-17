using MBHS.Core;
using MBHS.Systems.BandManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace MBHS.Systems.FormationEditor
{
    [RequireComponent(typeof(UIDocument))]
    public class FormationEditorView : MonoBehaviour
    {
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
        private IFormationSystem _formationSystem;
        private IBandManager _bandManager;

        private void Awake()
        {
            if (_uiDocument == null)
                _uiDocument = GetComponent<UIDocument>();
        }

        private void Start()
        {
            _formationSystem = ServiceLocator.Get<IFormationSystem>();
            _bandManager = ServiceLocator.Get<IBandManager>();

            SetupPreviewCamera();
            SetupFieldRenderer();
            SetupController();
            CreateStarterContent();
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
