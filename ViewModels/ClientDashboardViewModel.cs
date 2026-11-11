using CarWashSystem.Models;

namespace CarWashSystem.ViewModels
{
    public class ClientDashboardViewModel
    {
        public string ClientName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public int OrdersCount { get; set; }

        public decimal TotalSpent { get; set; }

        public int AvailableBays { get; set; }

        public int occupiedBays { get; set; }

        public int TotalBays { get; set; }

        public List<WashingBay> WashingBays { get; set; } = new();

        public List<PopularServiceViewModel> PopularServices { get; set; } = new();

        public List<ClientOrderViewModel> RecentOrders { get; set; } = new();
    }

    public class PopularServiceViewModel
    {
        public string Name { get; set; } = string.Empty;

        public int Quantity { get; set; }
    }

    public class ClientOrderViewModel
    {
        public int Id { get; set; }

        public DateTime? CreatedAt { get; set; }

        public string Status { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }
    }
}