using AutoMapper;
using InternetBanking.Core.Application.Enums;
using InternetBanking.Core.Application.Interfaces.Repositories;
using InternetBanking.Core.Application.Mappings;
using InternetBanking.Core.Application.Services;
using InternetBanking.Core.Application.ViewModels.Product;
using InternetBanking.Core.Domain.Entities;
using InternetBanking.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace InternetBanking.Tests;

public class ProductAccountNumberTests
{
    [Fact]
    public void ProductModel_EnforcesBoundedUniqueAccountNumber()
    {
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new ApplicationContext(options);

        var entityType = context.Model.FindEntityType(typeof(Product))!;
        var property = entityType.FindProperty(nameof(Product.AccountNumber))!;
        var index = entityType.GetIndexes().Single(i =>
            i.Properties.Count == 1 && i.Properties[0].Name == nameof(Product.AccountNumber));

        Assert.Equal(Product.AccountNumberLength, property.GetMaxLength());
        Assert.True(index.IsUnique);
    }

    [Fact]
    public async Task AccountCreation_RetriesWhenDatabaseReportsConcurrentDuplicateKey()
    {
        var repository = new CollisionProductRepository(failuresBeforeSuccess: 1);
        IMapper mapper = new MapperConfiguration(configuration => configuration.AddProfile<GeneralProfile>()).CreateMapper();
        var service = new ProductService(repository, new FakeUserService("user-1", true), mapper);

        var result = await service.Add(new SaveProductViewModel
        {
            UserId = "user-1",
            Amount = 100m,
            ProductType = ProductType.SavingAccount
        });

        Assert.False(result.HasError);
        Assert.Equal(2, repository.AddAttempts);
        Assert.NotNull(result.AccountNumber);
        Assert.Equal(Product.AccountNumberLength, result.AccountNumber!.Length);
        Assert.All(result.AccountNumber, character => Assert.InRange(character, '0', '9'));
    }

    private sealed class CollisionProductRepository : IProductRepository
    {
        private int _failuresRemaining;
        private int _nextId = 1;

        public CollisionProductRepository(int failuresBeforeSuccess)
        {
            _failuresRemaining = failuresBeforeSuccess;
        }

        public int AddAttempts { get; private set; }

        public Task<Product> AddAsync(Product entity)
        {
            AddAttempts++;
            if (_failuresRemaining-- > 0)
            {
                throw new DbUpdateException("Simulated duplicate account-number key.");
            }

            entity.Id = _nextId++;
            return Task.FromResult(entity);
        }

        public bool IsAccountNumberUniqueConstraintViolation(DbUpdateException exception) => true;
        public Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation) => operation();
        public Task<Product?> GetProductByAccountNumberAsync(string accountNumber) => Task.FromResult<Product?>(null);
        public Task<Product?> GetProductByAccountNumberAndUserIdAsync(string accountNumber, string userId) => Task.FromResult<Product?>(null);
        public Task<Product?> GetPrincipalProductByUserIdAsync(string userId) => Task.FromResult<Product?>(null);
        public Task<List<Product>> GetAllProductsByUserIdAsync(string userId) => Task.FromResult(new List<Product>());
        public Task UpdateAsync(Product entity, int id) => Task.CompletedTask;
        public Task DeleteAsync(Product entity) => Task.CompletedTask;
        public Task<List<Product>> GetAllAsync() => Task.FromResult(new List<Product>());
        public Task<List<Product>> GetAllWithIncludeAsync(List<string> properties) => GetAllAsync();
        public Task<Product?> GetByIdAsync(int id) => Task.FromResult<Product?>(null);
    }
}
