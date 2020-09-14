
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace SimplyBudgetShared.ViewModel
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T originalValue, T newValue, 
            [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(originalValue, newValue) == false)
            {
                originalValue = newValue;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                return true;
            }
            return false;
        }

        protected void RaisePropertyChanged<T>(Expression<Func<T>> propertyExpression)
        {
            if (propertyExpression is null) throw new ArgumentNullException("propertyExpression");

            MemberExpression? memberExpression = propertyExpression.Body as MemberExpression;
            if (memberExpression is null)
            {
                throw new ArgumentException("Must be a member access expression", "propertyExpression");
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberExpression.Member.Name));
        }
    }
}