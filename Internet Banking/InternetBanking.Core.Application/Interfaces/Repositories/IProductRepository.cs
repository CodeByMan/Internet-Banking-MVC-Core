using InternetBanking.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InternetBanking.Core.Application.Interfaces.Repositories
{
    public interface IProductRepository : IGenericRepository<Product>
    {
        Task<Product?> GetProductByAccountNumberAsync(string accountNumber);
        Task<Product?> GetProductByAccountNumberAndUserIdAsync(string accountNumber, string userId);
        Task<Product?> GetPrincipalProductByUserIdAsync(string userId);
        Task<List<Product>> GetAllProductsByUserIdAsync(string userId);
        bool IsAccountNumberUniqueConstraintViolation(DbUpdateException exception);
        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation);
    }
}
