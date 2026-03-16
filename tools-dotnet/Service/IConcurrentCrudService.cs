using System.Collections.Generic;
using System.Threading.Tasks;
using tools_dotnet.Dto;
using tools_dotnet.Paging;

namespace tools_dotnet.Service
{
    public interface IConcurrentCrudService<TDto, TIdType, TConcurrencyToken>
        where TDto : class, IDtoWithId<TIdType>
        where TIdType : struct
    {
        Task<TIdType> AddAsync(TDto item, CancellationToken cancellationToken = default);

        Task<IEnumerable<TDto>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<IPagedList<TDto>> GetAllAsync(
            IApiPagination apiPagination,
            CancellationToken cancellationToken = default
        );

        Task<TDto> GetByIdAsync(TIdType id, CancellationToken cancellationToken = default);

        Task UpdateAsync(
            TDto item,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        );

        Task RemoveAsync(
            TIdType id,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        );

        Task<TConcurrencyToken> GetConcurrencyTokenAsync(
            TIdType id,
            CancellationToken cancellationToken = default
        );
    }
}
