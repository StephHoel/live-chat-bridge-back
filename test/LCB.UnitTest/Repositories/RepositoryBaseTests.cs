using System;
using System.Threading.Tasks;
using LCB.Infrastructure.Repositories.Base;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LCB.UnitTest.Repositories;

public class RepositoryBaseTests
{
    [Fact]
    public async Task ExecuteAsync_CoversSuccessAndErrorBranches()
    {
        var repo = new ProbeRepo(new NullLogger<ProbeRepo>());

        var okResult = await repo.ExecuteProbeAsync(() => Task.FromResult(123), "OkResult");
        var okVoid = await repo.ExecuteVoidProbeAsync(() => Task.CompletedTask, "OkVoid");
        var errorResult = await repo.ExecuteProbeAsync<int>(() => throw new InvalidOperationException("boom"), "ErrorResult");
        await repo.ExecuteVoidProbeAsync(() => throw new InvalidOperationException("boom"), "ErrorVoid");

        Assert.Equal(123, okResult);
        Assert.True(okVoid);
        Assert.Equal(0, errorResult);
    }

    private sealed class ProbeRepo(ILogger logger) : RepositoryBase(logger)
    {
        public Task<TResult> ExecuteProbeAsync<TResult>(Func<Task<TResult>> work, string methodName)
            => ExecuteAsync(work, methodName);

        public async Task<bool> ExecuteVoidProbeAsync(Func<Task> work, string methodName)
        {
            await ExecuteAsync(work, methodName);
            return true;
        }
    }
}