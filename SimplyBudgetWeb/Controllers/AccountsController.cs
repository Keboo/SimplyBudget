using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimplyBudgetShared.Data;
using SimplyBudgetWeb.Data;

namespace SimplyBudgetWeb.Controllers;

[ApiController]
[Route("api/accounts")]
public class AccountsController(BudgetWebContext context) : ControllerBase
{
    [HttpGet]
    public async Task<AccountDto[]> GetAll()
    {
        var accounts = await context.Accounts.ToListAsync();
        var dtos = new List<AccountDto>();
        foreach (var account in accounts)
        {
            int currentAmount = await context.GetCurrentAmount(account.ID);
            dtos.Add(ToDto(account, currentAmount));
        }
        return dtos.ToArray();
    }

    [HttpPost]
    public async Task<ActionResult<AccountDto>> Create([FromBody] AccountRequest request)
    {
        var account = new Account
        {
            Name = request.Name,
            ValidatedDate = request.ValidatedDate ?? DateTime.Today,
        };
        context.Accounts.Add(account);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new { }, ToDto(account, 0));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AccountDto>> Update(int id, [FromBody] AccountRequest request)
    {
        var account = await context.Accounts.FindAsync(id);
        if (account is null) return NotFound();

        account.Name = request.Name;
        if (request.ValidatedDate.HasValue)
            account.ValidatedDate = request.ValidatedDate.Value;

        await context.SaveChangesAsync();
        int currentAmount = await context.GetCurrentAmount(account.ID);
        return ToDto(account, currentAmount);
    }

    [HttpPost("{id}/set-default")]
    public async Task<ActionResult<AccountDto>> SetDefault(int id)
    {
        var account = await context.Accounts.FindAsync(id);
        if (account is null) return NotFound();

        await context.SetAsDefaultAsync(account);
        await context.SaveChangesAsync();

        int currentAmount = await context.GetCurrentAmount(account.ID);
        return Ok(ToDto(account, currentAmount));
    }

    private static AccountDto ToDto(Account a, int currentAmount) => new(
        Id: a.ID,
        Name: a.Name,
        ValidatedDate: a.ValidatedDate,
        IsDefault: a.IsDefault,
        CurrentAmount: currentAmount
    );
}

public record AccountDto(
    int Id,
    string? Name,
    DateTime ValidatedDate,
    bool IsDefault,
    int CurrentAmount
);

public record AccountRequest(string? Name, DateTime? ValidatedDate);
