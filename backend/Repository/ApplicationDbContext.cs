using Domain.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Repository;

// IdentityUserContext (not IdentityDbContext) => AspNetUsers + claims/logins/tokens,
// but NO AspNetRoles / AspNetUserRoles tables. Roles are a UserRole enum column on
// ApplicationUser instead.
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityUserContext<ApplicationUser>(options)
{
    public DbSet<Station> Stations { get; set; }
    public DbSet<Pollutant> Pollutants { get; set; }
    public DbSet<Measurement> Measurements { get; set; }
    public DbSet<AlertSubscription> AlertSubscriptions { get; set; }
    public DbSet<AlertNotification> AlertNotifications { get; set; }
    public DbSet<EtlSyncLog> EtlSyncLogs { get; set; }
    public DbSet<InboundEventEntry> InboundEventEntries { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(e =>
        {
            // Store the role enum as a readable string rather than an int.
            e.Property(u => u.Role).HasConversion<string>();
        });

        builder.Entity<Station>(e =>
        {
            e.Property(s => s.Name).IsRequired();
            e.Property(s => s.City).IsRequired();
            e.HasIndex(s => s.ExternalId).IsUnique();
        });

        builder.Entity<Pollutant>(e =>
        {
            e.Property(p => p.Code).IsRequired();
            e.Property(p => p.DisplayName).IsRequired();
            e.Property(p => p.Unit).HasConversion<string>();
            e.HasIndex(p => p.Code).IsUnique();
        });

        builder.Entity<Measurement>(e =>
        {
            e.HasOne(m => m.Station)
                .WithMany(s => s.Measurements)
                .HasForeignKey(m => m.StationId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(m => m.Pollutant)
                .WithMany(p => p.Measurements)
                .HasForeignKey(m => m.PollutantId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(m => new { m.StationId, m.PollutantId, m.MeasuredAtUtc })
                .IsUnique()
                .HasDatabaseName("ux_measurement_station_pollutant_time");
        });

        builder.Entity<AlertSubscription>(e =>
        {
            // Deleting a user removes their subscriptions...
            e.HasOne(a => a.User)
                .WithMany(u => u.AlertSubscriptions)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ...but reference data in use cannot be deleted out from under a subscription.
            e.HasOne(a => a.Station)
                .WithMany(s => s.AlertSubscriptions)
                .HasForeignKey(a => a.StationId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(a => a.Pollutant)
                .WithMany(p => p.AlertSubscriptions)
                .HasForeignKey(a => a.PollutantId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(a => new { a.UserId, a.StationId, a.PollutantId })
                .IsUnique()
                .HasDatabaseName("ux_subscription_user_station_pollutant");

            e.HasIndex(a => new { a.StationId, a.PollutantId, a.IsActive })
                .HasDatabaseName("ix_subscription_eval");
        });

        builder.Entity<AlertNotification>(e =>
        {
            e.Property(n => n.Channel).HasConversion<string>();

            e.HasOne(n => n.AlertSubscription)
                .WithMany(a => a.Notifications)
                .HasForeignKey(n => n.AlertSubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(n => new { n.AlertSubscriptionId, n.SentAtUtc })
                .HasDatabaseName("ix_notification_subscription_time");
        });

        builder.Entity<EtlSyncLog>(e =>
        {
            e.Property(l => l.JobName).IsRequired();
            e.HasIndex(l => new { l.JobName, l.StartedAt })
                .HasDatabaseName("ix_etl_sync_log_job_started");
        });

        builder.Entity<InboundEventEntry>(e =>
        {
            e.Property(i => i.Status).HasConversion<string>();
            e.HasIndex(i => new { i.Status, i.ReceivedAtUtc })
                .HasDatabaseName("ix_inbound_status_received");
        });
    }
}
