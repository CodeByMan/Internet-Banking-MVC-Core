using InternetBanking.Core.Application.Dtos.Account;
using InternetBanking.Core.Application.ViewModels.User;

namespace InternetBanking.Core.Application.Interfaces.Services
{
    public interface IUserService
    {
        Task<UpdateUserResponse> ActiveUser(string id);
        Task<List<UserViewModel>> GetAllAsync();
        Task<SaveUserViewModel> GetByIdAsync(string id);
        Task<UpdateUserResponse> InactiveUser(string id);
        Task<AuthenticationResponse> LoginAsync(LoginViewModel vm);
        Task<RegisterResponse> RegisterAsync(SaveUserViewModel vm, string origin);
        Task SignOutAsync();
        Task<UpdateUserResponse> UpdateAsync(SaveUserViewModel vm, string id);
    }
}
