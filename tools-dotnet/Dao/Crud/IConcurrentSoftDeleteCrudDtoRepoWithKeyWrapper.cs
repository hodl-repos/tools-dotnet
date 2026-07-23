using tools_dotnet.Dao.Entity;
using tools_dotnet.Dao.KeyWrapper;

namespace tools_dotnet.Dao.Crud
{
    /// <summary>Defines concurrency-aware key-wrapper DTO CRUD operations for soft-deletable entities.</summary>
    public interface IConcurrentSoftDeleteCrudDtoRepoWithKeyWrapper<
        TEntity,
        TKeyWrapper,
        TDto,
        TInputDto,
        TConcurrencyToken
    > : IConcurrentCrudDtoRepoWithKeyWrapper<
            TEntity,
            TKeyWrapper,
            TDto,
            TInputDto,
            TConcurrencyToken
        >,
            IConcurrentSoftDeleteCrudRepoWithKeyWrapper<TEntity, TKeyWrapper, TConcurrencyToken>,
            ISoftDeleteReadDtoRepoWithKeyWrapper<TEntity, TKeyWrapper, TDto>
        where TEntity : class, IAuditableEntity, IEntity
        where TKeyWrapper : class, IKeyWrapper<TEntity>
        where TDto : class
        where TInputDto : class
    { }

    /// <summary>Defines concurrency-aware key-wrapper DTO CRUD operations for soft-deletable entities.</summary>
    public interface IConcurrentSoftDeleteCrudDtoRepoWithKeyWrapper<
        TEntity,
        TKeyWrapper,
        TDto,
        TConcurrencyToken
    > : IConcurrentSoftDeleteCrudDtoRepoWithKeyWrapper<
            TEntity,
            TKeyWrapper,
            TDto,
            TDto,
            TConcurrencyToken
        >,
            IConcurrentCrudDtoRepoWithKeyWrapper<TEntity, TKeyWrapper, TDto, TConcurrencyToken>
        where TEntity : class, IAuditableEntity, IEntity
        where TKeyWrapper : class, IKeyWrapper<TEntity>
        where TDto : class
    { }
}
