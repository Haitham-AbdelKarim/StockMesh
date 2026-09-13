using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.StoreId).IsRequired();

        builder.Property(c => c.UserId).IsRequired();

        builder.Property(c => c.Title)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(c => c.LastActiveAt).IsRequired();

        builder.HasIndex(c => new { c.StoreId, c.UserId, c.LastActiveAt })
            .HasDatabaseName("IX_Conversations_Store_User_Active");

        builder.HasOne<Store>()
            .WithMany()
            .HasForeignKey(c => c.StoreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}