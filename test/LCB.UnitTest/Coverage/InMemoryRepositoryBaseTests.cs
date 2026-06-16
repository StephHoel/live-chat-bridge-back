using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LCB.Infrastructure.Repositories.Base;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LCB.UnitTest.Coverage;

public class InMemoryRepositoryBaseTests
{
    [Fact]
    public async Task ReadAndWrite_CoverSuccessAndErrorBranches()
    {
        var repo = new ProbeRepo(new NullLogger<ProbeRepo>());

        var readOk = await repo.ReadProbe<int>(items => items.Count, "ReadOk");
        var writeOk = await repo.WriteProbe<bool>(items =>
        {
            items.Add(1);
            return true;
        }, "WriteOk");

        var readError = await repo.ReadProbe<int>(_ => throw new InvalidOperationException("boom"), "ReadError");
        var writeError = await repo.WriteProbe<bool>(_ => throw new InvalidOperationException("boom"), "WriteError");

        Assert.Equal(0, readOk);
        Assert.True(writeOk);
        Assert.Equal(0, readError);
        Assert.False(writeError);
    }

    private sealed class ProbeRepo(ILogger logger) : InMemoryRepositoryBase<int>(logger)
    {
        public Task<TResult> ReadProbe<TResult>(Func<IReadOnlyList<int>, TResult> work, string method)
            => Read(work, method);

        public Task<TResult> WriteProbe<TResult>(Func<List<int>, TResult> work, string method)
            => Write(work, method);
    }
}
