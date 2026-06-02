using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using WebInventory.Domain.Entities;
using WebInventory.Domain.Identity;

namespace WebInventory.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<CustomIdPattern> CustomIdPatterns => Set<CustomIdPattern>();
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<InventoryAccess> InventoryAccesses => Set<InventoryAccess>();
    public DbSet<InventoryField> InventoryFields => Set<InventoryField>();
    public DbSet<InventoryTag> InventoryTags => Set<InventoryTag>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<ItemLike> ItemLikes => Set<ItemLike>();
    public DbSet<ItemTag> ItemTags => Set<ItemTag>();
    public DbSet<ItemVersion> ItemVersions => Set<ItemVersion>();
    public DbSet<Tag> Tags => Set<Tag>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.SalesforceAccountId).HasMaxLength(18);
            entity.Property(e => e.SalesforceContactId).HasMaxLength(18);
            entity.HasIndex(e => e.SalesforceAccountId).IsUnique();
            entity.HasIndex(e => e.SalesforceContactId).IsUnique();
        });

        builder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.NormalizedName).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.NormalizedName).HasMaxLength(200).IsRequired();
        });

        builder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.NormalizedName).IsUnique();
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.NormalizedName).HasMaxLength(100).IsRequired();
            entity.Property<NpgsqlTsVector>("SearchVector")
                .HasColumnType("tsvector")
                .HasComputedColumnSql("to_tsvector('simple', coalesce(\"Name\", ''))", stored: true);
            entity.HasIndex("SearchVector").HasMethod("GIN");
        });

        builder.Entity<Inventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Title);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.DescriptionMarkdown).HasMaxLength(4000);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.RowVersion)
                .HasColumnName("xmin")
                .IsConcurrencyToken()
                .ValueGeneratedOnAddOrUpdate();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
            entity.HasOne(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.CategoryId);
            entity.Property<NpgsqlTsVector>("SearchVector")
                .HasColumnType("tsvector")
                .HasComputedColumnSql("to_tsvector('simple', coalesce(\"Title\", '') || ' ' || coalesce(\"DescriptionMarkdown\", ''))", stored: true);
            entity.HasIndex("SearchVector").HasMethod("GIN");
        });

        builder.Entity<InventoryField>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(400);
            entity.HasIndex(e => new { e.InventoryId, e.FieldType, e.SlotNumber }).IsUnique();
            entity.HasOne(e => e.Inventory)
                .WithMany(i => i.Fields)
                .HasForeignKey(e => e.InventoryId);
        });

        builder.Entity<InventoryAccess>(entity =>
        {
            entity.HasKey(e => new { e.InventoryId, e.UserId });
            entity.HasOne(e => e.Inventory)
                .WithMany(i => i.AccessList)
                .HasForeignKey(e => e.InventoryId);
        });

        builder.Entity<InventoryTag>(entity =>
        {
            entity.HasKey(e => new { e.InventoryId, e.TagId });
            entity.HasOne(e => e.Inventory)
                .WithMany(i => i.InventoryTags)
                .HasForeignKey(e => e.InventoryId);
            entity.HasOne(e => e.Tag)
                .WithMany(t => t.InventoryTags)
                .HasForeignKey(e => e.TagId);
        });

        builder.Entity<Item>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CustomId).HasMaxLength(120).IsRequired();
            entity.Property(e => e.RowVersion)
                .HasColumnName("xmin")
                .IsConcurrencyToken()
                .ValueGeneratedOnAddOrUpdate();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
            entity.HasIndex(e => new { e.InventoryId, e.CustomId }).IsUnique();
            entity.HasIndex(e => e.CreatedByUserId);
            entity.HasOne(e => e.Inventory)
                .WithMany(i => i.Items)
                .HasForeignKey(e => e.InventoryId);
            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.Property<NpgsqlTsVector>("SearchVector")
                .HasColumnType("tsvector")
                .HasComputedColumnSql("to_tsvector('simple', coalesce(\"CustomId\", '') || ' ' || coalesce(\"Text1\", '') || ' ' || coalesce(\"Text2\", '') || ' ' || coalesce(\"Text3\", ''))", stored: true);
            entity.HasIndex("SearchVector").HasMethod("GIN");
        });

        builder.Entity<ItemLike>(entity =>
        {
            entity.HasKey(e => new { e.ItemId, e.UserId });
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.HasOne(e => e.Item)
                .WithMany(i => i.Likes)
                .HasForeignKey(e => e.ItemId);
        });

        builder.Entity<ItemTag>(entity =>
        {
            entity.HasKey(e => new { e.ItemId, e.TagId });
            entity.HasOne(e => e.Item)
                .WithMany(i => i.ItemTags)
                .HasForeignKey(e => e.ItemId);
            entity.HasOne(e => e.Tag)
                .WithMany(t => t.ItemTags)
                .HasForeignKey(e => e.TagId);
        });

        builder.Entity<ItemVersion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ItemId, e.VersionNumber }).IsUnique();
            entity.Property(e => e.CustomId).HasMaxLength(120).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.HasOne(e => e.Item)
                .WithMany(i => i.Versions)
                .HasForeignKey(e => e.ItemId);
        });

        builder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BodyMarkdown).HasMaxLength(4000).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.HasOne(e => e.Inventory)
                .WithMany(i => i.Comments)
                .HasForeignKey(e => e.InventoryId);
        });

        builder.Entity<CustomIdPattern>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Pattern).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.HasIndex(e => new { e.InventoryId, e.Version }).IsUnique();
            entity.HasOne(e => e.Inventory)
                .WithMany(i => i.CustomIdPatterns)
                .HasForeignKey(e => e.InventoryId);
        });
    }
}
