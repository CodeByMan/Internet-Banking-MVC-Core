using InternetBanking.Core.Application.Enums;
using System.ComponentModel.DataAnnotations;

namespace InternetBanking.Core.Application.ViewModels.Product
{
    public class SaveProductViewModel
    {
        public int? Id { get; set; }
        public string? AccountNumber { get; set; }
        public string UserId { get; set; } = string.Empty;

        [Display(Name = "Opening Amount")]
        [DataType(DataType.Currency)]
        [Range(typeof(decimal), "0.01", "100000000", ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        [Display(Name = "Outstanding Debt")]
        public decimal Debt { get; set; }

        [Display(Name = "Product Type")]
        public ProductType ProductType { get; set; }

        [Display(Name = "Primary Account")]
        public bool IsPrincipal { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public bool HasError { get; set; }
        public string? Error { get; set; }
    }
}
