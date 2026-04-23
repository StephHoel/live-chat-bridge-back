using LCB.Domain.Entities;
using LCB.Domain.Interfaces.Repositories;
using LCB.Infrastructure.Repositories.Base;
using Microsoft.Extensions.Logging;

namespace LCB.Infrastructure.Repositories;

public class InMemoryUserRepository(ILogger<InMemoryUserRepository> Logger)
    : InMemoryRepositoryBase<User>(Logger), IUserRepository
{
    #region Public Methods

    public Task<bool> CreateAsync(IEnumerable<User> users)
        => Write(Create(users), nameof(CreateAsync));

    public Task<User?> GetByEmailAsync(string email)
        => Read(GetByEmail(email), nameof(GetByEmailAsync));

    #endregion Public Methods

    #region Private Methods

    private static Func<List<User>, bool> Create(IEnumerable<User> users)
        => list => { list.AddRange([.. users]); return true; };

    private static Func<IReadOnlyList<User>, User?> GetByEmail(string email)
        => list =>
            {
                Create([User.Create("teste@teste.com", "1234")]);
                return list.FirstOrDefault(m => m.Email.Equals(email));
            };

    #endregion Private Methods
}