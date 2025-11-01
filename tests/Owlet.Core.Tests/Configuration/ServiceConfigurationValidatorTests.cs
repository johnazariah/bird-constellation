using FluentAssertions;
using Microsoft.Extensions.Options;
using Owlet.Core.Configuration;
using Xunit;

namespace Owlet.Core.Tests.Configuration;

public class ServiceConfigurationValidatorTests
{
    private readonly ServiceConfigurationValidator _validator = new();

    [Fact]
    public void Validate_WithValidConfiguration_ReturnsSuccess()
    {
        // Arrange
        var config = new ServiceConfiguration
        {
            ServiceName = "OwletService",
            DisplayName = "Owlet Document Indexing Service",
            Description = "Indexes and searches local documents",
            StartMode = ServiceStartMode.Automatic,
            ServiceAccount = ServiceAccount.LocalSystem,
            StartupTimeout = TimeSpan.FromMinutes(2),
            CanStop = true,
            CanShutdown = true,
            FailureRestartDelay = TimeSpan.FromMinutes(1),
            MaxFailureRestarts = 3
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Failures.Should().BeNullOrEmpty();
    }

    [Theory]
    [InlineData("", "ServiceName is required")]
    [InlineData("   ", "ServiceName is required")]
    [InlineData(null, "ServiceName is required")]
    public void Validate_WithInvalidServiceName_ReturnsFailure(string? serviceName, string expectedError)
    {
        // Arrange
        var config = new ServiceConfiguration
        {
            ServiceName = serviceName!,
            DisplayName = "Valid Display Name",
            Description = "Valid Description"
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("ServiceName"))
            .And.Contain(f => f.Contains(expectedError));
    }

    [Fact]
    public void Validate_WithEmptyDisplayName_ReturnsFailure()
    {
        // Arrange
        var config = new ServiceConfiguration
        {
            ServiceName = "OwletService",
            DisplayName = "",
            Description = "Valid Description"
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("DisplayName"));
    }

    [Fact]
    public void Validate_WithEmptyDescription_ReturnsFailure()
    {
        // Arrange
        var config = new ServiceConfiguration
        {
            ServiceName = "OwletService",
            DisplayName = "Owlet Service",
            Description = ""
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("Description"));
    }

    [Theory]
    [InlineData(5)]
    [InlineData(9)]
    [InlineData(0)]
    public void Validate_WithStartupTimeoutBelowMinimum_ReturnsFailure(int seconds)
    {
        // Arrange
        var config = new ServiceConfiguration
        {
            ServiceName = "OwletService",
            DisplayName = "Owlet Service",
            Description = "Test",
            StartupTimeout = TimeSpan.FromSeconds(seconds)
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("StartupTimeout") && f.Contains("at least 10 seconds"));
    }

    [Theory]
    [InlineData(301)] // 5 minutes + 1 second
    [InlineData(600)] // 10 minutes
    public void Validate_WithStartupTimeoutAboveMaximum_ReturnsFailure(int seconds)
    {
        // Arrange
        var config = new ServiceConfiguration
        {
            ServiceName = "OwletService",
            DisplayName = "Owlet Service",
            Description = "Test",
            StartupTimeout = TimeSpan.FromSeconds(seconds)
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("StartupTimeout") && f.Contains("cannot exceed 5 minutes"));
    }

    [Theory]
    [InlineData(10)]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(120)]
    [InlineData(300)] // 5 minutes exactly
    public void Validate_WithStartupTimeoutInValidRange_ReturnsSuccess(int seconds)
    {
        // Arrange
        var config = new ServiceConfiguration
        {
            ServiceName = "OwletService",
            DisplayName = "Owlet Service",
            Description = "Test",
            StartupTimeout = TimeSpan.FromSeconds(seconds)
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(29)]
    [InlineData(15)]
    [InlineData(0)]
    public void Validate_WithFailureRestartDelayBelowMinimum_ReturnsFailure(int seconds)
    {
        // Arrange
        var config = new ServiceConfiguration
        {
            ServiceName = "OwletService",
            DisplayName = "Owlet Service",
            Description = "Test",
            FailureRestartDelay = TimeSpan.FromSeconds(seconds)
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("FailureRestartDelay") && f.Contains("at least 30 seconds"));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    [InlineData(100)]
    public void Validate_WithInvalidMaxFailureRestarts_ReturnsFailure(int maxRestarts)
    {
        // Arrange
        var config = new ServiceConfiguration
        {
            ServiceName = "OwletService",
            DisplayName = "Owlet Service",
            Description = "Test",
            MaxFailureRestarts = maxRestarts
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(f => f.Contains("MaxFailureRestarts") && f.Contains("between 0 and 10"));
    }

    [Fact]
    public void Validate_WithMultipleErrors_ReturnsAllFailures()
    {
        // Arrange
        var config = new ServiceConfiguration
        {
            ServiceName = "",
            DisplayName = "",
            Description = "",
            StartupTimeout = TimeSpan.FromSeconds(5),
            FailureRestartDelay = TimeSpan.FromSeconds(10),
            MaxFailureRestarts = 15
        };

        // Act
        var result = _validator.Validate(null, config);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().HaveCountGreaterThan(3);
        result.Failures.Should().Contain(f => f.Contains("ServiceName"));
        result.Failures.Should().Contain(f => f.Contains("DisplayName"));
        result.Failures.Should().Contain(f => f.Contains("Description"));
        result.Failures.Should().Contain(f => f.Contains("StartupTimeout"));
        result.Failures.Should().Contain(f => f.Contains("FailureRestartDelay"));
        result.Failures.Should().Contain(f => f.Contains("MaxFailureRestarts"));
    }
}
