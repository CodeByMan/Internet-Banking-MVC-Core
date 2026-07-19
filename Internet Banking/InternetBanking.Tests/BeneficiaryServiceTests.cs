using AutoMapper;
using InternetBanking.Core.Application.Enums;
using InternetBanking.Core.Application.Mappings;
using InternetBanking.Core.Application.Services;
using InternetBanking.Core.Domain.Entities;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace InternetBanking.Tests;

public class BeneficiaryServiceTests
{
    [Fact]
    public async Task DeleteOwnedAsync_DoesNotDeleteAnotherUsersBeneficiary()
    {
        var repository = new FakeBeneficiaryRepository();
        repository.Beneficiaries.Add(new Beneficiary
        {
            Id = 10,
            UserOwnerId = "other-user",
            AccountNumberBeneficiary = "DEST"
        });

        var products = new FakeProductService();
        IMapper mapper = new MapperConfiguration(configuration => configuration.AddProfile<GeneralProfile>()).CreateMapper();
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "current-user"),
                new Claim(ClaimTypes.Role, Roles.Client.ToString())
            }, "Test"))
        };

        var service = new BeneficiaryService(
            repository,
            new FakeUserService("current-user", true),
            products,
            new HttpContextAccessor { HttpContext = context },
            mapper);

        bool deleted = await service.DeleteOwnedAsync(10);

        Assert.False(deleted);
        Assert.Single(repository.Beneficiaries);
    }

    [Fact]
    public async Task DeleteOwnedAsync_DeletesCurrentUsersBeneficiary()
    {
        var repository = new FakeBeneficiaryRepository();
        repository.Beneficiaries.Add(new Beneficiary
        {
            Id = 10,
            UserOwnerId = "current-user",
            AccountNumberBeneficiary = "DEST"
        });

        var products = new FakeProductService();
        IMapper mapper = new MapperConfiguration(configuration => configuration.AddProfile<GeneralProfile>()).CreateMapper();
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "current-user"),
                new Claim(ClaimTypes.Role, Roles.Client.ToString())
            }, "Test"))
        };

        var service = new BeneficiaryService(
            repository,
            new FakeUserService("current-user", true),
            products,
            new HttpContextAccessor { HttpContext = context },
            mapper);

        bool deleted = await service.DeleteOwnedAsync(10);

        Assert.True(deleted);
        Assert.Empty(repository.Beneficiaries);
    }
}
