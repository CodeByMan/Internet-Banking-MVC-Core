using InternetBanking.Core.Application.Enums;
using InternetBanking.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;

namespace InternetBanking.Infrastructure.Identity.Seeds
{
    public static class DefaultAdminUser
    {
        public static async Task SeedAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, string password)
        {
            EnsurePasswordConfigured(password);

            var user = await userManager.FindByNameAsync("adminuser");
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = "adminuser",
                    Email = "adminuser@email.com",
                    FirstName = "John",
                    LastName = "Doe",
                    IdNumber = "873123123",
                    PhoneNumber = "8093821234",
                    IsActive = true,
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                    LockoutEnabled = true
                };

                var createResult = await userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                {
                    throw new InvalidOperationException("Unable to seed the development administrator account.");
                }
            }

            if (!user.LockoutEnabled)
            {
                user.LockoutEnabled = true;
                var updateResult = await userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    throw new InvalidOperationException("Unable to enable lockout for the development seed account.");
                }
            }

            if (!await userManager.IsInRoleAsync(user, Roles.Admin.ToString()))
            {
                var roleResult = await userManager.AddToRoleAsync(user, Roles.Admin.ToString());
                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException("Unable to assign the development administrator role.");
                }
            }
        }

        private static void EnsurePasswordConfigured(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("DemoSeed:AdminPassword must be configured through User Secrets or an environment variable.");
            }
        }
    }
}
