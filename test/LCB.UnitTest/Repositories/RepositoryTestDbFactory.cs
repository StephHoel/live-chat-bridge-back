using System;
using LCB.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LCB.UnitTest.Repositories;

internal static class RepositoryTestDbFactory
{
    internal sealed class DbScope : IDisposable
    {
        public required LcbDbContext Context { get; init; }
        public required SqliteConnection Connection { get; init; }

        public void Dispose()
        {
            Context.Dispose();
            Connection.Dispose();
        }
    }

    public static DbScope CreateContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<LcbDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new LcbDbContext(options);
        context.Database.EnsureCreated();

        return new DbScope
        {
            Context = context,
            Connection = connection
        };
    }
}