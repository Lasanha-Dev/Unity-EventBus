using Delegate = System.Delegate;

using Object = System.Object;

using System.Collections.Generic;

using System.Linq;

namespace Game.EventSystem
{
    internal sealed class EventDefinition
    {
        private readonly Dictionary<EventListenerPriority, Delegate> _eventListeners = new Dictionary<EventListenerPriority, Delegate>();

        public EventDefinition(Delegate listenerDelegate, EventListenerPriority listenerPriority)
        {
            AddListener(listenerDelegate, listenerPriority);
        }

        public void AddListener(Delegate listener, EventListenerPriority listenerPriority)
        {
            if (_eventListeners.TryGetValue(listenerPriority, out Delegate listenersDelegate))
            {
                _eventListeners[listenerPriority] = Delegate.Combine(listenersDelegate, listener);

                return;
            }

            _eventListeners[listenerPriority] = listener;
        }

        public void RemoveListener(Delegate listenerToRemove)
        {
            foreach (KeyValuePair<EventListenerPriority, Delegate> keyValuePair in _eventListeners)
            {
                if (keyValuePair.Value.GetInvocationList().Contains(listenerToRemove))
                {
                    _eventListeners[keyValuePair.Key] = Delegate.Remove(keyValuePair.Value, listenerToRemove);

                    break;
                }
            }
        }

        public void Invoke(Object eventParam = null)
        {
            InvokePriorityEvent(EventListenerPriority.High, eventParam);

            InvokePriorityEvent(EventListenerPriority.Medium, eventParam);

            InvokePriorityEvent(EventListenerPriority.Low, eventParam);
        }

        private void InvokePriorityEvent(EventListenerPriority listenerPriority, Object eventParameter = null)
        {
            if (_eventListeners.TryGetValue(listenerPriority, out Delegate listenersDelegate) == false)
            {
                return;
            }

            if (eventParameter is not null)
            {
                listenersDelegate?.DynamicInvoke(eventParameter);

                return;
            }

            listenersDelegate?.DynamicInvoke();
        }
    }
}