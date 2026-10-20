using SimplyBudgetWeb.Data;

namespace SimplyBudgetWeb.Services;

public record ReceiptLineItemDto(int Id, string Description, int AmountCents);

public record ReceiptDto(
    int Id,
    string? MerchantName,
    DateTime? TransactionDate,
    int? TotalAmountCents,
    string? Notes,
    string ProcessingStatus,
    string? ProcessingMessage,
    string FileName,
    DateTime UploadedAtUtc,
    ReceiptLineItemDto[] LineItems,
    bool IsLinked = false,
    ReceiptImageGeometry? ImageGeometry = null);

public static class ReceiptMapper
{
    public static ReceiptDto ToDto(Receipt receipt) => new(
        receipt.Id,
        receipt.MerchantName,
        receipt.TransactionDate,
        receipt.TotalAmountCents,
        receipt.Notes,
        receipt.ProcessingStatus,
        receipt.ProcessingMessage,
        receipt.FileName,
        receipt.UploadedAtUtc,
        receipt.LineItems
            .OrderBy(x => x.Id)
            .Select(x => new ReceiptLineItemDto(x.Id, x.Description, x.AmountCents))
            .ToArray(),
        ImageGeometry: receipt.ImageGeometryJson is { } json && ReceiptImageGeometry.TryParse(json, out var geometry)
            ? geometry
            : null);
}
