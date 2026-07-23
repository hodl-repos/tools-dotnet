using System;

namespace tools_dotnet.Time
{
    /// <summary>
    /// Provides the current time for components that need mockable clock access.
    /// </summary>
    public interface IClockProvider
    {
        /// <summary>
        /// Gets the current UTC timestamp.
        /// </summary>
        DateTimeOffset UtcNow { get; }
    }
}
