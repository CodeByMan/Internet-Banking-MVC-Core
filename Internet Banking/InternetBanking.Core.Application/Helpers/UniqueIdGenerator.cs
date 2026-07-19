using InternetBanking.Core.Domain.Entities;
using System.Security.Cryptography;

namespace InternetBanking.Core.Application.Helpers
{
    public static class UniqueIdGenerator
    {
        public static string GenerateUniqueId()
        {
            Span<char> accountNumber = stackalloc char[Product.AccountNumberLength];
            for (int index = 0; index < accountNumber.Length; index++)
            {
                accountNumber[index] = (char)('0' + RandomNumberGenerator.GetInt32(10));
            }

            return new string(accountNumber);
        }
    }
}
