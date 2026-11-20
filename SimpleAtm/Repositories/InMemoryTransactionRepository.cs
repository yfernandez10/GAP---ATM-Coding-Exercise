using SimpleAtm.Models;

namespace SimpleAtm.Repositories;

public sealed class InMemoryTransactionRepository : ITransactionRepository
{
    private readonly object gate = new();
    private readonly List<Entry> entries = [];

    public IReadOnlyList<Entry> ReadHistory()
    {
        lock (gate)
            return entries.AsEnumerable().Reverse().ToArray();
    }

    public string? Append(Transaction transaction, Func<Entry?, PostingDecision> decide)
    {
        lock (gate)
        {
            var prior = entries.Find(e => e.Transaction.RequestId == transaction.RequestId);
            if (prior is not null)
                return prior.Transaction == transaction ? null : "This form was already used. Reload and try again.";

            switch (decide(entries.LastOrDefault()))
            {
                case Rejected rejected:
                    return rejected.Reason;
                case Accepted accepted:
                    entries.Add(accepted.Entry);
                    return null;
                default:
                    throw new InvalidOperationException("Unknown posting decision.");
            }
        }
    }
}
