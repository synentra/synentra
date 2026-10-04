using Microsoft.EntityFrameworkCore;
using Synentra.Infrastructure.Persistence.Common.Repositories;
using Synentra.Infrastructure.Persistence.PostgreSQL.Contexts;

namespace Synentra.Infrastructure.Persistence.PostgreSQL.Repositories;

public class AgentRepository : BaseAgentRepository<PostgresqlApplicationContext>
{
    public AgentRepository(IDbContextFactory<PostgresqlApplicationContext> appContextFactory)
        : base(appContextFactory)
    {
    }
}
