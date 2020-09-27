using System.Windows;
using System.Windows.Input;
using Microsoft.Toolkit.Mvvm.Input;
using SimplyBudget.Utilities;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using SimplyBudgetShared.Utilities;

namespace SimplyBudget.ViewModels.Windows
{
    internal class LoadSnapshotsViewModel : Microsoft.Toolkit.Mvvm.ComponentModel.ObservableObject, IRequestClose
    {
        public event EventHandler<EventArgs> RequestClose;

        private readonly RelayCommand _loadCommand;

        public LoadSnapshotsViewModel()
        {
            _loadCommand = new RelayCommand(OnLoadCommand, CanLoadCommand);
            Snapshots = new ObservableCollection<SnapshotViewModel>();
            Init();
        }

        public void Init()
        {
            Snapshots.Clear();
            foreach (var vm in SnapshotManager.Instance.GetSnapshotFiles()
                                              .Select(x => new SnapshotViewModel
                                                               {
                                                                   DateTime = File.GetCreationTime(x),
                                                                   FilePath = x
                                                               }).OrderByDescending(x => x.DateTime))
            {
                Snapshots.Add(vm);
            }
        }

        public ObservableCollection<SnapshotViewModel> Snapshots { get; }

        public ICommand LoadCommand => _loadCommand;

        private SnapshotViewModel _selectedSnapshot;
        public SnapshotViewModel SelectedSnapshot
        {
            get => _selectedSnapshot;
            set
            {
                if (SetProperty(ref _selectedSnapshot, value))
                    _loadCommand.NotifyCanExecuteChanged();
            }
        }


        private bool CanLoadCommand()
        {
            return SelectedSnapshot != null;
        }

        private async void OnLoadCommand()
        {
            var selectedSnapshot = SelectedSnapshot;
            if (selectedSnapshot != null && 
                MessageBox.Show("Loading a snapshot will replace all existing data.\nAre you sure you want to do this?",
                    "Load Snapshot", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                try
                {
                    await SnapshotManager.Instance.LoadSnapshot(selectedSnapshot.FilePath);
                    RequestClose.Raise(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading snapshot\n" + ex.Message, "Snapshot");
                }
            }
        }

        public class SnapshotViewModel : Microsoft.Toolkit.Mvvm.ComponentModel.ObservableObject
        {
            private DateTime _dateTime;
            public DateTime DateTime
            {
                get => _dateTime;
                set => SetProperty(ref _dateTime, value);
            }

            private string _filePath;
            public string FilePath
            {
                get => _filePath;
                set => SetProperty(ref _filePath, value);
            }
        }
    }
}