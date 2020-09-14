using System.Windows;
using System.Windows.Input;
using Microsoft.Practices.Prism.Commands;
using SimplyBudget.Utilities;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using SimplyBudgetShared.Utilities;

namespace SimplyBudget.ViewModels.Windows
{
    internal class LoadSnapshotsViewModel : ViewModelBase, IRequestClose
    {
        public event EventHandler<EventArgs> RequestClose;

        private readonly ObservableCollection<SnapshotViewModel> _snapshots;
        private readonly DelegateCommand _loadCommand;

        public LoadSnapshotsViewModel()
        {
            _loadCommand = new DelegateCommand(OnLoadCommand, CanLoadCommand);
            _snapshots = new ObservableCollection<SnapshotViewModel>();
            Init();
        }

        public void Init()
        {
            _snapshots.Clear();
            foreach (var vm in SnapshotManager.Instance.GetSnapshotFiles()
                                              .Select(x => new SnapshotViewModel
                                                               {
                                                                   DateTime = File.GetCreationTime(x),
                                                                   FilePath = x
                                                               }).OrderByDescending(x => x.DateTime))
            {
                _snapshots.Add(vm);
            }
        }

        public ObservableCollection<SnapshotViewModel> Snapshots
        {
            get { return _snapshots; }
        }

        public ICommand LoadCommand
        {
            get { return _loadCommand; }
        }

        private SnapshotViewModel _selectedSnapshot;
        public SnapshotViewModel SelectedSnapshot
        {
            get { return _selectedSnapshot; }
            set
            {
                if (SetProperty(ref _selectedSnapshot, value))
                    _loadCommand.RaiseCanExecuteChanged();
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

        public class SnapshotViewModel : ViewModelBase
        {
            private DateTime _dateTime;
            public DateTime DateTime
            {
                get { return _dateTime; }
                set { SetProperty(ref _dateTime, value); }
            }

            private string _filePath;
            public string FilePath
            {
                get { return _filePath; }
                set { SetProperty(ref _filePath, value); }
            }
        }
    }
}