using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace TasteBudz.Backend.Infrastructure.Persistence;

public sealed class RelationalPersistenceExceptionClassifier : IPersistenceExceptionClassifier
{
    public bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException switch
        {
            SqliteException { SqliteErrorCode: 19 } => true,
            SqlException { Number: 2601 or 2627 } => true,
            _ => false,
        };
}
