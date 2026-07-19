using AutoMapper;
using InternetBanking.Core.Application.Dtos.Account;
using InternetBanking.Core.Application.Enums;
using InternetBanking.Core.Application.Interfaces.Repositories;
using InternetBanking.Core.Application.Interfaces.Services;
using InternetBanking.Core.Application.Mappings;
using InternetBanking.Core.Application.Services;
using InternetBanking.Core.Application.ViewModels.Beneficiary;
using InternetBanking.Core.Application.ViewModels.Payment;
using InternetBanking.Core.Application.ViewModels.Product;
using InternetBanking.Core.Application.ViewModels.User;
using InternetBanking.Core.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;

namespace InternetBanking.Tests;

public class PaymentServiceTests
{
    private const string CurrentUserId = "user-1";

    [Fact]
    public async Task ExpressPayment_RejectsSourceAccountOwnedByAnotherUser()
    {
        var products = new FakeProductService(
            Product("FOREIGN", "user-2", 500m),
            Product("DEST", "user-3", 100m));
        var repository = new FakePaymentRepository(products);
        var service = CreateService(repository, products);

        var result = await service.Add(Payment("FOREIGN", "DEST", 50m));

        Assert.True(result.HasError);
        Assert.Contains("belong to you", result.Error);
        Assert.Empty(repository.Payments);
        Assert.Equal(500m, products["FOREIGN"].Balance);
    }

    [Fact]
    public async Task ExpressPayment_DebitsOwnedSourceAndCreditsDestination()
    {
        var products = new FakeProductService(
            Product("SOURCE", CurrentUserId, 500m),
            Product("DEST", "user-2", 100m));
        var repository = new FakePaymentRepository(products);
        var service = CreateService(repository, products);

        var result = await service.Add(Payment("SOURCE", "DEST", 125.25m));

        Assert.False(result.HasError);
        Assert.Single(repository.Payments);
        Assert.Equal(374.75m, products["SOURCE"].Balance);
        Assert.Equal(225.25m, products["DEST"].Balance);
        Assert.True(repository.TransactionExecuted);
    }

    [Fact]
    public async Task ExpressPayment_RejectsInsufficientFundsWithoutWritingPayment()
    {
        var products = new FakeProductService(
            Product("SOURCE", CurrentUserId, 20m),
            Product("DEST", "user-2", 100m));
        var repository = new FakePaymentRepository(products);
        var service = CreateService(repository, products);

        var result = await service.Add(Payment("SOURCE", "DEST", 25m));

        Assert.True(result.HasError);
        Assert.Contains("Insufficient", result.Error);
        Assert.Empty(repository.Payments);
    }

    [Fact]
    public async Task ExpressPayment_RejectsUnknownDestination()
    {
        var products = new FakeProductService(Product("SOURCE", CurrentUserId, 100m));
        var repository = new FakePaymentRepository(products);
        var service = CreateService(repository, products);

        var result = await service.Add(Payment("SOURCE", "MISSING", 10m));

        Assert.True(result.HasError);
        Assert.Empty(repository.Payments);
    }

    [Fact]
    public async Task BeneficiaryPayment_RejectsDestinationNotOwnedAsBeneficiary()
    {
        var products = new FakeProductService(
            Product("SOURCE", CurrentUserId, 100m),
            Product("DEST", "user-2", 50m));
        var repository = new FakePaymentRepository(products);
        var beneficiaries = new FakeBeneficiaryRepository();
        var service = CreateService(repository, products, beneficiaries);

        var result = await service.BeneficiaryPayment(Payment("SOURCE", "DEST", 10m));

        Assert.True(result.HasError);
        Assert.Contains("not one of your beneficiaries", result.Error);
        Assert.Empty(repository.Payments);
    }

    [Fact]
    public async Task FailedDestinationUpdate_RollsBackPaymentAndBalances()
    {
        var products = new FakeProductService(
            Product("SOURCE", CurrentUserId, 100m),
            Product("DEST", "user-2", 50m))
        {
            ThrowOnUpdateNumber = 2
        };
        var repository = new FakePaymentRepository(products);
        var service = CreateService(repository, products);

        var result = await service.Add(Payment("SOURCE", "DEST", 25m));

        Assert.True(result.HasError);
        Assert.Empty(repository.Payments);
        Assert.Equal(100m, products["SOURCE"].Balance);
        Assert.Equal(50m, products["DEST"].Balance);
    }

    [Fact]
    public async Task ConcurrentBalanceChange_ReturnsSafeErrorAndRollsBack()
    {
        var products = new FakeProductService(
            Product("SOURCE", CurrentUserId, 100m),
            Product("DEST", "user-2", 50m))
        {
            ThrowConcurrencyOnUpdateNumber = 1
        };
        var repository = new FakePaymentRepository(products);
        var service = CreateService(repository, products);

        var result = await service.Add(Payment("SOURCE", "DEST", 25m));

        Assert.True(result.HasError);
        Assert.Contains("changed", result.Error);
        Assert.Empty(repository.Payments);
        Assert.Equal(100m, products["SOURCE"].Balance);
    }


    [Fact]
    public async Task CreditCardPayment_RejectsCreditCardAsRepaymentSource()
    {
        var source = Product("CARD-A", CurrentUserId, 5000m);
        source.ProductType = ProductType.CreditCard;
        source.Debt = 1000m;
        var destination = Product("CARD-B", CurrentUserId, 1500m);
        destination.ProductType = ProductType.CreditCard;
        destination.Debt = 500m;

        var products = new FakeProductService(source, destination);
        var repository = new FakePaymentRepository(products);
        var service = CreateService(repository, products);

        var result = await service.CreditCardPayment(Payment("CARD-A", "CARD-B", 250m));

        Assert.True(result.HasError);
        Assert.Contains("savings accounts", result.Error);
        Assert.Empty(repository.Payments);
        Assert.Equal(5000m, products["CARD-A"].Balance);
        Assert.Equal(1000m, products["CARD-A"].Debt);
        Assert.Equal(500m, products["CARD-B"].Debt);
    }

    [Fact]
    public async Task LoanPayment_RejectsNonSavingsRepaymentSource()
    {
        var source = Product("CARD-A", CurrentUserId, 5000m);
        source.ProductType = ProductType.CreditCard;
        source.Debt = 1000m;
        var destination = Product("LOAN-A", CurrentUserId, 0m);
        destination.ProductType = ProductType.Loan;
        destination.Debt = 2000m;

        var products = new FakeProductService(source, destination);
        var repository = new FakePaymentRepository(products);
        var service = CreateService(repository, products);

        var result = await service.LoanPayment(Payment("CARD-A", "LOAN-A", 250m));

        Assert.True(result.HasError);
        Assert.Contains("savings accounts", result.Error);
        Assert.Empty(repository.Payments);
        Assert.Equal(5000m, products["CARD-A"].Balance);
        Assert.Equal(1000m, products["CARD-A"].Debt);
        Assert.Equal(2000m, products["LOAN-A"].Debt);
    }

    [Fact]
    public void MonetaryProperties_UseDecimalPrecision()
    {
        Assert.Equal(typeof(decimal), typeof(InternetBanking.Core.Domain.Entities.Product).GetProperty(nameof(InternetBanking.Core.Domain.Entities.Product.Balance))!.PropertyType);
        Assert.Equal(typeof(decimal), typeof(InternetBanking.Core.Domain.Entities.Product).GetProperty(nameof(InternetBanking.Core.Domain.Entities.Product.Debt))!.PropertyType);
        Assert.Equal(typeof(decimal), typeof(InternetBanking.Core.Domain.Entities.Payment).GetProperty(nameof(InternetBanking.Core.Domain.Entities.Payment.Amount))!.PropertyType);
        Assert.Equal(0.30m, 0.10m + 0.20m);
    }

    private static PaymentService CreateService(
        FakePaymentRepository paymentRepository,
        FakeProductService productService,
        FakeBeneficiaryRepository? beneficiaryRepository = null)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, CurrentUserId),
                    new Claim(ClaimTypes.Name, "clientuser"),
                    new Claim(ClaimTypes.Role, Roles.Client.ToString())
                },
                "Test"))
        };

        IMapper mapper = new MapperConfiguration(configuration => configuration.AddProfile<GeneralProfile>()).CreateMapper();

        return new PaymentService(
            paymentRepository,
            beneficiaryRepository ?? new FakeBeneficiaryRepository(),
            new FakeUserService(CurrentUserId, true),
            productService,
            new HttpContextAccessor { HttpContext = httpContext },
            mapper,
            NullLogger<PaymentService>.Instance);
    }

    private static SavePaymentViewModel Payment(string source, string destination, decimal amount) => new()
    {
        SourceAccountNumber = source,
        DestinationAccountNumber = destination,
        Amount = amount,
        SubmissionToken = "test-token"
    };

    private static ProductViewModel Product(string account, string userId, decimal balance) => new()
    {
        Id = account.GetHashCode(),
        AccountNumber = account,
        UserId = userId,
        Balance = balance,
        Debt = 0m,
        ProductType = ProductType.SavingAccount,
        RowVersion = new byte[] { 1 }
    };
}

internal interface ITransactionalTestState
{
    object Capture();
    void Restore(object snapshot);
}

internal sealed class FakePaymentRepository : IPaymentRepository
{
    private readonly ITransactionalTestState _externalState;
    private int _nextId = 1;

    public FakePaymentRepository(ITransactionalTestState externalState)
    {
        _externalState = externalState;
    }

    public List<Payment> Payments { get; } = new();
    public bool TransactionExecuted { get; private set; }

    public Task<Payment> AddAsync(Payment entity)
    {
        entity.Id = _nextId++;
        Payments.Add(Clone(entity));
        return Task.FromResult(entity);
    }

    public Task UpdateAsync(Payment entity, int id) => Task.CompletedTask;
    public Task DeleteAsync(Payment entity)
    {
        Payments.RemoveAll(p => p.Id == entity.Id);
        return Task.CompletedTask;
    }
    public Task<List<Payment>> GetAllAsync() => Task.FromResult(Payments.Select(Clone).ToList());
    public Task<List<Payment>> GetAllWithIncludeAsync(List<string> properties) => GetAllAsync();
    public Task<Payment?> GetByIdAsync(int id) => Task.FromResult(Payments.FirstOrDefault(p => p.Id == id));
    public Task<List<Payment>> GetByAccountNumbersAsync(List<string> accountNumbers) =>
        Task.FromResult(Payments.Where(p => accountNumbers.Contains(p.SourceAccountNumber) || accountNumbers.Contains(p.DestinationAccountNumber)).Select(Clone).ToList());
    public Task<List<Payment>> GetRecentAsync(int count) => Task.FromResult(Payments.TakeLast(count).Reverse().Select(Clone).ToList());

    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation)
    {
        TransactionExecuted = true;
        var paymentSnapshot = Payments.Select(Clone).ToList();
        var externalSnapshot = _externalState.Capture();
        try
        {
            return await operation();
        }
        catch
        {
            Payments.Clear();
            Payments.AddRange(paymentSnapshot.Select(Clone));
            _externalState.Restore(externalSnapshot);
            throw;
        }
    }

    private static Payment Clone(Payment source) => new()
    {
        Id = source.Id,
        SourceAccountNumber = source.SourceAccountNumber,
        DestinationAccountNumber = source.DestinationAccountNumber,
        Amount = source.Amount,
        PaymentTypeId = source.PaymentTypeId,
        Created = source.Created,
        CreatedBy = source.CreatedBy,
        LastModified = source.LastModified,
        LastModifiedBy = source.LastModifiedBy
    };
}

internal sealed class FakeProductService : IProductService, ITransactionalTestState
{
    private readonly Dictionary<string, ProductViewModel> _products;
    private int _updateCount;

    public FakeProductService(params ProductViewModel[] products)
    {
        _products = products.ToDictionary(p => p.AccountNumber, Clone);
    }

    public int? ThrowOnUpdateNumber { get; set; }
    public int? ThrowConcurrencyOnUpdateNumber { get; set; }
    public bool ThrowOnTransfer { get; set; }
    public int TransferCallCount { get; private set; }
    public ProductViewModel this[string account] => _products[account];

    public Task<ProductViewModel?> GetProductByAccountNumber(string accountNumber) =>
        Task.FromResult(_products.TryGetValue(accountNumber, out var product) ? Clone(product) : null);

    public Task<ProductViewModel?> GetOwnedProductByAccountNumber(string accountNumber, string userId) =>
        Task.FromResult(_products.TryGetValue(accountNumber, out var product) && product.UserId == userId ? Clone(product) : null);

    public Task<List<ProductViewModel>> GetProductsByUserViewModel(string userId) =>
        Task.FromResult(_products.Values.Where(p => p.UserId == userId).Select(Clone).ToList());

    public Task Update(SaveProductViewModel vm, int id)
    {
        _updateCount++;
        if (ThrowConcurrencyOnUpdateNumber == _updateCount)
        {
            throw new DbUpdateConcurrencyException("Simulated row-version conflict.");
        }
        if (ThrowOnUpdateNumber == _updateCount)
        {
            throw new InvalidOperationException("Simulated destination update failure.");
        }

        var product = _products.Values.Single(p => p.Id == id);
        product.Balance = vm.Amount;
        product.Debt = vm.Debt;
        product.RowVersion = vm.RowVersion;
        return Task.CompletedTask;
    }

    public Task<SaveProductViewModel> Add(SaveProductViewModel vm) => Task.FromResult(vm);
    public Task Delete(int id) => Task.CompletedTask;
    public Task<SaveProductViewModel> GetByIdSaveViewModel(int id) => throw new NotSupportedException();
    public Task<List<ProductViewModel>> GetAllViewModel() => Task.FromResult(_products.Values.Select(Clone).ToList());
    public Task<int> GetProductsCreatedAllTheTime() => Task.FromResult(_products.Count);
    public Task TransferAmountToPrincipal(string userId, decimal amount)
    {
        TransferCallCount++;
        if (ThrowOnTransfer)
        {
            throw new InvalidOperationException("Simulated top-up failure.");
        }
        return Task.CompletedTask;
    }

    public object Capture() => _products.ToDictionary(pair => pair.Key, pair => Clone(pair.Value));

    public void Restore(object snapshot)
    {
        _products.Clear();
        foreach (var pair in (Dictionary<string, ProductViewModel>)snapshot)
        {
            _products[pair.Key] = Clone(pair.Value);
        }
        _updateCount = 0;
    }

    private static ProductViewModel Clone(ProductViewModel source) => new()
    {
        Id = source.Id,
        AccountNumber = source.AccountNumber,
        UserId = source.UserId,
        FullName = source.FullName,
        UserName = source.UserName,
        Balance = source.Balance,
        Debt = source.Debt,
        ProductType = source.ProductType,
        IsPrincipal = source.IsPrincipal,
        RowVersion = source.RowVersion.ToArray()
    };
}

internal sealed class FakeBeneficiaryRepository : IBeneficiaryRepository
{
    public List<Beneficiary> Beneficiaries { get; } = new();

    public Task<List<Beneficiary>> GetAllByOwnerAsync(string userId) =>
        Task.FromResult(Beneficiaries.Where(b => b.UserOwnerId == userId).ToList());
    public Task<bool> ExistsForOwnerAndAccountAsync(string userId, string accountNumber) =>
        Task.FromResult(Beneficiaries.Any(b => b.UserOwnerId == userId && b.AccountNumberBeneficiary == accountNumber));
    public Task<Beneficiary?> GetByIdAndOwnerAsync(int id, string userId) =>
        Task.FromResult(Beneficiaries.FirstOrDefault(b => b.Id == id && b.UserOwnerId == userId));
    public Task<Beneficiary> AddAsync(Beneficiary entity) { Beneficiaries.Add(entity); return Task.FromResult(entity); }
    public Task UpdateAsync(Beneficiary entity, int id) => Task.CompletedTask;
    public Task DeleteAsync(Beneficiary entity) { Beneficiaries.Remove(entity); return Task.CompletedTask; }
    public Task<List<Beneficiary>> GetAllAsync() => Task.FromResult(Beneficiaries.ToList());
    public Task<List<Beneficiary>> GetAllWithIncludeAsync(List<string> properties) => GetAllAsync();
    public Task<Beneficiary?> GetByIdAsync(int id) => Task.FromResult(Beneficiaries.FirstOrDefault(b => b.Id == id));
}

internal sealed class FakeUserService : IUserService
{
    private readonly string _id;
    private readonly bool _isActive;

    public FakeUserService(string id, bool isActive)
    {
        _id = id;
        _isActive = isActive;
    }

    public Task<SaveUserViewModel> GetByIdAsync(string id) => Task.FromResult(new SaveUserViewModel
    {
        Id = id,
        UserName = "clientuser",
        Email = "client@example.test",
        FirstName = "Client",
        LastName = "User",
        IdNumber = "1",
        UserType = Roles.Client,
        IsActive = id == _id && _isActive
    });

    public UpdateUserResponse UpdateResponse { get; set; } = new();

    public Task<UpdateUserResponse> ActiveUser(string id) => Task.FromResult(new UpdateUserResponse());
    public Task<List<UserViewModel>> GetAllAsync() => Task.FromResult(new List<UserViewModel>());
    public Task<UpdateUserResponse> InactiveUser(string id) => Task.FromResult(new UpdateUserResponse());
    public Task<AuthenticationResponse> LoginAsync(LoginViewModel vm) => throw new NotSupportedException();
    public Task<RegisterResponse> RegisterAsync(SaveUserViewModel vm, string origin) => throw new NotSupportedException();
    public Task SignOutAsync() => Task.CompletedTask;
    public Task<UpdateUserResponse> UpdateAsync(SaveUserViewModel vm, string id) => Task.FromResult(UpdateResponse);
}

