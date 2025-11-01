using FluentAssertions;
using Microsoft.Extensions.Options;
using Owlet.Core.Configuration;
using Xunit;

namespace Owlet.Core.Tests.Configuration;

public class NetworkConfigurationValidatorTests
{
    private readonly NetworkConfigurationValidator _validator = new();

    [Fact]
    public void Validate_WithValidConfiguration_ReturnsSuccess()
    {
        // Arrange
        var config = new NetworkConfiguration
        {
            Port = 5555,
            BindAddress = "127.0.0.1",
            EnableHttps = false,
            MaxRequestBodySize = 10 * 1024 * 1024,
            RequestTimeout = TimeSpan.FromMinutes(2),
            EnableCompression = true
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(1023)] // Below minimum
    [InlineData(80)]   // Privileged port
    [InlineData(443)]  // Privileged port
    [InlineData(0)]    // Invalid
    [InlineData(65536)] // Above maximum
    [InlineData(70000)] // Above maximum
    public void Validate_WithInvalidPort_ReturnsFailure(int port)
    {
        // Arrange
        var config = new NetworkConfiguration
        {
            Port = port,
            BindAddress = "127.0.0.1"
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("Port") && f.Contains("between 1024 and 65535"));
    }

    [Theory]
    [InlineData(1024)]
    [InlineData(5555)]
    [InlineData(8080)]
    [InlineData(65535)]
    public void Validate_WithValidPort_ReturnsSuccess(int port)
    {
        // Arrange
        var config = new NetworkConfiguration
        {
            Port = port,
            BindAddress = "127.0.0.1"
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WithEmptyBindAddress_ReturnsFailure(string? bindAddress)
    {
        // Arrange
        var config = new NetworkConfiguration
        {
            Port = 5555,
            BindAddress = bindAddress!
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("BindAddress"));
    }

    [Fact]
    public void Validate_WithHttpsEnabledButNoCertificate_ReturnsFailure()
    {
        // Arrange
        var config = new NetworkConfiguration
        {
            Port = 5555,
            BindAddress = "127.0.0.1",
            EnableHttps = true,
            CertificatePath = null
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("CertificatePath") && f.Contains("required when EnableHttps is true"));
    }

    [Fact]
    public void Validate_WithHttpsEnabledAndEmptyCertificate_ReturnsFailure()
    {
        // Arrange
        var config = new NetworkConfiguration
        {
            Port = 5555,
            BindAddress = "127.0.0.1",
            EnableHttps = true,
            CertificatePath = ""
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("CertificatePath"));
    }

    [Fact]
    public void Validate_WithHttpsEnabledButCertificateNotFound_ReturnsFailure()
    {
        // Arrange
        var config = new NetworkConfiguration
        {
            Port = 5555,
            BindAddress = "127.0.0.1",
            EnableHttps = true,
            CertificatePath = @"C:\NonExistent\cert.pfx"
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("Certificate file not found"));
    }

    [Fact]
    public void Validate_WithHttpsDisabled_DoesNotRequireCertificate()
    {
        // Arrange
        var config = new NetworkConfiguration
        {
            Port = 5555,
            BindAddress = "127.0.0.1",
            EnableHttps = false,
            CertificatePath = null
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(1023)] // Below 1KB
    [InlineData(512)]
    [InlineData(0)]
    public void Validate_WithMaxRequestBodySizeTooSmall_ReturnsFailure(long size)
    {
        // Arrange
        var config = new NetworkConfiguration
        {
            Port = 5555,
            BindAddress = "127.0.0.1",
            MaxRequestBodySize = size
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("MaxRequestBodySize") && f.Contains("at least 1024 bytes"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithInvalidRequestTimeout_ReturnsFailure(int seconds)
    {
        // Arrange
        var config = new NetworkConfiguration
        {
            Port = 5555,
            BindAddress = "127.0.0.1",
            RequestTimeout = TimeSpan.FromSeconds(seconds)
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("RequestTimeout") && f.Contains("at least 1 second"));
    }

    [Fact]
    public void Validate_WithMultipleErrors_ReturnsAllFailures()
    {
        // Arrange
        var config = new NetworkConfiguration
        {
            Port = 80,
            BindAddress = "",
            EnableHttps = true,
            CertificatePath = null,
            MaxRequestBodySize = 100,
            RequestTimeout = TimeSpan.Zero
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().HaveCountGreaterThan(3);
        result.Failures.Should().Contain(f => f.Contains("Port"));
        result.Failures.Should().Contain(f => f.Contains("BindAddress"));
        result.Failures.Should().Contain(f => f.Contains("CertificatePath"));
        result.Failures.Should().Contain(f => f.Contains("MaxRequestBodySize"));
        result.Failures.Should().Contain(f => f.Contains("RequestTimeout"));
    }
}
