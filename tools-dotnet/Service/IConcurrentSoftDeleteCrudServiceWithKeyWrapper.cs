using System.Threading.Tasks;
using tools_dotnet.Dao.KeyWrapper;

namespace tools_dotnet.Service
{
    public interface IConcurrentSoftDeleteCrudServiceWithKeyWrapper<
        TEntity,
        TKeyWrapper,
        TDto,
        TConcurrencyToken
    > : IConcurrentCrudServiceWithKeyWrapper<TEntity, TKeyWrapper, TDto, TConcurrencyToken>,
            ISoftDeleteReadServiceWithKeyWrapper<TEntity, TKeyWrapper, TDto>
        where TDto : class
        where TKeyWrapper : class, IKeyWrapper<TEntity>
    {
        Task RestoreAsync(
            TKeyWrapper keyWrapper,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        );

        Task HardRemoveAsync(
            TKeyWrapper keyWrapper,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        );
    }
}
