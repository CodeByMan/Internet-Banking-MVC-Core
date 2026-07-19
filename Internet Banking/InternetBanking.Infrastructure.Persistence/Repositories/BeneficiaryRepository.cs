using InternetBanking.Core.Application.Interfaces.Repositories;
using InternetBanking.Core.Domain.Entities;
using InternetBanking.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace InternetBanking.Infrastructure.Persistence.Repositories
{
    public class BeneficiaryRepository : GenericRepository<Beneficiary>, IBeneficiaryRepository
    {
        private readonly ApplicationContext _dbContext;

        public BeneficiaryRepository(ApplicationContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Beneficiary>> GetAllByOwnerAsync(string userId)
        {
            return await _dbContext.Beneficiaries
                .Where(b => b.UserOwnerId == userId)
                .OrderBy(b => b.Id)
                .ToListAsync();
        }

        public async Task<Beneficiary?> GetByIdAndOwnerAsync(int id, string userId)
        {
            return await _dbContext.Beneficiaries
                .FirstOrDefaultAsync(b => b.Id == id && b.UserOwnerId == userId);
        }

        public async Task<bool> ExistsForOwnerAndAccountAsync(string userId, string accountNumber)
        {
            return await _dbContext.Beneficiaries
                .AnyAsync(b => b.UserOwnerId == userId && b.AccountNumberBeneficiary == accountNumber);
        }
    }
}
