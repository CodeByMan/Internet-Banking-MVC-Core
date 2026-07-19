using InternetBanking.Core.Application.Interfaces.Repositories;
using InternetBanking.Core.Domain.Entities;
using InternetBanking.Infrastructure.Persistence.Contexts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace InternetBanking.Infrastructure.Persistence.Repositories
{
    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        private readonly ApplicationContext _dbContext;

        public ProductRepository(ApplicationContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }


        public override async Task<Product> AddAsync(Product entity)
        {
            try
            {
                return await base.AddAsync(entity);
            }
            catch (DbUpdateException)
            {
                _dbContext.Entry(entity).State = EntityState.Detached;
                throw;
            }
        }

        public async Task<Product?> GetProductByAccountNumberAsync(string accountNumber)
        {
            return await _dbContext.Products
                .Where(p => p.AccountNumber == accountNumber)
                .FirstOrDefaultAsync();
        }

        public async Task<Product?> GetProductByAccountNumberAndUserIdAsync(string accountNumber, string userId)
        {
            return await _dbContext.Products
                .Where(p => p.AccountNumber == accountNumber && p.UserId == userId)
                .FirstOrDefaultAsync();
        }

        public async Task<Product?> GetPrincipalProductByUserIdAsync(string userId)
        {
            return await _dbContext.Products
                .Where(p => p.UserId == userId && p.IsPrincipal)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Product>> GetAllProductsByUserIdAsync(string userId)
        {
            return await _dbContext.Products
                .Where(p => p.UserId == userId)
                .ToListAsync();
        }

        public bool IsAccountNumberUniqueConstraintViolation(DbUpdateException exception)
        {
            Exception? currentException = exception;
            while (currentException != null)
            {
                if (currentException is SqlException sqlException
                    && (sqlException.Number == 2601 || sqlException.Number == 2627))
                {
                    return true;
                }

                currentException = currentException.InnerException;
            }

            return false;
        }

        public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation)
        {
            if (!_dbContext.Database.IsRelational())
            {
                return await operation();
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var result = await operation();
                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                _dbContext.ChangeTracker.Clear();
                throw;
            }
        }
    }
}
