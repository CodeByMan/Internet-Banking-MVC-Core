using InternetBanking.Infrastructure.Identity.Seeds;
using InternetBanking.Infrastructure.Persistence.Seeds;

namespace InternetBanking.Tests;

public class SeedSafetyTests
{
    [Fact]
    public async Task DevelopmentSeeds_RequireLocallyConfiguredPasswords()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            DefaultAdminUser.SeedAsync(null!, null!, string.Empty));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            DefaultClientUser.SeedAsync(null!, null!, string.Empty));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            DemoDataSeeder.SeedAsync(null!, null!, string.Empty));
    }
}
