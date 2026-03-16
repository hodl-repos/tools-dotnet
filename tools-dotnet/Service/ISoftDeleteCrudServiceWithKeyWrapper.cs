using System.Collections.Generic;
using System.Threading.Tasks;
using tools_dotnet.Dao.KeyWrapper;
using tools_dotnet.Paging;

namespace tools_dotnet.Service
{
    public interface ISoftDeleteReadServiceWithKeyWrapper<TEntity, TKeyWrapper, TDto>
        where TDto : class
        where TKeyWrapper : class, IKeyWrapper<TEntity>
    {
        Task<IEnumerable<TDto>> GetAllIncludingDeletedAsync(
            TKeyWrapper keyWrapper,
            CancellationToken cancellationToken = default
        );

        Task<IEnumerable<TDto>> GetAllDeletedAsync(
            TKeyWrapper keyWrapper,
            CancellationToken cancellationToken = default
        );

        Task<TDto> GetByIdIncludingDeletedAsync(
            TKeyWrapper keyWrapper,
            CancellationToken cancellationToken = default
        );
    }

    public interface ISoftDeleteCrudServiceWithKeyWrapper<TEntity, TKeyWrapper, TDto>
        : ICrudServiceWithKeyWrapper<TEntity, TKeyWrapper, TDto>,
            ISoftDeleteReadServiceWithKeyWrapper<TEntity, TKeyWrapper, TDto>
        where TDto : class
        where TKeyWrapper : class, IKeyWrapper<TEntity>
    {
        Task RestoreAsync(TKeyWrapper keyWrapper, CancellationToken cancellationToken = default);

        Task HardRemoveAsync(
            TKeyWrapper keyWrapper,
            CancellationToken cancellationToken = default
        );
    }
}
