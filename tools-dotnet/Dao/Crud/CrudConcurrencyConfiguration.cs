using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Dto;

namespace tools_dotnet.Dao.Crud
{
    public sealed class CrudConcurrencyConfiguration
    {
        private static readonly ConcurrentDictionary<
            (Type Type, string PropertyName),
            PropertyInfo?
        > PropertyCache = new();

        private readonly Func<object?, object?, bool> _tokenComparer;
        private readonly Func<object?, string?> _tokenFormatter;

        private CrudConcurrencyConfiguration(
            string entityPropertyName,
            string requestPropertyName,
            Func<object?, object?, bool> tokenComparer,
            Func<object?, string?> tokenFormatter
        )
        {
            EntityPropertyName = !string.IsNullOrWhiteSpace(entityPropertyName)
                ? entityPropertyName
                : throw new ArgumentException(
                    "Entity property name must not be empty.",
                    nameof(entityPropertyName)
                );
            RequestPropertyName = !string.IsNullOrWhiteSpace(requestPropertyName)
                ? requestPropertyName
                : throw new ArgumentException(
                    "Request property name must not be empty.",
                    nameof(requestPropertyName)
                );
            _tokenComparer =
                tokenComparer ?? throw new ArgumentNullException(nameof(tokenComparer));
            _tokenFormatter =
                tokenFormatter ?? throw new ArgumentNullException(nameof(tokenFormatter));
        }

        public string EntityPropertyName { get; }

        public string RequestPropertyName { get; }

        public static CrudConcurrencyConfiguration UpdatedTimestamp(
            string entityPropertyName = nameof(IChangeTrackingEntity.UpdatedTimestamp),
            string requestPropertyName = nameof(IChangeTrackingDto.UpdatedTimestamp)
        )
        {
            return ForProperty<DateTimeOffset?>(
                entityPropertyName,
                requestPropertyName,
                formatter: value => value?.ToString("O")
            );
        }

        public static CrudConcurrencyConfiguration SqlServerRowVersion(
            string entityPropertyName = "RowVersion",
            string? requestPropertyName = null
        )
        {
            return ForProperty<byte[]>(
                entityPropertyName,
                requestPropertyName ?? entityPropertyName,
                ByteArrayEqualityComparer.Instance,
                value => value == null ? null : Convert.ToBase64String(value)
            );
        }

        public static CrudConcurrencyConfiguration PostgreSqlXmin(
            string entityPropertyName = "xmin",
            string? requestPropertyName = null
        )
        {
            return ForProperty<uint>(
                entityPropertyName,
                requestPropertyName ?? entityPropertyName,
                formatter: value => value.ToString(CultureInfo.InvariantCulture)
            );
        }

        public static CrudConcurrencyConfiguration ForProperty<TConcurrencyToken>(
            string entityPropertyName,
            string? requestPropertyName = null,
            IEqualityComparer<TConcurrencyToken>? comparer = null,
            Func<TConcurrencyToken, string?>? formatter = null
        )
        {
            comparer ??= EqualityComparer<TConcurrencyToken>.Default;
            requestPropertyName ??= entityPropertyName;

            return new CrudConcurrencyConfiguration(
                entityPropertyName,
                requestPropertyName,
                (dbToken, requestToken) => TokensEqual(dbToken, requestToken, comparer),
                token => FormatToken(token, formatter)
            );
        }

        internal bool TryGetPersistedToken(object persistedEntity, out object? token)
        {
            return TryGetPropertyValue(persistedEntity, EntityPropertyName, out token);
        }

        internal bool TryGetRequestToken(object? requestEntityOrDto, out object? token)
        {
            return TryGetPropertyValue(requestEntityOrDto, RequestPropertyName, out token);
        }

        internal bool TryGetDatabaseToken(PropertyValues databaseValues, out object? token)
        {
            if (databaseValues.Properties.All(x => x.Name != EntityPropertyName))
            {
                token = null;
                return false;
            }

            token = databaseValues[EntityPropertyName];
            return true;
        }

        internal bool TokensEqual(object? dbToken, object? requestToken)
        {
            return _tokenComparer(dbToken, requestToken);
        }

        internal string? FormatToken(object? token)
        {
            return _tokenFormatter(token);
        }

        private static bool TryGetPropertyValue(
            object? instance,
            string propertyName,
            out object? value
        )
        {
            value = null;

            if (instance == null)
            {
                return false;
            }

            var property = PropertyCache.GetOrAdd(
                (instance.GetType(), propertyName),
                key =>
                    key.Type.GetProperty(
                        key.PropertyName,
                        BindingFlags.Instance | BindingFlags.Public
                    )
            );

            if (property == null || !property.CanRead)
            {
                return false;
            }

            value = property.GetValue(instance);
            return true;
        }

        private static bool TokensEqual<TConcurrencyToken>(
            object? dbToken,
            object? requestToken,
            IEqualityComparer<TConcurrencyToken> comparer
        )
        {
            if (dbToken == null || requestToken == null)
            {
                return dbToken == null && requestToken == null;
            }

            if (
                !TryConvertToken(dbToken, out TConcurrencyToken typedDbToken)
                || !TryConvertToken(requestToken, out TConcurrencyToken typedRequestToken)
            )
            {
                return false;
            }

            return comparer.Equals(typedDbToken, typedRequestToken);
        }

        private static string? FormatToken<TConcurrencyToken>(
            object? token,
            Func<TConcurrencyToken, string?>? formatter
        )
        {
            if (token == null)
            {
                return null;
            }

            if (TryConvertToken(token, out TConcurrencyToken typedToken))
            {
                if (formatter != null)
                {
                    return formatter(typedToken);
                }

                if (typedToken is IFormattable formattable)
                {
                    return formattable.ToString(null, CultureInfo.InvariantCulture);
                }

                object? boxedToken = typedToken;
                return boxedToken?.ToString();
            }

            return token.ToString();
        }

        private static bool TryConvertToken<TConcurrencyToken>(
            object token,
            out TConcurrencyToken typedToken
        )
        {
            try
            {
                typedToken = (TConcurrencyToken)token;
                return true;
            }
            catch (InvalidCastException)
            {
                typedToken = default!;
                return false;
            }
        }

        private sealed class ByteArrayEqualityComparer : IEqualityComparer<byte[]>
        {
            public static readonly ByteArrayEqualityComparer Instance = new();

            public bool Equals(byte[]? x, byte[]? y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (x == null || y == null || x.Length != y.Length)
                {
                    return false;
                }

                for (var i = 0; i < x.Length; i++)
                {
                    if (x[i] != y[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            public int GetHashCode(byte[] obj)
            {
                unchecked
                {
                    var hash = 17;

                    foreach (var value in obj)
                    {
                        hash = (hash * 31) + value;
                    }

                    return hash;
                }
            }
        }
    }
}

