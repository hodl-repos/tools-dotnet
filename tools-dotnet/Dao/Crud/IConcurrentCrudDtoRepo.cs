using System.Threading.Tasks;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Dto;

namespace tools_dotnet.Dao.Crud
{
    public interface IConcurrentCrudDtoRepo<TEntity, TIdType, TDto, TInputDto, TConcurrencyToken>
        : ICrudDtoRepo<TEntity, TIdType, TDto, TInputDto>,
            IConcurrentCrudRepo<TEntity, TIdType, TConcurrencyToken>
        where TEntity : class, IEntityWithId<TIdType>
        where TIdType : struct
        where TDto : class, IDtoWithId<TIdType>
        where TInputDto : IDtoWithId<TIdType>
    {
        Task UpdateAsync(TInputDto item, TConcurrencyToken concurrencyToken);
    }

    public interface IConcurrentCrudDtoRepo<TEntity, TIdType, TDto, TConcurrencyToken>
        : IConcurrentCrudDtoRepo<TEntity, TIdType, TDto, TDto, TConcurrencyToken>,
            ICrudDtoRepo<TEntity, TIdType, TDto>
        where TEntity : class, IEntityWithId<TIdType>
        where TIdType : struct
        where TDto : class, IDtoWithId<TIdType> { }
}
