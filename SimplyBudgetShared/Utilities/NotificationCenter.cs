using System;
using System.Collections.Generic;

namespace SimplyBudgetShared.Utilities
{
    public class NotificationCenter
    {
        private readonly Dictionary<Type, List<WeakReference<object>>> _listeners = new Dictionary<Type, List<WeakReference<object>>>();

        private static readonly Lazy<NotificationCenter> _lazyInstance =
            new Lazy<NotificationCenter>(() => new NotificationCenter());

        private static NotificationCenter Instance
        {
            get { return _lazyInstance.Value; }
        }

        private NotificationCenter()
        { }

        public static void Register<T>(IEventListener<T> listener) where T : Event
        {
            var type = typeof(T);
            List<WeakReference<object>> list;
            if (Instance._listeners.TryGetValue(type, out list) == false)
                list = Instance._listeners[type] = new List<WeakReference<object>>();
            for (int i = list.Count - 1; i >= 0; i--)
            {
                object tmp;
                if (list[i].TryGetTarget(out tmp) == false)
                    list.RemoveAt(i);
            }
            list.Add(new WeakReference<object>(listener));
        }

        public static void Unregister<T>(IEventListener<T> listener) where T : Event
        {
            var list = Instance._listeners[typeof(T)];
            if (list != null)
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    object tmp;
                    if (list[i].TryGetTarget(out tmp) == false || ReferenceEquals(tmp, listener))
                        list.RemoveAt(i);
                }
            }
        }

        public static void PostEvent<T>(T @event) where T : Event
        {
            List<WeakReference<object>> list;
            if (Instance._listeners.TryGetValue(typeof(T), out list) && list != null)
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    object listener;
                    if (list[i].TryGetTarget(out listener))
                        ((IEventListener<T>)listener).HandleEvent(@event);
                    else
                        list.RemoveAt(i);
                }
            }
        }
    }

    public abstract class Event
    { }

    public interface IEventListener<in T> where T : Event
    {
        void HandleEvent(T @event);
    }
}
