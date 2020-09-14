using System;
using JetBrains.Annotations;
using Windows.UI.Core;

namespace SimplyBudget.ViewModels
{
    public static class DispatcherEx
    {
        private static readonly CoreDispatcher _dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;

        public static void Init()
        { }

        public static async void Run([NotNull] Action action)
        {
            if (action == null) throw new ArgumentNullException("action");
            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => action());
        }
    }
}