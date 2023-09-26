using Flight_Detection.Entity.Models;
using Microsoft.EntityFrameworkCore;

namespace Flight_Detection.DataAccess
{
    public class AppDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //edit connection string to connect your server
            optionsBuilder.UseNpgsql(
                "server=localhost;port=5432;database=zekju;user id=postgres;password=123456;");
        }

        public DbSet<Route> Routes { get; set; }
        public DbSet<Flight> Flights { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Subscription>()
                .HasNoKey()
                .HasIndex(subscription => subscription.AgencyId);

            modelBuilder.Entity<Route>()
                .HasKey(c => c.RouteId);
            modelBuilder.Entity<Route>()
                .HasIndex(route => new { route.OriginCityId, route.DestinationCityId });

            modelBuilder.Entity<Route>()
                .HasMany(p => p.Flights)
                .WithOne(b => b.Route)
                .HasForeignKey(p => p.RouteId);

            modelBuilder.Entity<Flight>().HasKey(c => c.FlightId);
            modelBuilder.Entity<Flight>().HasIndex(c => c.RouteId);
        }
    }
}
