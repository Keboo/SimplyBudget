using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SimplyBudgetShared.DataTransfer;
using SimplyBudgetWeb.Data;

namespace SimplyBudgetWeb.Controllers;

[ApiController]
[Route("api/data-portability")]
public class DataPortabilityController(BudgetWebContext context) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    [HttpGet("export")]
    public async Task<IActionResult> Export(CancellationToken cancellationToken)
    {
        var exportPackage = await BudgetDataPortabilityService.ExportAsync(context, source: "web", cancellationToken);
        var fileName = $"simplybudget-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
        var payload = JsonSerializer.SerializeToUtf8Bytes(exportPackage, JsonOptions);
        return File(payload, "application/json", fileName);
    }

    [HttpPost("import")]
    public async Task<IActionResult> Import([FromBody] BudgetDataExportPackage? exportPackage, CancellationToken cancellationToken)
    {
        if (exportPackage is null)
        {
            return BadRequest("A data export payload is required.");
        }

        await BudgetDataPortabilityService.ImportAsync(context, exportPackage, cancellationToken);
        return NoContent();
    }
}
