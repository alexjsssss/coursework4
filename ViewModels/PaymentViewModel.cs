
namespace CarWashSystem.ViewModels
{
    public class PaymentViewModel
    {
        public int OrderId { get; set; }

        public string ClientName { get; set; } = "";

        public string WashingBayName { get; set; } = "";

        public decimal TotalAmount { get; set; }

        public int TotalDuration { get; set; }

        public string Status { get; set; } = "";

        public DateTime? CreatedAt { get; set; }

        public List<PaymentServiceViewModel> Services
        { get; set; } = new();
    }

    public class PaymentServiceViewModel
    {
        public string Name { get; set; } = "";

        public int DurationMinutes { get; set; }

        public decimal Price { get; set; }
    }
}


