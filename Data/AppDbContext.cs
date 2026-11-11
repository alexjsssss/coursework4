

using Microsoft.EntityFrameworkCore;
using CarWashSystem.Models;

namespace CarWashSystem.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<WashingBay> WashingBays { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Payment> Payments { get; set; }

        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Назви таблиць
            modelBuilder.Entity<Role>().ToTable("roles");
            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<WashingBay>().ToTable("washing_bays");
            modelBuilder.Entity<Service>().ToTable("services");
            modelBuilder.Entity<Order>().ToTable("orders");
            modelBuilder.Entity<OrderItem>().ToTable("order_items");
            modelBuilder.Entity<Payment>().ToTable("payments");


            // Первинні ключі
            modelBuilder.Entity<Role>()
                .Property(r => r.Id).HasColumnName("id");

            modelBuilder.Entity<User>()
                .Property(u => u.Id).HasColumnName("id");

            modelBuilder.Entity<WashingBay>()
                .Property(w => w.Id).HasColumnName("id");

            modelBuilder.Entity<Service>()
                .Property(s => s.Id).HasColumnName("id");

            modelBuilder.Entity<Order>()
                .Property(o => o.Id).HasColumnName("id");

            modelBuilder.Entity<OrderItem>()
                .Property(i => i.Id).HasColumnName("id");

            modelBuilder.Entity<Payment>()
                .Property(p => p.Id).HasColumnName("id");


            // Roles
            modelBuilder.Entity<Role>()
                .Property(r => r.Name).HasColumnName("name");


            // Users
            modelBuilder.Entity<User>()
                .Property(u => u.FullName).HasColumnName("full_name");

            modelBuilder.Entity<User>()
                .Property(u => u.Email).HasColumnName("email");

            modelBuilder.Entity<User>()
                .Property(u => u.Phone).HasColumnName("phone");

            modelBuilder.Entity<User>()
                .Property(u => u.PasswordHash).HasColumnName("password_hash");

            modelBuilder.Entity<User>()
                .Property(u => u.RoleId).HasColumnName("role_id");

            modelBuilder.Entity<User>()
                .Property(u => u.CreatedAt).HasColumnName("created_at");

            modelBuilder.Entity<User>()
                .Property(u => u.IsActive).HasColumnName("is_active");


            // Washing bays
            modelBuilder.Entity<WashingBay>()
                .Property(w => w.Name).HasColumnName("name");

            modelBuilder.Entity<WashingBay>()
                .Property(w => w.Status).HasColumnName("status");


            // Services
            modelBuilder.Entity<Service>()
                .Property(s => s.Name).HasColumnName("name");

            modelBuilder.Entity<Service>()
                .Property(s => s.Description).HasColumnName("description");

            modelBuilder.Entity<Service>()
                .Property(s => s.Price).HasColumnName("price");

            modelBuilder.Entity<Service>()
                .Property(s => s.IsActive).HasColumnName("is_active");

            modelBuilder.Entity<Service>()
                .Property(s => s.DurationMinutes).HasColumnName("duration_minutes");


            // Orders
            modelBuilder.Entity<Order>()
                .Property(o => o.UserId).HasColumnName("client_id");

            modelBuilder.Entity<Order>()
                .Property(o => o.WashingBayId).HasColumnName("washing_bay_id");

            modelBuilder.Entity<Order>()
                .Property(o => o.Status).HasColumnName("status");

            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount).HasColumnName("total_amount");

            modelBuilder.Entity<Order>()
                .Property(o => o.CreatedAt).HasColumnName("created_at");


            // Order items
            modelBuilder.Entity<OrderItem>()
                .Property(i => i.OrderId).HasColumnName("order_id");

            modelBuilder.Entity<OrderItem>()
                .Property(i => i.ServiceId).HasColumnName("service_id");

            modelBuilder.Entity<OrderItem>()
                .Property(i => i.Quantity).HasColumnName("quantity");

            modelBuilder.Entity<OrderItem>()
                .Property(i => i.UnitPrice).HasColumnName("unit_price");


            // Payments
            modelBuilder.Entity<Payment>()
                .Property(p => p.OrderId).HasColumnName("order_id");

            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount).HasColumnName("amount");

            modelBuilder.Entity<Payment>()
                .Property(p => p.Method).HasColumnName("method");

            modelBuilder.Entity<Payment>()
                .Property(p => p.Status).HasColumnName("status");

            modelBuilder.Entity<Payment>()
                .Property(p => p.PaidAt).HasColumnName("paid_at");


            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("notifications");

                entity.Property(n => n.Id)
                    .HasColumnName("id");

                entity.Property(n => n.UserId)
                    .HasColumnName("user_id");

                entity.Property(n => n.OrderId)
                    .HasColumnName("order_id");

                entity.Property(n => n.Title)
                    .HasColumnName("title");

                entity.Property(n => n.Message)
                    .HasColumnName("message");

                entity.Property(n => n.Type)
                    .HasColumnName("type");

                entity.Property(n => n.IsRead)
                    .HasColumnName("is_read");

                entity.Property(n => n.CreatedAt)
                    .HasColumnName("created_at");
            });
        }
    }
}



