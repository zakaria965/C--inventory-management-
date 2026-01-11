using Microsoft.EntityFrameworkCore;
using InventoryManagementSystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace InventoryManagementSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Purchase> Purchases { get; set; }
        public DbSet<Outgoing> Outgoings { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Salary> Salaries { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<MissionVision> MissionVisions { get; set; }
        public DbSet<TeamProfile> TeamProfiles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Product
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.SKU).IsUnique();
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.CostPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.SellingPrice).HasColumnType("decimal(18,2)");
            });

            // Configure Purchase
            modelBuilder.Entity<Purchase>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Product)
                    .WithMany(p => p.Purchases)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Outgoing
            modelBuilder.Entity<Outgoing>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Product)
                    .WithMany(p => p.Outgoings)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Order)
                    .WithMany()
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure Order
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.OrderNumber).IsUnique();
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
            });

            // Configure OrderItem
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Order)
                    .WithMany(o => o.OrderItems)
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Product)
                    .WithMany(p => p.OrderItems)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Employee
            modelBuilder.Entity<Employee>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Salary).HasColumnType("decimal(18,2)");
            });

                // Configure Salary
                modelBuilder.Entity<Salary>(entity =>
                {
                    entity.HasKey(e => e.Id);
                    entity.HasOne(e => e.Employee)
                        .WithMany()
                        .HasForeignKey(e => e.EmployeeId)
                        .OnDelete(DeleteBehavior.Restrict);
                    entity.Property(e => e.SalaryAmount).HasColumnType("decimal(18,2)");
                    entity.Property(e => e.AdvanceAmount).HasColumnType("decimal(18,2)");
                    entity.Property(e => e.NetSalary).HasColumnType("decimal(18,2)");
                    entity.Property(e => e.HoursWorked).HasColumnType("decimal(18,2)");
                });

            // Configure Message
            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(e => e.Id);
            });

            // Configure Supplier
            modelBuilder.Entity<Supplier>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name).IsUnique();
            });
        }
    }
}

