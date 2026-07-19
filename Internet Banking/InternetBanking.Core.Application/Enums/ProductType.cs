using System.ComponentModel.DataAnnotations;

namespace InternetBanking.Core.Application.Enums
{
    public enum ProductType
    {
        [Display(Name = "Savings Account")]
        SavingAccount = 1,
        [Display(Name = "Credit Card")]
        CreditCard,
        [Display(Name = "Loan")]
        Loan
    }
}
