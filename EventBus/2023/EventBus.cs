using Game.Global.Management;
using System;

using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

namespace Game.EventSystem
{
    public static class EventBus
    {
        private static readonly Dictionary<Type, EventDefinition> _eventDefinitions = new Dictionary<Type, EventDefinition>();

        static EventBus()
        {
            SceneManager.sceneUnloaded += ResetEventDefinitions;
        }

        private static void ResetEventDefinitions(Scene arg0)
        {
            if(arg0 == SceneManager.GetActiveScene())
            {
                _eventDefinitions.Clear();
            }
        }

        public static void Subscribe<T>(Action<T> eventListener, EventListenerPriority listenerPriority = EventListenerPriority.Low) where T : IEvent
        {
            Type eventType = typeof(T);

            if (EventListenerAttendsRequiredEventParams(eventType, eventListener) is false)
            {
                return;
            }

            if (EventListenerImplementsListenerInterface(eventListener) is false)
            {
                return;
            }

            SubscribeEventListenerToEvent(eventListener, eventType, listenerPriority);
        }

        public static void Subscribe(Action eventListener, Type eventType, EventListenerPriority listenerPriority = EventListenerPriority.Low)
        {
            if (EventListenerAttendsRequiredEventParams(eventType, eventListener) is false)
            {
                return;
            }

            if (EventListenerImplementsListenerInterface(eventListener) is false)
            {
                return;
            }

            SubscribeEventListenerToEvent(eventListener, eventType, listenerPriority);
        }

        private static void SubscribeEventListenerToEvent(Delegate eventListener, Type eventType, EventListenerPriority listenerPriority)
        {
            if (_eventDefinitions.TryGetValue(eventType, out EventDefinition eventDefinition))
            {
                eventDefinition.AddListener(eventListener, listenerPriority);

                return;
            }

            _eventDefinitions.Add(eventType, new EventDefinition());

            _eventDefinitions[eventType].AddListener(eventListener, listenerPriority);
        }

        private static bool EventListenerAttendsRequiredEventParams(Type eventType, Delegate eventListener)
        {
            Type parameteresListenerType = typeof(Action<>).MakeGenericType(eventType);

            bool eventRequiresParameter = typeof(IEventParameter).IsAssignableFrom(eventType);

            Type eventListenerType = eventListener.GetType();

            if (eventRequiresParameter && eventListenerType == parameteresListenerType)
            {
                return true;
            }

            if (eventRequiresParameter is false && eventListenerType == typeof(Action))
            {
                return true;
            }

            if (eventRequiresParameter is false && eventListenerType == parameteresListenerType)
            {
                GlobalLogger.LogError($"{eventListener.Method.Name} Method Is Including Unecessary Params To Event: {eventType}");

                return false;
            }

            if (eventRequiresParameter && eventListenerType == typeof(Action))
            {
                GlobalLogger.LogError($"{eventListener.Method.Name} Does Not Implement Includes Necessary Params To Event: {eventType}");

                return false;
            }

            return false;
        }

        private static bool EventListenerImplementsListenerInterface(Delegate eventListener)
        {
            if (typeof(IEventListener).IsAssignableFrom(eventListener.Method.DeclaringType) == false)
            {
                GlobalLogger.LogError($"{eventListener.Method.DeclaringType} Does Not Implement IEventListener");

                return false;
            }

            return true;
        }

        public static void Unsubscribe(Action listenerToRemove, Type eventType)
        {
            UnsubscribeListenerFromEvent(listenerToRemove, eventType);
        }

        public static void Unsubscribe<T>(Action<T> listenerToRemove) where T : IEvent
        {
            UnsubscribeListenerFromEvent(listenerToRemove, typeof(T));
        }

        private static void UnsubscribeListenerFromEvent(Delegate listenerToRemove, Type eventType)
        {
            if (_eventDefinitions.ContainsKey(eventType) == false)
            {
                return;
            }

            _eventDefinitions[eventType].RemoveListener(listenerToRemove);
        }
        
        public static void Invoke(Type @eventType, IEventInvoker eventNotifier, IEventParameter eventParam = null)
        {
            if (_eventDefinitions.TryGetValue(@eventType, out EventDefinition eventDefinition) is false)
            {
                GlobalLogger.LogWarning($"Invoking {@eventType} Event Without Any Listeners To This Event");

                return;
            }

            if (typeof(IEventParameter).IsAssignableFrom(@eventType) && eventParam == null)
            {
                GlobalLogger.LogError($"Invoking {@eventType} Event With Null Parameter Where Is Necessary To Send Params");

                return;
            }

            eventDefinition.Invoke(eventParam);
        }
    }
}