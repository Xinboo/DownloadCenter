using DownloadCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DownloadCenter.Infrastructure.Persistence.Configurations;

public class ProductFileConfiguration : IEntityTypeConfiguration<ProductFile>
{
    public void Configure(EntityTypeBuilder<ProductFile> builder)
    {
        builder.HasIndex(pf => new { pf.ProductId, pf.FileId }).IsUnique().HasFilter("\"IsDeleted\" = false");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(pf => pf.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<FileRecord>()
            .WithMany()
            .HasForeignKey(pf => pf.FileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(pf => pf.DisplayName).HasMaxLength(200);
        builder.Property(pf => pf.Description).HasMaxLength(1000);
    }
}
