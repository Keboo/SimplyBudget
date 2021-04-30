using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using SimplyBudget.Messaging;
using SimplyBudget.Properties;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;

namespace SimplyBudget.ViewModels
{
    public class SettingsViewModel : ObservableObject
    {
        private IMessenger Messenger { get; }
        public ICommand SaveCommand { get; }
        public ICommand OpenFolderCommand { get; }

        public SettingsViewModel(IMessenger messenger)
        {
            SaveCommand = new RelayCommand(OnSaveCommand);
            OpenFolderCommand = new RelayCommand(OpenFolder);
            Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        }

        private void OnSaveCommand()
        {
            if (string.Equals(StorageLocation, Settings.Default.StorageLocation, StringComparison.Ordinal))
            {
                return;
            }
            Settings.Default.StorageLocation = StorageLocation;
            Settings.Default.Save();
            Messenger.Send(new StorageLocationChanged());
        }

        private string _StorageLocation = Settings.Default.StorageLocation;
        public string StorageLocation
        {
            get => _StorageLocation;
            set => SetProperty(ref _StorageLocation, value);
        }

        private void OpenFolder()
        {
            string targetPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(StorageLocation));
            Process.Start("explorer.exe", targetPath);
        }
    }
}
