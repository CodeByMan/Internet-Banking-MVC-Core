using InternetBanking.Core.Application.Enums;

namespace InternetBanking.Core.Application.ViewModels.Payment
{
    public class TransactionHistoryItemViewModel
    {
        public int Id { get; set; }
        public string SourceAccountNumber { get; set; } = string.Empty;
        public string DestinationAccountNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public PaymentType PaymentType { get; set; }
        public DateTime? Created { get; set; }
        public string Direction { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
