using GlobalLogger = Game.Global.Management.GlobalLogger;

using System;

using System.Collections.Generic;

#if UNITY_EDITOR
using File = System.IO.File;
using UnityEditor;
using System.Reflection;
#endif

namespace Game.EventSystem
{
    public static class EventBus
    {
        private static readonly Dictionary<string, EventDefinition> _eventDefinitions = new Dictionary<string, EventDefinition>();

        public static void Subscribe<T>(Action<T> eventListener, EventListenerPriority listenerPriority = EventListenerPriority.Low) where T : IEventParameter
        {
            SubscribeEventListenerToEvent(eventListener, typeof(T).Name, listenerPriority);
        }

        public static void Subscribe(Action eventListener, string eventName, EventListenerPriority listenerPriority = EventListenerPriority.Low)
        {
            SubscribeEventListenerToEvent(eventListener, eventName, listenerPriority);
        }

        private static void SubscribeEventListenerToEvent(Delegate eventListener, string eventName, EventListenerPriority listenerPriority)
        {
            if (_eventDefinitions.TryGetValue(eventName, out EventDefinition eventDefinition))
            {
                eventDefinition.AddListener(eventListener, listenerPriority);

                return;
            }

            _eventDefinitions.Add(eventName, new EventDefinition(eventListener, listenerPriority));
        }

        public static void Unsubscribe(Action listenerToRemove, string eventName)
        {
            UnsubscribeListenerFromEvent(listenerToRemove, eventName);
        }

        public static void Unsubscribe<T>(Action<T> listenerToRemove) where T : IEventParameter
        {
            UnsubscribeListenerFromEvent(listenerToRemove, nameof(T));
        }

        private static void UnsubscribeListenerFromEvent(Delegate listenerToRemove, string eventName)
        {
            if (_eventDefinitions.ContainsKey(eventName) == false)
            {
                return;
            }

            _eventDefinitions[eventName].RemoveListener(listenerToRemove);
        }

        public static void Invoke(string eventName, object eventParam = null)
        {
            if (_eventDefinitions.TryGetValue(eventName, out EventDefinition eventDefinition) is false)
            {
                GlobalLogger.LogWarning($"Invoking {eventName} Event Without Any Listeners To This Event");

                return;
            }

            eventDefinition.Invoke(eventParam);
        }
#if UNITY_EDITOR

        private const string DEFAULT_NAMESPACE_HEADER = "Game";

        private const string EVENT_COMPILATION_ERROR_00 = "<color=#FF0000>Failed To Compile Events Of Class</color>";

        private const string EVENT_COMPILATION_ERROR_01 = "Is Not Being Unsubscribed, Which May Cause Memory Leak. Make Sure To Remove The Instance Listener From The Event During The Object Life Cycle";

        [InitializeOnLoadMethod]
        private static void CompileSceneEvents()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            ClearLog();

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                Type[] types = assembly.GetTypes();

                foreach (Type type in types)
                {
                    if (type.Namespace == null || type.Namespace.Contains(DEFAULT_NAMESPACE_HEADER) is false)
                    {
                        continue;
                    }

                    string typeClassCode = GetTypeCode(type);

                    if (typeClassCode == null || IsTypeAEventListener(typeClassCode) is false)
                    {
                        continue;
                    }

                    MethodInfo[] methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

                    foreach (MethodInfo method in methods)
                    {
                        if (IsMethodAEventListener(typeClassCode, method) is false)
                        {
                            continue;
                        }

                        if (IsTypeStatic(type) && method.IsStatic)
                        {
                            continue;
                        }

                        if (ListenerIsGettingUnsubscribed(typeClassCode, method) is false)
                        {
                            GlobalLogger.LogMessage($"{EVENT_COMPILATION_ERROR_00} {method.DeclaringType.Name}: The Method {method.Name} {EVENT_COMPILATION_ERROR_01}");

                            return;
                        }
                    }
                }
            }

            GlobalLogger.LogMessage($"<color=#17FF32>Successfully Compiled Events</color>");
        }

        private static bool IsTypeStatic(Type type)
        {
            return type.IsAbstract && type.IsSealed;
        }

        private static bool IsTypeAEventListener(string typeString)
        {
            if (TypeCodeContainsString(typeString, "EventBus.Subscribe"))
            {
                return true;
            }

            return false;
        }

        private static bool ListenerIsGettingUnsubscribed(string typeCodeString, MethodInfo listenerMethod)
        {
            string unsubscriptionPattern0 = $"EventBus.Unsubscribe({listenerMethod.Name}";

            if(TypeCodeContainsString(typeCodeString, unsubscriptionPattern0))
            {
                return true;
            }

            if (listenerMethod.GetParameters().Length == 0)
            {
                return false;
            }

            Type genericParameterType = listenerMethod.GetParameters()[0].ParameterType;

            string unsubscriptionPattern1 = $"EventBus.Unsubscribe<{genericParameterType.Name}>({listenerMethod.Name}";

            if (TypeCodeContainsString(typeCodeString, unsubscriptionPattern1))
            {
                return true;
            }

            return false;
        }

        private static bool IsMethodAEventListener(string typeCodeString, MethodInfo listenerMethod)
        {
            string subscriptionPattern0 = $"EventBus.Subscribe({listenerMethod.Name}";

            if(TypeCodeContainsString(typeCodeString, subscriptionPattern0))
            {
                return true;
            }

            if (listenerMethod.GetParameters().Length == 0)
            {
                return false;
            }

            Type genericParameterType = listenerMethod.GetParameters()[0].ParameterType;

            string subscriptionPattern1 = $"EventBus.Subscribe<{genericParameterType.Name}>({listenerMethod.Name}";

            if (TypeCodeContainsString(typeCodeString, subscriptionPattern1))
            {
                return true;
            }

            return false;
        }

        private static bool TypeCodeContainsString(string typeCode, string stringToCheck)
        {
            string cleanedListenerClassCode = typeCode.Replace(" ", string.Empty);

            string cleanedSubscriptionPattern0 = stringToCheck.Replace(" ", string.Empty);

            return cleanedListenerClassCode.Contains(cleanedSubscriptionPattern0, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetTypeCode(Type type)
        {
            string[] guids = AssetDatabase.FindAssets($"{type.Name} t:script");

            if (guids.Length == 0)
            {
                return null;
            }

            string scriptPath = AssetDatabase.GUIDToAssetPath(guids[0]);

            return File.ReadAllText(scriptPath);
        }

        private static void ClearLog()
        {
            var assembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
            var type = assembly.GetType("UnityEditor.LogEntries");
            var method = type.GetMethod("Clear");
            method.Invoke(new object(), null);
        }
#endif
    }
}