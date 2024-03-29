﻿using Microsoft.Xaml.Behaviors;
using System;
using System.Windows;
using System.Windows.Controls;

namespace SimplyBudget.Behaviors;

public class SelectAllOnFocusBehavior : Behavior<TextBox>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        DoSelection();
        AssociatedObject.GotFocus += OnGotFocus;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.GotFocus -= OnGotFocus;
        base.OnDetaching();
    }

    private void OnGotFocus(object sender, RoutedEventArgs routedEventArgs)
    {
        DoSelection();
    }

    private void DoSelection()
    {
        if (AssociatedObject.IsFocused)
        {
            Dispatcher.BeginInvoke(new Action(() => AssociatedObject.SelectAll()));
        }
    }
}