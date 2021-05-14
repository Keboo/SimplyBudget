using SimplyBudget.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace SimplyBudget.TemplateSelectors
{
    public class ExpenseCategoryTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? DefaultTemplate { get; set; }
        public DataTemplate? EditingTemplate { get; set; }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            if (item is ExpenseCategoryViewModelEx expenseCategory &&
                container is ListBoxItem lbi && lbi.IsSelected)
            {
                return expenseCategory.IsEditing ? EditingTemplate : DefaultTemplate;
            }
            return DefaultTemplate;
        }
    }
}