using SimpleAtm.Models;
using SimpleAtm.Repositories;

namespace SimpleAtm.Services;

public sealed class AtmService(ITransactionRepository repository, TransactionValidator validator) : IAtmService
{
    public IReadOnlyList<Entry> History() => repository.ReadHistory();

    public string? Submit(TransactionForm form)
    {
        var error = validator.Validate(form, out var transaction);
        if (error is not null)
            return error;

        return repository.Append(transaction!, latest => Apply(transaction!, latest));
    }

    private PostingDecision Apply(Transaction tx, Entry? latest)
    {
        decimal checking = latest?.CheckingAfter ?? 0m;
        decimal savings = latest?.SavingsAfter ?? 0m;
        decimal selectedDelta = tx.Operation == Operation.Deposit ? tx.Amount : -tx.Amount;

        if (tx.Account == Account.Checking)
            checking += selectedDelta;
        else
            savings += selectedDelta;

        if (tx.DestinationAccount == Account.Checking)
            checking += tx.Amount;
        else if (tx.DestinationAccount == Account.Savings)
            savings += tx.Amount;

        var error = validator.ValidateBalances(checking, savings);
        if (error is not null)
            return new Rejected(error);

        return new Accepted(new Entry(tx, DateTimeOffset.UtcNow, checking, savings));
    }
}
