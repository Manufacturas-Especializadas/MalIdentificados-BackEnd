using Microsoft.EntityFrameworkCore;
using Application.Common.Interfaces;
using Domain.Entities;

namespace Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Lines> Lines => Set<Lines>();

        public DbSet<Client> Clients => Set<Client>();

        public DbSet<PartNumber> PartNumbers => Set<PartNumber>();

        public DbSet<ContainerValidation> ContainerValidations => Set<ContainerValidation>();

        public DbSet<ScanDetail> ScanDetails => Set<ScanDetail>();

        public DbSet<QualityApprover> QualityApprovers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Lines>(entity =>
            {
                entity.ToTable("Lines");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.LineName).HasColumnName("lineName").HasMaxLength(50).IsRequired();
                entity.Property(e => e.IsActive).HasColumnName("isActive");
                entity.Property(e => e.CreatedAt).HasColumnName("createdAt");
            });

            modelBuilder.Entity<Client>(entity =>
            {
                entity.ToTable("Clients");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ClientName).HasColumnName("clientName").HasMaxLength(100).IsRequired();
                entity.Property(e => e.IsActive).HasColumnName("isActive");
                entity.Property(e => e.CreatedAt).HasColumnName("createdAt");
            });

            modelBuilder.Entity<PartNumber>(entity =>
            {
                entity.ToTable("PartNumbers");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PartNumbersCode).HasColumnName("partNumbers").IsRequired();
                entity.Property(e => e.IdClient).HasColumnName("idClient");
                entity.Property(e => e.IdLine).HasColumnName("idLine");
                entity.Property(e => e.DefaultStandardPack).HasColumnName("defaultStandardPack").IsRequired(false);
                entity.Property(e => e.IsActive).HasColumnName("isActive");
                entity.Property(e => e.CreatedAt).HasColumnName("createdAt");

                entity.HasOne(d => d.Client)
                    .WithMany(p => p.PartNumbers)
                    .HasForeignKey(d => d.IdClient)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Line)
                    .WithMany(p => p.PartNumbers)
                    .HasForeignKey(d => d.IdLine)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ContainerValidation>(entity =>
            {
                entity.ToTable("ContainerValidations");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ContainerNumber).HasColumnName("containerNumber").IsRequired();
                entity.Property(e => e.PayrollNumber).HasColumnName("payrollNumber");
                entity.Property(e => e.ExpectedPartCode).HasColumnName("expectedPartCode");
                entity.Property(e => e.IdPartNumber).HasColumnName("idPartNumber");
                entity.Property(e => e.RequiredQuantity).HasColumnName("requiredQuantity");
                entity.Property(e => e.ScannedQuantity).HasColumnName("scannedQuantity");
                entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20);

                entity.HasOne(d => d.PartNumber)
                    .WithMany(p => p.ContainerValidations)
                    .HasForeignKey(d => d.IdPartNumber)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ScanDetail>(entity =>
            {
                entity.ToTable("ScanDetails");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.IdValidation).HasColumnName("idValidation");
                entity.Property(e => e.ScannedPartCode).HasColumnName("scannedPartCode").HasMaxLength(100).IsRequired();
                entity.Property(e => e.IsCorrect).HasColumnName("isCorrect");
                entity.Property(e => e.ScanDate).HasColumnName("scanDate");

                entity.HasOne(d => d.ContainerValidation)
                    .WithMany(p => p.ScanDetails)
                    .HasForeignKey(d => d.IdValidation)
                    .OnDelete(DeleteBehavior.Cascade); 
            });
        }
    }
}