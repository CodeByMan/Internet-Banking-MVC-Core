using InternetBanking.Core.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace InternetBanking.Core.Domain.Entities
{
    public class Product : AuditableBaseEntity
    {
        public const int AccountNumberLength = 9;

        [MaxLength(AccountNumberLength)]
        public string AccountNumber { get; set; } = string.Empty;
        public string UserId { get; set; }
        public decimal Balance { get; set; }
        public decimal Debt { get; set; }
        public int ProductTypeId { get; set; }
        public bool IsPrincipal { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
