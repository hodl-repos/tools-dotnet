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
        public static IPagedList<T> SortFilterAndPage<T>(
            this IQueryable<T> query,
            IApiPagination apiPagination,
            IPaginationProcessor paginationProcessor,
            object[]? paginationFilterParameters = null
        )
        {
            var paginationModel = new PaginationModel()
            {
                Filters = apiPagination.Filters,
                Sorts = apiPagination.Sorts,
                Page = apiPagination.Page,
                PageSize = apiPagination.PageSize,
            };

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
            object[]? paginationFilterParameters = null
        )
        {
            var paginationModel = new PaginationModel()
            {
                Filters = apiPagination.Filters,
                Sorts = apiPagination.Sorts,
                Page = apiPagination.Page,
                PageSize = apiPagination.PageSize,
            };

            query = paginationProcessor.Apply(
                paginationModel,
                query,
                applyPagination: false,
                dataForCustomMethods: paginationFilterParameters
            );

            var itemCount = await query.CountAsync();

            query = paginationProcessor.Apply(
                paginationModel,
                query,
                applyFiltering: false,
                applySorting: false
            );

            var list = await query.ToListAsync();

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
            object? mapperParameters = null
        )
        {
            var paginationModel = new PaginationModel()
            {
                Filters = apiPagination.Filters,
                Sorts = apiPagination.Sorts,
                Page = apiPagination.Page,
                PageSize = apiPagination.PageSize,
            };

            query = paginationProcessor.Apply(
                paginationModel,
                query,
                applyPagination: false,
                dataForCustomMethods: paginationFilterParameters
            );

            var itemCount = await query.CountAsync();

            query = paginationProcessor.Apply(
                paginationModel,
                query,
                applyFiltering: false,
                applySorting: false
            );

            List<TDto> list;

            if (withProjection)
            {
                if (mapperParameters != null)
                {
                    list = await query
                        .ProjectTo<TDto>(mapper.ConfigurationProvider, mapperParameters)
                        .ToListAsync();
                }
                else
                {
                    list = await query.ProjectTo<TDto>(mapper.ConfigurationProvider).ToListAsync();
                }
            }
            else
            {
                list = mapper.Map<List<TDto>>(await query.ToListAsync());
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
