using Microsoft.EntityFrameworkCore;
using Synentra.Infrastructure.Persistence.Common.Repositories;
using Synentra.Infrastructure.Persistence.Sqlite.Contexts;

namespace Synentra.Infrastructure.Persistence.Sqlite.Repositories;

public class AuditRepository : BaseAuditRepository<SqliteApplicationContext>
{
    public AuditRepository(IDbContextFactory<SqliteApplicationContext> appContextFactory)
        : base(appContextFactory)
    {
    }
}