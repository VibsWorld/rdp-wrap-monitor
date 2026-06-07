using RdpWrapMonitor.Service.Config;
using RdpWrapMonitor.Service.Monitor;
using RdpWrapMonitor.Service.Email;
using RdpWrapMonitor.Service.Utils;

namespace RdpWrapMonitor.Service;

public class RdpWrapMonitorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RdpWrapMonitorService> _logger;
    private readonly ServiceConfig _config;
    private readonly TimeSpan _checkInterval;

    public RdpWrapMonitorService(
        IServiceProvider serviceProvider,
        ILogger<RdpWrapMonitorService> logger,
        ServiceConfig config)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _config = config;
        _checkInterval = TimeSpan.FromHours(config.CheckIntervalHours);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RDPWrap Monitor Service starting. Check interval: {Interval}", _checkInterval);

        // Wait a bit on startup to ensure system is ready
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndUpdateAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("RDPWrap Monitor Service stopping due to cancellation");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during update check");

                // Try to send error notification
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var emailNotifier = scope.ServiceProvider.GetRequiredService<Email.IEmailNotifier>();
                    await emailNotifier.SendErrorNotificationAsync(ex.ToString(), stoppingToken);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to send error notification email");
                }
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("RDPWrap Monitor Service stopped");
    }

    private async Task CheckAndUpdateAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Checking for rdpwrap.ini updates...");

        using var scope = _serviceProvider.CreateScope();
        var iniMonitor = scope.ServiceProvider.GetRequiredService<Monitor.IIniMonitor>();
        var termServiceController = scope.ServiceProvider.GetRequiredService<Utils.ITermServiceController>();
        var emailNotifier = scope.ServiceProvider.GetRequiredService<Email.IEmailNotifier>();

        // Check if remote has updates
        bool hasUpdates = await iniMonitor.HasUpdatesAsync(cancellationToken);

        if (!hasUpdates)
        {
            _logger.LogInformation("No updates found. Next check in {Interval}", _checkInterval);
            return;
        }

        _logger.LogInformation("Update detected! Starting update process...");

        try
        {
            // Step 1: Stop TermService
            _logger.LogInformation("Step 1/4: Stopping TermService...");
            await termServiceController.StopAsync(cancellationToken);
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

            // Step 2: Download and save new INI
            _logger.LogInformation("Step 2/4: Downloading new rdpwrap.ini...");
            var remoteContent = await iniMonitor.GetRemoteContentAsync(cancellationToken);
            var localPath = _config.LocalIniPath;

            // Ensure directory exists
            var directory = Path.GetDirectoryName(localPath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(localPath, remoteContent, cancellationToken);
            _logger.LogInformation("rpdwrap.ini saved to {Path}", localPath);

            // Step 3: Start TermService
            _logger.LogInformation("Step 3/4: Starting TermService...");
            await termServiceController.StartAsync(cancellationToken);
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

            // Step 4: Send notification email
            _logger.LogInformation("Step 4/4: Sending notification email...");
            var versionInfo = ExtractVersionInfo(remoteContent);
            await emailNotifier.SendUpdateNotificationAsync(versionInfo, cancellationToken);

            _logger.LogInformation("Update process completed successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during update process");
            await emailNotifier.SendErrorNotificationAsync(ex.ToString(), cancellationToken);
            throw;
        }
    }

    private static string ExtractVersionInfo(string content)
    {
        // Extract the Updated= line from the INI file
        var lines = content.Split('\n');
        foreach (var line in lines)
        {
            if (line.StartsWith("Updated=", StringComparison.OrdinalIgnoreCase))
            {
                return line.Substring(8).Trim();
            }
        }
        return "Unknown version";
    }
}
