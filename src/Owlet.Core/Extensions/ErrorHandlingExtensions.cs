using Owlet.Core.ErrorHandling;

namespace Owlet.Core.Extensions;

/// <summary>
/// Extension methods for error handling services.
/// </summary>
public static class ErrorHandlingExtensions
{
    /// <summary>
    /// Adds global exception handling.
    /// </summary>
    public static IServiceCollection AddOwletErrorHandling(this IServiceCollection services)
    {
        services.AddHostedService<GlobalExceptionHandler>();
        return services;
    }
}
