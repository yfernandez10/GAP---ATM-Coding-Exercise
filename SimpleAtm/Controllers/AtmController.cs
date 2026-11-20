using Microsoft.AspNetCore.Mvc;
using SimpleAtm.Models;
using SimpleAtm.Services;

namespace SimpleAtm.Controllers;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AtmController(IAtmService service) : Controller
{
    [HttpGet("/")]
    public IActionResult Index()
    {
        var form = new TransactionForm
        {
            RequestId = Guid.NewGuid(),
            ActionType = Operation.Deposit,
            SelectedAccount = Account.Checking
        };
        return View(new Dashboard(form, service.History()));
    }

    [HttpPost("/transactions"), ValidateAntiForgeryToken]
    public IActionResult Submit([Bind(Prefix = "Form")] TransactionForm form)
    {
        if (ModelState.IsValid)
        {
            var error = service.Submit(form);
            if (error is null)
            {
                TempData["Notice"] = "Transaction completed.";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, error);
        }
        return View("Index", new Dashboard(form, service.History()));
    }
}
