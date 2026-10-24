using Microsoft.Data.Sqlite;

namespace MilkApiManager.Tests.Integration;

public class SqliteNativeProviderTests
{
    [Fact]
    public void SqliteNativeProvider_UsesPatchedEngineVersion()
    {
        SQLitePCL.Batteries_V2.Init();

        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT sqlite_version()";
        var sqliteVersion = Version.Parse((string)command.ExecuteScalar()!);

        Assert.True(
            sqliteVersion >= new Version(3, 50, 2),
            $"Expected SQLite 3.50.2 or newer, but native provider reported {sqliteVersion}.");
    }
}
