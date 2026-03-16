using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using tools_dotnet.Dao.Crud;
using tools_dotnet.Exceptions;

namespace tools_dotnet.Dao.Crud.Impl
{
    internal static class CrudConcurrencyHelper
    {
        public static void EnsureMatchingConcurrencyToken(
            CrudConcurrencyConfiguration configuration,
            object persistedEntity,
            object? requestEntityOrDto
        )
        {
            if (!configuration.TryGetPersistedToken(persistedEntity, out var dbToken))
            {
                return;
            }

            if (
                !TryGetRequestConcurrencyToken(
                    configuration,
                    requestEntityOrDto,
                    out var requestConcurrencyToken
                )
            )
            {
                return;
            }

            EnsureMatchingTokens(configuration, dbToken, requestConcurrencyToken);
        }

        public static void EnsureMatchingConcurrencyTokenValue(
            CrudConcurrencyConfiguration configuration,
            object persistedEntity,
            object? requestConcurrencyToken
        )
        {
            if (!configuration.TryGetPersistedToken(persistedEntity, out var dbToken))
            {
                return;
            }

            EnsureMatchingTokens(configuration, dbToken, requestConcurrencyToken);
        }

        public static bool TryGetRequestConcurrencyToken(
            CrudConcurrencyConfiguration configuration,
            object? requestEntityOrDto,
            out object? requestConcurrencyToken
        )
        {
            return configuration.TryGetRequestToken(requestEntityOrDto, out requestConcurrencyToken);
        }

        public static TConcurrencyToken GetRequiredRequestConcurrencyToken<TConcurrencyToken>(
            CrudConcurrencyConfiguration configuration,
            object? requestEntityOrDto
        )
        {
            if (!configuration.TryGetRequestToken(requestEntityOrDto, out var requestConcurrencyToken))
            {
                throw CreateMissingConcurrencyTokenException(
                    $"The request type '{requestEntityOrDto?.GetType().Name ?? "unknown"}' "
                        + $"does not expose the configured concurrency token property "
                        + $"'{configuration.RequestPropertyName}'."
                );
            }

            return ConvertTokenOrThrow<TConcurrencyToken>(
                requestConcurrencyToken,
                configuration.RequestPropertyName,
                "request"
            );
        }

        public static TConcurrencyToken GetRequiredPersistedConcurrencyToken<TConcurrencyToken>(
            CrudConcurrencyConfiguration configuration,
            object persistedEntity
        )
        {
            if (!configuration.TryGetPersistedToken(persistedEntity, out var dbConcurrencyToken))
            {
                throw CreateMissingConcurrencyTokenException(
                    $"The entity type '{persistedEntity.GetType().Name}' does not expose the "
                        + $"configured concurrency token property '{configuration.EntityPropertyName}'."
                );
            }

            return ConvertTokenOrThrow<TConcurrencyToken>(
                dbConcurrencyToken,
                configuration.EntityPropertyName,
                "entity"
            );
        }

        internal static Task<ConcurrentModificationException> CreateConcurrentModificationExceptionAsync(
            CrudConcurrencyConfiguration configuration,
            DbUpdateConcurrencyException exception,
            object? requestConcurrencyToken,
            CancellationToken cancellationToken = default
        )
        {
            return CreateConcurrentModificationExceptionInternalAsync(
                configuration,
                exception,
                requestConcurrencyToken,
                cancellationToken
            );
        }

        internal static ConcurrentModificationException CreateConcurrentModificationException(
            CrudConcurrencyConfiguration configuration,
            object? dbConcurrencyToken,
            object? requestConcurrencyToken,
            Exception? innerException = null
        )
        {
            var dbConcurrencyStamp = configuration.FormatToken(dbConcurrencyToken);
            var requestConcurrencyStamp = configuration.FormatToken(requestConcurrencyToken);

            return innerException == null
                ? new ConcurrentModificationException(
                    dbConcurrencyStamp,
                    requestConcurrencyStamp
                )
                : new ConcurrentModificationException(
                    dbConcurrencyStamp,
                    requestConcurrencyStamp,
                    innerException
                );
        }

        private static async Task<ConcurrentModificationException> CreateConcurrentModificationExceptionInternalAsync(
            CrudConcurrencyConfiguration configuration,
            DbUpdateConcurrencyException exception,
            object? requestConcurrencyToken,
            CancellationToken cancellationToken
        )
        {
            var dbConcurrencyToken = await TryGetDatabaseConcurrencyTokenAsync(
                configuration,
                exception,
                cancellationToken
            );

            return CreateConcurrentModificationException(
                configuration,
                dbConcurrencyToken,
                requestConcurrencyToken,
                exception
            );
        }

        private static async Task<object?> TryGetDatabaseConcurrencyTokenAsync(
            CrudConcurrencyConfiguration configuration,
            DbUpdateConcurrencyException exception,
            CancellationToken cancellationToken
        )
        {
            foreach (var entry in exception.Entries)
            {
                try
                {
                    var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken);

                    if (databaseValues == null)
                    {
                        continue;
                    }

                    if (configuration.TryGetDatabaseToken(databaseValues, out var dbConcurrencyToken))
                    {
                        return dbConcurrencyToken;
                    }
                }
                catch (InvalidOperationException)
                {
                    continue;
                }
            }

            return null;
        }

        private static void EnsureMatchingTokens(
            CrudConcurrencyConfiguration configuration,
            object? dbConcurrencyToken,
            object? requestConcurrencyToken
        )
        {
            if (!configuration.TokensEqual(dbConcurrencyToken, requestConcurrencyToken))
            {
                throw CreateConcurrentModificationException(
                    configuration,
                    dbConcurrencyToken,
                    requestConcurrencyToken
                );
            }
        }

        private static TConcurrencyToken ConvertTokenOrThrow<TConcurrencyToken>(
            object? rawToken,
            string propertyName,
            string tokenSource
        )
        {
            if (rawToken is TConcurrencyToken token)
            {
                return token;
            }

            throw CreateMissingConcurrencyTokenException(
                $"The {tokenSource} concurrency token '{propertyName}' cannot be converted to "
                    + $"'{typeof(TConcurrencyToken).Name}'."
            );
        }

        private static InvalidOperationException CreateMissingConcurrencyTokenException(string message)
        {
            return new(message);
        }
    }
}
