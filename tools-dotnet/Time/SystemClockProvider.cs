using System;

namespace tools_dotnet.Time
{
    /// <summary>
    /// Uses the system UTC clock.
    /// </summary>
    public sealed class SystemClockProvider : IClockProvider
    {
        /// <summary>
        /// Gets the shared system clock provider instance.
        /// </summary>
        public static SystemClockProvider Instance { get; } = new();

        private SystemClockProvider() { }

        /// <inheritdoc />
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
