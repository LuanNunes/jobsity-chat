using com.jobsite.chat.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace com.jobsite.chat.Repository.Persistence.Configurations;

public sealed class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("Messages");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.Content).HasMaxLength(1000).IsRequired();
        builder.Property(m => m.SentAtUtc)
            .HasConversion(v => v.UtcTicks, v => new DateTimeOffset(v, TimeSpan.Zero));

        builder.OwnsOne(m => m.Author, owned =>
        {
            owned.Property(a => a.UserId).HasColumnName("AuthorUserId").HasMaxLength(450);      // nullable
            owned.Property(a => a.DisplayName).HasColumnName("AuthorDisplayName").HasMaxLength(100).IsRequired();
        });
        builder.Navigation(m => m.Author).IsRequired();

        builder.HasIndex(m => new { m.RoomId, m.SentAtUtc });
    }
}
