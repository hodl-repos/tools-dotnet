using System.Collections.Generic;
using System.Threading.Tasks;
using tools_dotnet.Dto;

namespace tools_dotnet.Service
{
    public interface ISoftDeleteReadService<TDto, TIdType>
        where TDto : class, IDtoWithId<TIdType>
        where TIdType : struct
    {
        Task<IEnumerable<TDto>> GetAllIncludingDeletedAsync(
            CancellationToken cancellationToken = default
        );

        Task<IEnumerable<TDto>> GetAllDeletedAsync(
            CancellationToken cancellationToken = default
        );

        Task<TDto> GetByIdIncludingDeletedAsync(
            TIdType id,
            CancellationToken cancellationToken = default
        );
    }

    public interface ISoftDeleteCrudService<TDto, TIdType>
        : ICrudService<TDto, TIdType>,
            ISoftDeleteReadService<TDto, TIdType>
        where TDto : class, IDtoWithId<TIdType>
        where TIdType : struct
    {
        Task RestoreAsync(TIdType id, CancellationToken cancellationToken = default);

        Task HardRemoveAsync(TIdType id, CancellationToken cancellationToken = default);
    }
}
