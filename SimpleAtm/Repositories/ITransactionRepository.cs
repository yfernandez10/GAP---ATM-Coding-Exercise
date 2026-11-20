using SimpleAtm.Models;

namespace SimpleAtm.Repositories;

public interface ITransactionRepository
{
    IReadOnlyList<Entry> ReadHistory();

    // Compute from the latest entry and append as a single atomic operation.
    // A repeated command returns its original success without another append.
    string? Append(Transaction transaction, Func<Entry?, PostingDecision> decide);
}

public abstract record PostingDecision;
public sealed record Accepted(Entry Entry) : PostingDecision;
public sealed record Rejected(string Reason) : PostingDecision;
