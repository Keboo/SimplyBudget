using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace SimplyBudget.Controls;

/// <summary>
/// Interaction logic for BarGraph.xaml
/// </summary>
public partial class BarGraph
{
    public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
        "Items", typeof (IEnumerable<IBarGraphItem>), typeof (BarGraph), new PropertyMetadata(default(IEnumerable<IBarGraphItem>)));

    public IEnumerable<IBarGraphItem> Items
    {
        get => (IEnumerable<IBarGraphItem>)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public static readonly DependencyProperty BarBrushProperty = DependencyProperty.Register(
        "BarBrush", typeof(Brush), typeof(BarGraph), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(124, 190, 216))));

    public Brush BarBrush
    {
        get => (Brush)GetValue(BarBrushProperty);
        set => SetValue(BarBrushProperty, value);
    }

    public static readonly DependencyProperty LineBrushProperty = DependencyProperty.Register(
        "LineBrush", typeof(Brush), typeof(BarGraph), new PropertyMetadata(new SolidColorBrush(Color.FromRgb(37, 100, 124))));

    public Brush LineBrush
    {
        get => (Brush)GetValue(LineBrushProperty);
        set => SetValue(LineBrushProperty, value);
    }

    public static readonly DependencyProperty BarItemDisplayTemplateProperty = DependencyProperty.Register(
        "BarItemDisplayTemplate", typeof (DataTemplate), typeof (BarGraph), new PropertyMetadata(default(DataTemplate)));

    public DataTemplate BarItemDisplayTemplate
    {
        get => (DataTemplate)GetValue(BarItemDisplayTemplateProperty);
        set => SetValue(BarItemDisplayTemplateProperty, value);
    }

    public BarGraph()
    {
        InitializeComponent();
    }
}
