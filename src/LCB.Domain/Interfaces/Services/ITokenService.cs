using LCB.Domain.Entities;

namespace LCB.Domain.Interfaces.Services;

public interface ITokenService
{
    string GenerateToken(UserEntity user);
}