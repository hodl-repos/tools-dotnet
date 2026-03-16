using System.Threading.Tasks;
using tools_dotnet.Dto;

namespace tools_dotnet.Service
{
    public interface IConcurrentSoftDeleteCrudService<TDto, TIdType, TConcurrencyToken>
        : IConcurrentCrudService<TDto, TIdType, TConcurrencyToken>,
            ISoftDeleteReadService<TDto, TIdType>
        where TDto : class, IDtoWithId<TIdType>
        where TIdType : struct
    {
        Task RestoreAsync(
            TIdType id,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        );

        Task HardRemoveAsync(
            TIdType id,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        );
    }
}
