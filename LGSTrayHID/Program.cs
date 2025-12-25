using LGSTrayPrimitives;
using LGSTrayPrimitives.IPC;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using Tommy.Extensions.Configuration;

namespace LGSTrayHID;

internal static class GlobalSettings
{
    public static NativeDeviceManagerSettings settings = new();
}

internal class Program
{
    static async Task Main(string[] args)
    {
        // Load Logging config
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddTomlFile("appsettings.toml", optional: true, reloadOnChange: false);
        var loggingSettings = builder.Configuration.GetSection("Logging").Get<LoggingSettings>();

        // Determine logging settings
        bool enableLogging = loggingSettings?.Enabled ?? false;
        bool enableVerbose = loggingSettings?.Verbose ?? false;

        // Command-line overrides
        if (args.Contains("--log")) enableLogging = true;
        if (args.Contains("--verbose")) enableVerbose = true;

#if DEBUG
        enableLogging = true;
        // Note: HID daemon only enables verbose if explicitly requested
#endif

        // Initialize logging
        DiagnosticLogger.Initialize(enableLogging, enableVerbose);

        GlobalSettings.settings = builder.Configuration.GetSection("Native")
            .Get<NativeDeviceManagerSettings>() ?? GlobalSettings.settings;

        builder.Services.AddLGSMessagePipe();
        builder.Services.AddHostedService<HidppManagerService>();

        var host = builder.Build();

        _ = Task.Run(async () =>
        {
            bool ret = int.TryParse(args.ElementAtOrDefault(0), out int parentPid);
            if (!ret)
            {
#if DEBUG
                return;
#else
                // Started without a parent, assume invalid.
                Environment.Exit(0);
#endif
            }

            await Process.GetProcessById(parentPid).WaitForExitAsync();

            CancellationTokenSource cts = new(5000);
            await host.StopAsync(cts.Token);

            Environment.Exit(0);
        });

        await host.RunAsync();
    }
}