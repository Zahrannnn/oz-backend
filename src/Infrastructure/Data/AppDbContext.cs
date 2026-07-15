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
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Variant> Variants => Set<Variant>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<PasswordRecovery> PasswordRecoveries => Set<PasswordRecovery>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<PendingAlert> PendingAlerts => Set<PendingAlert>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();
    public DbSet<Exchange> Exchanges => Set<Exchange>();
    public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<School>(e =>
        {
            e.ToTable("school", t => t.HasCheckConstraint("CK_school_type", "[type] BETWEEN 1 AND 6"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("bigint");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired().HasColumnType("nvarchar(200)");
            e.Property(x => x.Type).HasColumnName("type").HasColumnType("tinyint").IsRequired();
            e.Property(x => x.IsArchived).HasColumnName("is_archived").HasDefaultValue(false);
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasIndex(x => x.Name).IsUnique().HasFilter("[is_archived] = 0");
            e.HasIndex(x => x.Type);
        });

        modelBuilder.Entity<GradeStage>(e =>
        {
            e.ToTable("grade_stage");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("bigint");
            e.Property(x => x.SchoolId).HasColumnName("school_id").HasColumnType("bigint");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired().HasColumnType("nvarchar(100)");
            e.Property(x => x.DisplayOrder).HasColumnName("display_order").HasDefaultValue(0);
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasOne(x => x.School).WithMany(s => s.GradeStages).HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.SchoolId, x.Name }).IsUnique();
            e.HasIndex(x => x.SchoolId);
        });

        modelBuilder.Entity<ItemType>(e =>
        {
            e.ToTable("item_type");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("bigint");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired().HasColumnType("nvarchar(100)");
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.ToTable("product", t => t.HasCheckConstraint("CK_product_gender", "[gender] IN (1, 2, 3)"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("bigint");
            e.Property(x => x.SchoolId).HasColumnName("school_id").HasColumnType("bigint");
            e.Property(x => x.GradeStageId).HasColumnName("grade_stage_id").HasColumnType("bigint");
            e.Property(x => x.ItemTypeId).HasColumnName("item_type_id").HasColumnType("bigint");
            e.Property(x => x.Gender).HasColumnName("gender").HasColumnType("tinyint").IsRequired();
            e.Property(x => x.Color).HasColumnName("color").HasMaxLength(100).HasColumnType("nvarchar(100)");
            e.Property(x => x.IsInSet).HasColumnName("is_in_set").HasDefaultValue(false);
            e.Property(x => x.IsArchived).HasColumnName("is_archived").HasDefaultValue(false);
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasOne(x => x.School).WithMany(s => s.Products).HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.GradeStage).WithMany(g => g.Products).HasForeignKey(x => x.GradeStageId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.ItemType).WithMany(i => i.Products).HasForeignKey(x => x.ItemTypeId).OnDelete(DeleteBehavior.NoAction);

            e.HasIndex(x => new { x.SchoolId, x.GradeStageId, x.ItemTypeId, x.Gender }).IsUnique().HasFilter("[is_archived] = 0");
            e.HasIndex(x => new { x.SchoolId, x.GradeStageId });
            e.HasIndex(x => x.ItemTypeId);
            e.HasIndex(x => new { x.SchoolId, x.GradeStageId, x.Gender }).HasFilter("[is_in_set] = 1");
        });

        modelBuilder.Entity<Variant>(e =>
        {
            e.ToTable("variant", t =>
            {
                t.HasCheckConstraint("CK_variant_stock_nonneg", "[stock] >= 0");
                t.HasCheckConstraint("CK_variant_threshold_nonneg", "[low_stock_threshold] >= 0");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("bigint");
            e.Property(x => x.ProductId).HasColumnName("product_id").HasColumnType("bigint");
            e.Property(x => x.SizeLabel).HasColumnName("size_label").HasMaxLength(50).IsRequired().HasColumnType("nvarchar(50)");
            e.Property(x => x.PriceInclVat).HasColumnName("price_incl_vat").HasColumnType("decimal(10,2)").IsRequired();
            e.Property(x => x.Stock).HasColumnName("stock").HasDefaultValue(0);
            e.Property(x => x.Reserved).HasColumnName("reserved").HasDefaultValue(0);
            e.Property(x => x.LowStockThreshold).HasColumnName("low_stock_threshold").HasDefaultValue(5);
            e.Property(x => x.IsArchived).HasColumnName("is_archived").HasDefaultValue(false);
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasOne(x => x.Product).WithMany(p => p.Variants).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.NoAction);

            e.HasIndex(x => new { x.ProductId, x.SizeLabel }).IsUnique().HasFilter("[is_archived] = 0");
            e.HasIndex(x => x.ProductId);
            e.HasIndex(x => new { x.Stock, x.LowStockThreshold }).HasFilter("[is_archived] = 0");
        });

        modelBuilder.Entity<ProductImage>(e =>
        {
            e.ToTable("product_image");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("bigint");
            e.Property(x => x.ProductId).HasColumnName("product_id").HasColumnType("bigint");
            e.Property(x => x.Url).HasColumnName("url").HasMaxLength(500).IsRequired().HasColumnType("nvarchar(500)");
            e.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0);
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasOne(x => x.Product).WithMany(p => p.Images).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.ProductId, x.SortOrder });
        });

        modelBuilder.Entity<Admin>(e =>
        {
            e.ToTable("admin");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(255).IsRequired().HasColumnType("nvarchar(255)");
            e.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(500).IsRequired().HasColumnType("nvarchar(500)");
            e.Property(x => x.PasswordSalt).HasColumnName("password_salt").HasMaxLength(500).IsRequired().HasColumnType("nvarchar(500)");
            e.Property(x => x.FailedAttempts).HasColumnName("failed_attempts").HasDefaultValue(0);
            e.Property(x => x.LockedUntil).HasColumnName("locked_until").HasColumnType("datetime2(3)");
            e.Property(x => x.LastLoginAt).HasColumnName("last_login_at").HasColumnType("datetime2(3)");
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<PasswordRecovery>(e =>
        {
            e.ToTable("password_recovery");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.AdminId).HasColumnName("admin_id");
            e.Property(x => x.CodeHash).HasColumnName("code_hash").HasMaxLength(500).IsRequired().HasColumnType("nvarchar(500)");
            e.Property(x => x.ExpiresAt).HasColumnName("expires_at").HasColumnType("datetime2(3)").IsRequired();
            e.Property(x => x.Used).HasColumnName("used").HasDefaultValue(false);
            e.Property(x => x.Attempts).HasColumnName("attempts").HasDefaultValue(0);
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasOne(x => x.Admin).WithMany().HasForeignKey(x => x.AdminId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.ToTable("audit_log");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("bigint").ValueGeneratedOnAdd();
            e.Property(x => x.ActorId).HasColumnName("actor_id");
            e.Property(x => x.Action).HasColumnName("action").HasMaxLength(100).IsRequired().HasColumnType("nvarchar(100)");
            e.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(100).IsRequired().HasColumnType("nvarchar(100)");
            e.Property(x => x.EntityId).HasColumnName("entity_id").HasMaxLength(50).IsRequired().HasColumnType("nvarchar(50)");
            e.Property(x => x.BeforeJson).HasColumnName("before_json").HasColumnType("nvarchar(max)");
            e.Property(x => x.AfterJson).HasColumnName("after_json").HasColumnType("nvarchar(max)");
            e.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(500).HasColumnType("nvarchar(500)");
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasIndex(x => x.CreatedAt).IsDescending();
            e.HasIndex(x => new { x.ActorId, x.CreatedAt }).IsDescending(false, true);
        });

        modelBuilder.Entity<PendingAlert>(e =>
        {
            e.ToTable("pending_alert");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("bigint").ValueGeneratedOnAdd();
            e.Property(x => x.VariantId).HasColumnName("variant_id").HasColumnType("bigint");
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(255).IsRequired().HasColumnType("nvarchar(255)");
            e.Property(x => x.EmailHash).HasColumnName("email_hash").HasMaxLength(64).IsRequired().HasColumnType("nvarchar(64)");
            e.Property(x => x.Notified).HasColumnName("notified").HasDefaultValue(false);
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.NotifiedAt).HasColumnName("notified_at").HasColumnType("datetime2(3)");

            e.HasOne(x => x.Variant).WithMany().HasForeignKey(x => x.VariantId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => new { x.VariantId, x.EmailHash }).IsUnique().HasFilter("[notified] = 0");
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.ToTable("order", t =>
            {
                t.HasCheckConstraint("CK_order_state", "[state] IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12)");
                t.HasCheckConstraint("CK_order_channel", "[channel] IN (1, 2)");
                t.HasCheckConstraint("CK_order_pickup_address", "[channel] = 2 AND [address_line] IS NULL OR [channel] = 1");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("bigint");
            e.Property(x => x.OrderNumber).HasColumnName("order_number").HasMaxLength(20).IsRequired().HasColumnType("varchar(20)");
            e.Property(x => x.TrackingToken).HasColumnName("tracking_token").HasMaxLength(64).HasColumnType("varchar(64)");
            e.Property(x => x.TrackingTokenHash).HasColumnName("tracking_token_hash").HasColumnType("binary(32)").IsRequired();
            e.Property(x => x.State).HasColumnName("state").HasColumnType("tinyint").IsRequired();
            e.Property(x => x.Channel).HasColumnName("channel").HasColumnType("tinyint").IsRequired();
            e.Property(x => x.CustomerName).HasColumnName("customer_name").HasMaxLength(200).IsRequired().HasColumnType("nvarchar(200)");
            e.Property(x => x.CustomerPhone).HasColumnName("customer_phone").HasMaxLength(20).IsRequired().HasColumnType("varchar(20)");
            e.Property(x => x.CustomerEmail).HasColumnName("customer_email").HasMaxLength(200).IsRequired().HasColumnType("varchar(200)");
            e.Property(x => x.AddressCity).HasColumnName("address_city").HasMaxLength(200).IsRequired().HasColumnType("nvarchar(200)");
            e.Property(x => x.AddressLine).HasColumnName("address_line").HasMaxLength(500).HasColumnType("nvarchar(500)");
            e.Property(x => x.DeliveryFee).HasColumnName("delivery_fee").HasColumnType("decimal(10,2)");
            e.Property(x => x.Total).HasColumnName("total").HasColumnType("decimal(10,2)");
            e.Property(x => x.BostaTrackingId).HasColumnName("bosta_tracking_id").HasMaxLength(100).HasColumnType("varchar(100)");
            e.Property(x => x.StateChangedAt).HasColumnName("state_changed_at").HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.CancelledAt).HasColumnName("cancelled_at").HasColumnType("datetime2(3)");
            e.Property(x => x.DeliveredAt).HasColumnName("delivered_at").HasColumnType("datetime2(3)");
            e.Property(x => x.PickedUpAt).HasColumnName("picked_up_at").HasColumnType("datetime2(3)");
            e.Property(x => x.HandedToCourierAt).HasColumnName("handed_to_courier_at").HasColumnType("datetime2(3)");
            e.Property(x => x.InTransitAt).HasColumnName("in_transit_at").HasColumnType("datetime2(3)");
            e.Property(x => x.ReturnedAt).HasColumnName("returned_at").HasColumnType("datetime2(3)");
            e.Property(x => x.CodFailedAt).HasColumnName("cod_failed_at").HasColumnType("datetime2(3)");

            e.HasIndex(x => x.TrackingTokenHash).IsUnique();
            e.HasIndex(x => x.BostaTrackingId).IsUnique().HasFilter("[bosta_tracking_id] IS NOT NULL");
            e.HasIndex(x => new { x.State, x.StateChangedAt }).HasFilter("[state] IN (1, 2, 8)");
            e.HasIndex(x => x.CustomerPhone);
            e.HasIndex(x => x.CreatedAt);
            e.HasIndex(x => x.State);
        });

        modelBuilder.Entity<OrderItem>(e =>
        {
            e.ToTable("order_item", t => t.HasCheckConstraint("CK_order_item_qty", "[qty] > 0"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("bigint");
            e.Property(x => x.OrderId).HasColumnName("order_id").HasColumnType("bigint");
            e.Property(x => x.VariantId).HasColumnName("variant_id").HasColumnType("bigint");
            e.Property(x => x.Qty).HasColumnName("qty").IsRequired();
            e.Property(x => x.UnitPriceSnapshot).HasColumnName("unit_price_snapshot").HasColumnType("decimal(10,2)").IsRequired();
            e.Property(x => x.LineTotalSnapshot).HasColumnName("line_total_snapshot").HasColumnType("decimal(10,2)").IsRequired();
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasOne(x => x.Order).WithMany(o => o.Items).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.Variant).WithMany().HasForeignKey(x => x.VariantId).OnDelete(DeleteBehavior.NoAction);

            e.HasIndex(x => x.OrderId);
            e.HasIndex(x => x.VariantId);
        });

        modelBuilder.Entity<EmailLog>(e =>
        {
            e.ToTable("email_log");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("bigint");
            e.Property(x => x.OrderId).HasColumnName("order_id").HasColumnType("bigint");
            e.Property(x => x.VariantId).HasColumnName("variant_id").HasColumnType("bigint");
            e.Property(x => x.Recipient).HasColumnName("recipient").HasMaxLength(200).IsRequired().HasColumnType("varchar(200)");
            e.Property(x => x.Template).HasColumnName("template").HasMaxLength(50).IsRequired().HasColumnType("varchar(50)");
            e.Property(x => x.Status).HasColumnName("status").HasColumnType("tinyint").HasDefaultValue(EmailStatus.Pending);
            e.Property(x => x.Error).HasColumnName("error").HasColumnType("nvarchar(max)");
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasIndex(x => new { x.Status, x.CreatedAt });
            e.HasIndex(x => x.OrderId);
        });

        modelBuilder.Entity<Exchange>(e =>
        {
            e.ToTable("exchange");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("bigint");
            e.Property(x => x.OrderId).HasColumnName("order_id").HasColumnType("bigint");
            e.Property(x => x.OrderItemId).HasColumnName("order_item_id").HasColumnType("bigint");
            e.Property(x => x.OldVariantId).HasColumnName("old_variant_id").HasColumnType("bigint");
            e.Property(x => x.NewVariantId).HasColumnName("new_variant_id").HasColumnType("bigint");
            e.Property(x => x.Qty).HasColumnName("qty").IsRequired();
            e.Property(x => x.PriceDelta).HasColumnName("price_delta").HasColumnType("decimal(10,2)").IsRequired();
            e.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(500);
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.OrderItem).WithMany().HasForeignKey(x => x.OrderItemId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.OldVariant).WithMany().HasForeignKey(x => x.OldVariantId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.NewVariant).WithMany().HasForeignKey(x => x.NewVariantId).OnDelete(DeleteBehavior.NoAction);

            e.HasIndex(x => x.OrderId);
        });

        modelBuilder.Entity<IdempotencyKey>(e =>
        {
            e.ToTable("idempotency_key");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").HasColumnType("bigint");
            e.Property(x => x.Key).HasColumnName("key").HasMaxLength(255).IsRequired().HasColumnType("nvarchar(255)");
            e.Property(x => x.RequestHash).HasColumnName("request_hash").HasMaxLength(64).HasColumnType("nvarchar(64)");
            e.Property(x => x.ResponseStatus).HasColumnName("response_status");
            e.Property(x => x.ResponseBody).HasColumnName("response_body").HasColumnType("nvarchar(max)");
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.ExpiresAt).HasColumnName("expires_at").HasColumnType("datetime2(3)").IsRequired();

            e.HasIndex(x => x.Key).IsUnique();
        });

        var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<School>().HasData(
            new School { Id = 1, Name = "Cairo Language School", Type = SchoolType.Language, IsArchived = false, CreatedAt = seedDate, UpdatedAt = seedDate },
            new School { Id = 2, Name = "Alexandria Experimental", Type = SchoolType.Experimental, IsArchived = false, CreatedAt = seedDate, UpdatedAt = seedDate },
            new School { Id = 3, Name = "Giza Arabic School", Type = SchoolType.Arabic, IsArchived = false, CreatedAt = seedDate, UpdatedAt = seedDate }
        );

        modelBuilder.Entity<GradeStage>().HasData(
            new GradeStage { Id = 1, SchoolId = 1, Name = "KG1", DisplayOrder = 1, CreatedAt = seedDate },
            new GradeStage { Id = 2, SchoolId = 1, Name = "KG2", DisplayOrder = 2, CreatedAt = seedDate },
            new GradeStage { Id = 3, SchoolId = 1, Name = "KG3", DisplayOrder = 3, CreatedAt = seedDate },
            new GradeStage { Id = 4, SchoolId = 1, Name = "FS1", DisplayOrder = 4, CreatedAt = seedDate },
            new GradeStage { Id = 5, SchoolId = 1, Name = "FS2", DisplayOrder = 5, CreatedAt = seedDate },
            new GradeStage { Id = 6, SchoolId = 1, Name = "FS3", DisplayOrder = 6, CreatedAt = seedDate },
            new GradeStage { Id = 7, SchoolId = 1, Name = "FS4", DisplayOrder = 7, CreatedAt = seedDate },
            new GradeStage { Id = 8, SchoolId = 1, Name = "FS5", DisplayOrder = 8, CreatedAt = seedDate },
            new GradeStage { Id = 9, SchoolId = 1, Name = "FS6", DisplayOrder = 9, CreatedAt = seedDate }
        );

        modelBuilder.Entity<ItemType>().HasData(
            new ItemType { Id = 1, Name = "T-Shirt", CreatedAt = seedDate },
            new ItemType { Id = 2, Name = "Polo", CreatedAt = seedDate },
            new ItemType { Id = 3, Name = "Shirt", CreatedAt = seedDate },
            new ItemType { Id = 4, Name = "Trousers", CreatedAt = seedDate },
            new ItemType { Id = 5, Name = "Shorts", CreatedAt = seedDate },
            new ItemType { Id = 6, Name = "Skirt", CreatedAt = seedDate },
            new ItemType { Id = 7, Name = "Pinafore", CreatedAt = seedDate },
            new ItemType { Id = 8, Name = "Sweater", CreatedAt = seedDate },
            new ItemType { Id = 9, Name = "Tracksuit", CreatedAt = seedDate },
            new ItemType { Id = 10, Name = "Socks", CreatedAt = seedDate }
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
