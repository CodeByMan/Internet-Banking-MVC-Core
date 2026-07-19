using InternetBanking.Core.Application.Dtos.Account;
using InternetBanking.Core.Application.Enums;
using InternetBanking.Core.Application.Interfaces.Services;
using InternetBanking.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InternetBanking.Infrastructure.Identity.Services
{
    public class AccountService : IAccountService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<AuthenticationResponse> AuthenticateAsync(AuthenticationRequest request)
        {
            AuthenticationResponse response = new();

            var user = await _userManager.FindByNameAsync(request.UserName);
            if (user == null)
            {
                response.HasError = true;
                response.Error = $"No account is registered for username '{request.UserName}'.";
                return response;
            }

            if (!user.IsActive)
            {
                response.HasError = true;
                response.Error = "This account is inactive. Please contact an administrator.";
                return response;
            }

            if (!user.LockoutEnabled)
            {
                user.LockoutEnabled = true;
                await _userManager.UpdateAsync(user);
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName!, request.Password, false, lockoutOnFailure: true);
            if (result.IsLockedOut)
            {
                response.HasError = true;
                response.Error = "This account is temporarily locked after repeated failed sign-in attempts. Please try again later.";
                return response;
            }

            if (!result.Succeeded)
            {
                response.HasError = true;
                response.Error = $"Invalid credentials for username '{request.UserName}'.";
                return response;
            }

            response.Id = user.Id;
            response.Email = user.Email!;
            response.UserName = user.UserName!;
            response.FirstName = user.FirstName;
            response.LastName = user.LastName;
            response.IdNumber = user.IdNumber;
            response.IsActive = user.IsActive;
            response.Roles = (await _userManager.GetRolesAsync(user)).ToList();

            return response;
        }

        public async Task SignOutAsync() => await _signInManager.SignOutAsync();

        public async Task<RegisterResponse> RegisterUserAsync(RegisterRequest request, string origin)
        {
            RegisterResponse response = new() { HasError = false };

            var userWithSameUserName = await _userManager.FindByNameAsync(request.UserName);
            if (userWithSameUserName != null)
            {
                response.HasError = true;
                response.Error = $"Username '{request.UserName}' is already taken.";
                return response;
            }

            var userWithSameEmail = await _userManager.FindByEmailAsync(request.Email);
            if (userWithSameEmail != null)
            {
                response.HasError = true;
                response.Error = $"Email '{request.Email}' is already registered.";
                return response;
            }

            var user = new ApplicationUser
            {
                Email = request.Email,
                IdNumber = request.IdNumber,
                FirstName = request.FirstName,
                LastName = request.LastName,
                UserName = request.UserName,
                IsActive = true,
                LockoutEnabled = true
            };

            if (!Enum.TryParse(typeof(Roles), request.Role, out var userRole))
            {
                response.HasError = true;
                response.Error = "Invalid user role.";
                return response;
            }

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                response.HasError = true;
                response.Error = string.Join(" ", result.Errors.Select(error => error.Description));
                return response;
            }

            response.Id = user.Id;
            await _userManager.AddToRoleAsync(user, userRole!.ToString()!);
            return response;
        }

        public async Task<UpdateUserResponse> UpdateUserAsync(RegisterRequest request, string userId)
        {
            var response = new UpdateUserResponse();
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                response.HasError = true;
                response.Error = "User not found.";
                return response;
            }

            var userWithSameUserName = await _userManager.FindByNameAsync(request.UserName);
            if (userWithSameUserName != null && userWithSameUserName.Id != userId)
            {
                response.HasError = true;
                response.Error = $"Username '{request.UserName}' is already taken.";
                return response;
            }

            var userWithSameEmail = await _userManager.FindByEmailAsync(request.Email);
            if (userWithSameEmail != null && userWithSameEmail.Id != userId)
            {
                response.HasError = true;
                response.Error = $"Email '{request.Email}' is already registered.";
                return response;
            }

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                var passwordErrors = new List<IdentityError>();
                foreach (var validator in _userManager.PasswordValidators)
                {
                    var validationResult = await validator.ValidateAsync(_userManager, user, request.Password);
                    if (!validationResult.Succeeded)
                    {
                        passwordErrors.AddRange(validationResult.Errors);
                    }
                }

                if (passwordErrors.Count > 0)
                {
                    response.HasError = true;
                    response.Error = string.Join(" ", passwordErrors.Select(error => error.Description).Distinct());
                    return response;
                }
            }

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.Email = request.Email;
            user.IdNumber = request.IdNumber;
            user.UserName = request.UserName;

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, request.Password);
                user.SecurityStamp = Guid.NewGuid().ToString();
            }

            var result = await _userManager.UpdateAsync(user);
            response.HasError = !result.Succeeded;
            response.Error = result.Succeeded
                ? null
                : string.Join(" ", result.Errors.Select(error => error.Description));
            return response;
        }

        public async Task<UpdateUserResponse> ActivateUserAsync(string id)
        {
            var response = new UpdateUserResponse();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                response.HasError = true;
                response.Error = "User not found.";
                return response;
            }

            user.IsActive = true;
            var result = await _userManager.UpdateAsync(user);
            response.HasError = !result.Succeeded;
            response.Error = result.Succeeded
                ? null
                : string.Join(" ", result.Errors.Select(error => error.Description));
            return response;
        }

        public async Task<UpdateUserResponse> InactivateUserAsync(string id)
        {
            var response = new UpdateUserResponse();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                response.HasError = true;
                response.Error = "User not found.";
                return response;
            }

            user.IsActive = false;
            user.SecurityStamp = Guid.NewGuid().ToString();
            var result = await _userManager.UpdateAsync(user);
            response.HasError = !result.Succeeded;
            response.Error = result.Succeeded
                ? null
                : string.Join(" ", result.Errors.Select(error => error.Description));
            return response;
        }

        public async Task<AuthenticationResponse> GetUserByIdAsync(string id)
        {
            AuthenticationResponse response = new();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                response.HasError = true;
                response.Error = "User not found.";
                return response;
            }

            var roles = await _userManager.GetRolesAsync(user);
            response.Id = user.Id;
            response.FirstName = user.FirstName;
            response.LastName = user.LastName;
            response.Email = user.Email!;
            response.IdNumber = user.IdNumber;
            response.UserName = user.UserName!;
            response.Roles = roles.ToList();
            response.IsActive = user.IsActive;
            return response;
        }

        public async Task<List<AuthenticationResponse>> GetAllUsersAsync()
        {
            var userList = await _userManager.Users.OrderBy(u => u.UserName).ToListAsync();
            List<AuthenticationResponse> userResponses = new();

            foreach (var user in userList)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userResponses.Add(new AuthenticationResponse
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email!,
                    IdNumber = user.IdNumber,
                    UserName = user.UserName!,
                    Roles = roles.ToList(),
                    IsActive = user.IsActive
                });
            }

            return userResponses;
        }
    }
}
