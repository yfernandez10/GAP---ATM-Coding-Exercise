using Moq;
using SimpleAtm.Models;
using SimpleAtm.Repositories;
using SimpleAtm.Services;
using Xunit;

namespace SimpleAtm.Tests;

public class AtmServiceTests
{
    private readonly Mock<ITransactionRepository> repository = new(MockBehavior.Strict);

    [Theory]
    [InlineData(Operation.Deposit, Account.Checking, 12550, 5000)]
    [InlineData(Operation.Withdraw, Account.Savings, 10000, 2450)]
    [InlineData(Operation.Transfer, Account.Checking, 7450, 7550)]
    [InlineData(Operation.Transfer, Account.Savings, 12550, 2450)]
    public void Submit_ValidAmount_ProposesCorrectEndingBalances(
        Operation operation, Account account, int checkingCents, int savingsCents)
    {
        var form = NewForm(operation, account, "25.50");
        var expected = new Transaction(form.RequestId, operation, account, 25.50m, form.DestinationAccount);
        var prior = new Entry(new Transaction(Guid.NewGuid(), Operation.Deposit, Account.Savings, 50m),
            DateTimeOffset.UnixEpoch, 100m, 50m);
        repository.Setup(r => r.Append(expected, It.IsAny<Func<Entry?, PostingDecision>>()))
            .Returns((Transaction tx, Func<Entry?, PostingDecision> decide) =>
            {
                var entry = Assert.IsType<Accepted>(decide(prior)).Entry;
                Assert.Equal(expected, entry.Transaction);
                Assert.Equal(checkingCents / 100m, entry.CheckingAfter);
                Assert.Equal(savingsCents / 100m, entry.SavingsAfter);
                return null;
            });
        var service = new AtmService(repository.Object, new TransactionValidator());

        Assert.Null(service.Submit(form));
        repository.Verify(r => r.Append(expected, It.IsAny<Func<Entry?, PostingDecision>>()), Times.Once);
    }

    [Theory]
    [InlineData(Operation.Withdraw)]
    [InlineData(Operation.Transfer)]
    public void Submit_EmptySourceAccount_ReturnsInsufficientFunds(Operation operation)
    {
        repository.Setup(r => r.Append(It.IsAny<Transaction>(), It.IsAny<Func<Entry?, PostingDecision>>()))
            .Returns((Transaction tx, Func<Entry?, PostingDecision> decide) =>
                Assert.IsType<Rejected>(decide(null)).Reason);
        var service = new AtmService(repository.Object, new TransactionValidator());

        var error = service.Submit(NewForm(operation, Account.Checking, "1"));

        Assert.Equal("Insufficient funds in the selected account.", error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-10")]
    [InlineData("1.234")]
    public void Submit_InvalidAmount_ReturnsErrorBeforeAccessingRepository(string? amount)
    {
        var service = new AtmService(repository.Object, new TransactionValidator());

        var error = service.Submit(NewForm(Operation.Deposit, Account.Checking, amount));

        Assert.False(string.IsNullOrWhiteSpace(error));
        repository.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData(Account.Checking)]
    [InlineData((Account)99)]
    public void Submit_InvalidDestination_RejectsTransferBeforePosting(Account? destination)
    {
        var form = NewForm(Operation.Transfer, Account.Checking, "5");
        form.DestinationAccount = destination;
        var service = new AtmService(repository.Object, new TransactionValidator());

        Assert.NotNull(service.Submit(form));
        repository.VerifyNoOtherCalls();
    }

    private static TransactionForm NewForm(Operation operation, Account account, string? amount) =>
        new()
        {
            RequestId = Guid.NewGuid(),
            ActionType = operation,
            SelectedAccount = account,
            DestinationAccount = operation != Operation.Transfer ? null :
                account == Account.Checking ? Account.Savings : Account.Checking,
            Amount = amount
        };
}
