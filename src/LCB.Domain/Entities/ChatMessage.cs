using LCB.Domain.Enums;

namespace LCB.Domain.Entities;

public class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string IdempotencyKey => $"{this.Provider}:{this.Timestamp:yyyyMMddHHmmss}:{this.Id}";
    public ProviderTypeEnum Provider { get; set; }
    public string Author { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public bool Processed { get; set; } = false;
}