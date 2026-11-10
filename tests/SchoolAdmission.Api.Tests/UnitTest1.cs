using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;
using Testcontainers.PostgreSql;
using Xunit;

namespace SchoolAdmission.Api.Tests;

public class DatabaseIntegrationTests
{
    [Fact]
    public async Task Database_ShouldConnect_AndApplyMigrations()
    {
        await using var postgres = new PostgreSqlBuilder()
            .WithDatabase("SchoolAdmissionTestDb")
            .WithUsername("schooladmin")
            .WithPassword("TestPassword123!")
            .Build();

        await postgres.StartAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(postgres.GetConnectionString())
            .Options;

        await using var context = new ApplicationDbContext(options);

        await context.Database.MigrateAsync();

        var classesTableExists = await context.Database
            .SqlQueryRaw<int>(
                """
                SELECT COUNT(*)::int AS "Value"
                FROM information_schema.tables
                WHERE table_schema = 'public'
                AND table_name = 'Classes'
                """)
            .SingleAsync();

        Assert.Equal(1, classesTableExists);
    }
}


// it verifes :
// 1. Starts a separate postgreSQL container
// 2. connects actual ApplicationDbContext to it
// 3. Applies the migrations that created for the project
// 4. checks whether the classes table exists.
// 5. passes only if the database setup works. 









