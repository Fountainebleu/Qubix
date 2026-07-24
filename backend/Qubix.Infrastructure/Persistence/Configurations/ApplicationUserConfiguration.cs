using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Qubix.Infrastructure.Identity;

namespace Qubix.Infrastructure.Persistence.Configurations;

internal sealed class ApplicationUserConfiguration :
    IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable(
            "AspNetUsers",
            table => table.HasCheckConstraint(
                "CK_AspNetUsers_DisplayName",
                "char_length(btrim(\"DisplayName\")) > 0"));

        builder.Property(user => user.DisplayName)
            .HasMaxLength(ApplicationUser.MaxDisplayNameLength)
            .IsRequired();

        builder.Property(user => user.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(user => user.NormalizedEmail)
            .HasDatabaseName("EmailIndex")
            .IsUnique()
            .HasFilter("\"NormalizedEmail\" IS NOT NULL");
    }
}
