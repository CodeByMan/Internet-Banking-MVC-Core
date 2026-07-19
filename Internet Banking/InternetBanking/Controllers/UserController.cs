using InternetBanking.Core.Application.Dtos.Account;
using InternetBanking.Core.Application.Enums;
using InternetBanking.Core.Application.Interfaces.Services;
using InternetBanking.Core.Application.ViewModels.Product;
using InternetBanking.Core.Application.ViewModels.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InternetBanking.WebApp.Controllers
{
    [Authorize(Roles = "Admin")]
    [AutoValidateAntiforgeryToken]
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly IProductService _productService;

        public UserController(IUserService userService, IProductService productService)
        {
            _userService = userService;
            _productService = productService;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userService.GetAllAsync();
            return View(users);
        }

        public IActionResult Create()
        {
            return View("SaveUser", new SaveUserViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Create(SaveUserViewModel vm)
        {
            if (string.IsNullOrWhiteSpace(vm.Password))
            {
                ModelState.AddModelError(nameof(vm.Password), "Password is required.");
            }

            if (string.IsNullOrWhiteSpace(vm.ConfirmPassword))
            {
                ModelState.AddModelError(nameof(vm.ConfirmPassword), "Password confirmation is required.");
            }

            if (!Enum.IsDefined(typeof(Roles), vm.UserType))
            {
                ModelState.AddModelError(nameof(vm.UserType), "Please select a valid user role.");
            }

            if (!ModelState.IsValid)
            {
                return View("SaveUser", vm);
            }

            var origin = Request.Headers["origin"].ToString();
            RegisterResponse response = await _userService.RegisterAsync(vm, origin);
            if (response.HasError)
            {
                vm.HasError = response.HasError;
                vm.Error = response.Error;
                return View("SaveUser", vm);
            }

            if (vm.UserType == Roles.Client && response.Id != null) 
            {
                SaveProductViewModel productVm = new()
                {
                    UserId = response.Id,
                    Amount = vm.InitialAmount.GetValueOrDefault(),
                    ProductType = ProductType.SavingAccount,
                    IsPrincipal = true
                };

                SaveProductViewModel createdProduct = await _productService.Add(productVm);
                if (createdProduct.HasError)
                {
                    TempData["UserActionError"] = "The user was created, but the principal account could not be issued. Open the user's products and create the principal savings account manually.";
                }
            }

            return RedirectToRoute(new { controller = "User", action = "Index" });
        }

        public async Task<IActionResult> Edit(string id)
        {
            SaveUserViewModel userVm = await _userService.GetByIdAsync(id); 
            return View("SaveUser", userVm);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(SaveUserViewModel vm)
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(vm.Id))
            {
                return View("SaveUser", vm);
            }

            SaveUserViewModel existingUser = await _userService.GetByIdAsync(vm.Id);
            if (existingUser.HasError)
            {
                vm.HasError = true;
                vm.Error = existingUser.Error ?? "The user could not be loaded.";
                return View("SaveUser", vm);
            }

            UpdateUserResponse updateResponse = await _userService.UpdateAsync(vm, vm.Id);
            if (updateResponse.HasError)
            {
                vm.HasError = true;
                vm.Error = updateResponse.Error ?? "The user could not be updated.";
                return View("SaveUser", vm);
            }

            if (existingUser.UserType == Roles.Client && vm.InitialAmount > 0)
            {
                try
                {
                    await _productService.TransferAmountToPrincipal(vm.Id, vm.InitialAmount.Value);
                }
                catch
                {
                    vm.InitialAmount = null;
                    vm.HasError = true;
                    vm.Error = "User details were updated, but the balance top-up could not be completed. No balance change was confirmed; retry the top-up separately.";
                    return View("SaveUser", vm);
                }
            }

            return RedirectToRoute(new { controller = "User", action = "Index" });
        }

        public async Task<IActionResult> ConfirmAction(string id)
        {
            SaveUserViewModel userVm = await _userService.GetByIdAsync(id);

            return View("ConfirmAction", userVm);
        }

        [HttpPost]
        public async Task<IActionResult> ActiveUser(SaveUserViewModel vm)
        {
            if (vm.Id == User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return Forbid();
            }

            UpdateUserResponse response = await _userService.ActiveUser(vm.Id);
            if (response.HasError)
            {
                TempData["UserActionError"] = response.Error ?? "The user could not be activated.";
            }
            return RedirectToRoute(new { controller = "User", action = "Index" });
        }

        [HttpPost]
        public async Task<IActionResult> InactiveUser(SaveUserViewModel vm)
        {
            if (vm.Id == User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return Forbid();
            }

            UpdateUserResponse response = await _userService.InactiveUser(vm.Id);
            if (response.HasError)
            {
                TempData["UserActionError"] = response.Error ?? "The user could not be deactivated.";
            }
            return RedirectToRoute(new { controller = "User", action = "Index" });
        }
    }
}
