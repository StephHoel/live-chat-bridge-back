using LCB.Domain.Entities;
using LCB.Domain.Repositories;

namespace LCB.Infrastructure.Repositories;

public class InMemoryUserRepository : IUserRepository
{
    private readonly List<User> _users = [];

    public Task Add(User user)
    {
        _users.Add(user);
        return Task.CompletedTask;
    }

    public Task<User?> GetByEmail(string email)
    {
        Add(User.Create("teste@teste.com","1234"));
        return Task.FromResult(_users.FirstOrDefault(x => x.Email == email));
    }
}