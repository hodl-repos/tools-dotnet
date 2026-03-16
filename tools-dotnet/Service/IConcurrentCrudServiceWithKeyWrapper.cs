using System.Collections.Generic;
using System.Threading.Tasks;
using tools_dotnet.Dao.KeyWrapper;
using tools_dotnet.Paging;

namespace tools_dotnet.Service
{
    public interface IConcurrentCrudServiceWithKeyWrapper<
        TEntity,
        TKeyWrapper,
        TDto,
        TConcurrencyToken
    >
        where TDto : class
        where TKeyWrapper : class, IKeyWrapper<TEntity>
    {
        Task<TKeyWrapper> AddAsync(
            TKeyWrapper keyWrapper,
            TDto item,
            CancellationToken cancellationToken = default
        );

        Task<IEnumerable<TDto>> GetAllAsync(
            TKeyWrapper keyWrapper,
            CancellationToken cancellationToken = default
        );

        Task<IPagedList<TDto>> GetAllAsync(
            IApiPagination apiPagination,
            TKeyWrapper keyWrapper,
            CancellationToken cancellationToken = default
        );

        Task<TDto> GetByIdAsync(
            TKeyWrapper keyWrapper,
            CancellationToken cancellationToken = default
        );

        Task UpdateAsync(
            TKeyWrapper keyWrapper,
            TDto item,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        );

        Task RemoveAsync(
            TKeyWrapper keyWrapper,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        );

        Task<TConcurrencyToken> GetConcurrencyTokenAsync(
            TKeyWrapper keyWrapper,
            CancellationToken cancellationToken = default
        );
    }
}
