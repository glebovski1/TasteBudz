using System.Text.Json;
using Microsoft.Data.SqlClient;
using TasteBudz.Backend.Infrastructure.Persistence;

// This probe is intentionally small: deployment scripts call it after SQL
// scripts run, and the only contract is a single JSON object on stdout.
var connectionString = Environment.GetEnvironmentVariable("TASTEBUDZ_CONN");

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        ready = false,
        error = "Environment variable TASTEBUDZ_CONN is required."
    }));
    return;
}

var requirements = EfCoreSchemaRequirements.GetRequiredTableColumns();
var missing = new List<string>();
var schemaVersions = new List<string>();

await using var connection = new SqlConnection(connectionString);
await connection.OpenAsync();

// Compare every required table and column against INFORMATION_SCHEMA so the
// app can fail deployment before runtime requests hit missing schema objects.
foreach (var entry in requirements)
{
    var tableName = entry.Key;

    await using var tableCommand = connection.CreateCommand();
    tableCommand.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @name;";
    tableCommand.Parameters.AddWithValue("@name", tableName);
    var tableCount = Convert.ToInt32(await tableCommand.ExecuteScalarAsync());

    if (tableCount != 1)
    {
        missing.Add($"table:{tableName}");
        continue;
    }

    await using var columnCommand = connection.CreateCommand();
    columnCommand.CommandText = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @name;";
    columnCommand.Parameters.AddWithValue("@name", tableName);

    var actualColumns = new HashSet<string>(StringComparer.Ordinal);
    await using var reader = await columnCommand.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        actualColumns.Add(reader.GetString(0));
    }

    foreach (var requiredColumn in entry.Value)
    {
        if (!actualColumns.Contains(requiredColumn))
        {
            missing.Add($"column:{tableName}.{requiredColumn}");
        }
    }
}

// SchemaVersions is optional for older databases, but reporting recent entries
// makes release logs easier to diagnose when a patch was applied out of order.
await using (var versionCommand = connection.CreateCommand())
{
    versionCommand.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'SchemaVersions';";
    var hasSchemaVersions = Convert.ToInt32(await versionCommand.ExecuteScalarAsync()) == 1;

    if (hasSchemaVersions)
    {
        versionCommand.Parameters.Clear();
        versionCommand.CommandText = "SELECT TOP (20) Version FROM dbo.SchemaVersions ORDER BY AppliedAtUtc DESC;";

        await using var versionReader = await versionCommand.ExecuteReaderAsync();
        while (await versionReader.ReadAsync())
        {
            schemaVersions.Add(versionReader.GetString(0));
        }
    }
}

Console.WriteLine(JsonSerializer.Serialize(new
{
    ready = missing.Count == 0,
    missingCount = missing.Count,
    requiredTableCount = requirements.Count,
    schemaVersionCount = schemaVersions.Count,
    schemaVersions,
    sampleMissing = missing.Take(20).ToArray()
}));
