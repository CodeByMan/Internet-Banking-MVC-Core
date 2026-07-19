using InternetBanking.Core.Application.Enums;
using System.ComponentModel.DataAnnotations;

namespace InternetBanking.Core.Application.ViewModels.User
{
    public class SaveUserViewModel
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "First name is required.")]
        [Display(Name = "First Name")]
        [DataType(DataType.Text)]
        public string? FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required.")]
        [Display(Name = "Last Name")]
        [DataType(DataType.Text)]
        public string? LastName { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "National ID is required.")]
        [Display(Name = "National ID")]
        [DataType(DataType.Text)]
        public string? IdNumber { get; set; }

        [Required(ErrorMessage = "Username is required.")]
        [Display(Name = "Username")]
        [DataType(DataType.Text)]
        public string UserName { get; set; } = string.Empty;

        [Display(Name = "Password")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        [DataType(DataType.Password)]
        public string? ConfirmPassword { get; set; }

        [Display(Name = "User Role")]
        public Roles UserType { get; set; }

        [DataType(DataType.Currency)]
        [Range(typeof(decimal), "0", "100000000", ErrorMessage = "Opening amount must be 0 or greater.")]
        [Display(Name = "Opening Amount")]
        public decimal? InitialAmount { get; set; }

        public bool? IsActive { get; set; }
        public bool HasError { get; set; }
        public string? Error { get; set; }
    }
}
