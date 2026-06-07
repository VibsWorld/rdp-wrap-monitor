using System.ServiceProcess;

namespace RdpWrapMonitor.Service.Utils;

public interface ITermServiceController
{
    Task StopAsync(CancellationToken cancellationToken);
    Task StartAsync(CancellationToken cancellationToken);
}

public class TermServiceController : ITermServiceController
{
    private readonly ILogger<TermServiceController> _logger;
    private const string ServiceName = "TermService";
    private const int OperationTimeoutMs = 30000;

    public TermServiceController(ILogger<TermServiceController> logger)
    {
        _logger = logger;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            _logger.LogInformation("Stopping {ServiceName}...", ServiceName);

            using var service = new ServiceController(ServiceName);

            if (service.Status == ServiceControllerStatus.Stopped)
            {
                _logger.LogInformation("{ServiceName} is already stopped", ServiceName);
                return;
            }

            if (service.Status == ServiceControllerStatus.StopPending)
            {
                _logger.LogInformation("{ServiceName} is already stopping, waiting...", ServiceName);
                service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromMilliseconds(OperationTimeoutMs));
                return;
            }

            service.Stop();
            service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromMilliseconds(OperationTimeoutMs));
            _logger.LogInformation("{ServiceName} stopped successfully", ServiceName);
        }, cancellationToken);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            _logger.LogInformation("Starting {ServiceName}...", ServiceName);

            using var service = new ServiceController(ServiceName);

            if (service.Status == ServiceControllerStatus.Running)
            {
                _logger.LogInformation("{ServiceName} is already running", ServiceName);
                return;
            }

            if (service.Status == ServiceControllerStatus.StartPending)
            {
                _logger.LogInformation("{ServiceName} is already starting, waiting...", ServiceName);
                service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMilliseconds(OperationTimeoutMs));
                return;
            }

            service.Start();
            service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMilliseconds(OperationTimeoutMs));
            _logger.LogInformation("{ServiceName} started successfully", ServiceName);
        }, cancellationToken);
    }
}
