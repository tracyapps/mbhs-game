using MBHS.Core;
using MBHS.Systems.SaveLoad;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace MBHS.UI.Screens
{
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuScreen : MonoBehaviour
    {
        private UIDocument _document;
        private Button _btnNewGame;
        private Button _btnContinue;
        private Button _btnFormationEditor;
        private Button _btnSongLibrary;
        private Button _btnSettings;
        private Button _btnQuit;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void Start()
        {
            var root = _document.rootVisualElement;

            _btnNewGame = root.Q<Button>("btn-new-game");
            _btnContinue = root.Q<Button>("btn-continue");
            _btnFormationEditor = root.Q<Button>("btn-formation-editor");
            _btnSongLibrary = root.Q<Button>("btn-song-library");
            _btnSettings = root.Q<Button>("btn-settings");
            _btnQuit = root.Q<Button>("btn-quit");

            _btnNewGame.clicked += OnNewGame;
            _btnContinue.clicked += OnContinue;
            _btnFormationEditor.clicked += OnFormationEditor;
            _btnSongLibrary.clicked += OnSongLibrary;
            _btnSettings.clicked += OnSettings;
            _btnQuit.clicked += OnQuit;

            // Check if save data exists for Continue button
            if (ServiceLocator.TryGet<ISaveSystem>(out var saveSystem))
            {
                _btnContinue.SetEnabled(saveSystem.HasSaveData);
            }
            else
            {
                _btnContinue.SetEnabled(false);
            }
        }

        private void OnNewGame()
        {
            Debug.Log("MainMenu: New Game");
            BandManagementScreen.IsNewGame = true;
            SceneManager.LoadScene("BandManagement");
        }

        private void OnContinue()
        {
            Debug.Log("MainMenu: Continue");
            BandManagementScreen.IsNewGame = false;
            SceneManager.LoadScene("BandManagement");
        }

        private void OnFormationEditor()
        {
            Debug.Log("MainMenu: Formation Editor");
            SceneManager.LoadScene("FormationEditor");
        }

        private void OnSongLibrary()
        {
            Debug.Log("MainMenu: Song Library");
            SongLibraryScreen.ReturnScene = "MainMenu";
            SceneManager.LoadScene("SongLibrary");
        }

        private void OnSettings()
        {
            Debug.Log("MainMenu: Settings (not yet implemented)");
        }

        private void OnQuit()
        {
            Debug.Log("MainMenu: Quit");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
