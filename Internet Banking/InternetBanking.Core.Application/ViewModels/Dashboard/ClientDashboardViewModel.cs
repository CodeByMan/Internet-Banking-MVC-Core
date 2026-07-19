using InternetBanking.Core.Application.ViewModels.Payment;
using InternetBanking.Core.Application.ViewModels.Product;

namespace InternetBanking.Core.Application.ViewModels.Dashboard
{
    public class ClientDashboardViewModel
    {
        public List<ProductViewModel> Products { get; set; } = new();
        public List<TransactionHistoryItemViewModel> RecentActivity { get; set; } = new();
        public decimal TotalAvailableBalance { get; set; }
        public decimal TotalOutstandingDebt { get; set; }
        public int TotalProducts { get; set; }
        public int SavingsAccounts { get; set; }
        public int CreditCards { get; set; }
        public int Loans { get; set; }
    }
}
