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

        Task<IEnumerable<TDto>> GetAllIncludingDeletedAsync(TKeyWrapper keyWrapper);

        Task<IEnumerable<TDto>> GetAllDeletedAsync(TKeyWrapper keyWrapper);

        Task<IPagedList<TDto>> GetAllAsync(IApiPagination apiPagination, TKeyWrapper keyWrapper);

        Task<TDto> GetByIdAsync(TKeyWrapper keyWrapper);

        Task<TDto> GetByIdIncludingDeletedAsync(TKeyWrapper keyWrapper);

        Task UpdateAsync(TKeyWrapper keyWrapper, TDto item);

        Task RemoveAsync(TKeyWrapper keyWrapper);

        Task RestoreAsync(TKeyWrapper keyWrapper);

        Task HardRemoveAsync(TKeyWrapper keyWrapper);
    }
}
