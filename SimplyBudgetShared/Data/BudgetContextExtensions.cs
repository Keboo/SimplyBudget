using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace SimplyBudgetShared.Data
{
    public static class BudgetContextExtensions
    {
        public static async Task<int> GetCurrentAmount(this BudgetContext context, int accountId)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            Account account = await context.Accounts
                .Include(x => x.ExpenseCategories)
                .FirstOrDefaultAsync(x => x.ID == accountId);

            return account?.ExpenseCategories.Sum(x => x.CurrentBalance) ?? 0;
        }
    }
}
