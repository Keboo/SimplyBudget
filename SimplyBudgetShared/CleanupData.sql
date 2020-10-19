DELETE FROM IncomeItem WHERE ID IN(
SELECT IncomeItem.ID FROM IncomeItem
LEFT JOIN ExpenseCategory 
ON IncomeItem.ExpenseCategoryID = ExpenseCategory.ID
WHERE ExpenseCategory.ID is null);

DELETE FROM Transfer WHERE Transfer.ID IN(
SELECT Transfer.ID FROM Transfer
LEFT JOIN ExpenseCategory 
ON Transfer.FromExpenseCategoryID = ExpenseCategory.ID
WHERE ExpenseCategory.ID is null);