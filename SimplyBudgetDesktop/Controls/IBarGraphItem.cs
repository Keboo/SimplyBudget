namespace SimplyBudget.Controls
{
    public interface IBarGraphItem
    {
        string BarTitle { get; }
        int BarPercentHeight { get; }
        int? LinePercentHeight { get; }
    }
}