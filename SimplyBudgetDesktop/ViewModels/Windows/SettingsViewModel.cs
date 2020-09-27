using System;
using System.IO;
using System.Windows.Input;
using Microsoft.Toolkit.Mvvm.Input;
using SimplyBudget.Properties;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;

namespace SimplyBudget.ViewModels.Windows
{
    internal class SettingsViewModel : Microsoft.Toolkit.Mvvm.ComponentModel.ObservableObject, IRequestClose
    {
        public event EventHandler<EventArgs> RequestClose;

        private readonly RelayCommand _saveCommand;

        public SettingsViewModel()
        {
            DataDirectory = Settings.Default.DataDirectory;
            if (string.IsNullOrWhiteSpace(DataDirectory))
                DataDirectory = Path.GetDirectoryName(DatabaseManager.Instance.CurrentDatabasePath);
            if (string.IsNullOrWhiteSpace(DataDirectory))
                DataDirectory = Path.GetFullPath(".");

            PickDirectoryCommand = new RelayCommand(OnPickDirectory);
            _saveCommand = new RelayCommand(OnSave, CanSave);
        }

        public string DataDirectory { get; }

        public ICommand PickDirectoryCommand { get; }

        public ICommand SaveCommand => _saveCommand;

        private string _newDataDirectory;
        public string NewDataDirectory
        {
            get => _newDataDirectory;
            set
            {
                if (SetProperty(ref _newDataDirectory, value))
                    _saveCommand.NotifyCanExecuteChanged();
            }
        }

        private bool _overwriteWithCurrent;
        public bool OverwriteWithCurrent
        {
            get => _overwriteWithCurrent;
            set => SetProperty(ref _overwriteWithCurrent, value);
        }

        private void OnPickDirectory()
        {
            //var diaglog = new FolderBrowserDialog();
            //if (diaglog.ShowDialog() == DialogResult.OK)
            //{
            //    NewDataDirectory = diaglog.SelectedPath;
            //}
        }

        private bool CanSave()
        {
            return string.IsNullOrWhiteSpace(NewDataDirectory) == false;
        }

        private async void OnSave()
        {
            Settings.Default.DataDirectory = NewDataDirectory;
            var dbPath = DatabaseManager.Instance.CurrentDatabasePath;
            await DatabaseManager.Instance.InitDatabase(NewDataDirectory, Path.GetFileName(dbPath));
            //TODO: need to send event.... 
            RequestClose.Raise(this, EventArgs.Empty);
        }
    }
}