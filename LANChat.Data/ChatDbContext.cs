using Microsoft.EntityFrameworkCore;
using LANChat.Core.Models;

namespace LANChat.Data
{
    public class ChatDbContext : DbContext
    {
        public ChatDbContext(DbContextOptions<ChatDbContext> options)
            : base(options)
        {
        }

        public DbSet<Message> Messages { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.SenderId).IsRequired();
                entity.Property(e => e.SenderName).IsRequired();
                entity.Property(e => e.Timestamp).IsRequired();
                entity.Property(e => e.Type).IsRequired();
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.IpAddress).IsRequired();
                entity.Property(e => e.Port).IsRequired();
                entity.Property(e => e.LastSeen).IsRequired();
            });
        }
    }
}