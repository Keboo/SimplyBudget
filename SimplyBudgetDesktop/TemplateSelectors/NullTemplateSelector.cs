using System.Windows;
using System.Windows.Controls;

namespace SimplyBudget.TemplateSelectors
{
    public class NullTemplateSelector : DataTemplateSelector
    {
        public DataTemplate NullTemplate { get; set; }
        public DataTemplate DefaultTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item == null)
                return NullTemplate;
            return DefaultTemplate;
        }
    }
}