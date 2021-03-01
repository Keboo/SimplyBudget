using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using SimplyBudget.Messaging;
using SimplyBudget.Properties;
using System;
using System.Windows.Input;

namespace SimplyBudget.ViewModels
{
    public class SettingsViewModel : ObservableObject
    {
        private IMessenger Messenger { get; }
        public ICommand SaveCommand { get; }

        public SettingsViewModel(IMessenger messenger)
        {
            SaveCommand = new RelayCommand(OnSaveCommand);
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        }

        private void OnSaveCommand()
        {
            if (string.Equals(ConnectionString, Settings.Default.DatabaseConnectionString, StringComparison.Ordinal))
            {
                return;
            }
            Settings.Default.DatabaseConnectionString = ConnectionString;
            Settings.Default.Save();
            Messenger.Send(new DatabaseConnectionStringChanged());
        }

        private string _ConnectionString = Settings.Default.DatabaseConnectionString;
        public string ConnectionString
        {
            get => _ConnectionString;
            set => SetProperty(ref _ConnectionString, value);
        }
    }
}
