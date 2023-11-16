using Microsoft.EntityFrameworkCore;

namespace SlopCrew.Server.Database;

public class SlopDbContext : DbContext {
    public DbSet<User> Users { get; set; } = null!;

    public SlopDbContext() { }
    public SlopDbContext(DbContextOptions<SlopDbContext> options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
        if (!optionsBuilder.IsConfigured) optionsBuilder.UseSqlite();
    }
}
