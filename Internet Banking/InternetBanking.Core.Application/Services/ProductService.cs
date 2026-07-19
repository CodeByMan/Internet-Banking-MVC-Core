using AutoMapper;
using InternetBanking.Core.Application.Enums;
using InternetBanking.Core.Application.Helpers;
using InternetBanking.Core.Application.Interfaces.Repositories;
using InternetBanking.Core.Application.Interfaces.Services;
using InternetBanking.Core.Application.ViewModels.Product;
using InternetBanking.Core.Application.ViewModels.User;
using InternetBanking.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InternetBanking.Core.Application.Services
{
    public class ProductService : GenericService<SaveProductViewModel, ProductViewModel, Product>, IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly IUserService _userService;
        private readonly IMapper _mapper;

        public ProductService(IProductRepository productRepository, IUserService userService, IMapper mapper) : base(productRepository, mapper)
        {
            _productRepository = productRepository;
            _userService = userService;
            _mapper = mapper;
        }

        public override async Task<SaveProductViewModel> Add(SaveProductViewModel vm)
        {
            const int maxAccountNumberAttempts = 10;

            for (int attempt = 1; attempt <= maxAccountNumberAttempts; attempt++)
            {
                vm.AccountNumber = UniqueIdGenerator.GenerateUniqueId();

                try
                {
                    if (vm.ProductType != ProductType.Loan)
                    {
                        return await base.Add(vm);
                    }

                    return await _productRepository.ExecuteInTransactionAsync(async () =>
                    {
                        var product = await base.Add(vm);
                        await TransferAmountToPrincipal(vm.UserId, product.Amount);

                        product.Debt += product.Amount;
                        product.Amount = 0;
                        await Update(product, product.Id!.Value);
                        return product;
                    });
                }
                catch (DbUpdateException ex) when (_productRepository.IsAccountNumberUniqueConstraintViolation(ex))
                {
                    if (attempt == maxAccountNumberAttempts)
                    {
                        vm.HasError = true;
                        vm.Error = "A unique account number could not be generated. Please try creating the product again.";
                        return vm;
                    }
                }
            }

            vm.HasError = true;
            vm.Error = "A unique account number could not be generated. Please try creating the product again.";
            return vm;
        }

        public async Task TransferAmountToPrincipal(string userId, decimal amount)
        {
            if (amount <= 0)
            {
                return;
            }

            var principalProduct = await _productRepository.GetPrincipalProductByUserIdAsync(userId);
            if (principalProduct != null)
            {
                principalProduct.Balance += amount;
                await _productRepository.UpdateAsync(principalProduct, principalProduct.Id);
            }
        }

        public override async Task Delete(int id)
        {
            await _productRepository.ExecuteInTransactionAsync(async () =>
            {
                var product = await base.GetByIdSaveViewModel(id);
                if (product.ProductType == ProductType.SavingAccount && product.Amount > 0)
                {
                    await TransferAmountToPrincipal(product.UserId, product.Amount);
                }

                await base.Delete(id);
                return true;
            });
        }

        public async Task<ProductViewModel?> GetProductByAccountNumber(string accountNumber)
        {
            var product = await _productRepository.GetProductByAccountNumberAsync(accountNumber);
            return product == null ? null : await MapProductAsync(product);
        }

        public async Task<ProductViewModel?> GetOwnedProductByAccountNumber(string accountNumber, string userId)
        {
            var product = await _productRepository.GetProductByAccountNumberAndUserIdAsync(accountNumber, userId);
            return product == null ? null : await MapProductAsync(product);
        }

        public async Task<List<ProductViewModel>> GetProductsByUserViewModel(string userId)
        {
            var products = await _productRepository.GetAllProductsByUserIdAsync(userId);
            SaveUserViewModel user = await _userService.GetByIdAsync(userId);

            var fullName = user.HasError ? string.Empty : $"{user.FirstName} {user.LastName}".Trim();
            var userName = user.HasError ? string.Empty : user.UserName;

            return products
                .Select(p => new ProductViewModel
                {
                    Id = p.Id,
                    UserId = p.UserId,
                    UserName = userName,
                    FullName = fullName,
                    AccountNumber = p.AccountNumber,
                    Balance = p.Balance,
                    Debt = p.Debt,
                    IsPrincipal = p.IsPrincipal,
                    ProductType = (ProductType)p.ProductTypeId,
                    RowVersion = p.RowVersion
                })
                .OrderByDescending(p => p.IsPrincipal)
                .ThenBy(p => p.ProductType)
                .ThenBy(p => p.AccountNumber)
                .ToList();
        }

        private async Task<ProductViewModel> MapProductAsync(Product product)
        {
            var productVm = _mapper.Map<ProductViewModel>(product);
            if (!string.IsNullOrWhiteSpace(productVm.UserId))
            {
                SaveUserViewModel user = await _userService.GetByIdAsync(product.UserId);
                if (!user.HasError)
                {
                    productVm.UserId = user.Id ?? productVm.UserId;
                    productVm.UserName = user.UserName;
                    productVm.FullName = $"{user.FirstName} {user.LastName}".Trim();
                }
            }

            return productVm;
        }

        public async Task<int> GetProductsCreatedAllTheTime()
        {
            var products = await _productRepository.GetAllAsync();
            return products.Count;
        }
    }
}
