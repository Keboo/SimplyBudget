using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace SimplyBudgetShared.ViewModel
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected bool SetProperty<T>(ref T originalValue, T newValue, 
            [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(originalValue, newValue) == false)
            {
                originalValue = newValue;
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
                return true;
            }
            return false;
        }

        protected void RaisePropertyChanged<T>(Expression<Func<T>> propertyExpression)
        {
            if (propertyExpression == null) throw new ArgumentNullException("propertyExpression");

            var memberExpression = propertyExpression.Body as MemberExpression;
            if (memberExpression == null)
                throw new ArgumentException("Must be a member access expression", "propertyExpression");

            var @event = PropertyChanged;
            if (@event != null)
                @event(this, new PropertyChangedEventArgs(memberExpression.Member.Name));
        }
    }
}