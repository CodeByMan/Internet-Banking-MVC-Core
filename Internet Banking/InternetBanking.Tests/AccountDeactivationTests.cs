using InternetBanking.Core.Application.Interfaces.Services;
using InternetBanking.Infrastructure.Identity;
using InternetBanking.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace InternetBanking.Tests;

public class AccountDeactivationTests
{
    [Fact]
    public async Task InactivateUser_RotatesSecurityStampAndMarksUserInactive()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UseInMemoryDatabase"] = "true"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddIdentityInfrastructure(configuration);
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var accountService = scope.ServiceProvider.GetRequiredService<IAccountService>();
        var user = new ApplicationUser
        {
            UserName = "activeclient",
            Email = "activeclient@example.test",
            FirstName = "Active",
            LastName = "Client",
            IdNumber = "1001",
            IsActive = true,
            LockoutEnabled = true
        };

        IdentityResult createResult = await userManager.CreateAsync(user, "Strong!Pass123");
        Assert.True(createResult.Succeeded);
        string? originalSecurityStamp = user.SecurityStamp;

        var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        var existingSessionPrincipal = await signInManager.CreateUserPrincipalAsync(user);

        var result = await accountService.InactivateUserAsync(user.Id);
        var reloadedUser = await userManager.FindByIdAsync(user.Id);

        Assert.False(result.HasError);
        Assert.NotNull(reloadedUser);
        Assert.False(reloadedUser!.IsActive);
        Assert.NotEqual(originalSecurityStamp, reloadedUser.SecurityStamp);

        CookieAuthenticationOptions cookieOptions = scope.ServiceProvider
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);
        var httpContext = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider
        };
        scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = httpContext;

        var scheme = new AuthenticationScheme(
            IdentityConstants.ApplicationScheme,
            IdentityConstants.ApplicationScheme,
            typeof(CookieAuthenticationHandler));
        var ticket = new AuthenticationTicket(
            existingSessionPrincipal,
            new AuthenticationProperties(),
            IdentityConstants.ApplicationScheme);
        var validationContext = new CookieValidatePrincipalContext(httpContext, scheme, cookieOptions, ticket);

        await cookieOptions.Events.OnValidatePrincipal!(validationContext);

        Assert.Null(validationContext.Principal);
    }
}

