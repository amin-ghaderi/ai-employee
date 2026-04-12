using AiEmployee.Domain.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiEmployee.Infrastructure.Persistence;

internal sealed class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("SystemSettings");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Key).IsRequired().HasMaxLength(SystemSetting.MaxKeyLength);
        builder.Property(e => e.Value).HasMaxLength(SystemSetting.MaxValueLength);
        builder.Property(e => e.UpdatedAtUtc).IsRequired();

        builder.HasIndex(e => e.Key).IsUnique();
    }
}
