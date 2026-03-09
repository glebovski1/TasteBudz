// Admin-only HTTP endpoint for audit-log queries.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TasteBudz.Backend.Contracts;
using TasteBudz.Backend.Modules.Moderation;

namespace TasteBudz.Backend.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/v1/audit-logs")]
/// <summary>
/// Returns append-only audit entries for admin review.
/// </summary>
public sealed class AuditLogsController(AuditLogService auditLogService) : ControllerBase
{
    [HttpGet]
    public Task<ListResponse<AuditLogEntryDto>> List([FromQuery] AuditLogQuery query, CancellationToken cancellationToken) =>
        auditLogService.ListAsync(query, cancellationToken);
}
