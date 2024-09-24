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
        Task<TIdType> AddAsync(TDto item);

        Task<IEnumerable<TDto>> GetAllAsync();

        Task<IPagedList<TDto>> GetAllAsync(IApiSieve apiSieve);

        Task<TDto> GetByIdAsync(TIdType id);

        Task UpdateAsync(TDto item);

        Task RemoveAsync(TIdType id);
    }
}