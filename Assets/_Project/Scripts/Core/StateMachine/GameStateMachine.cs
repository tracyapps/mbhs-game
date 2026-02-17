using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace MBHS.Core.StateMachine
{
    public class GameStateMachine
    {
        private IGameState _currentState;
        private readonly Dictionary<Type, IGameState> _states = new();
        private bool _isTransitioning;

        public IGameState CurrentState => _currentState;
        public Type CurrentStateType => _currentState?.GetType();

        public void AddState(IGameState state)
        {
            var type = state.GetType();
            if (_states.ContainsKey(type))
            {
                Debug.LogWarning($"GameStateMachine: State {type.Name} already registered. Replacing.");
            }

            _states[type] = state;
        }

        public async Task TransitionTo<T>() where T : IGameState
        {
            if (_isTransitioning)
            {
                Debug.LogWarning("GameStateMachine: Transition already in progress. Ignoring.");
                return;
            }

            var targetType = typeof(T);

            if (!_states.TryGetValue(targetType, out var nextState))
            {
                Debug.LogError($"GameStateMachine: State {targetType.Name} not registered.");
                return;
            }

            _isTransitioning = true;

            try
            {
                if (_currentState != null)
                {
                    Debug.Log($"GameStateMachine: Exiting {_currentState.GetType().Name}");
                    await _currentState.Exit();
                }

                _currentState = nextState;
                Debug.Log($"GameStateMachine: Entering {targetType.Name}");
                await _currentState.Enter();
            }
            finally
            {
                _isTransitioning = false;
            }
        }

        public void Update()
        {
            if (!_isTransitioning)
                _currentState?.Update();
        }
    }
}
