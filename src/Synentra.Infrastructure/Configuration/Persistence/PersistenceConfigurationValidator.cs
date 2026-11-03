using Microsoft.Extensions.Options;

namespace Synentra.Infrastructure.Configuration.Persistence;

public sealed class PersistenceConfigurationValidator
    : IValidateOptions<PersistenceConfiguration>
{
    private const string SqliteProvider = "Sqlite";
    private const string PostgresProvider = "Postgres";

    public ValidateOptionsResult Validate(
        string? name,
        PersistenceConfiguration options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Provider))
        {
            errors.Add("Persistence:Provider is required.");
        }
        else if (!IsSupportedProvider(options.Provider))
        {
            errors.Add(
                "Persistence:Provider must be " +
                "'Sqlite' or 'Postgres'.");
        }

        if (options.Providers is null)
        {
            errors.Add("Persistence:Providers is required.");
        }
        else if (options.Provider.Equals(
                     SqliteProvider,
                     StringComparison.OrdinalIgnoreCase))
        {
            ValidateSqlite(options.Providers.Sqlite, errors);
        }
        else if (options.Provider.Equals(
                     PostgresProvider,
                     StringComparison.OrdinalIgnoreCase))
        {
            ValidatePostgres(options.Providers.Postgres, errors);
        }

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }

    private static bool IsSupportedProvider(string provider)
    {
        return provider.Equals(
                   SqliteProvider,
                   StringComparison.OrdinalIgnoreCase)
               || provider.Equals(
                   PostgresProvider,
                   StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidateSqlite(
        SqliteConfiguration? options,
        List<string> errors)
    {
        const string property =
            "Persistence:Providers:Sqlite:ConnectionString";

        if (options is null ||
            string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            errors.Add($"{property} is required.");
            return;
        }
    }

    private static void ValidatePostgres(
        PostgreSqlConfiguration? options,
        List<string> errors)
    {
        const string property =
            "Persistence:Providers:Postgres:ConnectionString";

        if (options is null ||
            string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            errors.Add($"{property} is required.");
            return;
        }
    }
}