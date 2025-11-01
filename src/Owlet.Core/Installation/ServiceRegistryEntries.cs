namespace Owlet.Core.Installation;

/// <summary>
/// Windows service registry configuration for WiX installer.
/// These values define how the service is registered with Windows Service Control Manager.
/// </summary>
public record ServiceRegistryEntries
{
    /// <summary>
    /// Internal service name (must be unique, no spaces).
    /// </summary>
    public string ServiceName { get; init; } = "OwletService";

    /// <summary>
    /// Display name shown in Services management console.
    /// </summary>
    public string DisplayName { get; init; } = "Owlet Document Indexing Service";

    /// <summary>
    /// Detailed description shown in service properties.
    /// </summary>
    public string Description { get; init; } = "Indexes and searches local documents for fast retrieval";

    /// <summary>
    /// Full path to service executable (set during installation).
    /// </summary>
    public string ImagePath { get; init; } = @"C:\Program Files\Owlet\Owlet.Service.exe";

    /// <summary>
    /// Service type (Win32OwnProcess = standalone executable).
    /// </summary>
    public ServiceType Type { get; init; } = ServiceType.Win32OwnProcess;

    /// <summary>
    /// Service start mode (AutoStart = starts with Windows).
    /// </summary>
    public ServiceStartType Start { get; init; } = ServiceStartType.AutoStart;

    /// <summary>
    /// Error control level (Normal = log errors and continue boot).
    /// </summary>
    public ServiceErrorControl ErrorControl { get; init; } = ServiceErrorControl.Normal;

    /// <summary>
    /// Service account name (LocalSystem = highest privileges, no password).
    /// </summary>
    public string ObjectName { get; init; } = "LocalSystem";

    /// <summary>
    /// Service dependencies (empty = no dependencies).
    /// </summary>
    public string[] Dependencies { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Recovery actions to take on service failure.
    /// </summary>
    public FailureActions FailureActions { get; init; } = new()
    {
        ResetPeriod = TimeSpan.FromDays(1),
        Actions = new[]
        {
            new ServiceAction(ServiceActionType.Restart, TimeSpan.FromMinutes(1)),
            new ServiceAction(ServiceActionType.Restart, TimeSpan.FromMinutes(2)),
            new ServiceAction(ServiceActionType.Restart, TimeSpan.FromMinutes(5))
        }
    };
}

/// <summary>
/// Windows service type values.
/// </summary>
public enum ServiceType
{
    /// <summary>Service runs in its own process.</summary>
    Win32OwnProcess = 0x00000010,

    /// <summary>Service shares a process with other services.</summary>
    Win32ShareProcess = 0x00000020
}

/// <summary>
/// Windows service start type values.
/// </summary>
public enum ServiceStartType
{
    /// <summary>Started by Service Control Manager during system startup.</summary>
    AutoStart = 0x00000002,

    /// <summary>Started by Service Control Manager when a process calls StartService.</summary>
    DemandStart = 0x00000003,

    /// <summary>Cannot be started.</summary>
    Disabled = 0x00000004
}

/// <summary>
/// Windows service error control values.
/// </summary>
public enum ServiceErrorControl
{
    /// <summary>Error is logged, system startup continues.</summary>
    Normal = 0x00000001,

    /// <summary>Error is logged, system displays a message box, startup continues.</summary>
    Severe = 0x00000002,

    /// <summary>Error is logged, system restarts with last-known-good configuration.</summary>
    Critical = 0x00000003
}

/// <summary>
/// Service failure recovery actions configuration.
/// </summary>
public record FailureActions
{
    /// <summary>
    /// Time period after which the failure count resets to zero.
    /// </summary>
    public TimeSpan ResetPeriod { get; init; } = TimeSpan.FromDays(1);

    /// <summary>
    /// Actions to take on consecutive service failures.
    /// </summary>
    public ServiceAction[] Actions { get; init; } = Array.Empty<ServiceAction>();

    /// <summary>
    /// Optional command to run on service failure (not used for Owlet).
    /// </summary>
    public string? FailureCommand { get; init; }

    /// <summary>
    /// Optional reboot message (not used for Owlet).
    /// </summary>
    public string? RebootMessage { get; init; }
}

/// <summary>
/// Action to take on service failure.
/// </summary>
public record ServiceAction
{
    public ServiceAction(ServiceActionType type, TimeSpan delay)
    {
        Type = type;
        Delay = delay;
    }

    /// <summary>
    /// Type of action to perform.
    /// </summary>
    public ServiceActionType Type { get; init; }

    /// <summary>
    /// Delay before performing the action.
    /// </summary>
    public TimeSpan Delay { get; init; }
}

/// <summary>
/// Service failure action types.
/// </summary>
public enum ServiceActionType
{
    /// <summary>No action.</summary>
    None = 0,

    /// <summary>Restart the service.</summary>
    Restart = 1,

    /// <summary>Reboot the computer.</summary>
    Reboot = 2,

    /// <summary>Run a command.</summary>
    RunCommand = 3
}
