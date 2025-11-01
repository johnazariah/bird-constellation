namespace Owlet.Core.Installation;

/// <summary>
/// Windows Firewall rule configuration for WiX installer.
/// Defines inbound port access rule for the HTTP server.
/// </summary>
public record FirewallRule
{
    /// <summary>
    /// Firewall rule name (must be unique).
    /// </summary>
    public string Name { get; init; } = "Owlet Document Service - HTTP";

    /// <summary>
    /// Detailed description of the firewall rule.
    /// </summary>
    public string Description { get; init; } = "Allow HTTP access to Owlet document indexing service";

    /// <summary>
    /// Traffic direction (Inbound = external to service).
    /// </summary>
    public FirewallDirection Direction { get; init; } = FirewallDirection.Inbound;

    /// <summary>
    /// Action to take for matching traffic (Allow = permit connections).
    /// </summary>
    public FirewallAction Action { get; init; } = FirewallAction.Allow;

    /// <summary>
    /// Network protocol (TCP for HTTP).
    /// </summary>
    public FirewallProtocol Protocol { get; init; } = FirewallProtocol.TCP;

    /// <summary>
    /// Port number to allow (default 5555, configurable in appsettings.json).
    /// </summary>
    public int Port { get; init; } = 5555;

    /// <summary>
    /// Local IP addresses to allow (127.0.0.1 = localhost only).
    /// </summary>
    public string LocalAddresses { get; init; } = "127.0.0.1";

    /// <summary>
    /// Remote IP addresses allowed to connect (LocalSubnet = same network).
    /// </summary>
    public string RemoteAddresses { get; init; } = "LocalSubnet";

    /// <summary>
    /// Network profiles where rule is active (Domain, Private, Public).
    /// </summary>
    public FirewallProfiles Profiles { get; init; } = FirewallProfiles.Domain | FirewallProfiles.Private | FirewallProfiles.Public;

    /// <summary>
    /// Whether the firewall rule is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Interface types where rule applies (All = all network adapters).
    /// </summary>
    public string InterfaceTypes { get; init; } = "All";

    /// <summary>
    /// Edge traversal setting (Block = no edge traversal).
    /// </summary>
    public EdgeTraversalPolicy EdgeTraversal { get; init; } = EdgeTraversalPolicy.Block;
}

/// <summary>
/// Firewall traffic direction.
/// </summary>
public enum FirewallDirection
{
    /// <summary>External traffic coming to the service.</summary>
    Inbound,

    /// <summary>Service traffic going to external destinations.</summary>
    Outbound
}

/// <summary>
/// Firewall action for matching traffic.
/// </summary>
public enum FirewallAction
{
    /// <summary>Permit the connection.</summary>
    Allow,

    /// <summary>Block the connection.</summary>
    Block
}

/// <summary>
/// Network protocol types.
/// </summary>
public enum FirewallProtocol
{
    /// <summary>Transmission Control Protocol.</summary>
    TCP,

    /// <summary>User Datagram Protocol.</summary>
    UDP,

    /// <summary>Any protocol.</summary>
    Any
}

/// <summary>
/// Windows Firewall network profiles.
/// </summary>
[Flags]
public enum FirewallProfiles
{
    /// <summary>Domain-joined network profile.</summary>
    Domain = 1,

    /// <summary>Private network profile (home/work).</summary>
    Private = 2,

    /// <summary>Public network profile (untrusted networks).</summary>
    Public = 4,

    /// <summary>All network profiles.</summary>
    All = Domain | Private | Public
}

/// <summary>
/// Edge traversal policy for firewall rules.
/// </summary>
public enum EdgeTraversalPolicy
{
    /// <summary>Block edge traversal (default, most secure).</summary>
    Block,

    /// <summary>Allow edge traversal.</summary>
    Allow,

    /// <summary>Defer to application settings.</summary>
    DeferToApp,

    /// <summary>Defer to user settings.</summary>
    DeferToUser
}
