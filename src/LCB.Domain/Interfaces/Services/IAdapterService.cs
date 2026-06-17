using LCB.Domain.DTO;
using LCB.Domain.Entities;

namespace LCB.Domain.Interfaces.Services;

public interface IAdapterService
{
    Task<CommandDTO?> ParseAndDispatch(ChatMessageEntity message);
}