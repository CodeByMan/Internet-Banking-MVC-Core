using System.ComponentModel.DataAnnotations;

namespace InternetBanking.Core.Application.Enums
{
    public enum PaymentType
    {
        [Display(Name = "Payment")]
        Payment = 1,
        [Display(Name = "Transfer")]
        Transaction
    }
}
