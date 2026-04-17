using KPIDash.Data;
using KPIDash.Seeder;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var connectionString = config.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' not found.");

Console.WriteLine("=== KPIDash Seeder ===");
Console.WriteLine($"Database: {connectionString}");
Console.WriteLine();

var factory = new SeederConnectionFactory(connectionString);
var orchestrator = new SeedOrchestrator(factory);
orchestrator.Seed();
