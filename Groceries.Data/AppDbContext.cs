namespace Groceries.Data;

using Humanizer;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Item> Items => Set<Item>();
    public DbSet<ItemPurchase> ItemPurchases => Set<ItemPurchase>();
    public DbSet<ItemTagQuantity> ItemTagQuantities => Set<ItemTagQuantity>();
    public DbSet<List> Lists => Set<List>();
    public DbSet<Retailer> Retailers => Set<Retailer>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<TransactionTotal> TransactionTotals => Set<TransactionTotal>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Item>(entity =>
        {
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql();
        });

        modelBuilder.Entity<ItemBarcode>(entity =>
        {
            entity.ToTable("item_barcodes");
            entity.HasKey(e => new { e.ItemId, e.BarcodeData });

            entity.Property(e => e.Format)
                .HasDefaultValueSql();
        });

        modelBuilder.Entity<ItemPurchase>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("item_purchases");
        });

        modelBuilder.Entity<ItemTagQuantity>(entity =>
        {
            entity.HasNoKey();
        });

        modelBuilder.Entity<List>(entity =>
        {
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql();

            entity.OwnsMany(entity => entity.Items, entity =>
            {
                entity.ToTable("list_items");
            });
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql();

            entity.OwnsMany(e => e.Items, entity =>
            {
                entity.ToTable("transaction_items");

                entity.HasKey(e => new { e.TransactionId, e.ItemId });

                entity.Property(e => e.Price)
                    .HasPrecision(5, 2);
            });
        });

        modelBuilder.Entity<TransactionPromotion>(entity =>
        {
            entity.ToTable("transaction_promotions");

            entity.Property(e => e.Amount)
                .HasPrecision(5, 2);

            entity.HasMany(e => e.Items)
                .WithMany(e => e.TransactionPromotions)
                .UsingEntity<TransactionPromotionItem>()
                .ToTable("transaction_promotion_items");
        });

        modelBuilder.Entity<TransactionTotal>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("transaction_totals");
        });

        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            var idProperty = entity.FindProperty("Id");
            if (idProperty != null)
            {
                idProperty.SetColumnName($"{entity.ClrType.Name.Underscore().ToLowerInvariant()}_id");
                idProperty.SetDefaultValueSql(string.Empty);
            }
        }
    }
}
