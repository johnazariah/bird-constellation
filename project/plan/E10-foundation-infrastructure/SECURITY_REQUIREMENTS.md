# E10 Foundation & Infrastructure - Security Requirements

## Overview

This document defines the security requirements for Owlet's Windows service deployment. The principle is **defense in depth** with **least privilege access** while maintaining simple, reliable operation.

## Service Account Security

### Primary Service Account: LocalSystem

**Rationale:**
- No password management required
- Sufficient privileges for file system and network operations
- Standard for document indexing services
- Simplifies MSI installation (no credential prompts)

**Privileges:**
- ✅ Read/write access to `C:\ProgramData\Owlet\`
- ✅ Read access to user-specified document directories
- ✅ Network access for localhost HTTP server
- ✅ Windows Event Log write access
- ❌ No outbound internet access (local-first architecture)
- ❌ No access to system-protected directories
- ❌ No registry write access (except service configuration)

### Alternative Service Accounts (Future)

**NetworkService** (Lower privilege alternative):
- Suitable for environments requiring minimal privileges
- Requires explicit file system ACL configuration
- No Windows Event Log write access by default
- Installation complexity increases (ACL setup in MSI)

**LocalService** (Lowest privilege):
- Most restricted account
- Limited file system access
- No network access by default
- Not suitable for Owlet's requirements

**Custom User Account:**
- Maximum flexibility for enterprise environments
- Requires credential management during installation
- Increases MSI installer complexity significantly
- Reserved for future enterprise features

## Network Security

### HTTP Server Binding

**Default Configuration:**
```json
{
  "Network": {
    "BindAddress": "127.0.0.1",
    "Port": 5555,
    "EnableHttps": false
  }
}
```

**Security Properties:**
- ✅ Localhost-only binding (127.0.0.1) prevents external access
- ✅ User-configurable port (avoids conflicts, no privileged ports)
- ✅ No HTTPS required for local-only communication
- ✅ Firewall rule restricts access to local subnet only
- ❌ No external network exposure by default
- ❌ No authentication required (local trust boundary)

### HTTPS Configuration (Optional)

**When to Enable:**
- Service bound to non-localhost addresses
- Corporate security policies require encryption
- Integration with external monitoring tools

**Configuration:**
```json
{
  "Network": {
    "BindAddress": "0.0.0.0",
    "Port": 5555,
    "EnableHttps": true,
    "CertificatePath": "C:\\ProgramData\\Owlet\\cert.pfx",
    "CertificatePassword": "<encrypted or from key vault>"
  }
}
```

**Certificate Management:**
- Certificates stored in `C:\ProgramData\Owlet\Certificates\`
- Password retrieved from environment variables or Windows Credential Manager
- Self-signed certificates acceptable for local development
- CA-signed certificates recommended for production

### Firewall Rules

**Default Rule:**
- Name: "Owlet Document Service - HTTP"
- Direction: Inbound
- Protocol: TCP
- Port: 5555 (configurable)
- Local Addresses: 127.0.0.1
- Remote Addresses: LocalSubnet
- Profiles: Domain, Private, Public
- Action: Allow

**Security Considerations:**
- Rule created automatically during MSI installation
- Rule removed automatically during MSI uninstallation
- User can disable rule via Windows Firewall if desired
- Port changes require MSI reinstall or manual firewall update

## File System Security

### Owlet Data Directory

**Location:** `C:\ProgramData\Owlet\`

**Directory Structure:**
```
C:\ProgramData\Owlet\
├── Logs\              # Service logs (LocalSystem read/write)
├── Database\          # SQLite database (LocalSystem read/write)
├── Indexes\           # Document indexes (LocalSystem read/write)
├── Configuration\     # Runtime configuration (LocalSystem read/write)
└── Certificates\      # HTTPS certificates (LocalSystem read-only)
```

**Access Control Lists (ACLs):**
- **LocalSystem:** Full Control
- **Administrators:** Full Control
- **Users:** Read & Execute (for diagnostic tools)
- **Everyone:** No access

**Security Properties:**
- ✅ Isolated from user data (ProgramData vs AppData)
- ✅ Protected from unprivileged users
- ✅ Survives user logoff (ProgramData persists)
- ✅ Visible to administrators for troubleshooting

### User Document Directories

**Access Pattern:**
- Service reads user-specified directories for indexing
- No write access to user documents
- Index data stored in `C:\ProgramData\Owlet\Indexes\`
- User controls indexed directories via configuration

**Security Properties:**
- ✅ Read-only access to user documents
- ✅ No modification of user files
- ✅ Index data isolated from source documents
- ❌ Service cannot delete or encrypt user files

## Registry Security

### Service Configuration Registry Keys

**Location:** `HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\OwletService`

**Keys Created:**
- `DisplayName` (REG_SZ)
- `Description` (REG_SZ)
- `ImagePath` (REG_EXPAND_SZ)
- `ObjectName` (REG_SZ) - Service account
- `Start` (REG_DWORD) - Start type
- `Type` (REG_DWORD) - Service type
- `ErrorControl` (REG_DWORD)
- `FailureActions` (REG_BINARY)

**Access Control:**
- ✅ LocalSystem: Read access (service queries its own configuration)
- ✅ Administrators: Full Control (service management)
- ❌ Users: No access (prevents tampering)

**Security Properties:**
- Read-only access for service account
- No runtime registry writes (configuration via appsettings.json)
- Registry changes require service restart
- MSI installer manages all registry operations

### Application Configuration Registry Keys

**Not Used:**
- Owlet does not store application configuration in registry
- All configuration via JSON files in `C:\ProgramData\Owlet\Configuration\`
- Registry used only for Windows service registration

## Logging Security

### Windows Event Log

**Event Source:** "Owlet Service"  
**Log Name:** Application  
**Minimum Level:** Warning

**Logged Events:**
- ✅ Service startup/shutdown
- ✅ Configuration validation failures
- ✅ Critical errors (database corruption, disk full)
- ✅ Security-relevant events (invalid configuration, binding failures)
- ❌ Sensitive data (API keys, passwords, user documents)

**Access Control:**
- Event log visible to all users (standard Windows behavior)
- No sensitive information logged
- Detailed logs in file system (restricted access)

### File System Logs

**Location:** `C:\ProgramData\Owlet\Logs\`

**Log Files:**
- `owlet-YYYYMMDD.log` - Human-readable logs
- `owlet-structured-YYYYMMDD.json` - Machine-readable logs

**Security Properties:**
- ✅ Restricted to LocalSystem and Administrators
- ✅ Log rotation prevents disk exhaustion
- ✅ Sensitive data redacted automatically
- ✅ Structured logs enable SIEM integration

**Redaction Rules:**
- Passwords: `***REDACTED***`
- API keys: `***REDACTED***`
- File paths: Relative paths only (e.g., `Documents\file.pdf` not `C:\Users\John\Documents\file.pdf`)
- User names: Anonymized (e.g., `User-ABC123`)

## Data Protection

### Data at Rest

**SQLite Database:**
- Location: `C:\ProgramData\Owlet\Database\owlet.db`
- No encryption by default (local-only access)
- Optional: SQLCipher for encrypted database (future feature)
- Access restricted via file system ACLs

**Document Indexes:**
- Location: `C:\ProgramData\Owlet\Indexes\`
- Contains document metadata and search indexes
- No encryption by default
- No user document content stored (only references and metadata)

### Data in Transit

**Local HTTP:**
- No encryption (localhost trust boundary)
- HTTP over 127.0.0.1 acceptable for local communication
- Optional HTTPS for non-localhost scenarios

**External API (Future):**
- HTTPS required for any external communication
- Certificate validation enforced
- TLS 1.2+ only (no SSL, TLS 1.0, TLS 1.1)

## Threat Model

### Threats Mitigated

✅ **Unauthorized Network Access:**
- Mitigated by localhost-only binding and firewall rules

✅ **Unauthorized File Access:**
- Mitigated by file system ACLs and ProgramData isolation

✅ **Service Tampering:**
- Mitigated by registry ACLs and administrator-only service control

✅ **Configuration Tampering:**
- Mitigated by file system ACLs on configuration files

✅ **Log Tampering:**
- Mitigated by file system ACLs and Windows Event Log protection

### Threats Not Mitigated (Out of Scope)

❌ **Local Administrator Attacks:**
- Administrators can always access service data
- Mitigation: OS-level security (BitLocker, TPM)

❌ **Physical Access Attacks:**
- Physical access compromises all local services
- Mitigation: Full disk encryption, secure boot

❌ **Supply Chain Attacks:**
- Malicious dependencies or build tools
- Mitigation: Dependency scanning, signed builds (S40)

❌ **Social Engineering:**
- User tricks or phishing attacks
- Mitigation: User education, principle of least privilege

## Compliance Considerations

### Data Privacy (GDPR, CCPA)

**User Data:**
- Document metadata indexed locally (no cloud storage)
- User controls indexed directories
- Data deletion via uninstallation or manual file deletion

**Telemetry:**
- No telemetry by default
- Optional anonymous usage statistics (future feature)
- User consent required before enabling telemetry

### Enterprise Security

**Active Directory Integration:**
- Not required for Owlet v1
- Future feature for enterprise deployments

**Group Policy Support:**
- Not required for Owlet v1
- Future feature for enterprise deployments

**Audit Logging:**
- Windows Event Log provides basic audit trail
- Detailed audit logs via structured file logging

## Security Testing

### Static Analysis
- CodeQL scanning (GitHub Actions)
- Dependency vulnerability scanning (Dependabot)
- Code signing verification

### Dynamic Analysis
- Port scanning (only port 5555 accessible locally)
- Authentication testing (no authentication required for localhost)
- HTTPS configuration testing (optional HTTPS)

### Penetration Testing
- Local privilege escalation (out of scope for v1)
- Network-based attacks (mitigated by localhost-only binding)
- Configuration injection (mitigated by configuration validation)

## Security Updates

### Patching Strategy
- Critical security updates: Immediate patch release
- High-severity updates: Patch within 7 days
- Medium-severity updates: Patch within 30 days
- Low-severity updates: Next regular release

### Update Delivery
- GitHub Releases (manual download and install)
- Future: Auto-update mechanism (opt-in)
- MSI upgrade installation (preserves configuration)

## Security Contacts

**Report Security Issues:**
- GitHub Security Advisories (preferred)
- Email: security@owlet.example (future)
- Responsible disclosure: 90-day embargo

---

**Document Created:** November 1, 2025  
**Applies To:** E10 Foundation & Infrastructure  
**Status:** Living Document (updated as threats evolve)
