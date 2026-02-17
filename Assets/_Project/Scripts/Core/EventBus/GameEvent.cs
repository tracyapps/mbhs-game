using System.Collections.Generic;
using UnityEngine;

namespace MBHS.Core.Events
{
    [CreateAssetMenu(menuName = "MBHS/Events/Game Event")]
    public class GameEvent : ScriptableObject
    {
        private readonly List<IGameEventListener> _listeners = new();

        public void Raise()
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
                _listeners[i].OnEventRaised();
        }

        public void Register(IGameEventListener listener)
        {
            if (!_listeners.Contains(listener))
                _listeners.Add(listener);
        }

        public void Unregister(IGameEventListener listener)
        {
            _listeners.Remove(listener);
        }
    }
}
