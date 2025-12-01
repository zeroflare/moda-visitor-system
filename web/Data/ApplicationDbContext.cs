using Microsoft.EntityFrameworkCore;
using web.Models;

namespace web.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Counter> Counters { get; set; }
    public DbSet<MeetingRoom> MeetingRooms { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Counter>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.ToTable("counters");
        });

        modelBuilder.Entity<MeetingRoom>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CounterId).HasMaxLength(50).IsRequired();
            entity.ToTable("meetingrooms");
            
            // 建立外鍵關係（可選，如果需要資料庫層級的外鍵約束）
            entity.HasIndex(e => e.CounterId);
        });
    }
}

