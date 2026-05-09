using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using AmarShowsBook.Data;

namespace AmarShowsBook.Services
{
    public class SeatLockExpiryService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SeatLockExpiryService> _logger;

        public SeatLockExpiryService(
            IServiceScopeFactory scopeFactory,
            ILogger<SeatLockExpiryService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Seat Lock Expiry Service Started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope =
                        _scopeFactory.CreateScope();

                    var db =
                        scope.ServiceProvider
                        .GetRequiredService<ApplicationDbContext>();

                    var result =
                        await db.Database.ExecuteSqlRawAsync(
                            "SELECT fn_expire_seat_locks();",
                            stoppingToken);

                    _logger.LogInformation(
                        "Seat lock expiry job executed successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Seat lock expiry job failed");
                }

                await Task.Delay(
                    TimeSpan.FromMinutes(1),
                    stoppingToken);
            }
        }
    }
}