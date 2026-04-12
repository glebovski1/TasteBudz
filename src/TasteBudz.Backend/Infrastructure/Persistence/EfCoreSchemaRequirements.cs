using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using TasteBudz.Backend.Infrastructure.Persistence.Sqlite;

namespace TasteBudz.Backend.Infrastructure.Persistence;

public static class EfCoreSchemaRequirements
{
    private static readonly Lazy<IReadOnlyDictionary<string, string[]>> RequiredTableColumns = new(BuildRequiredTableColumns);

    public static IReadOnlyDictionary<string, string[]> GetRequiredTableColumns() => RequiredTableColumns.Value;

    private static IReadOnlyDictionary<string, string[]> BuildRequiredTableColumns()
    {
        var options = new DbContextOptionsBuilder<TasteBudzDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var dbContext = new TasteBudzDbContext(options);
        var tableColumns = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);

        foreach (var entityType in dbContext.Model.GetEntityTypes())
        {
            var tableName = entityType.GetTableName();

            if (string.IsNullOrWhiteSpace(tableName))
            {
                continue;
            }

            if (!tableColumns.TryGetValue(tableName, out var columns))
            {
                columns = new SortedSet<string>(StringComparer.Ordinal);
                tableColumns[tableName] = columns;
            }

            var storeObject = StoreObjectIdentifier.Table(tableName, entityType.GetSchema());

            foreach (var property in entityType.GetProperties())
            {
                var columnName = property.GetColumnName(storeObject);

                if (!string.IsNullOrWhiteSpace(columnName))
                {
                    columns.Add(columnName);
                }
            }
        }

        return tableColumns
            .OrderBy(item => item.Key, StringComparer.Ordinal)
            .ToDictionary(
                item => item.Key,
                item => item.Value.ToArray(),
                StringComparer.Ordinal);
    }
}
