using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace LGSTrayPrimitives.IPC;

public static class MessagePipeHelper
{
    /// <summary>
    /// Gets the session-specific pipe name to support multiple user sessions on the same machine.
    /// Each Windows user session gets its own isolated named pipe.
    /// </summary>
    private static string GetSessionSpecificPipeName()
    {
        // Use Windows Session ID to make the pipe unique per user session
        // This allows multiple users to run LGSTray simultaneously without conflicts
        int sessionId = Process.GetCurrentProcess().SessionId;
        return $"LGSTray_{sessionId}";
    }

    public static void AddLGSMessagePipe(this IServiceCollection services, bool hostAsServer = false)
    {
        string pipeName = GetSessionSpecificPipeName();

        // Log the pipe name for diagnostics (helps troubleshoot multi-user scenarios)
        DiagnosticLogger.Log($"Using named pipe: {pipeName} (Server: {hostAsServer})");

        services.AddMessagePipe(options =>
        {
            options.EnableCaptureStackTrace = true;
        })
        .AddNamedPipeInterprocess(pipeName, config =>
        {
            config.HostAsServer = hostAsServer;
        });
    }
}
