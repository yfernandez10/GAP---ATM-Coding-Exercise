# Simple ATM

A single user can deposit, withdraw, and transfer money between two USD accounts, Checking and Savings. Both start at zero. The dashboard displays each balance and a history of successful transactions. No login is required.

## Run locally

Extract the ZIP and open `SimpleAtm.slnx` in Visual Studio 2026 with the **ASP.NET and web development** workload and the .NET 8 SDK installed. Select the **SimpleAtm** launch profile and press **F5** to debug, or **Ctrl+F5** to run. The browser opens at http://localhost:5187.

From a terminal in the solution folder, use `dotnet run --project SimpleAtm`.

## Why MVC?

The app has one browser workflow. A Razor view displays both accounts, submits an HTML form, and shows validation feedback. ASP.NET Core MVC supports that flow directly. A successful submission redirects to the dashboard, where the user sees the updated balances and history. The page uses Bootstrap 5.3.9, bundled locally for offline use.

The transaction logic sits behind `IAtmService`. If a mobile client later needs JSON responses, an API controller can call the same service and reuse the validation and balance rules.

## Where the code lives

| File or type | Responsibility |
|---|---|
| `Views/Atm/Index.cshtml` | Renders the form, balance cards, and history from a single dashboard snapshot. |
| `wwwroot/transfer-form.js` | Shows **From account** and **To account** for transfers and keeps the two selections distinct. Deposits and withdrawals use one account. |
| `Controllers/AtmController.cs` | Handles `GET /` and `POST /transactions`, checks MVC binding errors, calls the service, and chooses the view or redirect. |
| `Services/AtmService.cs` | Implements `IAtmService`. Debits the source and credits the selected destination for a transfer. Deposits and withdrawals affect only the selected account. |
| `Services/TransactionValidator.cs` | Parses a positive amount with up to two decimal places into a C# `decimal`. Requires two valid, distinct accounts for a transfer and discards any destination on a deposit or withdrawal. Checks proposed balances for overdrafts and the supported maximum. |
| `Repositories/InMemoryTransactionRepository.cs` | Implements `ITransactionRepository`. Owns the history, detects duplicate submissions, and commits an accepted operation under a lock. |
| `Models/Transactions.cs` | Defines `TransactionForm` for browser input, `Transaction` for a validated request, `Entry` for a committed result, and `Dashboard` for the view. |
| `Program.cs` | Registers the repository as a singleton so requests share the same accounts, the service as scoped, and the stateless validator as a singleton. |

## Trace a transfer in the debugger

Deposit **$100** in Checking. Then set a breakpoint in `AtmController.Submit` and choose **Transfer**. Set **From account** to Checking and **To account** to Savings, then submit **$25.50**. Changing either account selector switches the transfer direction when needed.

1. The controller receives the form. The service calls the validator, which converts the text `"25.50"` into a decimal amount.
2. The repository acquires its lock, checks the request ID, and passes the latest entry to the service’s `Apply` method. The service calculates **$74.50 in Checking** and **$25.50 in Savings** and validates those balances.
3. The repository appends one entry containing both ending balances and a UTC timestamp. The controller redirects to the dashboard. History now contains two entries, the original deposit and the transfer.
4. Withdraw **$5** from Savings, then try withdrawing **$21**. The first withdrawal leaves **$20.50** in Savings. The second shows an insufficient-funds message. The history still has three entries.

## Storage and correctness tradeoffs

- **Concurrent submissions.** The balance check and append happen under the same lock. If two tabs try to spend the same funds, the second request sees the balance left by the first.
- **Retries.** Resubmitting an accepted request ID with the same values returns success without a second entry. Each fresh form receives a new ID, so two intentional deposits of the same amount can both succeed.
- **Persistence.** The in-memory list keeps setup simple. Refreshing the page preserves history. Restarting the app clears it. Duplicate detection scans the list, and the dashboard reads the full history. A database-backed version would need a transaction and a unique request ID to preserve the same guarantees across processes.

## Run the unit tests

Restore NuGet packages on the first run. In Visual Studio, open **Test → Test Explorer → Run All**. From the solution folder, use `dotnet test SimpleAtm.slnx`.

The xUnit suite covers three parts of a submission:

| Test class | What it checks |
|---|---|
| `AtmControllerTests` | Uses a mocked service to check dashboard defaults, success redirects, and how errors and form values are returned to the view. |
| `TransactionValidatorTests` | Checks source and destination selections directly, including missing or unknown accounts, transfers to the same account, and a leftover destination on a deposit or withdrawal. |
| `AtmServiceTests` | Uses the real validator and a Moq repository with known balances to verify deposits, withdrawals, transfers in both directions, and rejection of invalid amounts or insufficient funds. |
