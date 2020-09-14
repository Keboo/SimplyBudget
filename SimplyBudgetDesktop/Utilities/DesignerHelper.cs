using System.ComponentModel;

namespace SimplyBudget.Utilities
{
    public static class DesignerHelper
    {
        public static bool IsDesignMode
        {
            get { return (bool)(DesignerProperties.IsInDesignModeProperty.GetMetadata(typeof(System.Windows.DependencyObject)).DefaultValue); }
        }
    }
}