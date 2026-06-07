namespace RdpWrapMonitor.Service.Config;

public class ServiceConfig
{
    public string GmailAddress { get; set; } = string.Empty;
    public string EncryptedAppPassword { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public int CheckIntervalHours { get; set; } = 6;
    public string RemoteIniUrl { get; set; } = "https://raw.githubusercontent.com/sebaxakerhtc/rdpwrap.ini/master/rdpwrap.ini";
    public string LocalIniPath { get; set; } = @"C:\Program Files\RDP Wrapper\rdpwrap.ini";
    public string LogPath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "RdpWrapMonitor", "logs", "service.log");
    public string ConfigPath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "RdpWrapMonitor", "config.json");
}
