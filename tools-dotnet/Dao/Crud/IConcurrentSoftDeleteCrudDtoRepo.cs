using System.Threading.Tasks;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Dto;

namespace tools_dotnet.Dao.Crud
{
    public interface IConcurrentSoftDeleteCrudDtoRepo<
        TEntity,
        TIdType,
        TDto,
        TInputDto,
        TConcurrencyToken
    > : IConcurrentCrudDtoRepo<TEntity, TIdType, TDto, TInputDto, TConcurrencyToken>,
            IConcurrentSoftDeleteCrudRepo<TEntity, TIdType, TConcurrencyToken>,
            ISoftDeleteReadDtoRepo<TEntity, TIdType, TDto>
        where TEntity : class, IAuditableEntity, IEntityWithId<TIdType>
        where TIdType : struct
        where TDto : class, IDtoWithId<TIdType>
        where TInputDto : IDtoWithId<TIdType> { }

    public interface IConcurrentSoftDeleteCrudDtoRepo<
        TEntity,
        TIdType,
        TDto,
        TConcurrencyToken
    > : IConcurrentSoftDeleteCrudDtoRepo<TEntity, TIdType, TDto, TDto, TConcurrencyToken>,
            IConcurrentCrudDtoRepo<TEntity, TIdType, TDto, TConcurrencyToken>
        where TEntity : class, IAuditableEntity, IEntityWithId<TIdType>
        where TIdType : struct
        where TDto : class, IDtoWithId<TIdType> { }
}
