using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using tools_dotnet.Exceptions;

namespace tools_dotnet.Dao.Crud.Impl
{
    internal static class CrudDbUpdateExceptionTranslator
    {
        private const string LegacySqlClientExceptionTypeName = "System.Data.SqlClient.SqlException";
        private const string PostgresForeignKeyViolation = "23503";
        private const string PostgresRestrictViolation = "23001";
        private const string PostgresUniqueViolation = "23505";
        private const int SqlServerConstraintViolation = 547;
        private const string SqlServerExceptionTypeName = "Microsoft.Data.SqlClient.SqlException";

        private static readonly HashSet<int> SqlServerUniqueViolations = [2601, 2627];

        public static void ThrowIfKnown(DbUpdateException exception, bool onRemove)
        {
            var translatedException = Translate(exception, onRemove);

            if (translatedException != null)
            {
                throw translatedException;
            }
        }

        public static Exception? Translate(DbUpdateException exception, bool onRemove)
        {
            foreach (var currentException in EnumerateExceptionChain(exception.InnerException))
            {
                if (TryTranslateSqlStateException(currentException, onRemove, out var translatedException))
                {
                    return translatedException;
                }

                if (TryTranslateSqlServer(currentException, onRemove, out translatedException))
                {
                    return translatedException;
                }
            }

            return null;
        }

        private static IEnumerable<Exception> EnumerateExceptionChain(Exception? exception)
        {
            while (exception != null)
            {
                yield return exception;
                exception = exception.InnerException;
            }
        }

        private static bool TryTranslateSqlStateException(
            Exception exception,
            bool onRemove,
            out Exception? translatedException
        )
        {
            if (!TryGetStringProperty(exception, "SqlState", out var sqlState))
            {
                translatedException = null;
                return false;
            }

            switch (sqlState)
            {
                case PostgresForeignKeyViolation:
                case PostgresRestrictViolation:
                    translatedException = new DependentItemException(
                        exception.Message,
                        onRemove,
                        exception
                    );
                    return true;
                case PostgresUniqueViolation when !onRemove:
                    translatedException = new ConflictingItemException(
                        exception.Message,
                        exception
                    );
                    return true;
                default:
                    translatedException = null;
                    return false;
            }
        }

        private static bool TryTranslateSqlServer(
            Exception exception,
            bool onRemove,
            out Exception? translatedException
        )
        {
            var typeName = exception.GetType().FullName;

            if (
                typeName != SqlServerExceptionTypeName
                && typeName != LegacySqlClientExceptionTypeName
            )
            {
                translatedException = null;
                return false;
            }

            if (!TryGetIntProperty(exception, "Number", out var number))
            {
                translatedException = null;
                return false;
            }

            if (number == SqlServerConstraintViolation)
            {
                translatedException = new DependentItemException(
                    exception.Message,
                    onRemove,
                    exception
                );
                return true;
            }

            if (!onRemove && SqlServerUniqueViolations.Contains(number))
            {
                translatedException = new ConflictingItemException(
                    exception.Message,
                    exception
                );
                return true;
            }

            translatedException = null;
            return false;
        }

        private static bool TryGetStringProperty(
            Exception exception,
            string propertyName,
            out string? value
        )
        {
            if (TryGetPropertyValue(exception, propertyName, out var rawValue) && rawValue is string text)
            {
                value = text;
                return true;
            }

            value = null;
            return false;
        }

        private static bool TryGetIntProperty(
            Exception exception,
            string propertyName,
            out int value
        )
        {
            if (TryGetPropertyValue(exception, propertyName, out var rawValue))
            {
                switch (rawValue)
                {
                    case int intValue:
                        value = intValue;
                        return true;
                    case short shortValue:
                        value = shortValue;
                        return true;
                }
            }

            value = default;
            return false;
        }

        private static bool TryGetPropertyValue(
            object instance,
            string propertyName,
            out object? value
        )
        {
            var propertyInfo = instance
                .GetType()
                .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);

            if (propertyInfo == null || !propertyInfo.CanRead)
            {
                value = null;
                return false;
            }

            value = propertyInfo.GetValue(instance);
            return true;
        }
    }
}
