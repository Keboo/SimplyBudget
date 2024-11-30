﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SimplyBudget.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public string Greeting => "Welcome to Avalonia!";

    [ObservableProperty]
    private int _counter;

    [RelayCommand]
    public void Increment() => Counter++;
}