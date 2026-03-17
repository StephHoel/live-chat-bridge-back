using LCB.Domain.Entities;
using LCB.Domain.Repositories;

namespace LCB.Infrastructure.Repositories;

public class InMemoryUserRepository : IUserRepository
{
    private readonly List<User> _users = new();

    public Task Add(User user)
    {
        _users.Add(user);
        return Task.CompletedTask;
    }

    public Task<User?> GetByEmail(string email)
    {
        return Task.FromResult(_users.FirstOrDefault(x => x.Email == email));
    }
}