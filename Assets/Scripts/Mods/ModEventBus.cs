using System;
using System.Collections.Generic;
using UnityEngine;

namespace Madness.Mods
{
    public static class ModEventBus
    {
        private interface ISubscription
        {
            int Priority { get; }
            string Owner { get; }
            void Invoke(object context);
            bool Matches(Delegate callback);
        }

        private sealed class Subscription<T> : ISubscription where T : ModContext
        {
            private readonly Action<T> handler;
            public int Priority { get; private set; }
            public string Owner { get; private set; }

            public Subscription(Action<T> handler, int priority, string owner)
            {
                this.handler = handler;
                Priority = priority;
                Owner = string.IsNullOrEmpty(owner) ? "anonymous" : owner;
            }

            public void Invoke(object context)
            {
                handler((T)context);
            }

            public bool Matches(Delegate callback)
            {
                return callback is Action<T> && Delegate.Equals(handler, callback);
            }
        }

        private static readonly Dictionary<Type, List<ISubscription>> subscriptions =
            new Dictionary<Type, List<ISubscription>>();

        public static void Subscribe<T>(Action<T> handler, int priority = 0, string owner = "") where T : ModContext
        {
            if (handler == null) return;
            Type type = typeof(T);
            List<ISubscription> list;
            if (!subscriptions.TryGetValue(type, out list))
            {
                list = new List<ISubscription>();
                subscriptions.Add(type, list);
            }
            list.Add(new Subscription<T>(handler, priority, owner));
            list.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : ModContext
        {
            if (handler == null) return;
            List<ISubscription> list;
            if (!subscriptions.TryGetValue(typeof(T), out list)) return;
            list.RemoveAll(s => s.Matches(handler));
        }

        // Hakim modun tamamını temizlemek için kullanılır; modlar kendi callback referanslarını saklamak zorunda kalmaz.
        public static void UnsubscribeOwner(string owner)
        {
            if (string.IsNullOrEmpty(owner)) return;
            foreach (List<ISubscription> list in subscriptions.Values)
                list.RemoveAll(s => string.Equals(s.Owner, owner, StringComparison.OrdinalIgnoreCase));
        }

        public static T Publish<T>(T context) where T : ModContext
        {
            if (context == null) return null;
            List<ISubscription> list;
            if (!subscriptions.TryGetValue(typeof(T), out list)) return context;

            // Callback sırasında subscribe/unsubscribe yapılabilsin diye snapshot kullanılır.
            ISubscription[] snapshot = list.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
            {
                try
                {
                    snapshot[i].Invoke(context);
                }
                catch (Exception ex)
                {
                    // Bir modun hatası ana oyunu durdurmaz.
                    Debug.LogError("[ModEventBus] " + snapshot[i].Owner + " hook hatası: " + ex);
                }
                if (context.Cancelled) break;
            }
            return context;
        }

        public static void ClearAll()
        {
            subscriptions.Clear();
        }

    }
}
