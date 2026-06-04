using DownloadCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DownloadCenter.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.UserName).HasMaxLength(50);
        builder.HasIndex(u => u.UserName).IsUnique().HasFilter("\"IsDeleted\" = false");

        builder.Property(u => u.PasswordHash).HasMaxLength(200);
        builder.Property(u => u.NickName).HasMaxLength(50);
        builder.Property(u => u.Company).HasMaxLength(100);
        builder.Property(u => u.Phone).HasMaxLength(20);
    }
}
