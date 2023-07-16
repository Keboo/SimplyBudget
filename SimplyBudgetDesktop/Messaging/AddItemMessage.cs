using SimplyBudget.ViewModels;

namespace SimplyBudget.Messaging;

public record AddItemMessage(AddType Type, DateTime? Date, string? Description, IReadOnlyList<LineItem> Items)
{ }

public record LineItem(int Amount)
{ }
