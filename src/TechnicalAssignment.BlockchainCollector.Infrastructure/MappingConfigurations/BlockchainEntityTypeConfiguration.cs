using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechnicalAssignment.BlockchainCollector.Domain.Entities;

namespace TechnicalAssignment.BlockchainCollector.Infrastructure.MappingConfigurations;

internal class BlockchainEntityTypeConfiguration : IEntityTypeConfiguration<Blockchain>
{
    public void Configure(EntityTypeBuilder<Blockchain> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.Hash).IsUnique();

        builder.HasIndex(b => new { b.Name, b.CreatedAt });

        builder.Property(b => b.Name).HasMaxLength(32);
        builder.Property(b => b.Hash).HasMaxLength(128);
        builder.Property(b => b.Time).HasMaxLength(40);
        builder.Property(b => b.LastForkHash).HasMaxLength(128);
        builder.Property(b => b.PreviousHash).HasMaxLength(128);
        builder.Property(b => b.LatestUrl).HasMaxLength(200);
        builder.Property(b => b.PreviousUrl).HasMaxLength(200);
    }
}