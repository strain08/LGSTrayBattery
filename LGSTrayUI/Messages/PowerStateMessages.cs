namespace LGSTrayUI.Messages;

/// <summary>
/// Message sent when the system is entering suspend/standby mode.
/// </summary>
public sealed record SystemSuspendingMessage;

/// <summary>
/// Message sent when the system is resuming from suspend/standby mode.
/// </summary>
public sealed record SystemResumingMessage;
