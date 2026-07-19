using InternetBanking.Core.Domain.Entities;

namespace InternetBanking.Core.Application.Interfaces.Repositories
{
    public interface IPaymentRepository : IGenericRepository<Payment>
    {
        Task<List<Payment>> GetByAccountNumbersAsync(List<string> accountNumbers);
        Task<List<Payment>> GetRecentAsync(int count);
        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation);
    }
}
