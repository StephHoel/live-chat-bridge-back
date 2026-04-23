using LCB.Domain.Entities;

namespace LCB.Domain.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<bool> CreateAsync(IEnumerable<User> users);
}