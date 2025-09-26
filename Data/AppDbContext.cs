using Microsoft.EntityFrameworkCore;
using myChatGptTelegramBot.DTO_Models;

public class AppDbContext : DbContext
{
    public DbSet<Purchase> Purchases { get; set; }
    public DbSet<TaskItem> Tasks { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Для SQLite (файл в корне проекта)
        optionsBuilder.UseSqlite("Data Source=app.db");

        // Для PostgreSQL (раскомментируйте, если нужно):
        // optionsBuilder.UseNpgsql("Host=localhost;Database=mydb;Username=postgres;Password=secret");

        // Для SQL Server:
        // optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=MyAppDb;Trusted_Connection=true;");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Опционально: настройка моделей
        modelBuilder.Entity<Purchase>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Purpose).HasMaxLength(50);
        });

        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.ReminderFrequency).HasMaxLength(50);
        });
    }
}