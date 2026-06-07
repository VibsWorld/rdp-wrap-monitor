using RdpWrapMonitor.Service;
using RdpWrapMonitor.Service.Config;
using RdpWrapMonitor.Service.Monitor;
using RdpWrapMonitor.Service.Email;
using RdpWrapMonitor.Service.Utils;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

// Add Windows Services support
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "RDPWrap Monitor";
});

// Load configuration
var configPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "RdpWrapMonitor", "config.json");

builder.Services.Configure<ServiceConfig>(options =>
{
    if (File.Exists(configPath))
    {
        var json = File.ReadAllText(configPath);
        var config = System.Text.Json.JsonSerializer.Deserialize<ServiceConfig>(json);
        if (config != null)
        {
            options.GmailAddress = config.GmailAddress;
            options.EncryptedAppPassword = config.EncryptedAppPassword;
            options.RecipientEmail = config.RecipientEmail;
            options.CheckIntervalHours = config.CheckIntervalHours;
            options.RemoteIniUrl = config.RemoteIniUrl;
            options.LocalIniPath = config.LocalIniPath;
        }
    }
    options.LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "RdpWrapMonitor", "logs", "service.log");
    options.ConfigPath = configPath;
});

// Register services
builder.Services.AddSingleton<ServiceConfig>(sp =>
    sp.GetRequiredService<IOptions<ServiceConfig>>().Value);

builder.Services.AddHttpClient();
builder.Services.AddSingleton<IIniMonitor, IniMonitor>();
builder.Services.AddSingleton<IEmailNotifier, EmailNotifier>();
builder.Services.AddSingleton<ITermServiceController, TermServiceController>();
builder.Services.AddSingleton<RdpWrapMonitorService>();

// Add logging to file
builder.Logging.ClearProviders();
builder.Logging.AddEventLog();
builder.Logging.AddProvider(new FileLoggerProvider(
    Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "RdpWrapMonitor", "logs", "service.log")));

// Register the background service
builder.Services.AddHostedService(sp => sp.GetRequiredService<RdpWrapMonitorService>());

var host = builder.Build();
host.Run();

/// <summary>
/// Simple file logger provider
/// </summary>
public class FileLoggerProvider(string filePath) : ILoggerProvider
{
    private readonly string _filePath = filePath;

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(_filePath, categoryName);
    }

    public void Dispose() { }
}

/// <summary>
/// Simple file logger
/// </summary>
public class FileLogger : ILogger
{
    private readonly string _filePath;
    private readonly string _categoryName;
    private readonly object _lock = new();

    public FileLogger(string filePath, string categoryName)
    {
        _filePath = filePath;
        _categoryName = categoryName;

        // Ensure directory exists
        var directory = Path.GetDirectoryName(filePath);
        if (directory != null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var message = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{logLevel}] [{_categoryName}] {formatter(state, exception)}";

        lock (_lock)
        {
            try
            {
                File.AppendAllText(_filePath, message + Environment.NewLine);
            }
            catch
            {
                // Ignore file write errors
            }
        }
    }
}
