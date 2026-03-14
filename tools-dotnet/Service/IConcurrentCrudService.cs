using System.Threading.Tasks;
using tools_dotnet.Dto;

namespace tools_dotnet.Service
{
    public interface IConcurrentCrudService<TDto, TIdType, TConcurrencyToken>
        : ICrudService<TDto, TIdType>
        where TDto : class, IDtoWithId<TIdType>
        where TIdType : struct
    {
        Task UpdateAsync(TDto item, TConcurrencyToken concurrencyToken);

        Task RemoveAsync(TIdType id, TConcurrencyToken concurrencyToken);

        Task<TConcurrencyToken> GetConcurrencyTokenAsync(TIdType id);
    }
}
