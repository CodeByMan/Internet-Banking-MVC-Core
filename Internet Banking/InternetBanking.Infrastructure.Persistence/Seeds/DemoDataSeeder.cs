using InternetBanking.Core.Application.Enums;
using InternetBanking.Core.Domain.Entities;
using InternetBanking.Infrastructure.Identity.Entities;
using InternetBanking.Infrastructure.Persistence.Contexts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InternetBanking.Infrastructure.Persistence.Seeds
{
    public static class DemoDataSeeder
    {
        public static async Task SeedAsync(ApplicationContext dbContext, UserManager<ApplicationUser> userManager, string secondaryClientPassword)
        {
            if (string.IsNullOrWhiteSpace(secondaryClientPassword))
            {
                throw new InvalidOperationException("DemoSeed:SecondaryClientPassword must be configured through User Secrets or an environment variable.");
            }

            var primaryClient = await userManager.FindByNameAsync("clientuser");
            if (primaryClient == null)
            {
                return;
            }

            var legacyClient = await userManager.FindByNameAsync("sanaclient")
                ?? await userManager.FindByNameAsync("sarahclient");
            var secondaryClient = await userManager.FindByNameAsync("aliclient");

            if (secondaryClient == null && legacyClient != null)
            {
                // Preserve the existing user's ID so all products and history remain attached.
                secondaryClient = legacyClient;
            }
            else if (secondaryClient != null &&
                     legacyClient != null &&
                     secondaryClient.Id != legacyClient.Id)
            {
                // Move any legacy banking records to AliClient before deleting the duplicate user.
                await dbContext.Products
                    .Where(product => product.UserId == legacyClient.Id)
                    .ExecuteUpdateAsync(update =>
                        update.SetProperty(product => product.UserId, secondaryClient.Id));

                await dbContext.Beneficiaries
                    .Where(beneficiary => beneficiary.UserOwnerId == legacyClient.Id)
                    .ExecuteUpdateAsync(update =>
                        update.SetProperty(beneficiary => beneficiary.UserOwnerId, secondaryClient.Id));

                var deleteLegacyResult = await userManager.DeleteAsync(legacyClient);
                if (!deleteLegacyResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Unable to remove the legacy Sana/Sarah demo client.");
                }
            }

            if (secondaryClient == null)
            {
                secondaryClient = new ApplicationUser
                {
                    UserName = "aliclient",
                    Email = "ali.client@email.com",
                    FirstName = "Ali",
                    LastName = "Nawaz",
                    IdNumber = "42101567890",
                    PhoneNumber = "03001234567",
                    IsActive = true,
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                    LockoutEnabled = true
                };

                var createResult =
                    await userManager.CreateAsync(secondaryClient, secondaryClientPassword);

                if (!createResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Unable to seed the Ali demo client.");
                }
            }
            else
            {
                secondaryClient.UserName = "aliclient";
                secondaryClient.Email = "ali.client@email.com";
                secondaryClient.FirstName = "Ali";
                secondaryClient.LastName = "Nawaz";
                secondaryClient.IsActive = true;
                secondaryClient.EmailConfirmed = true;
                secondaryClient.PhoneNumberConfirmed = true;

                var updateAliResult =
                    await userManager.UpdateAsync(secondaryClient);

                if (!updateAliResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Unable to convert the legacy demo client to AliClient.");
                }

                if (!await userManager.CheckPasswordAsync(
                        secondaryClient,
                        secondaryClientPassword))
                {
                    var resetToken =
                        await userManager.GeneratePasswordResetTokenAsync(secondaryClient);

                    var resetResult =
                        await userManager.ResetPasswordAsync(
                            secondaryClient,
                            resetToken,
                            secondaryClientPassword);

                    if (!resetResult.Succeeded)
                    {
                        throw new InvalidOperationException(
                            "Unable to set the Ali demo client password.");
                    }
                }
            }
            if (!secondaryClient.LockoutEnabled)
            {
                secondaryClient.LockoutEnabled = true;
                var updateResult = await userManager.UpdateAsync(secondaryClient);
                if (!updateResult.Succeeded)
                {
                    throw new InvalidOperationException("Unable to enable lockout for the secondary demo client.");
                }
            }

            if (!await userManager.IsInRoleAsync(secondaryClient, Roles.Client.ToString()))
            {
                var roleResult = await userManager.AddToRoleAsync(secondaryClient, Roles.Client.ToString());
                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException("Unable to assign the secondary demo client role.");
                }
            }

            if (!await dbContext.Products.AnyAsync())
            {
                dbContext.Products.AddRange(
                    new Product
                    {
                        UserId = primaryClient.Id,
                        AccountNumber = "100100001",
                        Balance = 4200,
                        Debt = 0,
                        ProductTypeId = (int)ProductType.SavingAccount,
                        IsPrincipal = true,
                        CreatedBy = "DemoSeeder",
                        Created = DateTime.UtcNow.AddDays(-30)
                    },
                    new Product
                    {
                        UserId = primaryClient.Id,
                        AccountNumber = "100100002",
                        Balance = 1500,
                        Debt = 0,
                        ProductTypeId = (int)ProductType.SavingAccount,
                        IsPrincipal = false,
                        CreatedBy = "DemoSeeder",
                        Created = DateTime.UtcNow.AddDays(-29)
                    },
                    new Product
                    {
                        UserId = primaryClient.Id,
                        AccountNumber = "200100001",
                        Balance = 5000,
                        Debt = 1200,
                        ProductTypeId = (int)ProductType.CreditCard,
                        IsPrincipal = false,
                        CreatedBy = "DemoSeeder",
                        Created = DateTime.UtcNow.AddDays(-28)
                    },
                    new Product
                    {
                        UserId = primaryClient.Id,
                        AccountNumber = "300100001",
                        Balance = 0,
                        Debt = 7500,
                        ProductTypeId = (int)ProductType.Loan,
                        IsPrincipal = false,
                        CreatedBy = "DemoSeeder",
                        Created = DateTime.UtcNow.AddDays(-27)
                    },
                    new Product
                    {
                        UserId = secondaryClient.Id,
                        AccountNumber = "100200001",
                        Balance = 3100,
                        Debt = 0,
                        ProductTypeId = (int)ProductType.SavingAccount,
                        IsPrincipal = true,
                        CreatedBy = "DemoSeeder",
                        Created = DateTime.UtcNow.AddDays(-30)
                    },
                    new Product
                    {
                        UserId = secondaryClient.Id,
                        AccountNumber = "100200002",
                        Balance = 950,
                        Debt = 0,
                        ProductTypeId = (int)ProductType.SavingAccount,
                        IsPrincipal = false,
                        CreatedBy = "DemoSeeder",
                        Created = DateTime.UtcNow.AddDays(-26)
                    },
                    new Product
                    {
                        UserId = secondaryClient.Id,
                        AccountNumber = "200200001",
                        Balance = 4000,
                        Debt = 650,
                        ProductTypeId = (int)ProductType.CreditCard,
                        IsPrincipal = false,
                        CreatedBy = "DemoSeeder",
                        Created = DateTime.UtcNow.AddDays(-25)
                    },
                    new Product
                    {
                        UserId = secondaryClient.Id,
                        AccountNumber = "300200001",
                        Balance = 0,
                        Debt = 3000,
                        ProductTypeId = (int)ProductType.Loan,
                        IsPrincipal = false,
                        CreatedBy = "DemoSeeder",
                        Created = DateTime.UtcNow.AddDays(-24)
                    });

                await dbContext.SaveChangesAsync();
            }

            if (!await dbContext.Beneficiaries.AnyAsync())
            {
                dbContext.Beneficiaries.AddRange(
                    new Beneficiary
                    {
                        UserOwnerId = primaryClient.Id,
                        AccountNumberBeneficiary = "100200001",
                        CreatedBy = "DemoSeeder",
                        Created = DateTime.UtcNow.AddDays(-10)
                    },
                    new Beneficiary
                    {
                        UserOwnerId = secondaryClient.Id,
                        AccountNumberBeneficiary = "100100001",
                        CreatedBy = "DemoSeeder",
                        Created = DateTime.UtcNow.AddDays(-10)
                    });

                await dbContext.SaveChangesAsync();
            }

            if (!await dbContext.Payments.AnyAsync())
            {
                dbContext.Payments.AddRange(
                    new Payment
                    {
                        SourceAccountNumber = "100100001",
                        DestinationAccountNumber = "100200001",
                        Amount = 300,
                        PaymentTypeId = (int)PaymentType.Payment,
                        CreatedBy = "DemoSeeder",
                        Created = DateTime.UtcNow.AddDays(-7)
                    },
                    new Payment
                    {
                        SourceAccountNumber = "100100002",
                        DestinationAccountNumber = "200100001",
                        Amount = 250,
                        PaymentTypeId = (int)PaymentType.Payment,
                        CreatedBy = "DemoSeeder",
                        Created = DateTime.UtcNow.AddDays(-5)
                    },
                    new Payment
                    {
                        SourceAccountNumber = "100100001",
                        DestinationAccountNumber = "300100001",
                        Amount = 400,
                        PaymentTypeId = (int)PaymentType.Payment,
                        CreatedBy = "DemoSeeder",
                        Created = DateTime.UtcNow.AddDays(-3)
                    },
                    new Payment
                    {
                        SourceAccountNumber = "100100002",
                        DestinationAccountNumber = "100100001",
                        Amount = 150,
                        PaymentTypeId = (int)PaymentType.Transaction,
                        CreatedBy = "DemoSeeder",
                        Created = DateTime.UtcNow.AddDays(-2)
                    },
                    new Payment
                    {
                        SourceAccountNumber = "200100001",
                        DestinationAccountNumber = "100100001",
                        Amount = 200,
                        PaymentTypeId = (int)PaymentType.Transaction,
                        CreatedBy = "DemoSeeder",
                        Created = DateTime.UtcNow.AddHours(-10)
                    },
                    new Payment
                    {
                        SourceAccountNumber = "100200001",
                        DestinationAccountNumber = "100100001",
                        Amount = 120,
                        PaymentTypeId = (int)PaymentType.Payment,
                        CreatedBy = "DemoSeeder",
                        Created = DateTime.UtcNow.AddHours(-4)
                    });

                await dbContext.SaveChangesAsync();
            }
        }
    }
}


