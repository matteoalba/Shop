using Microsoft.EntityFrameworkCore;
using ShopSaga.PaymentService.Repository.Model;

namespace ShopSaga.PaymentService.Repository
{
    public class PaymentDbContext : DbContext
    {
        public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
        {
        }

        public DbSet<Payment> Payments { get; set; }
        public DbSet<PaymentRefund> PaymentRefunds { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurazione entità Payment
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(p => p.Id);
                
                entity.ToTable("Payments");
                
                entity.Property(p => p.Id)
                    .IsRequired();
                
                entity.Property(p => p.OrderId)
                    .IsRequired();
                
                entity.Property(p => p.Amount)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();
                
                entity.Property(p => p.Status)
                    .HasMaxLength(50)
                    .IsRequired();
                
                entity.Property(p => p.PaymentMethod)
                    .HasMaxLength(50)
                    .IsRequired();
                
                entity.Property(p => p.TransactionId)
                    .HasMaxLength(100)
                    .IsRequired(false);
                
                entity.Property(p => p.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETDATE()");
                
                entity.Property(p => p.UpdatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETDATE()");
            });

            // Configurazione entità PaymentRefund
            modelBuilder.Entity<PaymentRefund>(entity =>
            {
                entity.HasKey(pr => pr.Id);
                
                entity.ToTable("PaymentRefund");
                
                entity.Property(pr => pr.PaymentId)
                    .IsRequired();
                
                entity.Property(pr => pr.Amount)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();
                
                entity.Property(pr => pr.Reason)
                    .HasMaxLength(255)
                    .IsRequired();
                
                entity.Property(pr => pr.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETDATE()");
                
                entity.Property(pr => pr.UpdatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETDATE()");

                // Foreign Key tra PaymentRefund - Payment
                entity.HasOne(pr => pr.Payment)
                    .WithMany()
                    .HasForeignKey(pr => pr.PaymentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}