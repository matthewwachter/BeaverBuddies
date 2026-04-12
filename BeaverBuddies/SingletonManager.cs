using System;
using System.Collections.Concurrent;

namespace BeaverBuddies
{
    public static class SingletonManager
    {
        private static ConcurrentDictionary<Type, object> map = new ConcurrentDictionary<Type, object>();

        public static void Reset()
        {
            Plugin.Log("Resetting SingletonManager");
            foreach (object obj in map.Values)
            {
                if (obj is IResettableSingleton resettable)
                {
                    resettable.Reset();
                }
            }
            map.Clear();
        }

        public static T RegisterSingleton<T>(T singleton)
        {
            Type type = singleton.GetType();
            if (!map.TryAdd(type, singleton))
            {
                Plugin.LogWarning($"Singleton of type {type} already registered");
                map[type] = singleton;
            }
            return singleton;
        }

        /// <summary>
        /// Gets the singleton of the requested type, or null if it is not present.
        /// </summary>
        public static T GetSingleton<T>()
        {
            Type t = typeof(T);
            return map.TryGetValue(t, out object value) ? (T)value : default(T);
        }
    }

    /// <summary>
    /// Singleton which has static fields that need to be manually
    /// reset when unloading the map. This is appropriate in the case
    /// of fields that are frequently accessed from patched methods,
    /// where a map lookup would be inefficient, or for static fields
    /// which might be accessed as the map is loading, before the
    /// singleton is registered.
    /// </summary>
    public interface IResettableSingleton
    {
        void Reset();
    }

    public class RegisteredSingleton
    { 
        public RegisteredSingleton()
        {
            SingletonManager.RegisterSingleton(this);
        }
    }
}
