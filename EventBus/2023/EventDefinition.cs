using System;

using System.Linq;

using System.Collections.Generic;

namespace Game.EventSystem
{
    public sealed class EventDefinition
    {
        private readonly Dictionary<EventListenerPriority, Delegate> _eventListeners = new Dictionary<EventListenerPriority, Delegate>();

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
            foreach (EventListenerPriority item in _eventListeners.Keys)
            {
                if (_eventListeners.TryGetValue(item, out Delegate listenersDelegate) == false || listenersDelegate == null)
                {
                    continue;
                }

                if (_eventListeners[item].GetInvocationList().Contains(listenerToRemove))
                {
                    _eventListeners[item] = Delegate.Remove(listenersDelegate, listenerToRemove);

                    break;
                }
            }
        }

        public void Invoke(IEventParameter eventParam = null)
        {
            InvokePriorityEvent(EventListenerPriority.High, eventParam);

            InvokePriorityEvent(EventListenerPriority.Medium, eventParam);

            InvokePriorityEvent(EventListenerPriority.Low, eventParam);
        }

        private void InvokePriorityEvent(EventListenerPriority listenerPriority, IEventParameter eventParam = null)
        {
            if (_eventListeners.TryGetValue(listenerPriority, out Delegate listenersDelegate) == false)
            {
                return;
            }

            if(eventParam is not null)
            {
                listenersDelegate?.DynamicInvoke(eventParam);

                return;
            }

            listenersDelegate?.DynamicInvoke();
        }
    }
}