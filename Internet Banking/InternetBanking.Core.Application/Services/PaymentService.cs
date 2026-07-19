using AutoMapper;
using InternetBanking.Core.Application.Enums;
using InternetBanking.Core.Application.Interfaces.Repositories;
using InternetBanking.Core.Application.Interfaces.Services;
using InternetBanking.Core.Application.ViewModels.Payment;
using InternetBanking.Core.Application.ViewModels.Product;
using InternetBanking.Core.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace InternetBanking.Core.Application.Services
{
    public class PaymentService : GenericService<SavePaymentViewModel, PaymentViewModel, Payment>, IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IBeneficiaryRepository _beneficiaryRepository;
        private readonly IProductService _productService;
        private readonly IUserService _userService;
        private readonly string? _currentUserId;
        private readonly IMapper _mapper;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            IPaymentRepository paymentRepository,
            IBeneficiaryRepository beneficiaryRepository,
            IUserService userService,
            IProductService productService,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            ILogger<PaymentService> logger) : base(paymentRepository, mapper)
        {
            _paymentRepository = paymentRepository;
            _beneficiaryRepository = beneficiaryRepository;
            _productService = productService;
            _userService = userService;
            _mapper = mapper;
            _logger = logger;
            _currentUserId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        public override Task<SavePaymentViewModel> Add(SavePaymentViewModel vm)
        {
            return ExecuteFinancialOperationAsync(vm, async () =>
            {
                if (!await ValidateRequestAsync(vm)) return vm;

                var destinationProduct = await _productService.GetProductByAccountNumber(vm.DestinationAccountNumber);
                if (destinationProduct == null)
                {
                    return AddError(vm, "Destination account number was not found.");
                }

                var sourceProduct = await _productService.GetOwnedProductByAccountNumber(vm.SourceAccountNumber, _currentUserId!);
                if (sourceProduct == null)
                {
                    return AddError(vm, "Source account was not found or does not belong to you.");
                }

                if (!EnsureOwnedByCurrentUser(sourceProduct, vm, "You can only debit an account that belongs to you.")) return vm;
                if (!ValidateDistinctAccounts(sourceProduct, destinationProduct, vm)) return vm;
                if (!ValidateSufficientFunds(sourceProduct, vm.Amount, vm)) return vm;

                vm.PaymentType = PaymentType.Payment;
                SavePaymentViewModel payment = await base.Add(vm);

                if (sourceProduct.ProductType == ProductType.CreditCard)
                {
                    sourceProduct.Debt += vm.Amount;
                }

                sourceProduct.Balance -= vm.Amount;
                await UpdateProduct(sourceProduct);

                destinationProduct.Balance += vm.Amount;
                await UpdateProduct(destinationProduct);

                return payment;
            });
        }

        public Task<SavePaymentViewModel> CreditCardPayment(SavePaymentViewModel vm)
        {
            return ExecuteFinancialOperationAsync(vm, async () =>
            {
                if (!await ValidateRequestAsync(vm)) return vm;

                var destinationProduct = await _productService.GetOwnedProductByAccountNumber(vm.DestinationAccountNumber, _currentUserId!);
                if (destinationProduct == null)
                {
                    return AddError(vm, "Credit card account was not found or does not belong to you.");
                }

                if (destinationProduct.ProductType != ProductType.CreditCard)
                {
                    return AddError(vm, "The selected destination account is not a credit card.");
                }

                if (!EnsureOwnedByCurrentUser(destinationProduct, vm, "You can only pay your own credit cards.")) return vm;
                if (destinationProduct.Debt <= 0)
                {
                    return AddError(vm, "The selected credit card has no outstanding debt.");
                }

                var sourceProduct = await _productService.GetOwnedProductByAccountNumber(vm.SourceAccountNumber, _currentUserId!);
                if (sourceProduct == null)
                {
                    return AddError(vm, "Source account was not found or does not belong to you.");
                }

                if (!EnsureOwnedByCurrentUser(sourceProduct, vm, "You can only debit an account that belongs to you.")) return vm;
                if (!ValidateSavingsRepaymentSource(sourceProduct, vm)) return vm;
                if (!ValidateDistinctAccounts(sourceProduct, destinationProduct, vm)) return vm;

                if (vm.Amount > destinationProduct.Debt)
                {
                    vm.Amount = destinationProduct.Debt;
                }

                if (!ValidateSufficientFunds(sourceProduct, vm.Amount, vm)) return vm;

                vm.PaymentType = PaymentType.Payment;
                SavePaymentViewModel payment = await base.Add(vm);

                sourceProduct.Balance -= vm.Amount;
                await UpdateProduct(sourceProduct);

                destinationProduct.Balance += vm.Amount;
                destinationProduct.Debt -= vm.Amount;
                await UpdateProduct(destinationProduct);

                return payment;
            });
        }

        public Task<SavePaymentViewModel> LoanPayment(SavePaymentViewModel vm)
        {
            return ExecuteFinancialOperationAsync(vm, async () =>
            {
                if (!await ValidateRequestAsync(vm)) return vm;

                var destinationProduct = await _productService.GetOwnedProductByAccountNumber(vm.DestinationAccountNumber, _currentUserId!);
                if (destinationProduct == null)
                {
                    return AddError(vm, "Loan account was not found or does not belong to you.");
                }

                if (destinationProduct.ProductType != ProductType.Loan)
                {
                    return AddError(vm, "The selected destination account is not a loan.");
                }

                if (!EnsureOwnedByCurrentUser(destinationProduct, vm, "You can only pay your own loans.")) return vm;
                if (destinationProduct.Debt <= 0)
                {
                    return AddError(vm, "The selected loan has no outstanding debt.");
                }

                var sourceProduct = await _productService.GetOwnedProductByAccountNumber(vm.SourceAccountNumber, _currentUserId!);
                if (sourceProduct == null)
                {
                    return AddError(vm, "Source account was not found or does not belong to you.");
                }

                if (!EnsureOwnedByCurrentUser(sourceProduct, vm, "You can only debit an account that belongs to you.")) return vm;
                if (!ValidateSavingsRepaymentSource(sourceProduct, vm)) return vm;
                if (!ValidateDistinctAccounts(sourceProduct, destinationProduct, vm)) return vm;

                if (vm.Amount > destinationProduct.Debt)
                {
                    vm.Amount = destinationProduct.Debt;
                }

                if (!ValidateSufficientFunds(sourceProduct, vm.Amount, vm)) return vm;

                vm.PaymentType = PaymentType.Payment;
                SavePaymentViewModel payment = await base.Add(vm);

                sourceProduct.Balance -= vm.Amount;
                await UpdateProduct(sourceProduct);

                destinationProduct.Debt -= vm.Amount;
                await UpdateProduct(destinationProduct);

                return payment;
            });
        }

        public Task<SavePaymentViewModel> BeneficiaryPayment(SavePaymentViewModel vm)
        {
            return ExecuteFinancialOperationAsync(vm, async () =>
            {
                if (!await ValidateRequestAsync(vm)) return vm;

                if (!await _beneficiaryRepository.ExistsForOwnerAndAccountAsync(_currentUserId!, vm.DestinationAccountNumber))
                {
                    return AddError(vm, "The selected destination is not one of your beneficiaries.");
                }

                var destinationProduct = await _productService.GetProductByAccountNumber(vm.DestinationAccountNumber);
                if (destinationProduct == null)
                {
                    return AddError(vm, "Beneficiary account number was not found.");
                }

                var sourceProduct = await _productService.GetOwnedProductByAccountNumber(vm.SourceAccountNumber, _currentUserId!);
                if (sourceProduct == null)
                {
                    return AddError(vm, "Source account was not found or does not belong to you.");
                }

                if (!EnsureOwnedByCurrentUser(sourceProduct, vm, "You can only debit an account that belongs to you.")) return vm;
                if (!ValidateDistinctAccounts(sourceProduct, destinationProduct, vm)) return vm;
                if (!ValidateSufficientFunds(sourceProduct, vm.Amount, vm)) return vm;

                vm.PaymentType = PaymentType.Payment;
                SavePaymentViewModel payment = await base.Add(vm);

                if (sourceProduct.ProductType == ProductType.CreditCard)
                {
                    sourceProduct.Debt += vm.Amount;
                }

                sourceProduct.Balance -= vm.Amount;
                await UpdateProduct(sourceProduct);

                destinationProduct.Balance += vm.Amount;
                await UpdateProduct(destinationProduct);

                return payment;
            });
        }

        public Task<SavePaymentViewModel> AccountTrasfer(SavePaymentViewModel vm)
        {
            return ExecuteFinancialOperationAsync(vm, async () =>
            {
                if (!await ValidateRequestAsync(vm)) return vm;

                var destinationProduct = await _productService.GetOwnedProductByAccountNumber(vm.DestinationAccountNumber, _currentUserId!);
                if (destinationProduct == null)
                {
                    return AddError(vm, "Destination savings account was not found or does not belong to you.");
                }

                if (destinationProduct.ProductType != ProductType.SavingAccount)
                {
                    return AddError(vm, "Account transfers are only available between savings accounts.");
                }

                var sourceProduct = await _productService.GetOwnedProductByAccountNumber(vm.SourceAccountNumber, _currentUserId!);
                if (sourceProduct == null)
                {
                    return AddError(vm, "Source account was not found or does not belong to you.");
                }

                if (sourceProduct.ProductType != ProductType.SavingAccount)
                {
                    return AddError(vm, "Account transfers are only available between savings accounts.");
                }

                if (!EnsureOwnedByCurrentUser(sourceProduct, vm, "You can only debit an account that belongs to you.")) return vm;
                if (!ValidateDistinctAccounts(sourceProduct, destinationProduct, vm)) return vm;
                if (!ValidateSufficientFunds(sourceProduct, vm.Amount, vm)) return vm;

                vm.PaymentType = PaymentType.Transaction;
                SavePaymentViewModel payment = await base.Add(vm);

                sourceProduct.Balance -= vm.Amount;
                await UpdateProduct(sourceProduct);

                destinationProduct.Balance += vm.Amount;
                await UpdateProduct(destinationProduct);

                return payment;
            });
        }

        public Task<SavePaymentViewModel> CashAdvances(SavePaymentViewModel vm)
        {
            return ExecuteFinancialOperationAsync(vm, async () =>
            {
                if (!await ValidateRequestAsync(vm)) return vm;

                var destinationProduct = await _productService.GetOwnedProductByAccountNumber(vm.DestinationAccountNumber, _currentUserId!);
                if (destinationProduct == null)
                {
                    return AddError(vm, "Destination savings account was not found or does not belong to you.");
                }

                if (destinationProduct.ProductType != ProductType.SavingAccount)
                {
                    return AddError(vm, "Cash advances can only be deposited into a savings account.");
                }

                var sourceProduct = await _productService.GetOwnedProductByAccountNumber(vm.SourceAccountNumber, _currentUserId!);
                if (sourceProduct == null)
                {
                    return AddError(vm, "Credit card account was not found or does not belong to you.");
                }

                if (sourceProduct.ProductType != ProductType.CreditCard)
                {
                    return AddError(vm, "Cash advances can only be processed from a credit card.");
                }

                if (!EnsureOwnedByCurrentUser(sourceProduct, vm, "You can only use one of your own credit cards for a cash advance.")) return vm;
                if (!ValidateDistinctAccounts(sourceProduct, destinationProduct, vm)) return vm;

                decimal totalAmount = decimal.Round(vm.Amount * 1.0625m, 2, MidpointRounding.AwayFromZero);
                if (!ValidateSufficientFunds(sourceProduct, totalAmount, vm, "available credit")) return vm;

                vm.PaymentType = PaymentType.Transaction;
                SavePaymentViewModel payment = await base.Add(vm);

                sourceProduct.Balance -= totalAmount;
                sourceProduct.Debt += totalAmount;
                await UpdateProduct(sourceProduct);

                destinationProduct.Balance += vm.Amount;
                await UpdateProduct(destinationProduct);

                return payment;
            });
        }

        public async Task<int> GetPaymentsMadeLast24Hours()
        {
            var payments = await _paymentRepository.GetAllAsync();
            DateTime now = DateTime.UtcNow;
            DateTime twentyFourHoursAgo = now.AddHours(-24);

            return payments.Count(p => p.PaymentTypeId == (int)PaymentType.Payment && p.Created >= twentyFourHoursAgo && p.Created <= now);
        }

        public async Task<int> GetPaymentsMadeAllTheTime()
        {
            var payments = await _paymentRepository.GetAllAsync();
            return payments.Count(t => t.PaymentTypeId == (int)PaymentType.Payment);
        }

        public async Task<int> GetTransactionsMadeLast24Hours()
        {
            var transactions = await _paymentRepository.GetAllAsync();
            DateTime now = DateTime.UtcNow;
            DateTime twentyFourHoursAgo = now.AddHours(-24);

            return transactions.Count(p => p.PaymentTypeId == (int)PaymentType.Transaction && p.Created >= twentyFourHoursAgo && p.Created <= now);
        }

        public async Task<int> GetTransactionsMadeAllTheTime()
        {
            var transactions = await _paymentRepository.GetAllAsync();
            return transactions.Count(t => t.PaymentTypeId == (int)PaymentType.Transaction);
        }

        public async Task<List<TransactionHistoryItemViewModel>> GetRecentActivity(int count = 10)
        {
            var items = await _paymentRepository.GetRecentAsync(count);
            return items.Select(payment => MapToHistoryItem(payment, new HashSet<string>())).ToList();
        }

        public async Task<List<TransactionHistoryItemViewModel>> GetRecentActivityForUser(string userId, int count = 10)
        {
            var products = await _productService.GetProductsByUserViewModel(userId);
            var accountNumbers = products.Select(p => p.AccountNumber).Distinct().ToList();
            if (accountNumbers.Count == 0)
            {
                return new List<TransactionHistoryItemViewModel>();
            }

            var items = await _paymentRepository.GetByAccountNumbersAsync(accountNumbers);
            var ownedAccounts = accountNumbers.ToHashSet();

            return items
                .Take(count)
                .Select(payment => MapToHistoryItem(payment, ownedAccounts))
                .ToList();
        }

        private async Task<SavePaymentViewModel> ExecuteFinancialOperationAsync(
            SavePaymentViewModel vm,
            Func<Task<SavePaymentViewModel>> operation)
        {
            try
            {
                return await _paymentRepository.ExecuteInTransactionAsync(operation);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "A financial operation was rejected because account data changed concurrently.");
                return AddError(vm, "The account balance changed while the operation was being processed. No changes were applied. Please reload and try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "A financial operation failed and was rolled back.");
                return AddError(vm, "The operation could not be completed. No balance changes were applied.");
            }
        }

        private async Task<bool> ValidateRequestAsync(SavePaymentViewModel vm)
        {
            if (string.IsNullOrWhiteSpace(_currentUserId))
            {
                AddError(vm, "Your sign-in session is no longer valid. Please sign in again.");
                return false;
            }

            var currentUser = await _userService.GetByIdAsync(_currentUserId);
            if (currentUser.HasError || currentUser.IsActive != true)
            {
                AddError(vm, "Your account is no longer active. Please sign in again or contact an administrator.");
                return false;
            }

            if (vm.Amount <= 0 || vm.Amount > 100000000m)
            {
                AddError(vm, "Amount must be greater than zero and within the supported limit.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(vm.SourceAccountNumber) || string.IsNullOrWhiteSpace(vm.DestinationAccountNumber))
            {
                AddError(vm, "Both source and destination accounts are required.");
                return false;
            }

            return true;
        }

        private bool EnsureOwnedByCurrentUser(ProductViewModel product, SavePaymentViewModel vm, string message)
        {
            if (string.IsNullOrWhiteSpace(_currentUserId) || product.UserId != _currentUserId)
            {
                AddError(vm, message);
                return false;
            }

            return true;
        }


        private static bool ValidateSavingsRepaymentSource(ProductViewModel source, SavePaymentViewModel vm)
        {
            if (source.ProductType != ProductType.SavingAccount)
            {
                AddError(vm, "Credit card and loan repayments must use one of your savings accounts as the source.");
                return false;
            }

            return true;
        }

        private static bool ValidateDistinctAccounts(ProductViewModel source, ProductViewModel destination, SavePaymentViewModel vm)
        {
            if (source.AccountNumber == destination.AccountNumber)
            {
                AddError(vm, "Source and destination accounts cannot be the same.");
                return false;
            }

            return true;
        }

        private static bool ValidateSufficientFunds(
            ProductViewModel source,
            decimal amount,
            SavePaymentViewModel vm,
            string balanceLabel = "funds")
        {
            if (source.Balance < amount)
            {
                AddError(vm, $"Insufficient {balanceLabel} in the source account.");
                return false;
            }

            return true;
        }

        private static SavePaymentViewModel AddError(SavePaymentViewModel vm, string message)
        {
            vm.HasError = true;
            vm.Error = message;
            return vm;
        }

        private async Task UpdateProduct(ProductViewModel product)
        {
            SaveProductViewModel productVm = _mapper.Map<SaveProductViewModel>(product);
            await _productService.Update(productVm, productVm.Id!.Value);
        }

        private static TransactionHistoryItemViewModel MapToHistoryItem(Payment payment, HashSet<string> ownedAccounts)
        {
            bool sourceOwned = ownedAccounts.Contains(payment.SourceAccountNumber);
            bool destinationOwned = ownedAccounts.Contains(payment.DestinationAccountNumber);

            string direction = sourceOwned && destinationOwned
                ? "Internal"
                : sourceOwned
                    ? "Outgoing"
                    : destinationOwned
                        ? "Incoming"
                        : "System";

            string description = payment.PaymentTypeId == (int)PaymentType.Transaction
                ? "Transfer between accounts"
                : "Payment operation";

            return new TransactionHistoryItemViewModel
            {
                Id = payment.Id,
                SourceAccountNumber = payment.SourceAccountNumber,
                DestinationAccountNumber = payment.DestinationAccountNumber,
                Amount = payment.Amount,
                PaymentType = (PaymentType)payment.PaymentTypeId,
                Created = payment.Created,
                Direction = direction,
                Description = description
            };
        }
    }
}
