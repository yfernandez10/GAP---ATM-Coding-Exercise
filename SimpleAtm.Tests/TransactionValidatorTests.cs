using SimpleAtm.Models;
using SimpleAtm.Services;
using Xunit;

namespace SimpleAtm.Tests;

public class TransactionValidatorTests
{
    private readonly TransactionValidator validator = new();

    [Theory]
    [InlineData(null, Account.Savings, "Select a valid From account.")]
    [InlineData((Account)99, Account.Savings, "Select a valid From account.")]
    [InlineData(Account.Checking, null, "Select a valid To account.")]
    [InlineData(Account.Checking, (Account)99, "Select a valid To account.")]
    [InlineData(Account.Checking, Account.Checking, "Choose different accounts for the transfer.")]
    [InlineData(Account.Savings, Account.Savings, "Choose different accounts for the transfer.")]
    public void Validate_InvalidTransfer_IdentifiesProblemAndProducesNoTransaction(
        Account? source, Account? destination, string expectedError)
    {
        var form = CreateForm(Operation.Transfer, source, destination);

        var error = validator.Validate(form, out var transaction);

        Assert.Equal(expectedError, error);
        Assert.Null(transaction);
    }

    [Theory]
    [InlineData(Operation.Deposit)]
    [InlineData(Operation.Withdraw)]
    public void Validate_SingleAccountOperation_DiscardsLeftoverDestination(Operation operation)
    {
        var form = CreateForm(operation, Account.Checking, Account.Savings);

        var error = validator.Validate(form, out var transaction);

        Assert.Null(error);
        Assert.NotNull(transaction);
        Assert.Equal(Account.Checking, transaction.Account);
        Assert.Null(transaction.DestinationAccount);
    }

    private static TransactionForm CreateForm(Operation operation, Account? source, Account? destination) =>
        new()
        {
            RequestId = Guid.NewGuid(),
            ActionType = operation,
            SelectedAccount = source,
            DestinationAccount = destination,
            Amount = "10.00"
        };
}
