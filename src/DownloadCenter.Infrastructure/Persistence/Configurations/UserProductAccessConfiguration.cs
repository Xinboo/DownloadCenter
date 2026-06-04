using DownloadCenter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DownloadCenter.Infrastructure.Persistence.Configurations;

public class UserProductAccessConfiguration : IEntityTypeConfiguration<UserProductAccess>
{
    public void Configure(EntityTypeBuilder<UserProductAccess> builder)
    {
        builder.HasIndex(x => x.UserId).IsUnique().HasFilter("\"IsDeleted\" = false");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
