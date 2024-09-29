using Microsoft.EntityFrameworkCore;
using Serilog;
using SuperPlayGameServer.Application.WebSocketHandlers;
using SuperPlayGameServer.Application.PlayerManagement;
using SuperPlayGameServer.Core.Interfaces;
using SuperPlayGameServer.Infra.Data;
using SuperPlayGameServer.WebSockets;
using SuperPlayGameServer.Infra.Data.Repositories;
using SuperPlayGameServer.Application.Factory;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Register JsonSerializerOptions globally
builder.Services.AddSingleton(new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true // Global case-insensitive setting
});


// Set up Serilog for logging
ConfigureLogging();

builder.Host.UseSerilog();

// Configure Services and Dependency Injection
ConfigureServices(builder.Services, builder.Configuration);

// Build the application
var app = builder.Build();

// Apply migrations and ensure the database is created on startup
ApplyMigrations(app);

// Configure WebSocket Options
var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)  // Keeps WebSocket connection alive for 2 minutes
};

// Enable WebSocket handling in the app
app.UseWebSockets(webSocketOptions);

// Add WebSocket middleware to handle WebSocket connections
app.UseMiddleware<WebSocketMiddleware>();

// Log that the application has started successfully
Log.Information("SuperPlayGameServer is running...");

//Service shotdown hook to mark all players as logged out
var lifetime = app.Lifetime;
lifetime.ApplicationStopping.Register(() =>
{
    Log.Information("Application is shutting down. Marking all users as logged out.");
    using var scope = app.Services.CreateScope();
    var playerService = scope.ServiceProvider.GetRequiredService<IPlayerService>();

    // Use Task.Run to avoid deadlocks during shutdown
    Task.Run(async () => await MarkAllPlayersAsLoggedOut(playerService)).Wait();
});


// Helper method to mark all players as logged out
static async Task MarkAllPlayersAsLoggedOut(IPlayerService playerService)
{
    try
    {
        var players = await playerService.GetAllLoggedInPlayersAsync();
        foreach (var player in players)
        {
            player.IsLoggedIn = false;
            await playerService.UpdatePlayerStateAsync(player);
            Log.Information("Player {PlayerId} marked as logged out.", player.PlayerId);
        }
        Log.Information("All users have been successfully marked as logged out.");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error occurred while marking users as logged out during shutdown.");
    }
}

// Start the app
app.Run();

// Shutdown the logger gracefully
Log.CloseAndFlush();

/// <summary>
/// Configures logging using Serilog.
/// </summary>
void ConfigureLogging()
{
    Log.Logger = new LoggerConfiguration()
        .WriteTo.Console()
        .WriteTo.File("logs/server_log.txt", rollingInterval: RollingInterval.Day)
        .CreateLogger();
}

/// <summary>
/// Configures all services and dependency injection.
/// </summary>
void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    // Configure SQLite database
    services.AddDbContext<GameDbContext>(options =>
    {
        var connectionString = configuration.GetConnectionString("GameDatabase");

        // Configure SQLite with WAL mode
        options.UseSqlite(connectionString, sqliteOptions =>
        {
            sqliteOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
            sqliteOptions.CommandTimeout(60);
        });

        // Enable logging for SQL queries
        options.LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information)
               .EnableSensitiveDataLogging()
               .EnableDetailedErrors();
    }, ServiceLifetime.Scoped);

    // Register repositories
    services.AddScoped<IPlayerRepository, PlayerRepository>();
    services.AddScoped<IGiftRepository, GiftRepository>();

    // Register command handler factory and individual commands
    services.AddScoped<ICommandHandlerFactory, CommandHandlerFactory>();
    services.AddScoped<LoginCommand>();
    services.AddScoped<UpdateResourcesCommand>();
    services.AddScoped<SendGiftCommand>();
    services.AddScoped<GetBalanceCommand>();

    // Register WebSocketRouter and PlayerService
    services.AddScoped<WebSocketRouter>();
    services.AddScoped<IPlayerService, PlayerService>();

    // Configure logging
    services.AddLogging(loggingBuilder =>
    {
        loggingBuilder.AddDebug();
        loggingBuilder.AddConsole(options => options.IncludeScopes = true);
    });
}

/// <summary>
/// Applies pending database migrations and ensures the database is created.
/// </summary>
void ApplyMigrations(WebApplication app)
{
    using (var scope = app.Services.CreateScope()) // Access app.Services to get IServiceProvider
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<GameDbContext>();

        try
        {
            Log.Information("Applying pending migrations...");

            // Automatically apply any pending migrations
            dbContext.Database.Migrate();

            // Ensures the database exists
            dbContext.Database.EnsureCreated();

            // Set journal mode to WAL for better concurrency in SQLite
            dbContext.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");

            Log.Information("Migrations applied successfully.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while applying migrations: {ErrorMessage}", ex.Message);
            throw; // Re-throw the exception to halt application startup if critical
        }
    }


}

