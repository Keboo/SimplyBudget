using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using System;
using System.Windows.Input;

namespace SimplyBudget.ViewModels
{
    public class AddItemViewModel : ObservableObject
    {
        public ICommand SubmitCommand { get; }
        public IMessenger Messenger { get; }

        public AddItemViewModel(IMessenger messenger)
        {
            SubmitCommand = new RelayCommand(OnSubmit);
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        }

        private void OnSubmit()
        {
            Messenger.Send(new ItemAddedMessage());
        }

        public class ItemAddedMessage { }
    }
}
