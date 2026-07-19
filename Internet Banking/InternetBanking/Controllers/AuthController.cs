using InternetBanking.Core.Application.Dtos.Account;
using InternetBanking.Core.Application.Interfaces.Services;
using InternetBanking.Core.Application.ViewModels.User;
using InternetBanking.WebApp.Middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternetBanking.WebApp.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class AuthController : Controller
    {
        private readonly IUserService _userService;

        public AuthController(IUserService userService)
        {
            _userService = userService;
        }

        [ServiceFilter(typeof(LoginAuthorize))]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [ServiceFilter(typeof(LoginAuthorize))]
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            AuthenticationResponse userVm = await _userService.LoginAsync(vm);
            if (userVm != null && userVm.HasError != true)
            {
                return RedirectToRoute(new { controller = "Home", action = "Index" });
            }

            vm.HasError = userVm.HasError;
            vm.Error = userVm.Error;
            return View(vm);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> LogOut()
        {
            await _userService.SignOutAsync();
            HttpContext.Session.Clear();
            return RedirectToRoute(new { controller = "Auth", action = "Login" });
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
