using System.Threading.Tasks;
using tools_dotnet.Dto;

namespace tools_dotnet.Service
{
    public interface IConcurrentCrudService<TDto, TIdType, TConcurrencyToken>
        : ICrudService<TDto, TIdType>
        where TDto : class, IDtoWithId<TIdType>
        where TIdType : struct
    {
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

        Task<TConcurrencyToken> GetConcurrencyTokenAsync(
            TIdType id,
            CancellationToken cancellationToken = default
        );
    }
}
