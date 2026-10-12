using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Prisma.Api.Features.Clients;

public sealed class ClientEntityConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("clients");
        builder.HasKey(client => client.Id).HasName("pk_clients");

        builder.Property(client => client.Id).ValueGeneratedNever();
        builder.Property(client => client.CompanyName).IsRequired().HasColumnType("text");
        builder.Property(client => client.Segment).IsRequired().HasColumnType("text");
        builder.Property(client => client.ContactName).HasColumnType("text");
        builder.Property(client => client.Email).HasColumnType("text");
        builder.Property(client => client.Phone).HasColumnType("text");
        builder.Property(client => client.Notes).HasColumnType("text");
        builder.Property(client => client.CreatedAtUtc).IsRequired().HasColumnType("timestamp with time zone");
        builder.Property(client => client.UpdatedAtUtc).IsRequired().HasColumnType("timestamp with time zone");

        builder.HasMany(client => client.Sectors)
            .WithOne(sector => sector.Client)
            .HasForeignKey(sector => sector.ClientId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_client_sectors_clients_client_id");
    }
}

public sealed class ClientSectorEntityConfiguration : IEntityTypeConfiguration<ClientSector>
{
    public void Configure(EntityTypeBuilder<ClientSector> builder)
    {
        builder.ToTable("client_sectors");
        builder.HasKey(sector => sector.Id).HasName("pk_client_sectors");
        builder.HasAlternateKey(sector => new { sector.Id, sector.ClientId })
            .HasName("ak_client_sectors_id_client_id");

        builder.Property(sector => sector.Id).ValueGeneratedNever();
        builder.Property(sector => sector.ClientId).IsRequired();
        builder.Property(sector => sector.Name).IsRequired().HasColumnType("text");
        builder.Property(sector => sector.ResponsibleName).HasColumnType("text");
        builder.Property(sector => sector.PeopleCount).IsRequired(false);
        builder.Property(sector => sector.CreatedAtUtc).IsRequired().HasColumnType("timestamp with time zone");
        builder.Property(sector => sector.UpdatedAtUtc).IsRequired().HasColumnType("timestamp with time zone");

        builder.HasOne(sector => sector.Client)
            .WithMany(client => client.Sectors)
            .HasForeignKey(sector => sector.ClientId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_client_sectors_clients_client_id");
    }
}
