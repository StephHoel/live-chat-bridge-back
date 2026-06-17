using LCB.Domain.Entities;

namespace LCB.Domain.Interfaces.Repositories;

public interface IUserRepository
{
    Task<UserEntity?> GetByEmailAsync(string email);
    Task<bool> CreateAsync(IEnumerable<UserEntity> users);
}