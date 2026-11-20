namespace SimpleAtm.Models;

public enum Account { Checking, Savings }
public enum Operation { Deposit, Withdraw, Transfer }

public sealed class TransactionForm
{
    public Guid RequestId { get; set; }
    public Operation? ActionType { get; set; }
    public Account? SelectedAccount { get; set; }
    public Account? DestinationAccount { get; set; }
    public string? Amount { get; set; }
}

public sealed record Transaction(Guid RequestId, Operation Operation, Account Account, decimal Amount, Account? DestinationAccount = null);
public sealed record Entry(Transaction Transaction, DateTimeOffset When, decimal CheckingAfter, decimal SavingsAfter);
public sealed record Dashboard(TransactionForm Form, IReadOnlyList<Entry> Entries);
