using DownloadCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DownloadCenter.Infrastructure.Persistence.Configurations;

public class FileRecordConfiguration : IEntityTypeConfiguration<FileRecord>
{
    public void Configure(EntityTypeBuilder<FileRecord> builder)
    {
        builder.Property(f => f.OriginalName).HasMaxLength(256);
        builder.Property(f => f.StoredName).HasMaxLength(100);
        builder.Property(f => f.Extension).HasMaxLength(20);
        builder.Property(f => f.RelativePath).HasMaxLength(256);
    }
}
