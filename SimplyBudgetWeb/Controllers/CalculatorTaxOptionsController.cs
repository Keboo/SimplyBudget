using System.Text.Json;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using SimplyBudgetShared.Data;
using SimplyBudgetWeb.Data;

namespace SimplyBudgetWeb.Controllers;

[ApiController]
[Route("api/calculator-tax-options")]
public class CalculatorTaxOptionsController(BudgetWebContext context) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [HttpGet]
    public async Task<CalculatorTaxOptionsDto> Get()
    {
        var serialized = await context.Metadatas
            .Where(x => x.Key == Metadata.CALCULATOR_TAX_OPTIONS_KEY)
            .OrderBy(x => x.ID)
            .Select(x => x.Value)
            .FirstOrDefaultAsync();

        return new CalculatorTaxOptionsDto(ParseStoredOptions(serialized));
    }

    [HttpPut]
    public async Task<ActionResult<CalculatorTaxOptionsDto>> Update([FromBody] UpdateCalculatorTaxOptionsRequest request)
    {
        if (request.Options is null)
        {
            return BadRequest("Options are required.");
        }

        if (!TryValidateAndNormalizeForSave(request.Options, out var normalizedOptions, out var validationError))
        {
            return BadRequest(validationError);
        }

        var serialized = JsonSerializer.Serialize(normalizedOptions, JsonOptions);
        var existingMetadataRows = await context.Metadatas
            .AsTracking()
            .Where(x => x.Key == Metadata.CALCULATOR_TAX_OPTIONS_KEY)
            .OrderBy(x => x.ID)
            .ToListAsync();

        if (existingMetadataRows.Count == 0)
        {
            context.Metadatas.Add(new Metadata
            {
                Key = Metadata.CALCULATOR_TAX_OPTIONS_KEY,
                Value = serialized,
            });
        }
        else
        {
            existingMetadataRows[0].Value = serialized;

            if (existingMetadataRows.Count > 1)
            {
                context.Metadatas.RemoveRange(existingMetadataRows.Skip(1));
            }
        }

        await context.SaveChangesAsync();
        return Ok(new CalculatorTaxOptionsDto(normalizedOptions));
    }

    private static CalculatorTaxOptionDto[] ParseStoredOptions(string? serialized)
    {
        if (string.IsNullOrWhiteSpace(serialized))
        {
            return GetDefaultOptions();
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<IReadOnlyList<CalculatorTaxOptionDto>>(serialized, JsonOptions);
            return NormalizeForRead(parsed);
        }
        catch (JsonException)
        {
            return GetDefaultOptions();
        }
    }

    private static CalculatorTaxOptionDto[] NormalizeForRead(IReadOnlyList<CalculatorTaxOptionDto>? options)
    {
        if (options is null || options.Count == 0)
        {
            return GetDefaultOptions();
        }

        var normalized = new List<CalculatorTaxOptionDto>();
        foreach (var option in options)
        {
            var name = option.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            if (name.Length > 100 || option.Percentage <= 0m || option.Percentage > 100m)
            {
                continue;
            }

            normalized.Add(new CalculatorTaxOptionDto(
                Name: name,
                Percentage: decimal.Round(option.Percentage, 3, MidpointRounding.AwayFromZero),
                IsDefault: option.IsDefault
            ));
        }

        if (normalized.Count == 0)
        {
            return GetDefaultOptions();
        }

        var defaultIndex = normalized.FindIndex(x => x.IsDefault);
        if (defaultIndex >= 0)
        {
            normalized = [.. normalized.Select((option, index) => option with { IsDefault = index == defaultIndex })];
        }

        return [.. normalized];
    }

    private static bool TryValidateAndNormalizeForSave(
        IReadOnlyList<CalculatorTaxOptionDto> options,
        out CalculatorTaxOptionDto[] normalizedOptions,
        out string? validationError)
    {
        normalizedOptions = [];
        validationError = null;

        if (options.Count > 20)
        {
            validationError = "No more than 20 tax options are allowed.";
            return false;
        }

        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var normalized = new List<CalculatorTaxOptionDto>(options.Count);
        var defaultCount = 0;

        for (var index = 0; index < options.Count; index++)
        {
            var option = options[index];
            var name = option.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name))
            {
                validationError = $"Tax option #{index + 1} must include a name.";
                return false;
            }

            if (name.Length > 100)
            {
                validationError = $"Tax option '{name}' must be 100 characters or fewer.";
                return false;
            }

            if (option.Percentage <= 0m || option.Percentage > 100m)
            {
                validationError = $"Tax option '{name}' must be greater than 0 and no more than 100.";
                return false;
            }

            if (!seenNames.Add(name))
            {
                validationError = $"Tax option '{name}' is duplicated.";
                return false;
            }

            if (option.IsDefault)
            {
                defaultCount++;
            }

            normalized.Add(new CalculatorTaxOptionDto(
                Name: name,
                Percentage: decimal.Round(option.Percentage, 3, MidpointRounding.AwayFromZero),
                IsDefault: option.IsDefault
            ));
        }

        if (defaultCount > 1)
        {
            validationError = "Only one tax option can be marked as default.";
            return false;
        }

        normalizedOptions = [.. normalized];
        return true;
    }

    private static CalculatorTaxOptionDto[] GetDefaultOptions() =>
    [
        new CalculatorTaxOptionDto("Tax", 9.1m, false),
    ];
}

public record CalculatorTaxOptionsDto(IReadOnlyList<CalculatorTaxOptionDto> Options);

public record UpdateCalculatorTaxOptionsRequest(IReadOnlyList<CalculatorTaxOptionDto>? Options);

public record CalculatorTaxOptionDto(string Name, decimal Percentage, bool IsDefault);
