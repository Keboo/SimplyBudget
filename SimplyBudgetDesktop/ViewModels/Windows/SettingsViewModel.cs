using System;
using System.IO;
using System.Windows.Forms;
using System.Windows.Input;
using Microsoft.Practices.Prism.Commands;
using SimplyBudget.Properties;
using SimplyBudgetShared.Data;
using SimplyBudgetShared.Utilities;

namespace SimplyBudget.ViewModels.Windows
{
    internal class SettingsViewModel : ViewModelBase, IRequestClose
    {
        public event EventHandler<EventArgs> RequestClose;

        private readonly ICommand _pickDirectoryCommand;
        private readonly DelegateCommand _saveCommand;

        public SettingsViewModel()
        {
            _dataDirectory = Settings.Default.DataDirectory;
            if (string.IsNullOrWhiteSpace(DataDirectory))
                _dataDirectory = Path.GetDirectoryName(DatabaseManager.Instance.CurrentDatabasePath);
            if (string.IsNullOrWhiteSpace(DataDirectory))
                _dataDirectory = Path.GetFullPath(".");

            _pickDirectoryCommand = new DelegateCommand(OnPickDirectory);
            _saveCommand = new DelegateCommand(OnSave, CanSave);
        }

        private readonly string _dataDirectory;
        public string DataDirectory
        {
            get { return _dataDirectory; }
        }

        public ICommand PickDirectoryCommand
        {
            get { return _pickDirectoryCommand; }
        }

        public ICommand SaveCommand
        {
            get { return _saveCommand; }
        }

        private string _newDataDirectory;
        public string NewDataDirectory
        {
            get { return _newDataDirectory; }
            set
            {
                if (SetProperty(ref _newDataDirectory, value))
                    _saveCommand.RaiseCanExecuteChanged();
            }
        }

        private bool _overwriteWithCurrent;
        public bool OverwriteWithCurrent
        {
            get { return _overwriteWithCurrent; }
            set { SetProperty(ref _overwriteWithCurrent, value); }
        }

        private void OnPickDirectory()
        {
            var diaglog = new FolderBrowserDialog();
            if (diaglog.ShowDialog() == DialogResult.OK)
            {
                NewDataDirectory = diaglog.SelectedPath;
            }
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