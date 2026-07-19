using InternetBanking.Core.Application.Enums;
using InternetBanking.Core.Application.ViewModels.Beneficiary;
using InternetBanking.Core.Application.ViewModels.Product;
using System.ComponentModel.DataAnnotations;

namespace InternetBanking.Core.Application.ViewModels.Payment
{
    public class SavePaymentViewModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Please select a source account.")]
        [Display(Name = "Source Account")]
        public string SourceAccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please provide a destination account.")]
        [Display(Name = "Destination Account")]
        [DataType(DataType.Text)]
        public string DestinationAccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Amount is required.")]
        [Display(Name = "Amount")]
        [DataType(DataType.Currency)]
        [Range(typeof(decimal), "0.01", "100000000", ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        public PaymentType PaymentType { get; set; }

        [Required(ErrorMessage = "The payment form expired. Please reload the page and try again.")]
        public string SubmissionToken { get; set; } = string.Empty;
        public bool HasError { get; set; }
        public string? Error { get; set; }
        public List<ProductViewModel>? Products { get; set; }
        public List<ProductViewModel>? CreditCardsProducts { get; set; }
        public List<ProductViewModel>? LoanProducts { get; set; }
        public List<BeneficiaryViewModel>? Beneficiaries { get; set; }
    }
}
