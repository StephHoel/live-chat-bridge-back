using LCB.Domain.Entities;
using LCB.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace LCB.Infrastructure.Repositories;

public class InMemoryUserRepository(ILogger<InMemoryUserRepository> logger) : IUserRepository
{
    private readonly List<User> _users = [];

    public async Task Add(User user)
    {
        try
        {
            logger.LogInformation("[{method}] Starting...", [nameof(Add)]);

            _users.Add(user);

            return;
        }
        catch
        {
            logger.LogError("[{method}] Error on register", [nameof(Add)]);
            return;
        }
        finally
        {
            logger.LogInformation("[{method}] Finishing...", [nameof(Add)]);
        }
    }

    public async Task<User?> GetByEmail(string email)
    {
        try
        {
            logger.LogInformation("[{method}] Starting...", [nameof(GetByEmail)]);

            await Add(User.Create("teste@teste.com", "1234"));

            return _users.FirstOrDefault(x => x.Email == email);
        }
        catch
        {
            logger.LogError("[{method}] Error on get user", [nameof(GetByEmail)]);
            return null;
        }
        finally
        {
            logger.LogInformation("[{method}] Finishing...", [nameof(GetByEmail)]);
        }
    }
}