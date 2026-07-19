using AutoMapper;
using InternetBanking.Core.Application.Interfaces.Repositories;
using InternetBanking.Core.Application.Interfaces.Services;
using InternetBanking.Core.Application.ViewModels.Beneficiary;
using InternetBanking.Core.Domain.Entities;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace InternetBanking.Core.Application.Services
{
    public class BeneficiaryService : GenericService<SaveBeneficiaryViewModel, BeneficiaryViewModel, Beneficiary>, IBeneficiaryService
    {
        private readonly IBeneficiaryRepository _beneficiaryRepository;
        private readonly string? _currentUserId;
        private readonly IUserService _userService;
        private readonly IProductService _productService;
        private readonly IMapper _mapper;

        public BeneficiaryService(
            IBeneficiaryRepository beneficiaryRepository,
            IUserService userService,
            IProductService productService,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper) : base(beneficiaryRepository, mapper)
        {
            _beneficiaryRepository = beneficiaryRepository;
            _userService = userService;
            _productService = productService;
            _mapper = mapper;
            _currentUserId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        public override async Task<SaveBeneficiaryViewModel> Add(SaveBeneficiaryViewModel vm)
        {
            if (!await IsCurrentUserActiveAsync())
            {
                return AddError(vm, "Your account is no longer active. Please sign in again or contact an administrator.");
            }

            var product = await _productService.GetProductByAccountNumber(vm.AccountNumberBeneficiary);
            if (product == null)
            {
                return AddError(vm, "The beneficiary account number was not found.");
            }

            var products = await _productService.GetProductsByUserViewModel(_currentUserId!);
            if (products.Any(p => p.AccountNumber == vm.AccountNumberBeneficiary))
            {
                return AddError(vm, "You cannot add one of your own accounts as a beneficiary.");
            }

            if (await _beneficiaryRepository.ExistsForOwnerAndAccountAsync(_currentUserId!, vm.AccountNumberBeneficiary))
            {
                return AddError(vm, "This beneficiary is already registered.");
            }

            vm.UserOwnerId = _currentUserId;
            return await base.Add(vm);
        }

        public async Task<List<BeneficiaryViewModel>> GetAllBeneficiaryViewModel()
        {
            if (string.IsNullOrWhiteSpace(_currentUserId))
            {
                return new List<BeneficiaryViewModel>();
            }

            var beneficiaries = await _beneficiaryRepository.GetAllByOwnerAsync(_currentUserId!);
            var beneficiaryVms = beneficiaries
                .Select(async beneficiary =>
                {
                    var beneficiaryProduct = await _productService.GetProductByAccountNumber(beneficiary.AccountNumberBeneficiary);
                    if (beneficiaryProduct == null)
                    {
                        return null;
                    }

                    var user = await _userService.GetByIdAsync(beneficiaryProduct.UserId);
                    var fullName = user.HasError
                        ? beneficiaryProduct.FullName
                        : $"{user.FirstName} {user.LastName}".Trim();

                    return new BeneficiaryViewModel
                    {
                        Id = beneficiary.Id,
                        FullName = fullName,
                        AccountNumber = beneficiaryProduct.AccountNumber
                    };
                });

            return (await Task.WhenAll(beneficiaryVms))
                .Where(item => item != null)
                .Cast<BeneficiaryViewModel>()
                .OrderBy(item => item.FullName)
                .ToList();
        }

        public async Task<SaveBeneficiaryViewModel?> GetOwnedByIdAsync(int id)
        {
            if (string.IsNullOrWhiteSpace(_currentUserId))
            {
                return null;
            }

            var beneficiary = await _beneficiaryRepository.GetByIdAndOwnerAsync(id, _currentUserId);
            return beneficiary == null ? null : _mapper.Map<SaveBeneficiaryViewModel>(beneficiary);
        }

        public async Task<bool> DeleteOwnedAsync(int id)
        {
            if (string.IsNullOrWhiteSpace(_currentUserId))
            {
                return false;
            }

            var beneficiary = await _beneficiaryRepository.GetByIdAndOwnerAsync(id, _currentUserId);
            if (beneficiary == null)
            {
                return false;
            }

            await _beneficiaryRepository.DeleteAsync(beneficiary);
            return true;
        }

        private async Task<bool> IsCurrentUserActiveAsync()
        {
            if (string.IsNullOrWhiteSpace(_currentUserId))
            {
                return false;
            }

            var user = await _userService.GetByIdAsync(_currentUserId);
            return !user.HasError && user.IsActive == true;
        }

        private static SaveBeneficiaryViewModel AddError(SaveBeneficiaryViewModel vm, string message)
        {
            vm.HasError = true;
            vm.Error = message;
            return vm;
        }
    }
}
