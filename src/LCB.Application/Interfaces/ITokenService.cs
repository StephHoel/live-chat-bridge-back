using LCB.Domain.Entities;

namespace LCB.Application.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user);
}