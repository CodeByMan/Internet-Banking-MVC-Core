using System.ComponentModel.DataAnnotations;

namespace InternetBanking.Core.Application.ViewModels.User
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Please enter your username.")]
        [Display(Name = "Username")]
        [DataType(DataType.Text)]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your password.")]
        [Display(Name = "Password")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool HasError { get; set; }
        public string? Error { get; set; }
    }
}
