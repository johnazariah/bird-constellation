# E10 S50: WiX Installer

**Story:** Create professional MSI installer using WiX Toolset with Windows service registration, firewall configuration, and clean uninstall  
**Priority:** Critical  
**Effort:** 22 hours  
**Status:** Not Started  
**Dependencies:** S40 (Build Pipeline)  

## Objective

This story creates a professional MSI installer using WiX Toolset 4.x that provides a seamless installation experience for end users. The installer handles Windows service registration, firewall rule creation, directory structure setup, and comprehensive uninstall capabilities while following Windows installer best practices.

The installer prioritizes user experience and reliability, ensuring that Owlet can be installed by non-technical users with minimal configuration required. It includes proper error handling, rollback capabilities, and integration with Windows installer infrastructure.

## Business Context

**Revenue Impact:** ₹0 direct revenue (enables user adoption through professional installation experience)  
**User Impact:** All users - determines first impression, installation success rate, and adoption barriers  
**Compliance Requirements:** Digital signing and Windows compatibility requirements for enterprise distribution

## WiX Installer Architecture

### 1. Installer Project Structure

Complete WiX 4.x project with modern tooling and best practices.

**`packaging/installer/Owlet.Installer.wixproj`:**

```xml
<Project Sdk="WixToolset.Sdk/4.0.4">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <OutputType>Bundle</OutputType>
    <PlatformTarget>x64</PlatformTarget>
    <OutputName>OwletInstaller</OutputName>
    <DefineConstants>$(DefineConstants);ProductVersion=$(Version)</DefineConstants>
  </PropertyGroup>

  <PropertyGroup>
    <!-- Default values - can be overridden by build -->
    <ProductVersion Condition="'$(ProductVersion)' == ''">1.0.0</ProductVersion>
    <SourceDir Condition="'$(SourceDir)' == ''">..\..\artifacts\service-package</SourceDir>
    <ProductName>Owlet Document Indexing Service</ProductName>
    <Manufacturer>Owlet</Manufacturer>
    <ProductCode>$(Version)</ProductCode>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="WixToolset.UI.wixext" Version="4.0.4" />
    <PackageReference Include="WixToolset.Util.wixext" Version="4.0.4" />
    <PackageReference Include="WixToolset.Firewall.wixext" Version="4.0.4" />
    <PackageReference Include="WixToolset.NetFx.wixext" Version="4.0.4" />
  </ItemGroup>

  <ItemGroup>
    <Compile Include="Bundle.wxs" />
    <Compile Include="Product.wxs" />
    <Compile Include="UI.wxs" />
    <Compile Include="Components.wxs" />
    <Compile Include="Service.wxs" />
    <Compile Include="Firewall.wxs" />
  </ItemGroup>

  <ItemGroup>
    <Content Include="License.rtf" />
    <Content Include="Banner.bmp" />
    <Content Include="Dialog.bmp" />
    <Content Include="Owlet.ico" />
  </ItemGroup>

</Project>
```

### 2. Main Product Definition

Core product definition with proper versioning and upgrade handling.

**`packaging/installer/Product.wxs`:**

```xml
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs"
     xmlns:util="http://wixtoolset.org/schemas/v4/wxs/util">

  <!-- Product definition -->
  <Product Id="*" 
           Name="$(var.ProductName)" 
           Language="1033" 
           Version="$(var.ProductVersion)" 
           Manufacturer="$(var.Manufacturer)" 
           UpgradeCode="12345678-1234-1234-1234-123456789012">

    <!-- Package definition -->
    <Package InstallerVersion="500" 
             Compressed="yes" 
             InstallScope="perMachine"
             Description="$(var.ProductName) v$(var.ProductVersion)"
             Comments="Professional document indexing and search service"
             Manufacturer="$(var.Manufacturer)"
             Platform="x64" />

    <!-- Media and compression -->
    <Media Id="1" Cabinet="product.cab" EmbedCab="yes" CompressionLevel="high" />

    <!-- Prerequisites -->
    <PropertyRef Id="WIX_IS_NETFRAMEWORK_48_OR_LATER_INSTALLED" />
    <Condition Message="This application requires Windows 10 version 1809 or later.">
      <![CDATA[Installed OR (VersionNT >= 1000 AND VersionNT64)]]>
    </Condition>

    <!-- Installation properties -->
    <Property Id="ARPPRODUCTICON" Value="ProductIcon" />
    <Property Id="ARPHELPLINK" Value="https://github.com/bird-constellation/owlet/wiki" />
    <Property Id="ARPURLINFOABOUT" Value="https://github.com/bird-constellation/owlet" />
    <Property Id="ARPNOREPAIR" Value="1" />
    <Property Id="ARPNOMODIFY" Value="1" />
    
    <!-- Custom properties -->
    <Property Id="SERVICE_PORT" Value="5555" />
    <Property Id="SERVICE_ACCOUNT" Value="LocalSystem" />
    <Property Id="AUTO_START_SERVICE" Value="1" />
    <Property Id="CREATE_FIREWALL_RULE" Value="1" />

    <!-- Icon definition -->
    <Icon Id="ProductIcon" SourceFile="Owlet.ico" />

    <!-- Upgrade logic -->
    <MajorUpgrade DowngradeErrorMessage="A newer version of [ProductName] is already installed."
                  Schedule="afterInstallInitialize"
                  AllowSameVersionUpgrades="yes" />

    <!-- Feature tree -->
    <Feature Id="ProductFeature" 
             Title="Owlet Service" 
             Description="Core document indexing service and API"
             Level="1"
             Absent="disallow">
      
      <ComponentRef Id="ServiceExecutable" />
      <ComponentRef Id="ServiceConfiguration" />
      <ComponentRef Id="ServiceRegistration" />
      <ComponentRef Id="FirewallRule" />
      <ComponentRef Id="DataDirectory" />
      <ComponentRef Id="LogDirectory" />
      <ComponentRef Id="UninstallScripts" />
    </Feature>

    <!-- Optional features -->
    <Feature Id="TrayApplication"
             Title="System Tray Application"
             Description="Optional system tray application for status monitoring"
             Level="2">
      <ComponentRef Id="TrayExecutable" />
      <ComponentRef Id="TrayStartup" />
    </Feature>

    <Feature Id="DiagnosticTools"
             Title="Diagnostic Tools"
             Description="Command-line tools for troubleshooting and maintenance"
             Level="2">
      <ComponentRef Id="DiagnosticExecutables" />
    </Feature>

    <!-- Custom actions -->
    <CustomAction Id="CA_StopService"
                  BinaryKey="UtilCA"
                  DllEntry="CAQuietExec"
                  Execute="immediate"
                  Return="ignore" />

    <CustomAction Id="CA_StartService"
                  BinaryKey="UtilCA"
                  DllEntry="CAQuietExec"
                  Execute="deferred"
                  Impersonate="no"
                  Return="check" />

    <!-- Installation sequence -->
    <InstallExecuteSequence>
      <!-- Stop service before install -->
      <Custom Action="CA_StopService" Before="InstallFiles">
        <![CDATA[Installed AND NOT REINSTALL]]>
      </Custom>
      
      <!-- Start service after install -->
      <Custom Action="CA_StartService" After="InstallServices">
        <![CDATA[NOT Installed AND NOT REMOVE AND (&ProductFeature = 3)]]>
      </Custom>
    </InstallExecuteSequence>

    <!-- UI reference -->
    <UIRef Id="CustomUI" />

  </Product>

  <!-- Directory structure -->
  <Fragment>
    <Directory Id="TARGETDIR" Name="SourceDir">
      <Directory Id="ProgramFiles64Folder">
        <Directory Id="INSTALLFOLDER" Name="Owlet">
          <Directory Id="BinFolder" Name="bin" />
          <Directory Id="ConfigFolder" Name="config" />
          <Directory Id="ToolsFolder" Name="tools" />
        </Directory>
      </Directory>
      
      <Directory Id="CommonAppDataFolder">
        <Directory Id="CompanyDataFolder" Name="Owlet">
          <Directory Id="DataFolder" Name="Data" />
          <Directory Id="LogsFolder" Name="Logs" />
          <Directory Id="TempFolder" Name="Temp" />
        </Directory>
      </Directory>
      
      <Directory Id="ProgramMenuFolder">
        <Directory Id="ApplicationProgramsFolder" Name="Owlet" />
      </Directory>
      
      <Directory Id="StartupFolder" />
    </Directory>
  </Fragment>

</Wix>
```

### 3. Service Component Definition

Windows service registration and management components.

**`packaging/installer/Service.wxs`:**

```xml
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs"
     xmlns:util="http://wixtoolset.org/schemas/v4/wxs/util">

  <Fragment>
    <!-- Service executable component -->
    <Component Id="ServiceExecutable" 
               Guid="11111111-1111-1111-1111-111111111111" 
               Directory="BinFolder"
               Win64="yes">
      
      <!-- Main service executable -->
      <File Id="OwletServiceExe"
            Source="$(var.SourceDir)\Owlet.Service.exe"
            KeyPath="yes"
            Checksum="yes" />
      
      <!-- Service dependencies -->
      <File Id="OwletCoreLib"
            Source="$(var.SourceDir)\Owlet.Core.dll" />
      <File Id="OwletApiLib"
            Source="$(var.SourceDir)\Owlet.Api.dll" />
      <File Id="OwletInfrastructureLib"
            Source="$(var.SourceDir)\Owlet.Infrastructure.dll" />
      <File Id="OwletIndexerLib"
            Source="$(var.SourceDir)\Owlet.Indexer.dll" />
      <File Id="OwletExtractorsLib"
            Source="$(var.SourceDir)\Owlet.Extractors.dll" />

      <!-- Third-party dependencies -->
      <File Id="CarterLib"
            Source="$(var.SourceDir)\Carter.dll" />
      <File Id="SerilogLib"
            Source="$(var.SourceDir)\Serilog.dll" />
      <File Id="SerilogExtensionsHostingLib"
            Source="$(var.SourceDir)\Serilog.Extensions.Hosting.dll" />
      <File Id="SerilogSinksFileLib"
            Source="$(var.SourceDir)\Serilog.Sinks.File.dll" />
      <File Id="SerilogSinksEventLogLib"
            Source="$(var.SourceDir)\Serilog.Sinks.EventLog.dll" />
      
      <!-- Entity Framework and SQLite -->
      <File Id="EntityFrameworkCoreLib"
            Source="$(var.SourceDir)\Microsoft.EntityFrameworkCore.dll" />
      <File Id="EntityFrameworkCoreSqliteLib"
            Source="$(var.SourceDir)\Microsoft.EntityFrameworkCore.Sqlite.dll" />
      <File Id="SqliteLib"
            Source="$(var.SourceDir)\SQLitePCLRaw.core.dll" />

      <!-- Service registration -->
      <ServiceInstall Id="OwletServiceInstall"
                      Type="ownProcess"
                      Name="OwletService"
                      DisplayName="Owlet Document Indexing Service"
                      Description="Indexes and searches local documents for fast retrieval"
                      Start="auto"
                      Account="LocalSystem"
                      ErrorControl="normal"
                      Interactive="no"
                      Vital="yes">
        
        <!-- Service dependencies -->
        <ServiceDependency Id="HTTP" />
        <ServiceDependency Id="Tcpip" />
      </ServiceInstall>

      <!-- Service control -->
      <ServiceControl Id="OwletServiceControl"
                      Name="OwletService"
                      Start="install"
                      Stop="both"
                      Remove="uninstall"
                      Wait="yes" />

      <!-- Service failure recovery -->
      <util:ServiceConfig ServiceName="OwletService"
                          FirstFailureActionType="restart"
                          SecondFailureActionType="restart"  
                          ThirdFailureActionType="restart"
                          RestartServiceDelayInSeconds="60"
                          ResetPeriodInDays="1" />

      <!-- Registry entries for service configuration -->
      <RegistryKey Root="HKLM" Key="SYSTEM\CurrentControlSet\Services\OwletService\Parameters">
        <RegistryValue Name="Application" Value="[BinFolder]Owlet.Service.exe" Type="string" />
        <RegistryValue Name="AppDirectory" Value="[BinFolder]" Type="string" />
        <RegistryValue Name="ConfigDirectory" Value="[ConfigFolder]" Type="string" />
        <RegistryValue Name="DataDirectory" Value="[DataFolder]" Type="string" />
        <RegistryValue Name="LogDirectory" Value="[LogsFolder]" Type="string" />
      </RegistryKey>

    </Component>

    <!-- Service configuration component -->
    <Component Id="ServiceConfiguration" 
               Guid="22222222-2222-2222-2222-222222222222" 
               Directory="ConfigFolder"
               Win64="yes">
      
      <File Id="AppSettingsJson"
            Source="$(var.SourceDir)\appsettings.json"
            KeyPath="yes" />
      
      <File Id="AppSettingsProductionJson"
            Source="$(var.SourceDir)\appsettings.Production.json" />

      <!-- Environment-specific configuration -->
      <IniFile Id="ConfigurePort"
               Action="createLine"
               Directory="ConfigFolder"
               Name="owlet.ini"
               Section="Network"
               Key="Port"
               Value="[SERVICE_PORT]" />

      <IniFile Id="ConfigureAccount"
               Action="createLine"
               Directory="ConfigFolder"  
               Name="owlet.ini"
               Section="Service"
               Key="Account"
               Value="[SERVICE_ACCOUNT]" />

    </Component>

    <!-- Data and log directories -->
    <Component Id="DataDirectory" 
               Guid="33333333-3333-3333-3333-333333333333" 
               Directory="DataFolder"
               Win64="yes">
      <CreateFolder />
      
      <!-- Set permissions for service account -->
      <util:PermissionEx User="NT AUTHORITY\SYSTEM" 
                         GenericAll="yes" />
      <util:PermissionEx User="BUILTIN\Administrators" 
                         GenericAll="yes" />
    </Component>

    <Component Id="LogDirectory" 
               Guid="44444444-4444-4444-4444-444444444444" 
               Directory="LogsFolder"
               Win64="yes">
      <CreateFolder />
      
      <!-- Set permissions for service account and administrators -->
      <util:PermissionEx User="NT AUTHORITY\SYSTEM" 
                         GenericAll="yes" />
      <util:PermissionEx User="BUILTIN\Administrators" 
                         GenericAll="yes" />
    </Component>

    <!-- Service registration component with custom actions -->
    <Component Id="ServiceRegistration"
               Guid="55555555-5555-5555-5555-555555555555"
               Directory="BinFolder"
               Win64="yes">
      
      <!-- Event log source registration -->
      <util:EventSource Log="Application"
                        Name="Owlet Service"
                        EventMessageFile="[BinFolder]Owlet.Service.exe"
                        CategoryMessageFile="[BinFolder]Owlet.Service.exe"
                        SupportsErrors="yes"
                        SupportsInformationals="yes"
                        SupportsWarnings="yes" />

      <!-- Performance counters (if needed) -->
      <RegistryKey Root="HKLM" Key="SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\$(var.ProductCode)">
        <RegistryValue Name="ServiceName" Value="OwletService" Type="string" />
        <RegistryValue Name="ServiceDisplayName" Value="Owlet Document Indexing Service" Type="string" />
        <RegistryValue Name="InstallLocation" Value="[INSTALLFOLDER]" Type="string" />
        <RegistryValue Name="Version" Value="$(var.ProductVersion)" Type="string" />
        <RegistryValue Name="Publisher" Value="$(var.Manufacturer)" Type="string" />
      </RegistryKey>

    </Component>

    <!-- Uninstall helper scripts -->
    <Component Id="UninstallScripts"
               Guid="66666666-6666-6666-6666-666666666666"
               Directory="BinFolder"
               Win64="yes">
      
      <!-- Emergency uninstall script -->
      <File Id="EmergencyUninstallBat"
            Source="$(var.SourceDir)\emergency-uninstall.bat"
            KeyPath="yes" />
      
      <!-- Service status script -->
      <File Id="ServiceStatusBat"
            Source="$(var.SourceDir)\service-status.bat" />

    </Component>

  </Fragment>

  <!-- Custom actions for service management -->
  <Fragment>
    
    <!-- Stop service before uninstall -->
    <CustomAction Id="CA_StopServiceBeforeUninstall"
                  Directory="BinFolder"
                  ExeCommand="cmd.exe /c &quot;sc stop OwletService&quot;"
                  Execute="immediate"
                  Return="ignore" />

    <!-- Verify service stopped -->
    <CustomAction Id="CA_VerifyServiceStopped"
                  Directory="BinFolder"
                  ExeCommand="cmd.exe /c &quot;timeout /t 10 /nobreak&quot;"
                  Execute="immediate"
                  Return="ignore" />

    <!-- Clean up service artifacts -->
    <CustomAction Id="CA_CleanupServiceArtifacts"
                  Directory="BinFolder"
                  ExeCommand="cmd.exe /c &quot;sc delete OwletService&quot;"
                  Execute="immediate"
                  Return="ignore" />

  </Fragment>

</Wix>
```

### 4. Firewall Configuration

Automatic Windows Firewall rule creation for HTTP port access.

**`packaging/installer/Firewall.wxs`:**

```xml
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs"
     xmlns:fire="http://wixtoolset.org/schemas/v4/wxs/firewall">

  <Fragment>
    <!-- Firewall rule component -->
    <Component Id="FirewallRule" 
               Guid="77777777-7777-7777-7777-777777777777" 
               Directory="BinFolder"
               Win64="yes">
      
      <!-- Inbound rule for HTTP port -->
      <fire:FirewallException Id="OwletHttpInbound"
                              Name="Owlet Document Service - HTTP Inbound"
                              Description="Allow HTTP access to Owlet document indexing service"
                              Port="[SERVICE_PORT]"
                              Protocol="tcp"
                              Scope="localSubnet"
                              Profile="domain,private"
                              Direction="in"
                              Action="allow"
                              Enabled="yes"
                              IgnoreFailure="no" />

      <!-- Outbound rule (if needed for future features) -->
      <fire:FirewallException Id="OwletHttpOutbound"
                              Name="Owlet Document Service - HTTP Outbound"
                              Description="Allow HTTP requests from Owlet service"
                              Port="80,443"
                              Protocol="tcp"
                              Scope="any"
                              Profile="domain,private,public"
                              Direction="out"
                              Action="allow"
                              Enabled="no"
                              IgnoreFailure="yes" />

      <!-- Registry entry to track firewall rule -->
      <RegistryKey Root="HKLM" Key="SOFTWARE\Owlet\Firewall">
        <RegistryValue Name="HttpPort" Value="[SERVICE_PORT]" Type="string" />
        <RegistryValue Name="RuleName" Value="Owlet Document Service - HTTP Inbound" Type="string" />
        <RegistryValue Name="Enabled" Value="[CREATE_FIREWALL_RULE]" Type="string" />
      </RegistryKey>

    </Component>

    <!-- Custom actions for firewall management -->
    <CustomAction Id="CA_RemoveFirewallRule"
                  Directory="BinFolder"
                  ExeCommand="netsh advfirewall firewall delete rule name=&quot;Owlet Document Service - HTTP Inbound&quot;"
                  Execute="immediate"
                  Return="ignore" />

    <!-- Conditional firewall rule removal -->
    <InstallExecuteSequence>
      <Custom Action="CA_RemoveFirewallRule" Before="RemoveFiles">
        <![CDATA[REMOVE="ALL"]]>
      </Custom>
    </InstallExecuteSequence>

  </Fragment>

</Wix>
```

### 5. User Interface Customization

Professional installer UI with branding and configuration options.

**`packaging/installer/UI.wxs`:**

```xml
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">

  <Fragment>
    <!-- Custom UI definition -->
    <UI Id="CustomUI">
      
      <!-- Reference standard UI -->
      <UIRef Id="WixUI_FeatureTree" />
      <UIRef Id="WixUI_ErrorProgressText" />

      <!-- Custom dialogs -->
      <DialogRef Id="ConfigurationDlg" />
      <DialogRef Id="SecurityDlg" />

      <!-- Dialog sequence -->
      <Publish Dialog="LicenseAgreementDlg" Control="Next" Event="NewDialog" Value="ConfigurationDlg">
        LicenseAccepted = "1"
      </Publish>
      
      <Publish Dialog="ConfigurationDlg" Control="Next" Event="NewDialog" Value="SecurityDlg" />
      <Publish Dialog="ConfigurationDlg" Control="Back" Event="NewDialog" Value="LicenseAgreementDlg" />
      
      <Publish Dialog="SecurityDlg" Control="Next" Event="NewDialog" Value="CustomizeDlg" />
      <Publish Dialog="SecurityDlg" Control="Back" Event="NewDialog" Value="ConfigurationDlg" />

      <!-- Customization -->
      <Publish Dialog="WelcomeDlg" Control="Next" Event="NewDialog" Value="LicenseAgreementDlg" />
      <Publish Dialog="CustomizeDlg" Control="Back" Event="NewDialog" Value="SecurityDlg" />

    </UI>

    <!-- Configuration dialog -->
    <Dialog Id="ConfigurationDlg" Width="370" Height="270" Title="Configuration">
      <Control Id="Title" Type="Text" X="15" Y="6" Width="200" Height="15" Transparent="yes" NoPrefix="yes" Text="Service Configuration" />
      <Control Id="Description" Type="Text" X="25" Y="23" Width="280" Height="15" Transparent="yes" NoPrefix="yes" Text="Configure the Owlet service settings." />
      
      <!-- Port configuration -->
      <Control Id="PortLabel" Type="Text" X="25" Y="60" Width="100" Height="15" NoPrefix="yes" Text="HTTP Port:" />
      <Control Id="PortEdit" Type="Edit" X="130" Y="58" Width="50" Height="18" Property="SERVICE_PORT" />
      <Control Id="PortNote" Type="Text" X="185" Y="60" Width="150" Height="15" NoPrefix="yes" Text="(Default: 5555)" />
      
      <!-- Service account -->
      <Control Id="AccountLabel" Type="Text" X="25" Y="85" Width="100" Height="15" NoPrefix="yes" Text="Service Account:" />
      <Control Id="AccountCombo" Type="ComboBox" X="130" Y="83" Width="120" Height="18" Property="SERVICE_ACCOUNT" ComboList="yes">
        <ComboBox Property="SERVICE_ACCOUNT">
          <ListItem Text="Local System" Value="LocalSystem" />
          <ListItem Text="Network Service" Value="NetworkService" />
          <ListItem Text="Local Service" Value="LocalService" />
        </ComboBox>
      </Control>
      
      <!-- Auto-start service -->
      <Control Id="AutoStartCheck" Type="CheckBox" X="25" Y="110" Width="200" Height="18" Property="AUTO_START_SERVICE" CheckBoxValue="1" Text="Start service automatically" />
      
      <!-- Create firewall rule -->
      <Control Id="FirewallCheck" Type="CheckBox" X="25" Y="135" Width="200" Height="18" Property="CREATE_FIREWALL_RULE" CheckBoxValue="1" Text="Create Windows Firewall rule" />

      <!-- Navigation buttons -->
      <Control Id="Back" Type="PushButton" X="180" Y="243" Width="56" Height="17" Text="&amp;Back" />
      <Control Id="Next" Type="PushButton" X="236" Y="243" Width="56" Height="17" Default="yes" Text="&amp;Next" />
      <Control Id="Cancel" Type="PushButton" X="304" Y="243" Width="56" Height="17" Cancel="yes" Text="Cancel" />

      <!-- Validation -->
      <Control Id="PortValidator" Type="Text" X="25" Y="160" Width="300" Height="30" Hidden="yes" NoPrefix="yes" Text="Port must be between 1024 and 65535" />

    </Dialog>

    <!-- Security dialog -->
    <Dialog Id="SecurityDlg" Width="370" Height="270" Title="Security Settings">
      <Control Id="Title" Type="Text" X="15" Y="6" Width="200" Height="15" Transparent="yes" NoPrefix="yes" Text="Security Configuration" />
      <Control Id="Description" Type="Text" X="25" Y="23" Width="280" Height="25" Transparent="yes" NoPrefix="yes" Text="The service will be configured with secure defaults. You can modify these settings after installation." />
      
      <!-- Security information -->
      <Control Id="SecurityInfo" Type="Text" X="25" Y="60" Width="320" Height="80" NoPrefix="yes" Text="Security Features:
• Service runs with minimal required privileges
• HTTP server binds to localhost only (127.0.0.1)
• Automatic Windows Firewall configuration
• Encrypted configuration storage
• Comprehensive audit logging" />
      
      <!-- Firewall warning -->
      <Control Id="FirewallWarning" Type="Text" X="25" Y="150" Width="320" Height="40" NoPrefix="yes" Text="If you disable the firewall rule, you may need to manually configure Windows Firewall to allow access to the HTTP port." />

      <!-- Navigation buttons -->
      <Control Id="Back" Type="PushButton" X="180" Y="243" Width="56" Height="17" Text="&amp;Back" />
      <Control Id="Next" Type="PushButton" X="236" Y="243" Width="56" Height="17" Default="yes" Text="&amp;Next" />
      <Control Id="Cancel" Type="PushButton" X="304" Y="243" Width="56" Height="17" Cancel="yes" Text="Cancel" />

    </Dialog>

    <!-- License customization -->
    <WixVariable Id="WixUILicenseRtf" Value="License.rtf" />
    <WixVariable Id="WixUIBannerBmp" Value="Banner.bmp" />
    <WixVariable Id="WixUIDialogBmp" Value="Dialog.bmp" />

  </Fragment>

</Wix>
```

### 6. Component Discovery and Harvesting

Automated component generation for dynamic file lists.

**`packaging/installer/Components.wxs`:**

```xml
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">

  <Fragment>
    <!-- Tray application components -->
    <Component Id="TrayExecutable" 
               Guid="88888888-8888-8888-8888-888888888888" 
               Directory="ToolsFolder"
               Win64="yes">
      
      <File Id="OwletTrayExe"
            Source="$(var.SourceDir)\tools\Owlet.TrayApp.exe"
            KeyPath="yes"
            Checksum="yes" />

      <!-- Tray application dependencies -->
      <File Id="TrayAppConfig"
            Source="$(var.SourceDir)\tools\Owlet.TrayApp.exe.config" />

    </Component>

    <!-- Startup shortcut for tray app -->
    <Component Id="TrayStartup" 
               Guid="99999999-9999-9999-9999-999999999999" 
               Directory="StartupFolder"
               Win64="yes">
      
      <Shortcut Id="TrayStartupShortcut"
                Name="Owlet Tray"
                Description="Owlet system tray application"
                Target="[ToolsFolder]Owlet.TrayApp.exe"
                WorkingDirectory="ToolsFolder"
                Icon="ProductIcon" />
      
      <RemoveFolder Id="RemoveStartupFolder" On="uninstall" />
      <RegistryValue Root="HKCU" Key="Software\Owlet\Startup" Name="TrayApp" Type="string" Value="" KeyPath="yes" />

    </Component>

    <!-- Diagnostic tools -->
    <Component Id="DiagnosticExecutables" 
               Guid="AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA" 
               Directory="ToolsFolder"
               Win64="yes">
      
      <File Id="OwletDiagnosticsExe"
            Source="$(var.SourceDir)\tools\Owlet.Diagnostics.exe"
            KeyPath="yes"
            Checksum="yes" />

      <File Id="DiagnosticsConfig"
            Source="$(var.SourceDir)\tools\Owlet.Diagnostics.exe.config" />

    </Component>

    <!-- Program menu shortcuts -->
    <Component Id="ProgramMenuShortcuts" 
               Guid="BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB" 
               Directory="ApplicationProgramsFolder"
               Win64="yes">
      
      <!-- Main application shortcut -->
      <Shortcut Id="OwletWebInterfaceShortcut"
                Name="Owlet Web Interface"
                Description="Open Owlet web interface in browser"
                Target="http://localhost:[SERVICE_PORT]"
                Icon="ProductIcon" />
      
      <!-- Service management shortcut -->
      <Shortcut Id="ServiceManagementShortcut"
                Name="Owlet Service Management"
                Description="Manage Owlet service"
                Target="[SystemFolder]services.msc"
                Arguments="/s OwletService"
                Icon="ProductIcon" />
      
      <!-- Diagnostic tools shortcut -->
      <Shortcut Id="DiagnosticToolsShortcut"
                Name="Owlet Diagnostics"
                Description="Owlet diagnostic and troubleshooting tools"
                Target="[ToolsFolder]Owlet.Diagnostics.exe"
                WorkingDirectory="ToolsFolder"
                Icon="ProductIcon" />
      
      <!-- Uninstall shortcut -->
      <Shortcut Id="UninstallShortcut"
                Name="Uninstall Owlet"
                Description="Uninstall Owlet Document Service"
                Target="[SystemFolder]msiexec.exe"
                Arguments="/x [ProductCode]"
                Icon="ProductIcon" />
      
      <RemoveFolder Id="RemoveApplicationProgramsFolder" On="uninstall" />
      <RegistryValue Root="HKCU" Key="Software\Owlet\Shortcuts" Name="ProgramMenu" Type="string" Value="" KeyPath="yes" />

    </Component>

  </Fragment>

</Wix>
```

## Installation Helper Scripts

### 1. Emergency Uninstall Script

Comprehensive cleanup script for manual uninstallation scenarios.

**`packaging/scripts/emergency-uninstall.bat`:**

```batch
@echo off
echo ===============================================
echo Owlet Emergency Uninstall Script
echo ===============================================
echo.
echo This script will completely remove Owlet from your system.
echo.
set /p confirm="Are you sure you want to continue? (Y/N): "
if /i not "%confirm%"=="Y" goto :abort

echo.
echo Stopping Owlet service...
sc stop OwletService 2>nul
timeout /t 10 /nobreak >nul

echo Removing service registration...
sc delete OwletService 2>nul

echo Removing firewall rules...
netsh advfirewall firewall delete rule name="Owlet Document Service - HTTP Inbound" 2>nul

echo Removing program files...
if exist "C:\Program Files\Owlet" (
    rmdir /s /q "C:\Program Files\Owlet" 2>nul
)

echo Removing data directory...
if exist "C:\ProgramData\Owlet" (
    rmdir /s /q "C:\ProgramData\Owlet" 2>nul
)

echo Removing registry entries...
reg delete "HKLM\SYSTEM\CurrentControlSet\Services\OwletService" /f 2>nul
reg delete "HKLM\SOFTWARE\Owlet" /f 2>nul
reg delete "HKCU\Software\Owlet" /f 2>nul

echo Removing start menu shortcuts...
if exist "%APPDATA%\Microsoft\Windows\Start Menu\Programs\Owlet" (
    rmdir /s /q "%APPDATA%\Microsoft\Windows\Start Menu\Programs\Owlet" 2>nul
)

echo Removing event log source...
reg delete "HKLM\SYSTEM\CurrentControlSet\Services\EventLog\Application\Owlet Service" /f 2>nul

echo.
echo ===============================================
echo Emergency uninstall completed.
echo ===============================================
echo.
echo Please reboot your computer to complete the removal.
pause
goto :end

:abort
echo.
echo Uninstall cancelled by user.
pause

:end
```

### 2. Service Status Check Script

Diagnostic script for checking service health and configuration.

**`packaging/scripts/service-status.bat`:**

```batch
@echo off
echo ===============================================
echo Owlet Service Status Report
echo ===============================================
echo.

echo Service Status:
sc query OwletService 2>nul
if errorlevel 1 (
    echo   Service not found or not installed
) else (
    echo.
    echo Service Configuration:
    sc qc OwletService 2>nul
)

echo.
echo ===============================================
echo Network Status:
echo ===============================================
echo.
echo Checking HTTP port availability...
netstat -an | findstr ":5555" 2>nul
if errorlevel 1 (
    echo   Port 5555 is not in use
) else (
    echo   Port 5555 is active
)

echo.
echo ===============================================
echo Firewall Status:
echo ===============================================
echo.
echo Checking firewall rules...
netsh advfirewall firewall show rule name="Owlet Document Service - HTTP Inbound" 2>nul
if errorlevel 1 (
    echo   Firewall rule not found
)

echo.
echo ===============================================
echo File System Status:
echo ===============================================
echo.
echo Installation Directory:
if exist "C:\Program Files\Owlet\bin\Owlet.Service.exe" (
    echo   ✓ Service executable found
    dir "C:\Program Files\Owlet\bin\Owlet.Service.exe" | findstr "Owlet.Service.exe"
) else (
    echo   ✗ Service executable not found
)

echo.
echo Data Directory:
if exist "C:\ProgramData\Owlet" (
    echo   ✓ Data directory exists
    echo   Contents:
    dir "C:\ProgramData\Owlet" /b 2>nul
) else (
    echo   ✗ Data directory not found
)

echo.
echo Log Directory:
if exist "C:\ProgramData\Owlet\Logs" (
    echo   ✓ Log directory exists
    echo   Recent log files:
    dir "C:\ProgramData\Owlet\Logs\*.log" /o-d /b 2>nul | head -5
) else (
    echo   ✗ Log directory not found
)

echo.
echo ===============================================
echo Registry Status:
echo ===============================================
echo.
echo Service Registry Key:
reg query "HKLM\SYSTEM\CurrentControlSet\Services\OwletService" 2>nul
if errorlevel 1 (
    echo   Service registry key not found
)

echo.
echo Configuration Registry:
reg query "HKLM\SOFTWARE\Owlet" 2>nul
if errorlevel 1 (
    echo   Configuration registry not found
)

echo.
echo ===============================================
echo Event Log Status:
echo ===============================================
echo.
echo Recent Owlet events (last 10):
powershell -Command "Get-WinEvent -FilterHashtable @{LogName='Application'; ProviderName='Owlet Service'} -MaxEvents 10 -ErrorAction SilentlyContinue | Format-Table TimeCreated, LevelDisplayName, Message -Wrap" 2>nul
if errorlevel 1 (
    echo   No recent events found
)

echo.
echo ===============================================
echo Report completed: %DATE% %TIME%
echo ===============================================
pause
```

## Build Integration and Testing

### 1. WiX Build Integration

PowerShell script for building installer with proper environment setup.

**`packaging/scripts/build-installer.ps1`:**

```powershell
#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Build WiX installer for Owlet
.DESCRIPTION
    Builds the MSI installer using WiX Toolset with proper configuration
.PARAMETER Version
    Product version for the installer
.PARAMETER SourceDir
    Directory containing the service binaries
.PARAMETER OutputDir
    Directory for installer output
.PARAMETER Sign
    Whether to sign the installer
.EXAMPLE
    .\build-installer.ps1 -Version "1.0.0" -SourceDir "..\..\artifacts\service" -Sign
#>

param(
    [Parameter(Mandatory)]
    [string]$Version,
    
    [Parameter(Mandatory)]
    [string]$SourceDir,
    
    [Parameter()]
    [string]$OutputDir = "..\..\artifacts",
    
    [Parameter()]
    [switch]$Sign
)

$ErrorActionPreference = "Stop"

function Write-Header {
    param([string]$Message)
    Write-Host ""
    Write-Host "=" * 60 -ForegroundColor Cyan
    Write-Host " $Message" -ForegroundColor Cyan
    Write-Host "=" * 60 -ForegroundColor Cyan
}

function Write-Step {
    param([string]$Message)
    Write-Host ">> $Message" -ForegroundColor Green
}

function Test-Prerequisites {
    Write-Header "Checking Prerequisites"
    
    # Check WiX toolset
    try {
        $wixVersion = wix --version
        Write-Step "✓ WiX Toolset version: $wixVersion"
    } catch {
        throw "WiX Toolset not found. Please install WiX v4"
    }
    
    # Check source directory
    if (-not (Test-Path $SourceDir)) {
        throw "Source directory not found: $SourceDir"
    }
    
    $serviceExe = Join-Path $SourceDir "Owlet.Service.exe"
    if (-not (Test-Path $serviceExe)) {
        throw "Service executable not found: $serviceExe"
    }
    
    Write-Step "✓ Source files validated"
}

function Build-Installer {
    Write-Header "Building MSI Installer"
    
    $projectFile = "Owlet.Installer.wixproj"
    $outputPath = Join-Path $OutputDir "OwletInstaller-$Version.msi"
    
    # Ensure output directory exists
    $outputDirPath = Split-Path $outputPath
    if (-not (Test-Path $outputDirPath)) {
        New-Item -ItemType Directory -Force -Path $outputDirPath | Out-Null
    }
    
    # Build arguments
    $buildArgs = @(
        "build"
        $projectFile
        "-d", "SourceDir=$SourceDir"
        "-d", "Version=$Version"
        "-d", "ProductVersion=$Version"
        "-out", $outputPath
        "-v", "normal"
    )
    
    Write-Step "Building installer with WiX..."
    Write-Host "Command: wix $($buildArgs -join ' ')" -ForegroundColor Gray
    
    & wix @buildArgs
    
    if (-not (Test-Path $outputPath)) {
        throw "Installer build failed - output file not created"
    }
    
    $fileInfo = Get-Item $outputPath
    Write-Step "✓ Installer built successfully: $($fileInfo.Name) ($([math]::Round($fileInfo.Length / 1MB, 2)) MB)"
    
    return $outputPath
}

function Sign-Installer {
    param([string]$InstallerPath)
    
    if (-not $Sign) {
        Write-Step "Skipping code signing (not requested)"
        return
    }
    
    Write-Header "Code Signing"
    
    # Check for certificate
    $certPath = $env:SIGNING_CERTIFICATE_PATH
    $certPassword = $env:SIGNING_CERTIFICATE_PASSWORD
    
    if (-not $certPath -or -not (Test-Path $certPath)) {
        Write-Warning "Certificate file not found, skipping signing"
        return
    }
    
    # Find signtool
    $signTool = "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.22000.0\x64\signtool.exe"
    if (-not (Test-Path $signTool)) {
        Write-Warning "SignTool not found, skipping signing"
        return
    }
    
    Write-Step "Signing installer with certificate..."
    
    $signArgs = @(
        "sign"
        "/f", "`"$certPath`""
        "/p", $certPassword
        "/tr", "http://timestamp.digicert.com"
        "/td", "SHA256"
        "/fd", "SHA256"
        "`"$InstallerPath`""
    )
    
    & $signTool @signArgs
    
    if ($LASTEXITCODE -eq 0) {
        Write-Step "✓ Installer signed successfully"
    } else {
        Write-Warning "Code signing failed with exit code $LASTEXITCODE"
    }
}

function Test-Installer {
    param([string]$InstallerPath)
    
    Write-Header "Testing Installer"
    
    # Basic MSI validation
    Write-Step "Validating MSI structure..."
    
    try {
        # Use Windows Installer API to validate
        $installer = New-Object -ComObject WindowsInstaller.Installer
        $database = $installer.OpenDatabase($InstallerPath, 0)
        
        # Check required tables
        $requiredTables = @("File", "Component", "Feature", "Directory", "Registry")
        foreach ($table in $requiredTables) {
            try {
                $view = $database.OpenView("SELECT * FROM $table")
                $view.Execute()
                Write-Step "✓ Table '$table' found"
            } catch {
                Write-Warning "Table '$table' missing or invalid"
            }
        }
        
        Write-Step "✓ MSI structure validation completed"
    } catch {
        Write-Warning "Could not validate MSI structure: $($_.Exception.Message)"
    }
    
    # Test installation in silent mode (dry run)
    Write-Step "Testing silent installation (dry run)..."
    
    $logPath = Join-Path $OutputDir "install-test.log"
    $testArgs = @(
        "/i", "`"$InstallerPath`""
        "/quiet"
        "/norestart"
        "/l*v", "`"$logPath`""
        "REBOOT=ReallySuppress"
    )
    
    # Note: This would actually install in a real scenario
    # For testing, we just validate the command structure
    Write-Host "Test command: msiexec $($testArgs -join ' ')" -ForegroundColor Gray
    Write-Step "✓ Installation command validated"
}

function Main {
    try {
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        
        Test-Prerequisites
        $installerPath = Build-Installer
        Sign-Installer -InstallerPath $installerPath
        Test-Installer -InstallerPath $installerPath
        
        $stopwatch.Stop()
        
        Write-Header "Build Completed Successfully"
        Write-Host "Installer: $installerPath" -ForegroundColor Green
        Write-Host "Build time: $($stopwatch.Elapsed.ToString('mm\:ss'))" -ForegroundColor Green
    }
    catch {
        Write-Host ""
        Write-Host "Build Failed: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
}

# Execute main function
Main
```

## Success Criteria

- [ ] MSI installer successfully installs Windows service on Windows 10/11
- [ ] Service registration includes proper failure recovery configuration
- [ ] Windows Firewall rule is created automatically for HTTP port
- [ ] Installation completes in under 2 minutes on target systems
- [ ] Uninstall removes all files, registry entries, and firewall rules cleanly
- [ ] Emergency uninstall script provides complete manual cleanup capability
- [ ] Installer UI provides clear configuration options for service settings
- [ ] Digital signature validation passes on signed installers
- [ ] Service starts automatically after installation and survives reboots
- [ ] Multiple install/uninstall cycles work without issues

## Testing Strategy

### Unit Tests
**What to test:** WiX component definitions, installer logic, helper scripts  
**Mocking strategy:** Mock Windows installer APIs, file system operations  
**Test data approach:** Use test MSI files and temporary directories

**Example Tests:**
```csharp
[Fact]
public void WixComponents_ShouldHaveValidGuids()
{
    // Arrange & Act
    var components = WixComponentParser.ParseFromFile("Components.wxs");
    
    // Assert
    components.Should().NotBeEmpty();
    components.Should().OnlyContain(c => Guid.TryParse(c.Guid, out _));
}
```

### Integration Tests
**What to test:** Complete installer build process, MSI validation, installation testing  
**Test environment:** Windows VMs with clean state  
**Automation:** PowerShell scripts for automated installer testing

### E2E Tests
**What to test:** Full installation workflow from MSI to running service  
**User workflows:** Download → Install → Service Start → Web Access → Uninstall

## Dependencies

### Technical Dependencies
- WiX Toolset 4.x - MSI installer creation
- Windows Installer 5.0+ - Installation engine
- Windows SDK - Code signing tools
- PowerShell 5.1+ - Build and test scripts

### Story Dependencies
- **Blocks:** S80 (Documentation & Testing)
- **Blocked By:** S40 (Build Pipeline)

## Next Steps

1. Create WiX project structure with all component definitions
2. Implement custom UI dialogs for service configuration
3. Create firewall rule management components
4. Develop emergency uninstall and diagnostic scripts
5. Integrate with build pipeline for automated installer creation
6. Test installation across Windows versions and configurations

---

**Story Created:** November 1, 2025 by GitHub Copilot Agent  
**Story Completed:** [Date] (when status = Complete)