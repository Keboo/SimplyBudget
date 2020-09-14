using System;
using System.Collections.Generic;
using System.Windows;

namespace SimplyBudget.Utilities
{
    public static class SingletonWindow
    {
        private static readonly object _syncLock = new object();
        private static readonly Dictionary<Type, Window> _singletonWindows = new Dictionary<Type, Window>();

        public static void ShowWindow<T>() where T : Window, new()
        {
            lock (_syncLock)
            {
                Window existingWindow;
                if (_singletonWindows.TryGetValue(typeof (T), out existingWindow))
                    existingWindow.Activate();
                else
                {
                    var window = new T();
                    EventHandler closedHandler = null;
                    closedHandler = (sender, e) =>
                    {
                        window.Closed -= closedHandler;
                        _singletonWindows.Remove(typeof(T));
                    };
                    window.Closed += closedHandler;
                    _singletonWindows.Add(typeof(T), window);
                    window.Show();
                }
            }
        }
    }
}