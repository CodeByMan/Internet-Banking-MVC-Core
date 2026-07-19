using InternetBanking.Core.Application.Enums;
using System.ComponentModel.DataAnnotations;

namespace InternetBanking.Core.Application.ViewModels.Payment
{
    public class PaymentViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Source Account")]
        public string SourceAccountNumber { get; set; } = string.Empty;

        [Display(Name = "Destination Account")]
        public string DestinationAccountNumber { get; set; } = string.Empty;

        [Display(Name = "Amount")]
        public decimal Amount { get; set; }

        public PaymentType PaymentType { get; set; }
    }
}
