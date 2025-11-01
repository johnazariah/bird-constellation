using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Owlet.Core.Configuration;
using Xunit;

namespace Owlet.Core.Tests.Configuration;

public class LoggingConfigurationValidatorTests
{
    private readonly LoggingConfigurationValidator _validator = new();

    [Fact]
    public void Validate_WithValidConfiguration_ReturnsSuccess()
    {
        // Arrange
        var config = new LoggingConfiguration
        {
            MinimumLevel = LogLevel.Information,
            LogDirectory = @"C:\ProgramData\Owlet\Logs",
            MaxLogFileSizeBytes = 100 * 1024 * 1024,
            RetainedLogFiles = 10,
            RollingInterval = LogRollingInterval.Day,
            EnableWindowsEventLog = true,
            EnableStructuredLogging = true
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
    public void Validate_WithEmptyLogDirectory_ReturnsFailure(string? logDirectory)
    {
        // Arrange
        var config = new LoggingConfiguration
        {
            LogDirectory = logDirectory!
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("LogDirectory"));
    }

    [Theory]
    [InlineData(1048575)] // 1MB - 1 byte
    [InlineData(524288)]  // 512KB
    [InlineData(0)]
    public void Validate_WithMaxLogFileSizeTooSmall_ReturnsFailure(long size)
    {
        // Arrange
        var config = new LoggingConfiguration
        {
            LogDirectory = @"C:\Logs",
            MaxLogFileSizeBytes = size
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("MaxLogFileSizeBytes") && f.Contains("at least 1MB"));
    }

    [Theory]
    [InlineData(1048576)]   // 1MB exactly
    [InlineData(10485760)]  // 10MB
    [InlineData(104857600)] // 100MB
    public void Validate_WithValidMaxLogFileSize_ReturnsSuccess(long size)
    {
        // Arrange
        var config = new LoggingConfiguration
        {
            LogDirectory = @"C:\Logs",
            MaxLogFileSizeBytes = size
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithRetainedLogFilesLessThanOne_ReturnsFailure(int retainedFiles)
    {
        // Arrange
        var config = new LoggingConfiguration
        {
            LogDirectory = @"C:\Logs",
            RetainedLogFiles = retainedFiles
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("RetainedLogFiles") && f.Contains("at least 1"));
    }

    [Theory]
    [InlineData(101)]
    [InlineData(200)]
    [InlineData(1000)]
    public void Validate_WithRetainedLogFilesAboveMaximum_ReturnsFailure(int retainedFiles)
    {
        // Arrange
        var config = new LoggingConfiguration
        {
            LogDirectory = @"C:\Logs",
            RetainedLogFiles = retainedFiles
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("RetainedLogFiles") && f.Contains("cannot exceed 100"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public void Validate_WithValidRetainedLogFiles_ReturnsSuccess(int retainedFiles)
    {
        // Arrange
        var config = new LoggingConfiguration
        {
            LogDirectory = @"C:\Logs",
            RetainedLogFiles = retainedFiles
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithMultipleErrors_ReturnsAllFailures()
    {
        // Arrange
        var config = new LoggingConfiguration
        {
            LogDirectory = "",
            MaxLogFileSizeBytes = 1000,
            RetainedLogFiles = 0
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().HaveCount(3);
        result.Failures.Should().Contain(f => f.Contains("LogDirectory"));
        result.Failures.Should().Contain(f => f.Contains("MaxLogFileSizeBytes"));
        result.Failures.Should().Contain(f => f.Contains("RetainedLogFiles"));
    }
}
