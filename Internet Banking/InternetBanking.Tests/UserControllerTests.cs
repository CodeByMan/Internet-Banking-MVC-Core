using InternetBanking.Core.Application.Dtos.Account;
using InternetBanking.Core.Application.ViewModels.User;
using InternetBanking.WebApp.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace InternetBanking.Tests;

public class UserControllerTests
{
    [Fact]
    public async Task Create_BlankPassword_ReturnsSaveUserWithFieldValidationErrors()
    {
        var userService = new FakeUserService("user-1", true);
        var controller = new UserController(userService, new FakeProductService());
        var model = new SaveUserViewModel
        {
            FirstName = "New",
            LastName = "Client",
            Email = "new.client@example.test",
            IdNumber = "123456789",
            UserName = "newclient",
            UserType = InternetBanking.Core.Application.Enums.Roles.Client,
            Password = "",
            ConfirmPassword = ""
        };

        var result = await controller.Create(model);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("SaveUser", view.ViewName);
        Assert.Same(model, view.Model);
        Assert.False(controller.ModelState.IsValid);
        Assert.Contains(controller.ModelState[nameof(SaveUserViewModel.Password)]!.Errors,
            error => error.ErrorMessage == "Password is required.");
        Assert.Contains(controller.ModelState[nameof(SaveUserViewModel.ConfirmPassword)]!.Errors,
            error => error.ErrorMessage == "Password confirmation is required.");
    }

    [Fact]
    public async Task Edit_DoesNotTopUpWhenIdentityUpdateFails()
    {
        var userService = new FakeUserService("user-1", true)
        {
            UpdateResponse = new UpdateUserResponse
            {
                HasError = true,
                Error = "Username is already taken."
            }
        };
        var productService = new FakeProductService();
        var controller = new UserController(userService, productService);
        var model = ValidEditModel(100m);

        var result = await controller.Edit(model);

        var view = Assert.IsType<ViewResult>(result);
        var returnedModel = Assert.IsType<SaveUserViewModel>(view.Model);
        Assert.True(returnedModel.HasError);
        Assert.Contains("already taken", returnedModel.Error);
        Assert.Equal(0, productService.TransferCallCount);
    }

    [Fact]
    public async Task Edit_ReportsTopUpFailureAfterSuccessfulIdentityUpdate()
    {
        var userService = new FakeUserService("user-1", true);
        var productService = new FakeProductService { ThrowOnTransfer = true };
        var controller = new UserController(userService, productService);
        var model = ValidEditModel(100m);

        var result = await controller.Edit(model);

        var view = Assert.IsType<ViewResult>(result);
        var returnedModel = Assert.IsType<SaveUserViewModel>(view.Model);
        Assert.True(returnedModel.HasError);
        Assert.Contains("User details were updated", returnedModel.Error);
        Assert.Null(returnedModel.InitialAmount);
        Assert.Equal(1, productService.TransferCallCount);
    }

    private static SaveUserViewModel ValidEditModel(decimal topUp) => new()
    {
        Id = "user-1",
        FirstName = "Client",
        LastName = "User",
        Email = "client@example.test",
        IdNumber = "1",
        UserName = "clientuser",
        InitialAmount = topUp
    };
}
