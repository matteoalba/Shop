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

                entity.HasCheckConstraint("CK_Product_Price", "[Price] >= 0");
                entity.HasCheckConstraint("CK_Product_QuantityInStock", "[QuantityInStock] >= 0");
                
                // Indici per performance
                entity.HasIndex(e => e.Name).HasDatabaseName("IX_Products_Name");
                entity.HasIndex(e => e.Price).HasDatabaseName("IX_Products_Price");
            });

            // Configurazione StockReservation
            modelBuilder.Entity<StockReservation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasDefaultValueSql("NEWID()");
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50).HasDefaultValue("Reserved");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasCheckConstraint("CK_StockReservation_Quantity", "[Quantity] > 0");

                // Foreign key Product - StockReservation
                entity.HasOne(e => e.Product)
                      .WithMany(p => p.StockReservations)
                      .HasForeignKey(e => e.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);
                      
                // Indici per performance
                entity.HasIndex(e => e.OrderId).HasDatabaseName("IX_StockReservations_OrderId");
                entity.HasIndex(e => e.ProductId).HasDatabaseName("IX_StockReservations_ProductId");
                entity.HasIndex(e => e.Status).HasDatabaseName("IX_StockReservations_Status");
            });

            // Seeding dati iniziali per i prodotti
            SeedInitialData(modelBuilder);
        }

        private static void SeedInitialData(ModelBuilder modelBuilder)
        {
            var products = new[]
            {
                new Product
                {
                    Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440001"),
                    Name = "Laptop Gaming MSI GF63",
                    Description = "Laptop da gaming",
                    Price = 1299.99m,
                    QuantityInStock = 25,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440002"),
                    Name = "Mouse Wireless Logitech MX Master 3",
                    Description = "Mouse wireless ergonomico",
                    Price = 89.99m,
                    QuantityInStock = 150,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440003"),
                    Name = "Tastiera Meccanica Corsair K95",
                    Description = "Tastiera meccanica con retroilluminazione RGB",
                    Price = 159.99m,
                    QuantityInStock = 75,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440004"),
                    Name = "Monitor 4K Dell UltraSharp 27\"",
                    Description = "Monitor 4K con risoluzione UltraSharp",
                    Price = 449.99m,
                    QuantityInStock = 30,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440005"),
                    Name = "Smartphone Samsung Galaxy S24",
                    Description = "Smartphone con display AMOLED e fotocamera da 108MP",
                    Price = 799.99m,
                    QuantityInStock = 50,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440006"),
                    Name = "Cuffie Gaming SteelSeries Arctis 7",
                    Description = "Cuffie gaming wireless con audio surround",
                    Price = 129.99m,
                    QuantityInStock = 80,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440007"),
                    Name = "Webcam HD Logitech C920",
                    Description = "Webcam HD con risoluzione 1080p",
                    Price = 69.99m,
                    QuantityInStock = 120,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Product
                {
                    Id = Guid.Parse("550e8400-e29b-41d4-a716-446655440008"),
                    Name = "SSD NVMe Samsung 970 EVO 1TB",
                    Description = "SSD NVMe ad alte prestazioni con capacità di 1TB",
                    Price = 199.99m,
                    QuantityInStock = 40,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            };

            modelBuilder.Entity<Product>().HasData(products);
        }
    }
}