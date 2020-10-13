﻿
using Microsoft.EntityFrameworkCore;
using SimplyBudgetShared.Data;
using System.Windows;

namespace SimplyBudget
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            //BudgetContext.Instance.Database.EnsureCreated();
            BudgetContext.Instance.Database.Migrate();
            base.OnStartup(e);
        }
    }
}
