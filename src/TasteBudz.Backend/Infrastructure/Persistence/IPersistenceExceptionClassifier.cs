using Microsoft.EntityFrameworkCore;

namespace TasteBudz.Backend.Infrastructure.Persistence;

public interface IPersistenceExceptionClassifier
{
    bool IsUniqueConstraintViolation(DbUpdateException exception);
}
