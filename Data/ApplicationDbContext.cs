using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext {
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<User>().ToTable("Users");

        var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<User>().HasData(
            new User { Id = 1, Name = "Wes", Email = "wes@example.com", Department = "Engineering", CreatedAt = seedDate },
            new User { Id = 2, Name = "John", Email = "john@example.com", Department = "Marketing", CreatedAt = seedDate },
            new User { Id = 3, Name = "Jane", Email = "jane@example.com", Department = "HR", CreatedAt = seedDate}
        );
    }
}