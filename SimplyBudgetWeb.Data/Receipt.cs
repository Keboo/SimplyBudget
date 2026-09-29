using System.ComponentModel.DataAnnotations;
using SimplyBudgetShared.Data;

namespace SimplyBudgetWeb.Data;

public class Receipt
{
    public int Id { get; set; }

    [MaxLength(500)]
    public string BlobName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string ContentType { get; set; } = "image/jpeg";

    [MaxLength(300)]
    public string? MerchantName { get; set; }

    public DateTime? TransactionDate { get; set; }

    public int? TotalAmountCents { get; set; }

    public string? Notes { get; set; }

    [MaxLength(40)]
    public string ProcessingStatus { get; set; } = "needs-review";

    [MaxLength(1000)]
    public string? ProcessingMessage { get; set; }

    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;

    public List<ReceiptLineItem> LineItems { get; set; } = [];
}

public class ReceiptLineItem
{
    public int Id { get; set; }

    public int ReceiptId { get; set; }

    public Receipt? Receipt { get; set; }

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public int AmountCents { get; set; }
}

public class ReceiptExpenseLink
{
    public int ReceiptId { get; set; }

    public Receipt? Receipt { get; set; }

    public int ExpenseCategoryItemId { get; set; }

    public ExpenseCategoryItem? ExpenseCategoryItem { get; set; }
}
