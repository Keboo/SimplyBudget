using System.ComponentModel;

namespace SimplyBudget.Utilities
{
    public static class DesignerHelper
    {
        public static bool IsDesignMode => (bool)(DesignerProperties.IsInDesignModeProperty.GetMetadata(typeof(System.Windows.DependencyObject)).DefaultValue);
    }
}