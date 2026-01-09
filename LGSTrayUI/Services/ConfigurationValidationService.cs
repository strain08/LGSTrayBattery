using LGSTrayPrimitives;
using LGSTrayPrimitives.Messages;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.Messaging;
using Tommy.Extensions.Configuration;
using LGSTrayUI.Interfaces;

namespace LGSTrayUI.Services;

public class ConfigurationValidationService : IConfigurationValidationService
{
    private readonly IMessenger? _messenger;

    // Parameterless constructor for early instantiation (before DI container built)
    public ConfigurationValidationService()
    {
        _messenger = null;
    }

    // DI constructor for when messenger is available
    public ConfigurationValidationService(IMessenger messenger)
    {
        _messenger = messenger;

        // Register to receive HTTP server error messages
        _messenger.Register<HttpServerErrorMessage>(this, OnHttpServerError);
    }

    private void OnHttpServerError(object recipient, HttpServerErrorMessage message)
    {
        HandleHttpServerStartupError(message.ErrorMessage, message.Port);
    }

    public async Task<bool> LoadAndValidateConfiguration(ConfigurationManager config)
    {
        string settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.toml");

        try
        {
            config.AddTomlFile(settingsPath);
            return true;
        }
        catch (Exception ex)
        {
            // Try to repair duplicate keys (common issue with legacy settings)
            if (ex is FormatException)
            {
                if (await TryRepairConfiguration(config, settingsPath))
                {
                    return true;
                }
                // Fall through to user prompt if repair failed
            }

            // Handle file not found, parsing errors, or invalid data
            if (ex is FileNotFoundException || ex is InvalidDataException || ex is FormatException)
            {
                return await PromptForConfigurationReset(config, settingsPath);
            }

            // Rethrow unexpected exceptions
            throw;
        }
    }

    private async Task<bool> TryRepairConfiguration(ConfigurationManager config, string settingsPath)
    {
        try
        {
            DiagnosticLogger.Log("Attempting to repair configuration (remove duplicates, merge missing keys)...");
            var settingsManager = new TomlSettingsManager();
            settingsManager.Repair();              // Remove obsolete keys
            settingsManager.MergeMissingKeys();    // Add missing keys from defaults
            config.AddTomlFile(settingsPath);
            DiagnosticLogger.Log("Repair successful.");
            return true;
        }
        catch (Exception repairEx)
        {
            DiagnosticLogger.LogError($"Settings repair failed: {repairEx.Message}");
            return false;
        }
    }

    private async Task<bool> PromptForConfigurationReset(ConfigurationManager config, string settingsPath)
    {
        var result = MessageBox.Show(
            "Failed to read settings, do you want reset to default?",
            "LGSTray - Settings Load Error",
            MessageBoxButton.YesNo,
            MessageBoxImage.Error,
            MessageBoxResult.No
        );

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                await File.WriteAllBytesAsync(settingsPath, LGSTrayUI.Properties.Resources.defaultAppsettings);
                DiagnosticLogger.Log("Configuration reset to defaults.");
            }
            catch (Exception writeEx)
            {
                DiagnosticLogger.LogError($"Failed to write default configuration: {writeEx.Message}");
                ShowErrorAndShutdown("LGSTray - Fatal Error", "Could not write default configuration file.");
                return false;
            }
        }

        try
        {
            config.AddTomlFile(settingsPath);
            return true;
        }
        catch (Exception loadEx)
        {
            DiagnosticLogger.LogError($"Failed to load configuration after reset attempt: {loadEx.Message}");
            ShowErrorAndShutdown("LGSTray - Fatal Error", "Could not load configuration file.");
            return false;
        }
    }

    public bool ValidateAndEnforceSoftwareId(AppSettings appSettings)
    {
        if (appSettings.Native.Enabled && !appSettings.Native.IsSoftwareIdValid())
        {
            string message = appSettings.Native.GetSoftwareIdErrorMessage();
            ShowErrorAndShutdown("LGSTray - Invalid Configuration", message);
            return false;
        }
        return true;
    }

    public void HandleHttpServerStartupError(string errorMessage, int port)
    {
        string message = $"LGSTray failed to start HTTP server on port {port}.\n\n" +
                        $"Error: {errorMessage}\n\n" +
                        $"The port may be in use by another application or user.\n\n" +
                        $"To fix this:\n" +
                        $"• Close other applications using port {port}\n" +
                        $"• OR configure a different port in appsettings.toml [HTTPServer] section\n" +
                        $"• OR disable the HTTP server (enabled = false)\n\n" +
                        $"Note: The application will close.";

        ShowErrorAndShutdown("LGSTray - HTTP Server Error", message);
    }

    private void ShowErrorAndShutdown(string title, string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
            DiagnosticLogger.LogError($"Configuration validation failed: {title}");
            Application.Current.Shutdown();
        });
    }
}
