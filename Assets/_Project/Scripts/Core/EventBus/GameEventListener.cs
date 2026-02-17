using UnityEngine;
using UnityEngine.Events;

namespace MBHS.Core.Events
{
    public class GameEventListener : MonoBehaviour, IGameEventListener
    {
        [SerializeField] private GameEvent _event;
        [SerializeField] private UnityEvent _response;

        private void OnEnable()
        {
            _event?.Register(this);
        }

        private void OnDisable()
        {
            _event?.Unregister(this);
        }

        public void OnEventRaised()
        {
            _response?.Invoke();
        }
    }
}
