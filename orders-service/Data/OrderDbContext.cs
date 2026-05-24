using Microsoft.EntityFrameworkCore;
using orders_service.Domain;

namespace orders_service.Data;

public class OrderDbContext : DbContext
{
    public DbSet<OrderEvent> Events { get; set; }

    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderEvent>(entity =>
        {
            entity.ToTable("order_events");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderId).IsRequired();
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Data).IsRequired();
            entity.Property(e => e.Timestamp).IsRequired();
        });
    }
}