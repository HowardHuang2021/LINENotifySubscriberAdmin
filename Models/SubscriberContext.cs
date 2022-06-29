using Microsoft.EntityFrameworkCore;

namespace LINENotifySubscriberAdmin.Models
{
    public class SubscriberContext : DbContext
    {
        public SubscriberContext(DbContextOptions options) : base(options) { }

        public DbSet<Subscriber> Subscribers { get; set; }
    }
}
