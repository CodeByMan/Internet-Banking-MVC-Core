using InternetBanking.Core.Application.Enums;
using InternetBanking.Core.Application.Interfaces.Services;
using InternetBanking.Core.Application.ViewModels.Dashboard;
using InternetBanking.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace InternetBanking.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IProductService _productService;
        private readonly IPaymentService _paymentService;
        private readonly IUserService _userService;

        public HomeController(
            IProductService productService,
            IPaymentService paymentService,
            IUserService userService)
        {
            _productService = productService;
            _paymentService = paymentService;
            _userService = userService;
        }

        public IActionResult Index()
        {
            return RedirectToAction(User.IsInRole(Roles.Admin.ToString()) ? "Admin" : "Client");
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Admin()
        {
            var users = await _userService.GetAllAsync();
            var dashboardVm = new AdminDashboardViewModel
            {
                TotalProductsCreated = await _productService.GetProductsCreatedAllTheTime(),
                TotalActiveUsers = users.Count(u => u.IsActive == true),
                TotalInactiveUsers = users.Count(u => u.IsActive != true),
                TotalPaymentsMade = await _paymentService.GetPaymentsMadeAllTheTime(),
                PaymentsMadeLast24Hours = await _paymentService.GetPaymentsMadeLast24Hours(),
                TotalTransactionsMade = await _paymentService.GetTransactionsMadeAllTheTime(),
                TransactionsMadeLast24Hours = await _paymentService.GetTransactionsMadeLast24Hours(),
                RecentActivity = await _paymentService.GetRecentActivity(10)
            };

            return View("Admin", dashboardVm);
        }

        [Authorize(Roles = "Client")]
        public async Task<IActionResult> Client()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var products = await _productService.GetProductsByUserViewModel(currentUserId);
            var dashboard = new ClientDashboardViewModel
            {
                Products = products,
                RecentActivity = await _paymentService.GetRecentActivityForUser(currentUserId, 8),
                TotalProducts = products.Count,
                SavingsAccounts = products.Count(p => p.ProductType == ProductType.SavingAccount),
                CreditCards = products.Count(p => p.ProductType == ProductType.CreditCard),
                Loans = products.Count(p => p.ProductType == ProductType.Loan),
                TotalAvailableBalance = products.Sum(p => p.Balance),
                TotalOutstandingDebt = products.Sum(p => p.Debt)
            };

            return View("Client", dashboard);
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
