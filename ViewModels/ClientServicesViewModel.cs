
using CarWashSystem.Models;

namespace CarWashSystem.ViewModels
{
    public class ClientServicesViewModel
    {
        public List<Service> Services { get; set; } = new();

        public List<WashingBay> AvailableBays { get; set; } = new();
    }
}


