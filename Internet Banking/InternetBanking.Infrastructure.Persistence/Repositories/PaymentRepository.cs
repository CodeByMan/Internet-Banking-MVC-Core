using InternetBanking.Core.Application.Interfaces.Repositories;
using InternetBanking.Core.Domain.Entities;
using InternetBanking.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace InternetBanking.Infrastructure.Persistence.Repositories
{
    public class PaymentRepository : GenericRepository<Payment>, IPaymentRepository
    {
        private readonly ApplicationContext _dbContext;

        public PaymentRepository(ApplicationContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Payment>> GetByAccountNumbersAsync(List<string> accountNumbers)
        {
            return await _dbContext.Payments
                .Where(p => accountNumbers.Contains(p.SourceAccountNumber) || accountNumbers.Contains(p.DestinationAccountNumber))
                .OrderByDescending(p => p.Created)
                .ThenByDescending(p => p.Id)
                .ToListAsync();
        }

        public async Task<List<Payment>> GetRecentAsync(int count)
        {
            return await _dbContext.Payments
                .OrderByDescending(p => p.Created)
                .ThenByDescending(p => p.Id)
                .Take(count)
                .ToListAsync();
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
