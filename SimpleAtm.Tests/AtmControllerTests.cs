using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using SimpleAtm.Controllers;
using SimpleAtm.Models;
using SimpleAtm.Services;
using Xunit;

namespace SimpleAtm.Tests;

public class AtmControllerTests
{
    private readonly Mock<IAtmService> service = new(MockBehavior.Strict);

    [Fact]
    public void Index_ShowsHistoryAndPreparesNewDeposit()
    {
        var deposit = new Transaction(Guid.NewGuid(), Operation.Deposit, Account.Checking, 20m);
        Entry[] history = [new Entry(deposit, DateTimeOffset.UtcNow, 20m, 0m)];
        service.Setup(s => s.History()).Returns(history);

        var result = new AtmController(service.Object).Index();

        var dashboard = Assert.IsType<Dashboard>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal(history, dashboard.Entries);
        Assert.NotEqual(Guid.Empty, dashboard.Form.RequestId);
        Assert.Equal(Operation.Deposit, dashboard.Form.ActionType);
        Assert.Equal(Account.Checking, dashboard.Form.SelectedAccount);
    }

    [Fact]
    public void Submit_OnSuccess_RedirectsToIndexWithNotice()
    {
        var form = new TransactionForm();
        service.Setup(s => s.Submit(form)).Returns((string?)null);
        var controller = new AtmController(service.Object)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };

        var result = Assert.IsType<RedirectToActionResult>(controller.Submit(form));

        Assert.Equal(nameof(AtmController.Index), result.ActionName);
        Assert.Equal("Transaction completed.", controller.TempData["Notice"]);
        service.Verify(s => s.Submit(form), Times.Once);
    }

    [Fact]
    public void Submit_OnRejection_RedisplaysFormAndReason()
    {
        var form = new TransactionForm { Amount = "100" };
        const string reason = "Insufficient funds in the selected account.";
        service.Setup(s => s.Submit(form)).Returns(reason);
        service.Setup(s => s.History()).Returns(Array.Empty<Entry>());
        var controller = new AtmController(service.Object);

        var result = Assert.IsType<ViewResult>(controller.Submit(form));

        Assert.Equal("Index", result.ViewName);
        Assert.Same(form, Assert.IsType<Dashboard>(result.Model).Form);
        Assert.Equal(reason, Assert.Single(controller.ModelState[string.Empty]!.Errors).ErrorMessage);
    }

    [Fact]
    public void Submit_WithBindingError_SkipsServiceSubmission()
    {
        service.Setup(s => s.History()).Returns(Array.Empty<Entry>());
        var controller = new AtmController(service.Object);
        controller.ModelState.AddModelError("Form.SelectedAccount", "Invalid account.");

        var result = Assert.IsType<ViewResult>(controller.Submit(new TransactionForm()));

        Assert.Equal("Index", result.ViewName);
        service.Verify(s => s.Submit(It.IsAny<TransactionForm>()), Times.Never);
    }
}
