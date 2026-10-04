using Microsoft.EntityFrameworkCore;
using Synentra.Infrastructure.Persistence.Common.Repositories;
using Synentra.Infrastructure.Persistence.PostgreSQL.Contexts;

namespace Synentra.Infrastructure.Persistence.PostgreSQL.Repositories;

public class AgentHistoryRepository : BaseAgentHistoryRepository<PostgresqlApplicationContext>
{
    public AgentHistoryRepository(IDbContextFactory<PostgresqlApplicationContext> appContextFactory)
        : base(appContextFactory)
    {
    }
}
