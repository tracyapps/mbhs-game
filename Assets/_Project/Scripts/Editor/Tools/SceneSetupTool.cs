using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.IO;

namespace MBHS.Editor.Tools
{
    public static class SceneSetupTool
    {
        private const string ScenesPath = "Assets/_Project/Scenes";
        private const string SettingsPath = "Assets/_Project/Settings";
        private const string PrefabsPath = "Assets/_Project/Prefabs/Systems";

        [MenuItem("MBHS/Setup/Create All Scenes", false, 100)]
        public static void CreateAllScenes()
        {
            if (!EditorUtility.DisplayDialog("Create All Scenes",
                "This will create all game scenes and add them to Build Settings.\n\n" +
                "Existing scenes with the same names will be overwritten.\n\n" +
                "Continue?", "Create", "Cancel"))
                return;

            EnsureDirectories();

            // Create PanelSettings first so scenes can reference it
            CreatePanelSettingsAsset();

            // Create scenes in order
            CreateBootScene();
            CreateMainMenuScene();
            CreateBandManagementScene();
            CreateFormationEditorScene();
            CreateShowSimulationScene();

            // Set build settings
            SetupBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Done",
                "All scenes created and added to Build Settings.\n\n" +
                "Open Boot scene to start testing.", "OK");

            // Open Boot scene
            EditorSceneManager.OpenScene($"{ScenesPath}/Boot.unity");
        }

        [MenuItem("MBHS/Setup/Create Panel Settings Asset", false, 101)]
        public static void CreatePanelSettingsAsset()
        {
            EnsureDirectories();

            string path = $"{SettingsPath}/DefaultPanelSettings.asset";
            if (AssetDatabase.LoadAssetAtPath<PanelSettings>(path) != null)
            {
                Debug.Log("PanelSettings asset already exists.");
                return;
            }

            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1920, 1080);
            panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panelSettings.match = 0.5f;

            AssetDatabase.CreateAsset(panelSettings, path);
            AssetDatabase.SaveAssets();

            Debug.Log($"Created PanelSettings at {path}");
        }

        // =====================================================================
        // Scene Creators
        // =====================================================================

        private static void CreateBootScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            var camObj = new GameObject("Main Camera");
            var cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.1f, 0.1f);
            cam.tag = "MainCamera";
            camObj.AddComponent<AudioListener>();

            // Directional light
            var lightObj = new GameObject("Directional Light");
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.white;
            light.intensity = 1f;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // GameBootstrapper
            var bootstrapperObj = new GameObject("GameBootstrapper");
            // Add the component by type name since we're in the Editor assembly
            var bootstrapperType = FindType("MBHS.Runtime.GameBootstrapper");
            if (bootstrapperType != null)
                bootstrapperObj.AddComponent(bootstrapperType);
            else
                Debug.LogWarning("GameBootstrapper type not found. Add it manually.");

            SaveScene(scene, "Boot");
        }

        private static void CreateMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            var camObj = CreateDefaultCamera();

            // UI Document
            var uiObj = new GameObject("MainMenuUI");
            var uiDoc = uiObj.AddComponent<UIDocument>();

            // Assign PanelSettings if available
            AssignPanelSettings(uiDoc);

            // Assign UXML if available
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/_Project/UI/Documents/MainMenu.uxml");
            if (uxml != null)
                uiDoc.visualTreeAsset = uxml;

            // Add MainMenuController if available
            var controllerType = FindType("MBHS.UI.Screens.MainMenuScreen");
            if (controllerType != null)
                uiObj.AddComponent(controllerType);

            // Ambient light
            CreateDefaultLighting();

            SaveScene(scene, "MainMenu");
        }

        private static void CreateBandManagementScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camObj = CreateDefaultCamera();
            CreateDefaultLighting();

            // UI Document
            var uiObj = new GameObject("BandManagementUI");
            var uiDoc = uiObj.AddComponent<UIDocument>();
            AssignPanelSettings(uiDoc);

            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/_Project/UI/Documents/BandManagement.uxml");
            if (uxml != null)
                uiDoc.visualTreeAsset = uxml;

            SaveScene(scene, "BandManagement");
        }

        private static void CreateFormationEditorScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Main camera (for the 2D UI - will be overridden by UI Toolkit)
            var camObj = CreateDefaultCamera();

            CreateDefaultLighting();

            // Formation Editor root
            var editorObj = new GameObject("FormationEditor");

            var uiDoc = editorObj.AddComponent<UIDocument>();
            AssignPanelSettings(uiDoc);

            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/_Project/UI/Documents/FormationEditor.uxml");
            if (uxml != null)
                uiDoc.visualTreeAsset = uxml;

            // Add FormationEditorView
            var viewType = FindType("MBHS.Systems.FormationEditor.FormationEditorView");
            if (viewType != null)
                editorObj.AddComponent(viewType);

            // Event system for UI Toolkit input
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            // Input System module if available
            var inputModuleType = FindType("UnityEngine.InputSystem.UI.InputSystemUIInputModule");
            if (inputModuleType != null)
                eventSystem.AddComponent(inputModuleType);
            else
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            SaveScene(scene, "FormationEditor");
        }

        private static void CreateShowSimulationScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Main camera - positioned to view the field from the stands
            var camObj = new GameObject("Main Camera");
            var cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.3f, 0.5f, 0.8f); // sky blue
            cam.tag = "MainCamera";
            cam.transform.position = new Vector3(0f, 40f, -50f);
            cam.transform.rotation = Quaternion.Euler(40f, 0f, 0f);
            camObj.AddComponent<AudioListener>();

            // Stadium lighting
            var sunObj = new GameObject("Sun");
            var sun = sunObj.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.95f, 0.85f);
            sun.intensity = 1.5f;
            sunObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Fill light
            var fillObj = new GameObject("Fill Light");
            var fill = fillObj.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.6f, 0.7f, 1f);
            fill.intensity = 0.4f;
            fillObj.transform.rotation = Quaternion.Euler(30f, 150f, 0f);

            // Stadium lights (4 point lights at corners)
            CreateStadiumLight("StadiumLight_NE", new Vector3(55f, 30f, 30f));
            CreateStadiumLight("StadiumLight_NW", new Vector3(-55f, 30f, 30f));
            CreateStadiumLight("StadiumLight_SE", new Vector3(55f, 30f, -30f));
            CreateStadiumLight("StadiumLight_SW", new Vector3(-55f, 30f, -30f));

            // Show simulation UI
            var uiObj = new GameObject("ShowSimulationUI");
            var uiDoc = uiObj.AddComponent<UIDocument>();
            AssignPanelSettings(uiDoc);

            // Event system
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            var inputModuleType = FindType("UnityEngine.InputSystem.UI.InputSystemUIInputModule");
            if (inputModuleType != null)
                eventSystem.AddComponent(inputModuleType);
            else
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            SaveScene(scene, "ShowSimulation");
        }

        // =====================================================================
        // Build Settings
        // =====================================================================

        private static void SetupBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new($"{ScenesPath}/Boot.unity", true),
                new($"{ScenesPath}/MainMenu.unity", true),
                new($"{ScenesPath}/BandManagement.unity", true),
                new($"{ScenesPath}/FormationEditor.unity", true),
                new($"{ScenesPath}/ShowSimulation.unity", true)
            };

            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("Build Settings updated with 5 scenes.");
        }

        // =====================================================================
        // Helpers
        // =====================================================================

        private static void EnsureDirectories()
        {
            if (!AssetDatabase.IsValidFolder(ScenesPath))
            {
                string parent = Path.GetDirectoryName(ScenesPath).Replace('\\', '/');
                string folder = Path.GetFileName(ScenesPath);
                AssetDatabase.CreateFolder(parent, folder);
            }

            if (!AssetDatabase.IsValidFolder(SettingsPath))
            {
                string parent = Path.GetDirectoryName(SettingsPath).Replace('\\', '/');
                string folder = Path.GetFileName(SettingsPath);
                AssetDatabase.CreateFolder(parent, folder);
            }

            if (!AssetDatabase.IsValidFolder(PrefabsPath))
            {
                string parent = Path.GetDirectoryName(PrefabsPath).Replace('\\', '/');
                string folder = Path.GetFileName(PrefabsPath);
                if (!AssetDatabase.IsValidFolder(parent))
                    AssetDatabase.CreateFolder(
                        Path.GetDirectoryName(parent).Replace('\\', '/'),
                        Path.GetFileName(parent));
                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        private static void SaveScene(Scene scene, string sceneName)
        {
            string path = $"{ScenesPath}/{sceneName}.unity";
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"Created scene: {path}");
        }

        private static GameObject CreateDefaultCamera()
        {
            var camObj = new GameObject("Main Camera");
            var cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.12f, 0.15f);
            cam.tag = "MainCamera";
            camObj.AddComponent<AudioListener>();
            return camObj;
        }

        private static void CreateDefaultLighting()
        {
            var lightObj = new GameObject("Directional Light");
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.white;
            light.intensity = 1f;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static void CreateStadiumLight(string name, Vector3 position)
        {
            var obj = new GameObject(name);
            var light = obj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.97f, 0.9f);
            light.intensity = 2f;
            light.range = 80f;
            obj.transform.position = position;
        }

        private static void AssignPanelSettings(UIDocument uiDoc)
        {
            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(
                $"{SettingsPath}/DefaultPanelSettings.asset");

            if (panelSettings != null)
            {
                uiDoc.panelSettings = panelSettings;
            }
            else
            {
                Debug.LogWarning(
                    "PanelSettings not found. Run MBHS > Setup > Create Panel Settings Asset first.");
            }
        }

        private static System.Type FindType(string fullTypeName)
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullTypeName);
                if (type != null)
                    return type;
            }
            return null;
        }
    }
}
