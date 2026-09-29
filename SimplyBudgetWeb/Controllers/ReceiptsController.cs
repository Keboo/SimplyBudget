using Azure;
using Azure.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplyBudgetWeb.Data;
using SimplyBudgetWeb.Services;
using SimplyBudgetShared.Data;

namespace SimplyBudgetWeb.Controllers;

[ApiController]
[Route("api/receipts")]
public class ReceiptsController(
    BudgetWebContext context,
    IReceiptImageStore imageStore,
    IReceiptAnalyzer analyzer,
    ILogger<ReceiptsController> logger,
    IBudgetMonthDataCache? budgetMonthDataCache = null,
    IBudgetMonthUpdateNotifier? budgetMonthUpdateNotifier = null) : ControllerBase
{
    private const long MaximumImageBytes = 15 * 1024 * 1024;
    private static readonly Dictionary<string, string> SupportedImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp",
    };
    private readonly IBudgetMonthDataCache monthDataCache = budgetMonthDataCache ?? NullBudgetMonthDataCache.Instance;
    private readonly IBudgetMonthUpdateNotifier monthUpdates = budgetMonthUpdateNotifier ?? NullBudgetMonthUpdateNotifier.Instance;

    [HttpGet]
    public async Task<ReceiptDto[]> GetAll()
    {
        var receipts = await context.Receipts
            .Include(x => x.LineItems)
            .OrderByDescending(x => x.UploadedAtUtc)
            .ToListAsync();
        return receipts.Select(ReceiptMapper.ToDto).ToArray();
    }

    [HttpGet("matches")]
    public async Task<ActionResult<ReceiptDto[]>> FindMatches([FromQuery] int amount, [FromQuery] DateTime date)
    {
        if (amount <= 0)
            return BadRequest("Amount must be greater than zero.");

        var start = date.Date.AddDays(-5);
        var end = date.Date.AddDays(5);
        var linkedReceiptIds = context.ReceiptExpenseLinks.Select(x => x.ReceiptId);
        var matches = await context.Receipts
            .Include(x => x.LineItems)
            .Where(x =>
                x.TotalAmountCents == amount &&
                x.TransactionDate >= start &&
                x.TransactionDate <= end &&
                !linkedReceiptIds.Contains(x.Id))
            .ToListAsync();

        return matches
            .OrderBy(x => Math.Abs((x.TransactionDate!.Value.Date - date.Date).Days))
            .ThenByDescending(x => x.UploadedAtUtc)
            .Select(ReceiptMapper.ToDto)
            .ToArray();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReceiptDto>> GetById(int id)
    {
        var receipt = await context.Receipts
            .Include(x => x.LineItems)
            .FirstOrDefaultAsync(x => x.Id == id);
        return receipt is null ? NotFound() : ReceiptMapper.ToDto(receipt);
    }

    [HttpGet("{id:int}/image")]
    public async Task<IActionResult> GetImage(int id, CancellationToken cancellationToken)
    {
        var receipt = await context.Receipts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (receipt is null)
            return NotFound();

        try
        {
            var stream = await imageStore.OpenReadAsync(receipt.BlobName, cancellationToken);
            return File(stream, receipt.ContentType, enableRangeProcessing: true);
        }
        catch (ReceiptIntegrationNotConfiguredException exception)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, detail: exception.Message);
        }
        catch (RequestFailedException exception) when (exception.Status == StatusCodes.Status404NotFound)
        {
            logger.LogWarning(exception, "Receipt image blob {BlobName} was not found.", receipt.BlobName);
            return NotFound("Receipt image not found.");
        }
        catch (RequestFailedException exception)
        {
            logger.LogError(exception, "Receipt image storage request failed for receipt {ReceiptId}.", receipt.Id);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, detail: "Receipt storage is unavailable.");
        }
        catch (AuthenticationFailedException exception)
        {
            logger.LogError(exception, "Receipt image storage authentication failed for receipt {ReceiptId}.", receipt.Id);
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, detail: "Receipt storage access is unavailable.");
        }
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaximumImageBytes + 1024 * 1024)]
    public async Task<ActionResult<ReceiptDto>> Upload(
        [FromForm] IFormFile? image,
        CancellationToken cancellationToken)
    {
        if (image is null || image.Length == 0)
            return BadRequest("An image is required.");
        if (image.Length > MaximumImageBytes)
            return BadRequest("Receipt images must be 15 MB or smaller.");

        var extension = Path.GetExtension(image.FileName);
        var fileName = Path.GetFileName(image.FileName);
        if (fileName.Length > 255)
            return BadRequest("Receipt file names must be 255 characters or fewer.");
        if (!SupportedImageTypes.TryGetValue(extension, out var contentType) ||
            !string.Equals(image.ContentType, contentType, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Upload a JPEG, PNG, or WebP receipt image.");
        }

        await using var buffer = new MemoryStream((int)image.Length);
        await image.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();
        if (!HasExpectedImageSignature(bytes, contentType))
            return BadRequest("The uploaded file does not contain a valid image.");

        string blobName;
        try
        {
            await using var uploadStream = new MemoryStream(bytes, writable: false);
            blobName = await imageStore.UploadAsync(
                fileName,
                contentType,
                uploadStream,
                cancellationToken);
        }
        catch (ReceiptIntegrationNotConfiguredException exception)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, detail: exception.Message);
        }
        catch (RequestFailedException exception)
        {
            logger.LogError(exception, "Receipt image upload failed.");
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, detail: "Receipt storage is unavailable.");
        }
        catch (AuthenticationFailedException exception)
        {
            logger.LogError(exception, "Receipt image upload could not authenticate to storage.");
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, detail: "Receipt storage access is unavailable.");
        }

        var receipt = new Receipt
        {
            BlobName = blobName,
            FileName = fileName,
            ContentType = contentType,
            ProcessingStatus = "processing",
        };
        context.Receipts.Add(receipt);
        await context.SaveChangesAsync(cancellationToken);

        try
        {
            var extraction = await analyzer.AnalyzeAsync(bytes, cancellationToken);
            receipt.MerchantName = extraction.MerchantName;
            receipt.TransactionDate = extraction.TransactionDate?.Date;
            receipt.TotalAmountCents = extraction.TotalAmountCents;
            receipt.LineItems = extraction.LineItems
                .Select(x => new ReceiptLineItem
                {
                    Description = x.Description,
                    AmountCents = x.AmountCents,
                })
                .ToList();
            receipt.ProcessingStatus = "needs-review";
            receipt.ProcessingMessage = extraction.TotalAmountCents is null
                ? "Review the receipt details; the total was not recognized."
                : null;
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (ReceiptIntegrationNotConfiguredException exception)
        {
            receipt.ProcessingStatus = "needs-review";
            receipt.ProcessingMessage = exception.Message;
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (RequestFailedException exception)
        {
            logger.LogWarning(exception, "Receipt extraction failed for receipt {ReceiptId}.", receipt.Id);
            receipt.ProcessingStatus = "needs-review";
            receipt.ProcessingMessage = "Automatic extraction failed. Enter or correct the receipt details.";
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (AuthenticationFailedException exception)
        {
            logger.LogWarning(exception, "Receipt extraction could not authenticate for receipt {ReceiptId}.", receipt.Id);
            receipt.ProcessingStatus = "needs-review";
            receipt.ProcessingMessage = "Automatic extraction is unavailable. Enter or correct the receipt details.";
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidDataException exception)
        {
            logger.LogWarning(exception, "Receipt extraction returned invalid amounts for receipt {ReceiptId}.", receipt.Id);
            receipt.ProcessingStatus = "needs-review";
            receipt.ProcessingMessage = "Automatic extraction returned invalid values. Enter or correct the receipt details.";
            await context.SaveChangesAsync(cancellationToken);
        }

        return CreatedAtAction(nameof(GetById), new { id = receipt.Id }, ReceiptMapper.ToDto(receipt));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ReceiptDto>> Update(int id, [FromBody] ReceiptUpdateRequest request)
    {
        var receipt = await context.Receipts
            .AsTracking()
            .Include(x => x.LineItems)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (receipt is null)
            return NotFound();
        if (request.MerchantName?.Length > 300)
            return BadRequest("Merchant name must be 300 characters or fewer.");
        if (request.Notes?.Length > 10000)
            return BadRequest("Notes must be 10,000 characters or fewer.");
        if (request.LineItems is null ||
            request.LineItems.Length > 200 ||
            request.LineItems.Any(x =>
                string.IsNullOrWhiteSpace(x.Description) ||
                x.Description.Length > 500 ||
                x.AmountCents <= 0))
        {
            return BadRequest("Receipt line items must have a description and a positive amount.");
        }
        if (request.TotalAmountCents is <= 0)
            return BadRequest("Receipt total must be greater than zero.");

        var linkedExpenseDate = await context.ReceiptExpenseLinks
            .Where(x => x.ReceiptId == id)
            .Select(x => (DateTime?)x.ExpenseCategoryItem!.Date)
            .FirstOrDefaultAsync();

        receipt.MerchantName = Normalize(request.MerchantName);
        receipt.TransactionDate = request.TransactionDate?.Date;
        receipt.TotalAmountCents = request.TotalAmountCents;
        receipt.Notes = Normalize(request.Notes);
        receipt.ProcessingStatus = "reviewed";
        receipt.ProcessingMessage = null;
        context.ReceiptLineItems.RemoveRange(receipt.LineItems);
        receipt.LineItems = request.LineItems
            .Select(x => new ReceiptLineItem
            {
                Description = x.Description.Trim(),
                AmountCents = x.AmountCents,
            })
            .ToList();
        await context.SaveChangesAsync();
        if (linkedExpenseDate.HasValue)
        {
            monthDataCache.InvalidateMonth(linkedExpenseDate.Value);
            await monthUpdates.NotifyMonthUpdated(linkedExpenseDate.Value);
        }
        return ReceiptMapper.ToDto(receipt);
    }

    private static bool HasExpectedImageSignature(byte[] bytes, string contentType)
        => contentType switch
        {
            "image/jpeg" => bytes.Length >= 3 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff,
            "image/png" => bytes.Length >= 8 &&
                bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }),
            "image/webp" => bytes.Length >= 12 &&
                bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
                bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            _ => false,
        };

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public record ReceiptLineItemUpdateRequest(string Description, int AmountCents);

public record ReceiptUpdateRequest(
    string? MerchantName,
    DateTime? TransactionDate,
    int? TotalAmountCents,
    string? Notes,
    ReceiptLineItemUpdateRequest[] LineItems);
