namespace LGSTrayPrimitives.Messages;

/// <summary>
/// Message sent when the HTTP server fails to start (e.g., port already in use).
/// </summary>
/// <param name="ErrorMessage">The error message describing why the server failed to start</param>
/// <param name="Port">The port that failed to bind</param>
public sealed record HttpServerErrorMessage(string ErrorMessage, int Port);
