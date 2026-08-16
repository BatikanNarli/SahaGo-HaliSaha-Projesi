using System.Data;
using Halisaha.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Halisaha.IntegrationTests;

public sealed class SqlSchemaIntegrationTests
{
    private static readonly string[] ExpectedTables =
    [
        "__EFMigrationsHistory", "Amenities", "AuditLogs", "Businesses", "BusinessMembers",
        "Facilities", "FacilityImages", "FieldAmenities", "FieldBlocks", "Fields",
        "IdempotencyRecords", "NotificationDeliveries", "OutboxMessages", "PaymentEvents",
        "Payments", "PriceRules", "RefreshTokens", "Refunds", "Reservations", "ReservationSlots",
        "RoleClaims", "Roles", "SpecialHours", "UserClaims", "UserLogins", "UserRoles", "Users",
        "UserTokens", "WeeklyHours"
    ];

    [SqlServerFact]
    public async Task AppliedSchemaContainsExpectedTablesRolesAndSlotConstraint()
    {
        var connectionString = Environment.GetEnvironmentVariable(SqlServerFactAttribute.ConnectionVariable)!;
        var options = new DbContextOptionsBuilder<HalisahaDbContext>().UseSqlServer(connectionString).Options;
        await using var dbContext = new HalisahaDbContext(options);
        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync();

        await using var tableCommand = connection.CreateCommand();
        tableCommand.CommandText = "SELECT [name] FROM sys.tables ORDER BY [name]";
        var actualTables = new List<string>();
        await using (var reader = await tableCommand.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync()) actualTables.Add(reader.GetString(0));
        }
        Assert.Equal(ExpectedTables.Order(), actualTables);

        await using var roleCommand = connection.CreateCommand();
        roleCommand.CommandText = "SELECT [Name] FROM dbo.Roles ORDER BY [Name]";
        var roles = new List<string>();
        await using (var reader = await roleCommand.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync()) roles.Add(reader.GetString(0));
        }
        Assert.Equal(["BusinessOwner", "BusinessStaff", "Customer", "SystemAdmin"], roles);

        await using var migrationCommand = connection.CreateCommand();
        migrationCommand.CommandText = "SELECT COUNT(*) FROM dbo.__EFMigrationsHistory";
        Assert.Equal(2, Convert.ToInt32(await migrationCommand.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture));

        await using var indexCommand = connection.CreateCommand();
        indexCommand.CommandText = """
            SELECT [is_unique], [filter_definition]
            FROM sys.indexes
            WHERE [object_id] = OBJECT_ID(N'dbo.ReservationSlots')
              AND [name] = N'IX_ReservationSlots_FieldId_SlotStartUtc'
            """;
        await using var indexReader = await indexCommand.ExecuteReaderAsync(CommandBehavior.SingleRow);
        Assert.True(await indexReader.ReadAsync());
        Assert.True(indexReader.GetBoolean(0));
        Assert.Equal("([IsActive]=(1))", indexReader.GetString(1));
    }
}

internal sealed class SqlServerFactAttribute : FactAttribute
{
    public const string ConnectionVariable = "HALISAHA_SQL_TEST_CONNECTION";

    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ConnectionVariable)))
        {
            Skip = $"Set {ConnectionVariable} to run the local SQL Server schema test.";
        }
    }
}
