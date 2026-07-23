using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using tools_dotnet.Exceptions;
using tools_dotnet.Pagination.Models;
using tools_dotnet.Pagination.Services;
using tools_dotnet.Paging;
using tools_dotnet.Paging.Impl;

namespace tools_dotnet.Utility
{
    /// <summary>Applies filtering, sorting, paging, and DTO projection to queryable sources.</summary>
    public static class QueryableExtensions
    {
        private static PaginationModel CreatePaginationModel(IApiPagination apiPagination)
        {
            return new PaginationModel()
            {
                Filters = apiPagination.Filters,
                Sorts = apiPagination.Sorts,
                Page = apiPagination.Page,
                PageSize = apiPagination.PageSize,
            };
        }

        /// <summary>Applies filtering, sorting, and paging to a query.</summary>
        /// <typeparam name="T">Type of item in the source query.</typeparam>
        /// <param name="query">Source query.</param>
        /// <param name="apiPagination">Requested filters, sorts, page, and page size.</param>
        /// <param name="paginationProcessor">Processor used to translate pagination values.</param>
        /// <param name="paginationFilterParameters">Optional data passed to custom filter and sort methods.</param>
        /// <returns>A materialized page and its metadata.</returns>
        /// <exception cref="InvalidPaginationFilterException">A filter is invalid.</exception>
        /// <exception cref="InvalidPaginationSortException">A sort is invalid.</exception>
        public static IPagedList<T> SortFilterAndPage<T>(
            this IQueryable<T> query,
            IApiPagination apiPagination,
            IPaginationProcessor paginationProcessor,
            object[]? paginationFilterParameters = null
        )
        {
            var paginationModel = CreatePaginationModel(apiPagination);
            var optimizedProcessor = paginationProcessor as IDeserializedPaginationProcessor;

            if (optimizedProcessor != null)
            {
                var deserializedModel = optimizedProcessor.Deserialize(paginationModel);

                query = optimizedProcessor.Apply(
                    deserializedModel,
                    query,
                    applyPagination: false,
                    dataForCustomMethods: paginationFilterParameters
                );

                var optimizedItemCount = query.Count();

                query = optimizedProcessor.Apply(
                    deserializedModel,
                    query,
                    applyFiltering: false,
                    applySorting: false
                );

                var optimizedList = query.ToList();

                return new PagedList<T>(
                    optimizedList,
                    apiPagination.Page ?? 1,
                    apiPagination.PageSize,
                    optimizedItemCount
                );
            }

            query = paginationProcessor.Apply(
                paginationModel,
                query,
                applyPagination: false,
                dataForCustomMethods: paginationFilterParameters
            );

            var itemCount = query.Count();

            query = paginationProcessor.Apply(
                paginationModel,
                query,
                applyFiltering: false,
                applySorting: false
            );

            var list = query.ToList();

            return new PagedList<T>(
                list,
                apiPagination.Page ?? 1,
                apiPagination.PageSize,
                itemCount
            );
        }

        /// <summary>Applies filtering, sorting, and paging and materializes the result asynchronously.</summary>
        /// <typeparam name="TEntity">Type of entity in the source query.</typeparam>
        /// <param name="query">Source query.</param>
        /// <param name="apiPagination">Requested filters, sorts, page, and page size.</param>
        /// <param name="paginationProcessor">Processor used to translate pagination values.</param>
        /// <param name="paginationFilterParameters">Optional data passed to custom filter and sort methods.</param>
        /// <param name="cancellationToken">Token used to cancel database operations.</param>
        /// <returns>A task containing the materialized page and its metadata.</returns>
        /// <exception cref="InvalidPaginationFilterException">A filter is invalid.</exception>
        /// <exception cref="InvalidPaginationSortException">A sort is invalid.</exception>
        public static async Task<IPagedList<TEntity>> SortFilterAndPageAsync<TEntity>(
            this IQueryable<TEntity> query,
            IApiPagination apiPagination,
            IPaginationProcessor paginationProcessor,
            object[]? paginationFilterParameters = null,
            CancellationToken cancellationToken = default
        )
        {
            var paginationModel = CreatePaginationModel(apiPagination);
            var optimizedProcessor = paginationProcessor as IDeserializedPaginationProcessor;

            if (optimizedProcessor != null)
            {
                var deserializedModel = optimizedProcessor.Deserialize(paginationModel);

                query = optimizedProcessor.Apply(
                    deserializedModel,
                    query,
                    applyPagination: false,
                    dataForCustomMethods: paginationFilterParameters
                );

                var optimizedItemCount = await query.CountAsync(cancellationToken);

                query = optimizedProcessor.Apply(
                    deserializedModel,
                    query,
                    applyFiltering: false,
                    applySorting: false
                );

                var optimizedList = await query.ToListAsync(cancellationToken);

                return new PagedList<TEntity>(
                    optimizedList,
                    apiPagination.Page ?? 1,
                    apiPagination.PageSize,
                    optimizedItemCount
                );
            }

            query = paginationProcessor.Apply(
                paginationModel,
                query,
                applyPagination: false,
                dataForCustomMethods: paginationFilterParameters
            );

            var itemCount = await query.CountAsync(cancellationToken);

            query = paginationProcessor.Apply(
                paginationModel,
                query,
                applyFiltering: false,
                applySorting: false
            );

            var list = await query.ToListAsync(cancellationToken);

            return new PagedList<TEntity>(
                list,
                apiPagination.Page ?? 1,
                apiPagination.PageSize,
                itemCount
            );
        }

        /// <summary>Applies filtering, sorting, paging, and AutoMapper projection asynchronously.</summary>
        /// <typeparam name="TEntity">Type of entity in the source query.</typeparam>
        /// <typeparam name="TDto">Type returned for each projected item.</typeparam>
        /// <param name="query">Source query.</param>
        /// <param name="apiPagination">Requested filters, sorts, page, and page size.</param>
        /// <param name="paginationProcessor">Processor used to translate pagination values.</param>
        /// <param name="mapper">AutoMapper instance used for projection or object mapping.</param>
        /// <param name="withProjection">Whether to use query projection instead of in-memory mapping.</param>
        /// <param name="paginationFilterParameters">Optional data passed to custom filter and sort methods.</param>
        /// <param name="mapperParameters">Optional values supplied to AutoMapper.</param>
        /// <param name="cancellationToken">Token used to cancel database operations.</param>
        /// <returns>A task containing the projected page and its metadata.</returns>
        /// <exception cref="InvalidPaginationFilterException">A filter is invalid.</exception>
        /// <exception cref="InvalidPaginationSortException">A sort is invalid.</exception>
        public static async Task<IPagedList<TDto>> SortFilterAndPageWithProjectToAsync<
            TEntity,
            TDto
        >(
            this IQueryable<TEntity> query,
            IApiPagination apiPagination,
            IPaginationProcessor paginationProcessor,
            IMapper mapper,
            bool withProjection = true,
            object[]? paginationFilterParameters = null,
            object? mapperParameters = null,
            CancellationToken cancellationToken = default
        )
        {
            var paginationModel = CreatePaginationModel(apiPagination);
            var optimizedProcessor = paginationProcessor as IDeserializedPaginationProcessor;

            if (optimizedProcessor != null)
            {
                var deserializedModel = optimizedProcessor.Deserialize(paginationModel);

                query = optimizedProcessor.Apply(
                    deserializedModel,
                    query,
                    applyPagination: false,
                    dataForCustomMethods: paginationFilterParameters
                );

                var optimizedItemCount = await query.CountAsync(cancellationToken);

                query = optimizedProcessor.Apply(
                    deserializedModel,
                    query,
                    applyFiltering: false,
                    applySorting: false
                );

                return await CreateProjectedPagedListAsync<TEntity, TDto>(
                    query,
                    apiPagination,
                    mapper,
                    withProjection,
                    mapperParameters,
                    optimizedItemCount,
                    cancellationToken
                );
            }

            query = paginationProcessor.Apply(
                paginationModel,
                query,
                applyPagination: false,
                dataForCustomMethods: paginationFilterParameters
            );

            var itemCount = await query.CountAsync(cancellationToken);

            query = paginationProcessor.Apply(
                paginationModel,
                query,
                applyFiltering: false,
                applySorting: false
            );

            return await CreateProjectedPagedListAsync<TEntity, TDto>(
                query,
                apiPagination,
                mapper,
                withProjection,
                mapperParameters,
                itemCount,
                cancellationToken
            );
        }

        private static async Task<IPagedList<TDto>> CreateProjectedPagedListAsync<TEntity, TDto>(
            IQueryable<TEntity> query,
            IApiPagination apiPagination,
            IMapper mapper,
            bool withProjection,
            object? mapperParameters,
            int itemCount,
            CancellationToken cancellationToken
        )
        {
            List<TDto> list;

            if (withProjection)
            {
                if (mapperParameters != null)
                {
                    list = await query
                        .ProjectTo<TDto>(mapper.ConfigurationProvider, mapperParameters)
                        .ToListAsync(cancellationToken);
                }
                else
                {
                    list = await query
                        .ProjectTo<TDto>(mapper.ConfigurationProvider)
                        .ToListAsync(cancellationToken);
                }
            }
            else
            {
                list = mapper.Map<List<TDto>>(await query.ToListAsync(cancellationToken));
            }

            return new PagedList<TDto>(
                list,
                apiPagination.Page ?? 1,
                apiPagination.PageSize,
                itemCount
            );
        }
    }
}
