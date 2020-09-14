using System;
using System.ComponentModel;
using JetBrains.Annotations;
using SQLite;
using SimplyBudgetShared.Data;

namespace SimplyBudget.ViewModels
{
    public class ViewModelBase : SimplyBudgetShared.ViewModel.ViewModelBase
    {
        [NotNull]
        protected SQLiteAsyncConnection GetDatabaseConnection()
        {
            return DatabaseManager.Instance.Connection;
        }

        [Obsolete]
        protected bool DesignModeEnabled
        {
            get
            {
#if WPF
                return (bool)(DesignerProperties.IsInDesignModeProperty.GetMetadata(typeof(System.Windows.DependencyObject)).DefaultValue);
#else
                //Windows.ApplicationModel.DesignMode.DesignModeEnabled
                return false;
#endif
            }
        }
    }
}