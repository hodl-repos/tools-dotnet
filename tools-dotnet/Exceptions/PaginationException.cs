using System;

namespace tools_dotnet.Exceptions
{
    /// <summary>
    /// Base exception for invalid pagination query parameters.
    /// </summary>
    public abstract class PaginationException : Exception
    {
        /// <summary>
        /// Creates a pagination exception.
        /// </summary>
        protected PaginationException(
            string message,
            string parameterName,
            PaginationErrorCode errorCode,
            string? field = null,
            string? value = null,
            string? @operator = null,
            Type? targetType = null,
            Exception? innerException = null
        )
            : base(message, innerException)
        {
            ParameterName = parameterName;
            ErrorCode = errorCode;
            Field = field;
            Value = value;
            Operator = @operator;
            TargetType = targetType;
        }

        /// <summary>
        /// Query parameter that caused the error.
        /// </summary>
        public string ParameterName { get; }

        /// <summary>
        /// Machine-readable pagination error code.
        /// </summary>
        public PaginationErrorCode ErrorCode { get; }

        /// <summary>
        /// Field path that caused the error, when available.
        /// </summary>
        public string? Field { get; }

        /// <summary>
        /// Raw value that caused the error, when available.
        /// </summary>
        public string? Value { get; }

        /// <summary>
        /// Operator token that caused the error, when available.
        /// </summary>
        public string? Operator { get; }

        /// <summary>
        /// Target member type used for value conversion, when available.
        /// </summary>
        public Type? TargetType { get; }
    }
}
