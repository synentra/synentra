using Microsoft.EntityFrameworkCore;

namespace Synentra.Persistence.PostgreSql;

public sealed class PostgreSqlSynentraDbContext(DbContextOptions<PostgreSqlSynentraDbContext> options) : SynentraDbContext(options);
