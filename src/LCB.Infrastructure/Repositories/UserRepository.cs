using LCB.Domain.Entities;
using LCB.Domain.Interfaces.Repositories;
using LCB.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LCB.Infrastructure.Repositories.Base;

namespace LCB.Infrastructure.Repositories;

public class UserRepository(LcbDbContext context,
                            ILogger<UserRepository> logger)
    : RepositoryBase(logger), IUserRepository
{
    public async Task<UserEntity?> GetByEmailAsync(string email)
        => await ExecuteAsync(() =>
        {
            return context.Users.FirstOrDefaultAsync(x => x.Email == email);
        }, nameof(GetByEmailAsync));

    public async Task<bool> CreateAsync(IEnumerable<UserEntity> users)
        => await ExecuteAsync(async () =>
        {
            await context.Users.AddRangeAsync(users);
            return await context.SaveChangesAsync() > 0;
        }, nameof(CreateAsync));
}
