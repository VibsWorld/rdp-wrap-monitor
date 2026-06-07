using System.Security.Cryptography;
using System.Text;
using RdpWrapMonitor.Service.Config;

namespace RdpWrapMonitor.Service.Monitor;

public interface IIniMonitor
{
    Task<bool> HasUpdatesAsync(CancellationToken cancellationToken);
    Task<string> GetRemoteContentAsync(CancellationToken cancellationToken);
}

public class IniMonitor : IIniMonitor
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ServiceConfig _config;
    private readonly ILogger<IniMonitor> _logger;

    public IniMonitor(
        IHttpClientFactory httpClientFactory,
        ServiceConfig config,
        ILogger<IniMonitor> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    public async Task<bool> HasUpdatesAsync(CancellationToken cancellationToken)
    {
        try
        {
            string? remoteHash = await GetRemoteHashAsync(cancellationToken);
            string? localHash = await GetLocalHashAsync(cancellationToken);

            if (remoteHash == null || localHash == null)
            {
                _logger.LogWarning("Could not compute hashes - remote: {Remote}, local: {Local}",
                    remoteHash == null, localHash == null);
                return false;
            }

            bool hasChanges = !string.Equals(remoteHash, localHash, StringComparison.Ordinal);
            _logger.LogInformation("Update check: Remote hash = {RemoteHash}, Local hash = {LocalHash}, Has changes = {HasChanges}",
                remoteHash[..8], localHash[..8], hasChanges);
            return hasChanges;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for updates");
            return false;
        }
    }

    public async Task<string> GetRemoteContentAsync(CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        return await client.GetStringAsync(_config.RemoteIniUrl, cancellationToken);
    }

    private async Task<string?> GetRemoteHashAsync(CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var content = await client.GetStringAsync(_config.RemoteIniUrl, cancellationToken);
            return ComputeHash(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching remote INI file");
            return null;
        }
    }

    private async Task<string?> GetLocalHashAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(_config.LocalIniPath))
            {
                _logger.LogWarning("Local INI file does not exist: {Path}", _config.LocalIniPath);
                return null;
            }

            var content = await File.ReadAllTextAsync(_config.LocalIniPath, cancellationToken);
            return ComputeHash(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading local INI file");
            return null;
        }
    }

    private static string ComputeHash(string content)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
}
