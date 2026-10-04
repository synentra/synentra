using Microsoft.EntityFrameworkCore;
using Synentra.Infrastructure.Persistence.Common.Repositories;
using Synentra.Infrastructure.Persistence.Sqlite.Contexts;

namespace Synentra.Infrastructure.Persistence.Sqlite.Repositories;

public class AgentRepository : BaseAgentRepository<SqliteApplicationContext>
{
    public AgentRepository(IDbContextFactory<SqliteApplicationContext> appContextFactory)
        : base(appContextFactory)
    {
    }
}