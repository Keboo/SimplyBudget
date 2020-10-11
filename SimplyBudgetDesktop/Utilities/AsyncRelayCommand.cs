using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SimplyBudget.Utilities
{
    //
    // Summary:
    //     A command whose sole purpose is to relay its functionality to other objects by
    //     invoking delegates. The default return value for the Microsoft.Toolkit.Mvvm.Input.RelayCommand.CanExecute(System.Object)
    //     method is true. This type does not allow you to accept command parameters in
    //     the Microsoft.Toolkit.Mvvm.Input.RelayCommand.Execute(System.Object) and Microsoft.Toolkit.Mvvm.Input.RelayCommand.CanExecute(System.Object)
    //     callback methods.
    public sealed class AsyncRelayCommand : IRelayCommand, ICommand
    {
        //
        // Summary:
        //     The System.Action to invoke when Microsoft.Toolkit.Mvvm.Input.RelayCommand.Execute(System.Object)
        //     is used.
        private readonly Func<Task> _execute;

        //
        // Summary:
        //     The optional action to invoke when Microsoft.Toolkit.Mvvm.Input.RelayCommand.CanExecute(System.Object)
        //     is used.
        private readonly Func<bool>? _canExecute;

        public event EventHandler? CanExecuteChanged;

        //
        // Summary:
        //     Initializes a new instance of the Microsoft.Toolkit.Mvvm.Input.RelayCommand class
        //     that can always execute.
        //
        // Parameters:
        //   execute:
        //     The execution logic.
        public AsyncRelayCommand(Func<Task> execute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }

        //
        // Summary:
        //     Initializes a new instance of the Microsoft.Toolkit.Mvvm.Input.RelayCommand class.
        //
        // Parameters:
        //   execute:
        //     The execution logic.
        //
        //   canExecute:
        //     The execution status logic.
        public AsyncRelayCommand(Func<Task> execute, Func<bool> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        //
        // Summary:
        //     Notifies that the System.Windows.Input.ICommand.CanExecute(System.Object) property
        //     has changed.
        public void NotifyCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke() ?? true;
        }

        private int _isExecuting;

        public async void Execute(object? parameter)
        {
            if (Interlocked.CompareExchange(ref _isExecuting, 1, 0) == 0)
            {
                try
                {
                    if (CanExecute(parameter))
                    {
                        await _execute();
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref _isExecuting, 0);
                }
            }
        }
    }
}
