using System.ComponentModel.DataAnnotations;

namespace InternetBanking.Core.Application.ViewModels.Beneficiary
{
    public class SaveBeneficiaryViewModel
    {
        public int? Id { get; set; }
        public string? UserOwnerId { get; set; }

        [Required(ErrorMessage = "Please enter an account number.")]
        [Display(Name = "Beneficiary Account Number")]
        public string AccountNumberBeneficiary { get; set; } = string.Empty;

        public bool HasError { get; set; }
        public string? Error { get; set; }
    }
}
