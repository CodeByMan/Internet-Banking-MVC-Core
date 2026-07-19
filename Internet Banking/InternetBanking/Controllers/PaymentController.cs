using InternetBanking.Core.Application.Enums;
using InternetBanking.Core.Application.Interfaces.Services;
using InternetBanking.Core.Application.ViewModels.Payment;
using InternetBanking.WebApp.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InternetBanking.WebApp.Controllers
{
    [Authorize(Roles = "Client")]
    [AutoValidateAntiforgeryToken]
    public class PaymentController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly IProductService _productService;
        private readonly IBeneficiaryService _beneficiaryService;
        private readonly string? _currentUserId;

        public PaymentController(
            IPaymentService paymentService,
            IProductService productService,
            IHttpContextAccessor httpContextAccessor,
            IBeneficiaryService beneficiaryService)
        {
            _paymentService = paymentService;
            _productService = productService;
            _beneficiaryService = beneficiaryService;
            _currentUserId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        public async Task<IActionResult> ExpressPayment()
        {
            var model = NewPaymentModel();
            model.Products = await GetUserProductsAsync();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ExpressPayment(SavePaymentViewModel vm)
        {
            ValidateAndConsumeSubmissionToken(vm);
            if (!ModelState.IsValid)
            {
                RenewSubmissionToken(vm);
                vm.Products = await GetUserProductsAsync();
                return View(vm);
            }

            var paymentVm = await _paymentService.Add(vm);
            if (paymentVm.HasError)
            {
                RenewSubmissionToken(vm);
                vm.HasError = paymentVm.HasError;
                vm.Error = paymentVm.Error;
                vm.Products = await GetUserProductsAsync();
                return View(vm);
            }

            return RedirectToRoute(new { controller = "Payment", action = "History" });
        }

        public async Task<IActionResult> CreditCardPayment()
        {
            var products = await GetUserProductsAsync();
            var model = NewPaymentModel();
            model.Products = products.Where(c => c.ProductType == ProductType.SavingAccount).ToList();
            model.CreditCardsProducts = products.Where(c => c.ProductType == ProductType.CreditCard).ToList();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CreditCardPayment(SavePaymentViewModel vm)
        {
            ValidateAndConsumeSubmissionToken(vm);
            if (!ModelState.IsValid)
            {
                RenewSubmissionToken(vm);
                var products = await GetUserProductsAsync();
                vm.Products = products.Where(c => c.ProductType == ProductType.SavingAccount).ToList();
                vm.CreditCardsProducts = products.Where(c => c.ProductType == ProductType.CreditCard).ToList();
                return View(vm);
            }

            var paymentVm = await _paymentService.CreditCardPayment(vm);
            if (paymentVm.HasError)
            {
                RenewSubmissionToken(vm);
                var products = await GetUserProductsAsync();
                vm.HasError = paymentVm.HasError;
                vm.Error = paymentVm.Error;
                vm.Products = products.Where(c => c.ProductType == ProductType.SavingAccount).ToList();
                vm.CreditCardsProducts = products.Where(c => c.ProductType == ProductType.CreditCard).ToList();
                return View(vm);
            }

            return RedirectToRoute(new { controller = "Payment", action = "History" });
        }

        public async Task<IActionResult> LoanPayment()
        {
            var products = await GetUserProductsAsync();
            var model = NewPaymentModel();
            model.Products = products.Where(c => c.ProductType == ProductType.SavingAccount).ToList();
            model.LoanProducts = products.Where(c => c.ProductType == ProductType.Loan).ToList();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> LoanPayment(SavePaymentViewModel vm)
        {
            ValidateAndConsumeSubmissionToken(vm);
            if (!ModelState.IsValid)
            {
                RenewSubmissionToken(vm);
                var products = await GetUserProductsAsync();
                vm.Products = products.Where(c => c.ProductType == ProductType.SavingAccount).ToList();
                vm.LoanProducts = products.Where(c => c.ProductType == ProductType.Loan).ToList();
                return View(vm);
            }

            var paymentVm = await _paymentService.LoanPayment(vm);
            if (paymentVm.HasError)
            {
                RenewSubmissionToken(vm);
                var products = await GetUserProductsAsync();
                vm.HasError = paymentVm.HasError;
                vm.Error = paymentVm.Error;
                vm.Products = products.Where(c => c.ProductType == ProductType.SavingAccount).ToList();
                vm.LoanProducts = products.Where(c => c.ProductType == ProductType.Loan).ToList();
                return View(vm);
            }

            return RedirectToRoute(new { controller = "Payment", action = "History" });
        }

        public async Task<IActionResult> BeneficiaryPayment()
        {
            var model = NewPaymentModel();
            model.Products = await GetUserProductsAsync();
            model.Beneficiaries = await _beneficiaryService.GetAllBeneficiaryViewModel();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Confirm(SavePaymentViewModel vm)
        {
            ValidateSubmissionToken(vm);
            if (!ModelState.IsValid)
            {
                if (!PaymentSubmissionTokenHelper.IsValid(HttpContext, vm.SubmissionToken))
                {
                    RenewSubmissionToken(vm);
                }
                vm.Products = await GetUserProductsAsync();
                vm.Beneficiaries = await _beneficiaryService.GetAllBeneficiaryViewModel();
                return View("BeneficiaryPayment", vm);
            }

            var beneficiary = (await _beneficiaryService.GetAllBeneficiaryViewModel())
                .FirstOrDefault(b => b.AccountNumber == vm.DestinationAccountNumber);

            if (beneficiary == null)
            {
                vm.HasError = true;
                vm.Error = "Please select a valid beneficiary.";
                vm.Products = await GetUserProductsAsync();
                vm.Beneficiaries = await _beneficiaryService.GetAllBeneficiaryViewModel();
                return View("BeneficiaryPayment", vm);
            }

            var confirmVm = new ConfirmPaymentViewModel
            {
                BeneficiaryName = beneficiary.FullName,
                SourceAccountNumber = vm.SourceAccountNumber,
                DestinationAccountNumber = vm.DestinationAccountNumber,
                Amount = vm.Amount,
                SubmissionToken = vm.SubmissionToken
            };

            return View("ConfirmPayment", confirmVm);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmPayment(ConfirmPaymentViewModel confirmVm)
        {
            ValidateAndConsumeSubmissionToken(confirmVm);
            if (!ModelState.IsValid)
            {
                return await ReturnToBeneficiaryPaymentAsync(confirmVm, "The payment form expired or contains invalid values. Please review it and try again.");
            }

            var payment = new SavePaymentViewModel
            {
                SourceAccountNumber = confirmVm.SourceAccountNumber,
                DestinationAccountNumber = confirmVm.DestinationAccountNumber,
                Amount = confirmVm.Amount,
                SubmissionToken = confirmVm.SubmissionToken
            };

            var paymentVm = await _paymentService.BeneficiaryPayment(payment);
            if (paymentVm.HasError)
            {
                return await ReturnToBeneficiaryPaymentAsync(paymentVm, paymentVm.Error);
            }

            return RedirectToRoute(new { controller = "Payment", action = "History" });
        }

        public async Task<IActionResult> AccountTransfer()
        {
            var savingsAccounts = (await GetUserProductsAsync())
                .Where(c => c.ProductType == ProductType.SavingAccount)
                .ToList();

            var model = NewPaymentModel();
            model.Products = savingsAccounts;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AccountTransfer(SavePaymentViewModel vm)
        {
            ValidateAndConsumeSubmissionToken(vm);
            if (!ModelState.IsValid)
            {
                RenewSubmissionToken(vm);
                vm.Products = (await GetUserProductsAsync()).Where(c => c.ProductType == ProductType.SavingAccount).ToList();
                return View(vm);
            }

            var paymentVm = await _paymentService.AccountTrasfer(vm);
            if (paymentVm.HasError)
            {
                RenewSubmissionToken(vm);
                vm.HasError = paymentVm.HasError;
                vm.Error = paymentVm.Error;
                vm.Products = (await GetUserProductsAsync()).Where(c => c.ProductType == ProductType.SavingAccount).ToList();
                return View(vm);
            }

            return RedirectToRoute(new { controller = "Payment", action = "History" });
        }

        public async Task<IActionResult> CashAdvances()
        {
            var products = await GetUserProductsAsync();
            var model = NewPaymentModel();
            model.CreditCardsProducts = products.Where(p => p.ProductType == ProductType.CreditCard).ToList();
            model.Products = products.Where(p => p.ProductType == ProductType.SavingAccount).ToList();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CashAdvances(SavePaymentViewModel vm)
        {
            ValidateAndConsumeSubmissionToken(vm);
            if (!ModelState.IsValid)
            {
                RenewSubmissionToken(vm);
                var products = await GetUserProductsAsync();
                vm.CreditCardsProducts = products.Where(p => p.ProductType == ProductType.CreditCard).ToList();
                vm.Products = products.Where(p => p.ProductType == ProductType.SavingAccount).ToList();
                return View(vm);
            }

            var paymentVm = await _paymentService.CashAdvances(vm);
            if (paymentVm.HasError)
            {
                RenewSubmissionToken(vm);
                var products = await GetUserProductsAsync();
                vm.HasError = paymentVm.HasError;
                vm.Error = paymentVm.Error;
                vm.CreditCardsProducts = products.Where(p => p.ProductType == ProductType.CreditCard).ToList();
                vm.Products = products.Where(p => p.ProductType == ProductType.SavingAccount).ToList();
                return View(vm);
            }

            return RedirectToRoute(new { controller = "Payment", action = "History" });
        }

        public async Task<IActionResult> History()
        {
            if (string.IsNullOrWhiteSpace(_currentUserId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var history = await _paymentService.GetRecentActivityForUser(_currentUserId, 50);
            return View(history);
        }

        private SavePaymentViewModel NewPaymentModel()
        {
            return new SavePaymentViewModel
            {
                SubmissionToken = PaymentSubmissionTokenHelper.Issue(HttpContext)
            };
        }

        private void RenewSubmissionToken(SavePaymentViewModel vm)
        {
            vm.SubmissionToken = PaymentSubmissionTokenHelper.Issue(HttpContext);
            ModelState.Remove(nameof(vm.SubmissionToken));
        }

        private void ValidateSubmissionToken(SavePaymentViewModel vm)
        {
            if (!PaymentSubmissionTokenHelper.IsValid(HttpContext, vm.SubmissionToken))
            {
                ModelState.AddModelError(nameof(vm.SubmissionToken), "This payment form has expired. Please reload the page and try again.");
            }
        }

        private void ValidateAndConsumeSubmissionToken(SavePaymentViewModel vm)
        {
            if (!PaymentSubmissionTokenHelper.Consume(HttpContext, vm.SubmissionToken))
            {
                ModelState.AddModelError(nameof(vm.SubmissionToken), "This payment was already submitted or the form expired. Please reload the page and try again.");
            }
        }

        private async Task<IActionResult> ReturnToBeneficiaryPaymentAsync(SavePaymentViewModel vm, string? error)
        {
            RenewSubmissionToken(vm);
            vm.HasError = true;
            vm.Error = error ?? "The payment could not be completed.";
            vm.Products = await GetUserProductsAsync();
            vm.Beneficiaries = await _beneficiaryService.GetAllBeneficiaryViewModel();
            return View("BeneficiaryPayment", vm);
        }

        private async Task<List<InternetBanking.Core.Application.ViewModels.Product.ProductViewModel>> GetUserProductsAsync()
        {
            if (string.IsNullOrWhiteSpace(_currentUserId))
            {
                return new List<InternetBanking.Core.Application.ViewModels.Product.ProductViewModel>();
            }

            return await _productService.GetProductsByUserViewModel(_currentUserId);
        }
    }
}
