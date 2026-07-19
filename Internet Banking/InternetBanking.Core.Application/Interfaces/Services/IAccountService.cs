using InternetBanking.Core.Application.Dtos.Account;

namespace InternetBanking.Core.Application.Interfaces.Services
{
    public interface IAccountService
    {
        Task<AuthenticationResponse> AuthenticateAsync(AuthenticationRequest request);
        Task SignOutAsync();
        Task<RegisterResponse> RegisterUserAsync(RegisterRequest request, string origin);
        Task<List<AuthenticationResponse>> GetAllUsersAsync();
        Task<AuthenticationResponse> GetUserByIdAsync(string id);
        Task<UpdateUserResponse> UpdateUserAsync(RegisterRequest request, string userId);
        Task<UpdateUserResponse> ActivateUserAsync(string id);
        Task<UpdateUserResponse> InactivateUserAsync(string id);
    }
}
