
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarWashSystem.Models
{
    [Table("orders")]
    public class Order
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("client_id")]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        public int WashingBayId { get; set; }

        [ForeignKey(nameof(WashingBayId))]
        public WashingBay WashingBay { get; set; } = null!;

        public DateTime? CreatedAt { get; set; }

        [Required]
        public string Status { get; set; } = string.Empty;

        [Column(TypeName = "numeric(10,2)")]
        public decimal TotalAmount { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        public Payment? Payment { get; set; }
    }
}

