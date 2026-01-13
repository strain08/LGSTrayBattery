using LGSTrayPrimitives;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace LGSTrayUI.Interfaces;

public interface IConfigurationValidationService
{
    /// <summary>
    /// Loads configuration from appsettings.toml with error handling, repair attempts, and user prompts.
    /// Returns true if configuration loaded successfully (or recovered), false if application should exit.
    /// </summary>
    Task<bool> LoadAndValidateConfiguration(ConfigurationManager config);

    /// <summary>
    /// Validates software ID configuration. Returns true if valid.
    /// Shows error dialog and initiates shutdown if invalid.
    /// </summary>
    bool ValidateAndEnforceSoftwareId(AppSettings appSettings);

    /// <summary>
    /// Handles HTTP server startup failures.
    /// Shows error dialog and initiates shutdown.
    /// </summary>
    void HandleHttpServerStartupError(string errorMessage, int port);
}
