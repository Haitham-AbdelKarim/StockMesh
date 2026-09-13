using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class AgentToolCallLogConfiguration : IEntityTypeConfiguration<AgentToolCallLog>
{
    public void Configure(EntityTypeBuilder<AgentToolCallLog> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.StoreId).IsRequired();

        builder.Property(l => l.UserId).IsRequired();

        builder.Property(l => l.ConversationId).IsRequired();

        builder.Property(l => l.Question)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(l => l.ToolCalls)
            .HasMaxLength(8000)
            .IsRequired();

        builder.Property(l => l.FinalAnswer)
            .HasMaxLength(8000)
            .IsRequired();

        builder.HasIndex(l => new { l.ConversationId, l.CreatedAt })
            .HasDatabaseName("IX_AgentToolCallLogs_Conversation_Created");

        builder.HasOne<Conversation>()
            .WithMany()
            .HasForeignKey(l => l.ConversationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}