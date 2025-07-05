using Microsoft.EntityFrameworkCore;
using ShopSaga.StockService.Repository.Model;

namespace ShopSaga.StockService.Repository
{
    public class StockDbContext : DbContext
    {
        public StockDbContext(DbContextOptions<StockDbContext> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<StockReservation> StockReservations { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
                entity.Property(e => e.QuantityInStock).HasDefaultValue(0);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                // Check constraints
                entity.HasCheckConstraint("CK_Product_Price", "[Price] >= 0");
                entity.HasCheckConstraint("CK_Product_QuantityInStock", "[QuantityInStock] >= 0");
            });

            // Configurazione StockReservation
            modelBuilder.Entity<StockReservation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50).HasDefaultValue("Reserved");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                // Check constraint
                entity.HasCheckConstraint("CK_StockReservation_Quantity", "[Quantity] > 0");

                // Foreign key Product - StockReservation
                entity.HasOne(e => e.Product)
                      .WithMany(p => p.StockReservations)
                      .HasForeignKey(e => e.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}