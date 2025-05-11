using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using TechnicalAssignment.BlockchainCollector.Domain.Entities;
using TechnicalAssignment.BlockchainCollector.Infrastructure.MappingConfigurations;

namespace TechnicalAssignment.BlockchainCollector.Infrastructure;

[ExcludeFromCodeCoverage]
public class BlockchainDbContext : DbContext
{
    public BlockchainDbContext(DbContextOptions<BlockchainDbContext> options)
        : base(options)
    {
    }

    public DbSet<Blockchain> Blockchains { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new BlockchainEntityTypeConfiguration());
    }
}