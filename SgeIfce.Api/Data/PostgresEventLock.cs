using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace SgeIfce.Api.Data;

internal static class PostgresEventLock
{
    public static async Task<IAsyncDisposable?> AcquireAsync(
        AppDbContext context,
        string eventId)
    {
        if (!string.Equals(
                context.Database.ProviderName,
                "Npgsql.EntityFrameworkCore.PostgreSQL",
                StringComparison.Ordinal))
        {
            return null;
        }

        await context.Database.OpenConnectionAsync();
        var connection = context.Database.GetDbConnection();
        var lockKey = $"sge-event-capacity:{eventId}";

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT pg_advisory_lock(hashtextextended(@lockKey, 0))";
            AddLockParameter(command, lockKey);
            await command.ExecuteScalarAsync();
            return new EventLock(context.Database, connection, lockKey);
        }
        catch
        {
            await context.Database.CloseConnectionAsync();
            throw;
        }
    }

    private static void AddLockParameter(DbCommand command, string lockKey)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = "lockKey";
        parameter.Value = lockKey;
        command.Parameters.Add(parameter);
    }

    private sealed class EventLock : IAsyncDisposable
    {
        private readonly DatabaseFacade _database;
        private readonly DbConnection _connection;
        private readonly string _lockKey;

        public EventLock(DatabaseFacade database, DbConnection connection, string lockKey)
        {
            _database = database;
            _connection = connection;
            _lockKey = lockKey;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await using var command = _connection.CreateCommand();
                command.CommandText = "SELECT pg_advisory_unlock(hashtextextended(@lockKey, 0))";
                AddLockParameter(command, _lockKey);
                await command.ExecuteScalarAsync();
            }
            finally
            {
                await _database.CloseConnectionAsync();
            }
        }
    }
}
