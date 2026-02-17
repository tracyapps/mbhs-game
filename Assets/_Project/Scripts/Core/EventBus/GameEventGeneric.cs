using System.Collections.Generic;
using UnityEngine;

namespace MBHS.Core.Events
{
    public class GameEvent<T> : ScriptableObject
    {
        private readonly List<IGameEventListener<T>> _listeners = new();

        public void Raise(T value)
        {
            for (int i = _listeners.Count - 1; i >= 0; i--)
                _listeners[i].OnEventRaised(value);
        }

        public void Register(IGameEventListener<T> listener)
        {
            if (!_listeners.Contains(listener))
                _listeners.Add(listener);
        }

        public void Unregister(IGameEventListener<T> listener)
        {
            _listeners.Remove(listener);
        }
    }
}
