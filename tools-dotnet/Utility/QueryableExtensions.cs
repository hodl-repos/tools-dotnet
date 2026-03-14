using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using tools_dotnet.Pagination.Models;
using tools_dotnet.Pagination.Services;
using tools_dotnet.Paging;
using tools_dotnet.Paging.Impl;

namespace tools_dotnet.Utility
{
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
