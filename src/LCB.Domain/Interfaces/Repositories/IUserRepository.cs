using LCB.Domain.Entities;

namespace LCB.Domain.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmail(string email);
    Task Add(User user);
}