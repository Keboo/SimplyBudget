using Microsoft.Toolkit.Mvvm.Input;
using SimplyBudget.Commands;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SimplyBudget.Utilities
{
    public static class AttachedProperties
    {
        #region Updating Command Parameter

        public static readonly DependencyProperty UpdatingCommandParameterProperty =
            DependencyProperty.RegisterAttached("UpdatingCommandParameter",
            typeof(object), typeof(AttachedProperties),
            new PropertyMetadata(default(object), OnUpdatingCommandParameterChanged));

        private static void OnUpdatingCommandParameterChanged(DependencyObject @do, DependencyPropertyChangedEventArgs e)
        {
            var commandSource = @do as ICommandSource;
            if (commandSource != null)
            {
                ((dynamic)commandSource).CommandParameter = e.NewValue;
                var command = commandSource.Command as IRelayCommand;
                if (command != null)
                    command.NotifyCanExecuteChanged();
                var raiseCanExecute = commandSource.Command as IRaiseCanExecute;
                if (raiseCanExecute != null)
                    raiseCanExecute.NotifyCanExecuteChanged();
            }
        }

        public static void SetUpdatingCommandParameter(DependencyObject @do, object value)
        {
            @do.SetValue(UpdatingCommandParameterProperty, value);
        }

        public static object GetUpdatingCommandParameter(DependencyObject @do)
        {
            return @do.GetValue(UpdatingCommandParameterProperty);
        }

        #endregion Updating Command Parameter

        #region Double Click Command

        public static readonly DependencyProperty DoubleClickCommandProperty =
            DependencyProperty.RegisterAttached("DoubleClickCommand",
            typeof(ICommand),
            typeof(AttachedProperties),
            new PropertyMetadata(default(ICommand), OnDoubleClickCommandChanged));

        private static void OnDoubleClickCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as Control;
            if (control != null)
            {
                if (e.NewValue != null)
                    control.MouseDoubleClick += ControlOnMouseDoubleClick;
                if (e.OldValue != null)
                    control.MouseDoubleClick -= ControlOnMouseDoubleClick;
            }
        }

        private static void ControlOnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var element = sender as DependencyObject;
            if (element != null)
            {
                var command = element.GetValue(DoubleClickCommandProperty) as ICommand;
                var commandParameter = element.GetValue(DoubleClickCommandParameterProperty);
                if (command != null && command.CanExecute(commandParameter))
                    command.Execute(commandParameter);
            }
        }

        public static void SetDoubleClickCommand(Control control, ICommand value)
        {
            control.SetValue(DoubleClickCommandProperty, value);
        }

        public static ICommand GetDoubleClickCommand(Control control)
        {
            return (ICommand)control.GetValue(DoubleClickCommandProperty);
        }

        #endregion Double Click Command

        #region Double Click Command Parameter

        public static readonly DependencyProperty DoubleClickCommandParameterProperty =
            DependencyProperty.RegisterAttached("DoubleClickCommandParameter", typeof(object), typeof(AttachedProperties), new PropertyMetadata(default(object)));

        public static void SetDoubleClickCommandParameter(UIElement element, object value)
        {
            element.SetValue(DoubleClickCommandParameterProperty, value);
        }

        public static object GetDoubleClickCommandParameter(UIElement element)
        {
            return element.GetValue(DoubleClickCommandParameterProperty);
        }

        #endregion Double Click Command Parameter

        #region Boolean Visibility

        public static readonly DependencyProperty BooleanVisibilityProperty =
            DependencyProperty.RegisterAttached("BooleanVisibility", typeof(bool),
            typeof(AttachedProperties),
            new PropertyMetadata(true, OnBooleanVisibilityChanged));

        private static void OnBooleanVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.SetValue(UIElement.VisibilityProperty, (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed);
        }

        public static void SetBooleanVisibility(UIElement element, bool value)
        {
            element.SetValue(BooleanVisibilityProperty, value);
        }

        public static bool GetBooleanVisibility(UIElement element)
        {
            return (bool)element.GetValue(BooleanVisibilityProperty);
        }

        #endregion Boolean Visibility

        public static readonly DependencyProperty SortPropertyNameProperty =
            DependencyProperty.RegisterAttached("SortPropertyName", typeof(string),
            typeof(AttachedProperties), new PropertyMetadata(default(string), OnSortPropertyNameChanged));

        private static void OnSortPropertyNameChanged(DependencyObject @do, DependencyPropertyChangedEventArgs e)
        {
            var column = @do as GridViewColumn;
            if (column is null) return;

            //TODO: This will break if I ever move the style
            var existingStyle = Application.Current.Resources[typeof (GridViewColumnHeader)] as Style;

            var headerStyle = column.HeaderContainerStyle ??
                              (column.HeaderContainerStyle = new Style(typeof(GridViewColumnHeader), existingStyle));
            var existing = headerStyle.Setters.OfType<Setter>().FirstOrDefault(x => x.Property == SortPropertyNameProperty);
            if (existing != null)
                headerStyle.Setters.Remove(existing);
            headerStyle.Setters.Add(new Setter(SortPropertyNameProperty, e.NewValue));
        }

        public static void SetSortPropertyName(GridViewColumn element, string value)
        {
            element.SetValue(SortPropertyNameProperty, value);
        }

        public static string GetSortPropertyName(GridViewColumn element)
        {
            return (string)element.GetValue(SortPropertyNameProperty);
        }

    }
}