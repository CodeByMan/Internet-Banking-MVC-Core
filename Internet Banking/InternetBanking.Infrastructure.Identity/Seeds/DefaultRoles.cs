using InternetBanking.Core.Application.Enums;
using InternetBanking.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;

namespace InternetBanking.Infrastructure.Identity.Seeds
{
    public static class DefaultRoles
    {
        public static async Task SeedAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            foreach (var roleName in new[] { Roles.Admin.ToString(), Roles.Client.ToString() })
            {
                if (await roleManager.RoleExistsAsync(roleName))
                {
                    continue;
                }

                var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"Unable to create the '{roleName}' role.");
                }
            }
        }
    }
}
