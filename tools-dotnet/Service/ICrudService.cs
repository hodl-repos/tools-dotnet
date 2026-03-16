using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using tools_dotnet.Dto;
using tools_dotnet.Paging;

namespace tools_dotnet.Service
{
    public interface ICrudService<TDto, TIdType>
        where TDto : class, IDtoWithId<TIdType>
        where TIdType : struct
    {
        Task<TIdType> AddAsync(TDto item, CancellationToken cancellationToken = default);

        Task<IEnumerable<TDto>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<IEnumerable<TDto>> GetAllIncludingDeletedAsync(
            CancellationToken cancellationToken = default
        );

        Task<IEnumerable<TDto>> GetAllDeletedAsync(
            CancellationToken cancellationToken = default
        );

        Task<IPagedList<TDto>> GetAllAsync(
            IApiPagination apiPagination,
            CancellationToken cancellationToken = default
        );

        Task<TDto> GetByIdAsync(TIdType id, CancellationToken cancellationToken = default);

        Task<TDto> GetByIdIncludingDeletedAsync(
            TIdType id,
            CancellationToken cancellationToken = default
        );

        Task UpdateAsync(TDto item, CancellationToken cancellationToken = default);

        Task RemoveAsync(TIdType id, CancellationToken cancellationToken = default);

        Task RestoreAsync(TIdType id, CancellationToken cancellationToken = default);

        Task HardRemoveAsync(TIdType id, CancellationToken cancellationToken = default);
    }
}
