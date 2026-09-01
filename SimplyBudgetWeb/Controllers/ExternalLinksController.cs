using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using SimplyBudgetShared.Data;

using SimplyBudgetWeb.Data;
using System.Text.RegularExpressions;

namespace SimplyBudgetWeb.Controllers;

[ApiController]
[Route("api/external-links")]
public class ExternalLinksController(BudgetWebContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ExternalLinkRuleDto[]> GetAll()
    {
        var rules = await context.ExternalLinkRules
            .OrderBy(r => r.Name)
            .ToListAsync();
        return rules.Select(ToDto).ToArray();
    }

    [HttpPost]
    public async Task<ActionResult<ExternalLinkRuleDto>> Create([FromBody] ExternalLinkRuleRequest request)
    {
        if (Validate(request) is { } error) return error;

        var rule = new ExternalLinkRule
        {
            Name = request.Name,
            RuleRegex = request.RuleRegex,
            Url = request.Url,
        };
        context.ExternalLinkRules.Add(rule);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new { }, ToDto(rule));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ExternalLinkRuleDto>> Update(int id, [FromBody] ExternalLinkRuleRequest request)
    {
        if (Validate(request) is { } error) return error;

        var rule = await context.ExternalLinkRules.FirstOrDefaultAsync(r => r.ID == id);
        if (rule is null) return NotFound();

        rule.Name = request.Name;
        rule.RuleRegex = request.RuleRegex;
        rule.Url = request.Url;

        context.ExternalLinkRules.Update(rule);
        await context.SaveChangesAsync();
        return ToDto(rule);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var rule = await context.ExternalLinkRules.FindAsync(id);
        if (rule is null) return NotFound();

        context.ExternalLinkRules.Remove(rule);
        await context.SaveChangesAsync();
        return NoContent();
    }

    private BadRequestObjectResult? Validate(ExternalLinkRuleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RuleRegex))
        {
            return BadRequest("A regular expression is required");
        }

        try
        {
            _ = Regex.Match("", request.RuleRegex, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
        }
        catch (ArgumentException)
        {
            return BadRequest("The regular expression is not valid");
        }

        if (string.IsNullOrWhiteSpace(request.Url) ||
            !Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return BadRequest("An absolute http(s) URL is required");
        }

        return null;
    }

    private static ExternalLinkRuleDto ToDto(ExternalLinkRule rule) => new(
        Id: rule.ID,
        Name: rule.Name,
        RuleRegex: rule.RuleRegex,
        Url: rule.Url
    );
}

public record ExternalLinkRuleDto(
    int Id,
    string? Name,
    string? RuleRegex,
    string? Url
);

public record ExternalLinkRuleRequest(string? Name, string? RuleRegex, string? Url);
