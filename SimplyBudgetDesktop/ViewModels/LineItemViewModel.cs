using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SimplyBudget.Messaging;
using SimplyBudgetShared.Data;
using System.Windows.Input;

namespace SimplyBudget.ViewModels;

public class LineItemViewModel : ObservableObject
{
    public ICommand SetAmountCommand { get; }

    public IList<ExpenseCategory> ExpenseCategories { get; }
    private IMessenger Messenger { get; }

    public LineItemViewModel(IList<ExpenseCategory> expenseCategories, IMessenger messenger)
    {
        ExpenseCategories = expenseCategories ?? throw new ArgumentNullException(nameof(expenseCategories));
        Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        SetAmountCommand = new RelayCommand(OnSetAmount);
    }

    private void OnSetAmount()
    {
        Amount = SetAmountCallback?.Invoke(this) ?? DesiredAmount;
    }

    private int _amount;
    public int Amount
    {
        get => _amount;
        set
        {
            if (SetProperty(ref _amount, value))
            {
                Messenger.Send(new LineItemAmountUpdated());
            }
        }
    }

    private int _desiredAmount;
    public int DesiredAmount
    {
        get => _desiredAmount;
        set => SetProperty(ref _desiredAmount, value);
    }

    public Func<LineItemViewModel, int>? SetAmountCallback { get; set; }

    private ExpenseCategory? _selectedCategory;
    public ExpenseCategory? SelectedCategory
    {
        get => _selectedCategory;
        set => SetProperty(ref _selectedCategory, value);
    }
}

