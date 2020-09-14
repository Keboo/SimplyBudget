using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Practices.Prism.Commands;

namespace SimplyBudget.ViewModels
{
    internal abstract class ViewEditViewModel<T> : ViewModelBase
    {
        private readonly DelegateCommand _saveCommand;

        protected ViewEditViewModel()
        {
            _saveCommand = new DelegateCommand(OnSaveCommand);
        }

        public ICommand SaveCommand
        {
            get { return _saveCommand; }
        }

        private bool _isEditing;
        public bool IsEditing
        {
            get { return _isEditing; }
            private set { SetProperty(ref _isEditing, value); }
        }

        public async Task SetItemToEditAsync(T itemToEdit)
        {
            IsEditing = true;
            await SetPropertiesToEditAsync(itemToEdit);
        }

        public virtual Task LoadDefaultView()
        {
            return Task.FromResult(0);
        }

        private async void OnSaveCommand()
        {
            if (IsEditing)
                await SaveAsync();
            else
                await CreateAsync();
        }

        protected abstract Task CreateAsync();
        protected abstract Task SaveAsync();
        protected abstract Task SetPropertiesToEditAsync(T itemToEdit);
    }
}