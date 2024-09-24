using System.Collections.Generic;
using System.Threading.Tasks;
using tools_dotnet.Dao.KeyWrapper;
using tools_dotnet.Paging;

namespace tools_dotnet.Service
{
    public interface ICrudServiceWithKeyWrapper<TEntity, TKeyWrapper, TDto>
        where TDto : class
        where TKeyWrapper : class, IKeyWrapper<TEntity>
    {
        Task<TKeyWrapper> AddAsync(TKeyWrapper keyWrapper, TDto item);

        Task<IEnumerable<TDto>> GetAllAsync(TKeyWrapper keyWrapper);

        Task<IPagedList<TDto>> GetAllAsync(IApiSieve apiSieve, TKeyWrapper keyWrapper);

        Task<TDto> GetByIdAsync(TKeyWrapper keyWrapper);

        Task UpdateAsync(TKeyWrapper keyWrapper, TDto item);

        Task RemoveAsync(TKeyWrapper keyWrapper);
    }
}