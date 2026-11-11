
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarWashSystem.Models
{
    [Table("washing_bays")]
    public class WashingBay
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Status { get; set; } = "available";

        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}

