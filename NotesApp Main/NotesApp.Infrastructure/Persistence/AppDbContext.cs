using Microsoft.EntityFrameworkCore;
using NotesApp.Domain.Entities;

namespace NotesApp.Infrastructure.Persistence
{
      public class AppDbContext : DbContext
      {
            public DbSet<User> Users => Set<User>();
            public DbSet<Note> Notes => Set<Note>();
            public DbSet<Reminder> Reminders { get; set; }

            public DbSet<Notification> Notifications { get; set; }
            public DbSet<DeviceToken> DeviceTokens { get; set; }

            public AppDbContext(DbContextOptions<AppDbContext> options)
                : base(options)
            {
            }

            
            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                  base.OnModelCreating(modelBuilder);
                  modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

                  // ================= USER =================
                  modelBuilder.Entity<User>(entity =>
                  {
                        entity.HasKey(x => x.Id);

                        // 🔥 GLOBAL SOFT DELETE FILTER
                        entity.HasQueryFilter(x => !x.IsDeleted);

                        entity.HasIndex(x => x.Email)
                      .IsUnique()
                      .HasFilter("[Email] IS NOT NULL AND [IsDeleted] = 0");

                        entity.HasIndex(x => x.UserName)
                      .IsUnique()
                      .HasFilter("[UserName] IS NOT NULL AND [IsDeleted] = 0");

                        entity.Property(x => x.FirstName).IsRequired(false);
                        entity.Property(x => x.LastName).IsRequired(false);
                        entity.Property(x => x.UserName).IsRequired(false);
                        entity.Property(x => x.Email).IsRequired(false);
                        entity.Property(x => x.PasswordHash).IsRequired(false);

                        entity.Property(x => x.ProfileImagePath).IsRequired();
                        entity.Property(x => x.IsGuest).IsRequired();
                        entity.Property(x => x.IsDeleted).IsRequired();
                        entity.Property(x => x.CreatedAt).IsRequired();
                        entity.Property(x => x.UpdatedAt).IsRequired();
                  });

                  // ================= DEVICE TOKEN =================
                  modelBuilder.Entity<DeviceToken>(entity =>
                  {
                        entity.HasKey(x => x.Id);

                        entity.Property(x => x.Token)
                        .IsRequired();

                        entity.Property(x => x.Platform)
                        .IsRequired();

                        entity.Property(x => x.CreatedAt)
                        .IsRequired();
                  });


                  // ================= NOTE =================
                  modelBuilder.Entity<Note>(entity =>
                  {
                        entity.HasKey(x => x.Id);

                        entity.Property(x => x.Title)
                      .IsRequired()
                      .HasMaxLength(200);

                        entity.Property(x => x.IsPasswordProtected).IsRequired();
                        entity.Property(x => x.IsDeleted).IsRequired();
                        entity.Property(x => x.CreatedAt).IsRequired();
                        entity.Property(x => x.UpdatedAt).IsRequired();

                        entity.Property(x => x.UserId).IsRequired();
                  });

                  // ================= NOTIFICATION =================
                  modelBuilder.Entity<Notification>(entity =>
                  {
                        entity.HasKey(x => x.Id);

                        entity.Property(x => x.Title).IsRequired();
                        entity.Property(x => x.Message).IsRequired();
                        entity.Property(x => x.IsRead).IsRequired();
                        entity.Property(x => x.CreatedAt).IsRequired();
                  });

                  // ================= REMINDER =================
                  modelBuilder.Entity<Reminder>(entity =>
                  {
                        entity.HasKey(x => x.Id);

                        entity.Property(x => x.Title)
                      .IsRequired()
                      .HasMaxLength(200);

                        entity.Property(x => x.RemindAt).IsRequired();
                        entity.Property(x => x.CreatedAt).IsRequired();
                        entity.Property(x => x.UserId).IsRequired();
                        
                        // Default Type to InApp | Email if not set (optional fallback)
                        entity.Property(x => x.Type).IsRequired();
                  });
            }
      }
}
