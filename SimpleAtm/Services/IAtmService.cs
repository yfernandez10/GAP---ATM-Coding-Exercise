using SimpleAtm.Models;

namespace SimpleAtm.Services;

public interface IAtmService
{
    IReadOnlyList<Entry> History();
    string? Submit(TransactionForm form);
}
