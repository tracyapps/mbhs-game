using System.Threading.Tasks;
using MBHS.Core;
using MBHS.Core.StateMachine;
using MBHS.Systems.SaveLoad;
using MBHS.Systems.ContentPipeline;
using MBHS.Systems.BandManagement;
using MBHS.Systems.FormationEditor;
using MBHS.Systems.MusicConductor;
using MBHS.Systems.Scoring;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace MBHS.Runtime
{
    public class GameBootstrapper : MonoBehaviour
    {
        private GameStateMachine _stateMachine;

        private async void Awake()
        {
            DontDestroyOnLoad(gameObject);

            Debug.Log("GameBootstrapper: Initializing...");

            await InitializeAddressables();
            RegisterServices();
            await LoadPlayerData();
            InitializeStateMachine();

            Debug.Log("GameBootstrapper: Initialization complete.");

            // Navigate to main menu if we're in the Boot scene
            if (SceneManager.GetActiveScene().name == "Boot")
            {
                SceneManager.LoadScene("MainMenu");
            }
        }

        private async Task InitializeAddressables()
        {
            Debug.Log("GameBootstrapper: Initializing Addressables...");
            await Addressables.InitializeAsync().Task;
        }

        private void RegisterServices()
        {
            Debug.Log("GameBootstrapper: Registering services...");

            ServiceLocator.Register<ISaveSystem>(new LocalSaveSystem());
            ServiceLocator.Register<IContentCatalog>(new LocalContentCatalog());
            ServiceLocator.Register<IBandManager>(new BandManager());
            ServiceLocator.Register<IFormationSystem>(new FormationSystem());
            ServiceLocator.Register<IMusicConductor>(
                gameObject.AddComponent<MusicConductorBehaviour>());
            ServiceLocator.Register<IScoringEngine>(new ScoringEngine());
        }

        private async Task LoadPlayerData()
        {
            Debug.Log("GameBootstrapper: Loading player data...");
            var saveSystem = ServiceLocator.Get<ISaveSystem>();
            await saveSystem.LoadAsync();
        }

        private void InitializeStateMachine()
        {
            _stateMachine = new GameStateMachine();

            // States will be registered as they are implemented
            // _stateMachine.AddState(new MainMenuState());
            // _stateMachine.AddState(new BandManagementState());
            // _stateMachine.AddState(new FormationEditorState());
            // _stateMachine.AddState(new ShowSimulationState());
            // _stateMachine.AddState(new ResultsState());

            ServiceLocator.Register(_stateMachine);
        }

        private void Update()
        {
            _stateMachine?.Update();
        }

        private void OnDestroy()
        {
            ServiceLocator.Reset();
        }
    }
}
