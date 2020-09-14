using System;

namespace SimplyBudget.ViewModels
{
    public interface IRequestClose
    {
        event EventHandler<EventArgs> RequestClose;
    }
}