using Microsoft.EntityFrameworkCore;

namespace Synentra.Persistence.Sqlite;

public sealed class SqliteSynentraDbContext(DbContextOptions<SqliteSynentraDbContext> options) : SynentraDbContext(options);
