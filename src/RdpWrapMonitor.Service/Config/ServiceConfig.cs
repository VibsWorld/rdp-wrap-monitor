namespace RdpWrapMonitor.Service.Config;

public class ServiceConfig
{
    public string GmailAddress { get; set; } = string.Empty;
    public string EncryptedAppPassword { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public int CheckIntervalHours { get; set; } = 6;
    public string RemoteIniUrl { get; set; } = "https://raw.githubusercontent.com/sebaxakerhtc/rdpwrap.ini/master/rdpwrap.ini";

    // RDP Wrapper installation path - configurable via appsettings.json
    public string LocalRdpWrapPath { get; set; } = @"C:\Program Files\RDP Wrapper\";

    // Full path to rdpwrap.ini - computed from LocalRdpWrapPath if not explicitly set
    public string LocalIniPath { get; set; } = string.Empty;

    public string LogPath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "RdpWrapMonitor", "logs", "service.log");
    public string ConfigPath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "RdpWrapMonitor", "config.json");

    // Initialize LocalIniPath from LocalRdpWrapPath
    public void Initialize()
    {
        if (string.IsNullOrEmpty(LocalIniPath))
        {
            LocalIniPath = Path.Combine(LocalRdpWrapPath, "rdpwrap.ini");
        }
    }
}
