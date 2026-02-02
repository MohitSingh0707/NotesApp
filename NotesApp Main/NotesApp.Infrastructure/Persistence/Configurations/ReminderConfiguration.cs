using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotesApp.Domain.Entities;

namespace NotesApp.Infrastructure.Persistence.Configurations
{
    public class ReminderConfiguration : IEntityTypeConfiguration<Reminder>
    {
        public void Configure(EntityTypeBuilder<Reminder> builder)
        {
            builder.HasKey(r => r.Id);

            builder.Property(r => r.Title)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.HasIndex(r => new { r.UserId, r.RemindAt });
        }
    }
}
