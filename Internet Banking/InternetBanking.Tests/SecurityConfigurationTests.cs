using InternetBanking.Core.Application.Enums;
using InternetBanking.Infrastructure.Identity;
using InternetBanking.WebApp.Controllers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity;

namespace InternetBanking.Tests;

public class SecurityConfigurationTests
{
    [Fact]
    public void FinancialAndAdministrativeControllers_RequireExpectedRoles()
    {
        Assert.Equal(Roles.Client.ToString(), GetAuthorizeAttribute<PaymentController>().Roles);
        Assert.Equal(Roles.Client.ToString(), GetAuthorizeAttribute<BeneficiaryController>().Roles);
        Assert.Equal(Roles.Admin.ToString(), GetAuthorizeAttribute<UserController>().Roles);
        Assert.Equal(Roles.Admin.ToString(), GetAuthorizeAttribute<ProductController>().Roles);
    }

    [Theory]
    [InlineData(typeof(PaymentController))]
    [InlineData(typeof(BeneficiaryController))]
    [InlineData(typeof(UserController))]
    [InlineData(typeof(ProductController))]
    [InlineData(typeof(AuthController))]
    public void StateChangingControllers_EnableAutomaticAntiforgeryValidation(Type controllerType)
    {
        Assert.NotNull(controllerType.GetCustomAttributes(typeof(AutoValidateAntiforgeryTokenAttribute), true).SingleOrDefault());
    }

    [Fact]
    public void IdentityConfiguration_EnablesAccountLockout()
    {
        var settings = new Dictionary<string, string?>
        {
            ["UseInMemoryDatabase"] = "true"
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdentityInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();

        IdentityOptions options = provider.GetRequiredService<IOptions<IdentityOptions>>().Value;

        Assert.True(options.Lockout.AllowedForNewUsers);
        Assert.Equal(5, options.Lockout.MaxFailedAccessAttempts);
        Assert.Equal(TimeSpan.FromMinutes(5), options.Lockout.DefaultLockoutTimeSpan);
    }


    [Fact]
    public void IdentityConfiguration_ValidatesSecurityStampAndActiveStatusOnEveryRequest()
    {
        var settings = new Dictionary<string, string?>
        {
            ["UseInMemoryDatabase"] = "true"
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdentityInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();

        SecurityStampValidatorOptions stampOptions = provider
            .GetRequiredService<IOptions<SecurityStampValidatorOptions>>()
            .Value;
        CookieAuthenticationOptions cookieOptions = provider
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);

        Assert.Equal(TimeSpan.Zero, stampOptions.ValidationInterval);
        Assert.NotNull(cookieOptions.Events.OnValidatePrincipal);
    }

    private static AuthorizeAttribute GetAuthorizeAttribute<TController>()
    {
        return typeof(TController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>()
            .Single();
    }
}
