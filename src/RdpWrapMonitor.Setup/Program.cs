using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RdpWrapMonitor.Setup;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=============================================");
        Console.WriteLine("  RDPWrap Monitor Service - Setup Utility");
        Console.WriteLine("=============================================");
        Console.WriteLine();

        // Ensure config directory exists
        var configDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RdpWrapMonitor");

        if (!Directory.Exists(configDir))
        {
            Directory.CreateDirectory(configDir);
            Console.WriteLine($"Created config directory: {configDir}");
        }

        var configPath = Path.Combine(configDir, "config.json");

        // Load existing config or create new
        var config = LoadExistingConfig(configPath);

        Console.WriteLine("Gmail Setup");
        Console.WriteLine("-----------");
        Console.WriteLine("IMPORTANT: You need to use a Gmail App Password, not your regular password.");
        Console.WriteLine("To get an App Password:");
        Console.WriteLine("  1. Go to: https://myaccount.google.com/apppasswords");
        Console.WriteLine("  2. Select 'App passwords' (you may need 2FA enabled)");
        Console.WriteLine("  3. Create a new app password for 'Mail' on 'Windows Computer'");
        Console.WriteLine("  4. Copy the 16-character password shown");
        Console.WriteLine();

        Console.Write($"Gmail Address [{config.GmailAddress}]: ");
        var gmailInput = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(gmailInput))
        {
            config.GmailAddress = gmailInput;
        }

        Console.Write("Gmail App Password (16 characters): ");
        var appPassword = ReadPassword();
        Console.WriteLine();

        if (!string.IsNullOrEmpty(appPassword))
        {
            config.EncryptedAppPassword = EncryptPassword(appPassword);
        }

        Console.WriteLine();
        Console.WriteLine("Email Notification Settings");
        Console.WriteLine("---------------------------");
        Console.Write($"Recipient Email (default: {config.GmailAddress}): ");
        var recipientInput = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(recipientInput))
        {
            config.RecipientEmail = recipientInput;
        }
        else if (string.IsNullOrEmpty(config.RecipientEmail))
        {
            config.RecipientEmail = config.GmailAddress;
        }

        Console.WriteLine();
        Console.WriteLine("Update Check Settings");
        Console.WriteLine("---------------------");
        Console.Write($"Check interval in hours (default: {config.CheckIntervalHours}): ");
        var intervalInput = Console.ReadLine()?.Trim();
        if (int.TryParse(intervalInput, out var interval) && interval > 0)
        {
            config.CheckIntervalHours = interval;
        }

        Console.WriteLine();
        Console.WriteLine("RDPWrap Settings");
        Console.WriteLine("----------------");
        Console.WriteLine($"Remote INI URL: {config.RemoteIniUrl}");
        Console.Write($"RDP Wrapper Installation Path [{config.LocalRdpWrapPath}]: ");
        var rdpPathInput = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(rdpPathInput))
        {
            // Ensure path ends with backslash
            config.LocalRdpWrapPath = rdpPathInput.TrimEnd('\\') + "\\";
        }
        // Compute LocalIniPath from the RDPWrap path
        config.LocalIniPath = Path.Combine(config.LocalRdpWrapPath, "rdpwrap.ini");
        Console.WriteLine($"Local INI Path (computed): {config.LocalIniPath}");

        // Save configuration
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        var json = JsonSerializer.Serialize(config, options);
        await File.WriteAllTextAsync(configPath, json);

        Console.WriteLine();
        Console.WriteLine("=============================================");
        Console.WriteLine("  Configuration saved successfully!");
        Console.WriteLine($"  Config file: {configPath}");
        Console.WriteLine("=============================================");
        Console.WriteLine();
        Console.WriteLine("Next steps:");
        Console.WriteLine("  1. Build the service: dotnet build RdpWrapMonitor.Service.csproj");
        Console.WriteLine("  2. Install the service: sc create \"RDPWrap Monitor\" binPath= \"<path>\\RdpWrapMonitor.Service.exe\"");
        Console.WriteLine("  3. Start the service: sc start \"RDPWrap Monitor\"");
        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    private static ServiceConfig LoadExistingConfig(string configPath)
    {
        if (File.Exists(configPath))
        {
            try
            {
                var json = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<ServiceConfig>(json);
                if (config != null)
                {
                    Console.WriteLine("Loaded existing configuration.");
                    Console.WriteLine();
                    return config;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not load existing config: {ex.Message}");
                Console.WriteLine("Creating new configuration.");
                Console.WriteLine();
            }
        }

        return new ServiceConfig();
    }

    private static string EncryptPassword(string password)
    {
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        var encryptedBytes = ProtectedData.Protect(
            passwordBytes,
            null,
            DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encryptedBytes);
    }

    private static string ReadPassword()
    {
        var password = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                break;
            }
            if (key.Key == ConsoleKey.Backspace && password.Length > 0)
            {
                password.Remove(password.Length - 1, 1);
                Console.Write("\b \b");
            }
            else if (!char.IsControl(key.KeyChar))
            {
                password.Append(key.KeyChar);
                Console.Write("*");
            }
        }
        return password.ToString();
    }
}

public class ServiceConfig
{
    public string GmailAddress { get; set; } = string.Empty;
    public string EncryptedAppPassword { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public int CheckIntervalHours { get; set; } = 6;
    public string RemoteIniUrl { get; set; } = "https://raw.githubusercontent.com/sebaxakerhtc/rdpwrap.ini/master/rdpwrap.ini";
    public string LocalRdpWrapPath { get; set; } = @"C:\Program Files\RDP Wrapper\";
    public string LocalIniPath { get; set; } = string.Empty;
}
