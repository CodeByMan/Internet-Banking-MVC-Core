using InternetBanking.Core.Domain.Entities;

namespace InternetBanking.Core.Application.Interfaces.Repositories
{
    public interface IBeneficiaryRepository : IGenericRepository<Beneficiary>
    {
        Task<List<Beneficiary>> GetAllByOwnerAsync(string userId);
        Task<Beneficiary?> GetByIdAndOwnerAsync(int id, string userId);
        Task<bool> ExistsForOwnerAndAccountAsync(string userId, string accountNumber);
    }
}
