
using System.ComponentModel.DataAnnotations;

namespace CarWashSystem.ViewModels
{
    public class CreateOrderViewModel
    {
        [Range(1, int.MaxValue)]
        public int WashingBayId { get; set; }

        [Required]
        [MinLength(1)]
        public List<int> ServiceIds { get; set; } = new();
    }
}
