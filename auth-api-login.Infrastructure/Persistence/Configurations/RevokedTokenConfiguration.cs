namespace auth_api_login.Infrastructure.Persistence.Configurations;

public class RevokedTokenConfiguration : IEntityTypeConfiguration<RevokedToken>
{
    public void Configure(EntityTypeBuilder<RevokedToken> builder)
    {
        builder.ToTable("revoked_tokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Jti)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(t => t.Jti)
            .IsUnique();
    }
}
