using System.Globalization;
using System.Text.RegularExpressions;
using SimpleAtm.Models;

namespace SimpleAtm.Services;

public sealed class TransactionValidator
{
    public string? Validate(TransactionForm form, out Transaction? transaction)
    {
        transaction = null;
        if (form.RequestId == Guid.Empty)
            return "Reload the page to start a new transaction.";
        if (form.ActionType is not Operation operation || !Enum.IsDefined(operation))
            return "Select a valid operation.";
        if (form.SelectedAccount is not Account account || !Enum.IsDefined(account))
            return operation == Operation.Transfer ? "Select a valid From account." : "Select a valid account.";

        Account? destination = null;
        if (operation == Operation.Transfer)
        {
            destination = form.DestinationAccount;
            if (destination is null || !Enum.IsDefined(destination.Value))
                return "Select a valid To account.";
            if (destination == account)
                return "Choose different accounts for the transfer.";
        }

        var text = form.Amount?.Trim() ?? "";
        if (text.Length > 18 || !Regex.IsMatch(text, @"\A[0-9]{1,15}(\.[0-9]{1,2})?\z") ||
            !decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount) ||
            amount <= 0)
            return "Enter a positive amount, such as 25 or 25.50 (up to 15 whole digits).";

        transaction = new Transaction(form.RequestId, operation, account, amount, destination);
        return null;
    }

    public string? ValidateBalances(decimal checkingAfter, decimal savingsAfter)
    {
        if (checkingAfter < 0 || savingsAfter < 0)
            return "Insufficient funds in the selected account.";

        const decimal maxBalance = 999_999_999_999_999.99m;
        return checkingAfter > maxBalance || savingsAfter > maxBalance
            ? "This transaction exceeds the supported account balance." : null;
    }
}
