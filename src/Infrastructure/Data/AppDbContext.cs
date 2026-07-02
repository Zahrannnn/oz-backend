using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Oz.Domain.Entities;

namespace Oz.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<School> Schools => Set<School>();
    public DbSet<GradeStage> GradeStages => Set<GradeStage>();
    public DbSet<ItemType> ItemTypes => Set<ItemType>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<School>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.NameAr).HasMaxLength(200).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(200).IsRequired();
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(x => x.Slug).IsUnique();
        });

        modelBuilder.Entity<GradeStage>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.NameAr).HasMaxLength(100).IsRequired();
            e.Property(x => x.SortOrder).HasDefaultValue(0);
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasOne(x => x.School).WithMany(s => s.GradeStages).HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.SchoolId, x.Name }).IsUnique();
            e.HasIndex(x => x.SchoolId);
        });

        modelBuilder.Entity<ItemType>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.NameAr).HasMaxLength(100).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(100).IsRequired();
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(x => x.Name).IsUnique();
            e.HasIndex(x => x.Slug).IsUnique();
        });

        modelBuilder.Entity<School>().HasData(
            new School { Id = 1, Name = "Cairo Language School", NameAr = "مدرسة القاهرة للغات", Slug = "cairo-language-school", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new School { Id = 2, Name = "Alexandria Experimental", NameAr = "مدرسة الإسكندرية التجريبية", Slug = "alexandria-experimental", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new School { Id = 3, Name = "Giza Arabic School", NameAr = "مدرسة الجيزة العربية", Slug = "giza-arabic-school", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );

        modelBuilder.Entity<GradeStage>().HasData(
            new GradeStage { Id = 1, SchoolId = 1, Name = "KG1", NameAr = "كي جي 1", SortOrder = 1, IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new GradeStage { Id = 2, SchoolId = 1, Name = "KG2", NameAr = "كي جي 2", SortOrder = 2, IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new GradeStage { Id = 3, SchoolId = 1, Name = "KG3", NameAr = "كي جي 3", SortOrder = 3, IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new GradeStage { Id = 4, SchoolId = 1, Name = "FS1", NameAr = "صف اول", SortOrder = 4, IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new GradeStage { Id = 5, SchoolId = 1, Name = "FS2", NameAr = "صف ثاني", SortOrder = 5, IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new GradeStage { Id = 6, SchoolId = 1, Name = "FS3", NameAr = "صف ثالث", SortOrder = 6, IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new GradeStage { Id = 7, SchoolId = 1, Name = "FS4", NameAr = "صف رابع", SortOrder = 7, IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new GradeStage { Id = 8, SchoolId = 1, Name = "FS5", NameAr = "صف خامس", SortOrder = 8, IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new GradeStage { Id = 9, SchoolId = 1, Name = "FS6", NameAr = "صف سادس", SortOrder = 9, IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );

        modelBuilder.Entity<ItemType>().HasData(
            new ItemType { Id = 1, Name = "T-Shirt", NameAr = "تي شيرت", Slug = "t-shirt", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ItemType { Id = 2, Name = "Polo", NameAr = "بولو", Slug = "polo", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ItemType { Id = 3, Name = "Shirt", NameAr = "قميص", Slug = "shirt", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ItemType { Id = 4, Name = "Trousers", NameAr = "بنطلون", Slug = "trousers", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ItemType { Id = 5, Name = "Shorts", NameAr = "شورت", Slug = "shorts", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ItemType { Id = 6, Name = "Skirt", NameAr = "تنورة", Slug = "skirt", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ItemType { Id = 7, Name = "Pinafore", NameAr = "مريلة", Slug = "pinafore", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ItemType { Id = 8, Name = "Sweater", NameAr = "سترة", Slug = "sweater", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ItemType { Id = 9, Name = "Tracksuit", NameAr = "بدلة رياضية", Slug = "tracksuit", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ItemType { Id = 10, Name = "Socks", NameAr = "جوارب", Slug = "socks", IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=OzDev;Trusted_Connection=True;");
        return new AppDbContext(optionsBuilder.Options);
    }
}