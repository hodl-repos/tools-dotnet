using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Sieve.Models;
using Sieve.Services;
using System.Linq;
using tools_dotnet.Paging;
using tools_dotnet.Paging.Impl;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace tools_dotnet.Utility
{
    public static class QueryableExtensions
    {
        public static IPagedList<T> SortFilterAndPage<T>(this IQueryable<T> query,
            IApiSieve apiSieve, ISieveProcessor sieveProcessor, object[]? sieveFilterParameters = null)
        {
            var sieveModel = new SieveModel()
            {
                Filters = apiSieve.Filters,
                Sorts = apiSieve.Sorts,
                Page = apiSieve.Page,
                PageSize = apiSieve.PageSize
            };

            query = sieveProcessor.Apply(sieveModel, query, applyPagination: false, dataForCustomMethods: sieveFilterParameters);

            var itemCount = query.Count();

            query = sieveProcessor.Apply(sieveModel, query, applyFiltering: false, applySorting: false);

            var list = query.ToList();

            return new PagedList<T>(list, apiSieve.Page ?? 1, apiSieve.PageSize, itemCount);
        }

        public static async Task<IPagedList<TEntity>> SortFilterAndPageAsync<TEntity>(this IQueryable<TEntity> query,
            IApiSieve apiSieve, ISieveProcessor sieveProcessor, object[]? sieveFilterParameters = null)
        {
            var sieveModel = new SieveModel()
            {
                Filters = apiSieve.Filters,
                Sorts = apiSieve.Sorts,
                Page = apiSieve.Page,
                PageSize = apiSieve.PageSize
            };

            query = sieveProcessor.Apply(sieveModel, query, applyPagination: false, dataForCustomMethods: sieveFilterParameters);

            var itemCount = await query.CountAsync();

            query = sieveProcessor.Apply(sieveModel, query, applyFiltering: false, applySorting: false);

            var list = await query.ToListAsync();

            return new PagedList<TEntity>(list, apiSieve.Page ?? 1, apiSieve.PageSize, itemCount);
        }

        public static async Task<IPagedList<TDto>> SortFilterAndPageWithProjectToAsync<TEntity, TDto>(this IQueryable<TEntity> query,
            IApiSieve apiSieve, ISieveProcessor sieveProcessor, IMapper mapper, bool withProjection = true,
            object[]? sieveFilterParameters = null, object? mapperParameters = null)
        {
            var sieveModel = new SieveModel()
            {
                Filters = apiSieve.Filters,
                Sorts = apiSieve.Sorts,
                Page = apiSieve.Page,
                PageSize = apiSieve.PageSize
            };

            query = sieveProcessor.Apply(sieveModel, query, applyPagination: false, dataForCustomMethods: sieveFilterParameters);

            var itemCount = await query.CountAsync();

            query = sieveProcessor.Apply(sieveModel, query, applyFiltering: false, applySorting: false);

            List<TDto> list;

            if (withProjection)
            {
                if (mapperParameters != null)
                {
                    list = await query.ProjectTo<TDto>(mapper.ConfigurationProvider, mapperParameters).ToListAsync();
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

            return new PagedList<TDto>(list, apiSieve.Page ?? 1, apiSieve.PageSize, itemCount);
        }
    }
}