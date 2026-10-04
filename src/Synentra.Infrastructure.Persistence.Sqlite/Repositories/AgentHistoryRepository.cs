using Microsoft.EntityFrameworkCore;
using Synentra.Infrastructure.Persistence.Common.Repositories;
using Synentra.Infrastructure.Persistence.Sqlite.Contexts;

namespace Synentra.Infrastructure.Persistence.Sqlite.Repositories;

public class AgentHistoryRepository : BaseAgentHistoryRepository<SqliteApplicationContext>
{
    public AgentHistoryRepository(IDbContextFactory<SqliteApplicationContext> appContextFactory)
        : base(appContextFactory)
    {
    }
}
