using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using tools_dotnet.Dao.Crud;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Dto;

namespace tools_dotnet.Service.Abstract
{
    /// <summary>Provides a validated DTO service base with a soft-delete lifecycle.</summary>
    public abstract class BaseSoftDeleteCrudService<TEntity, TIdType, TDto, TRepo, TValidator>
        : BaseCrudService<TEntity, TIdType, TDto, TRepo, TValidator>,
            ISoftDeleteCrudService<TDto, TIdType>
        where TEntity : class, IAuditableEntity, IEntityWithId<TIdType>
        where TIdType : struct
        where TDto : class, IDtoWithId<TIdType>
        where TRepo : ISoftDeleteCrudDtoRepo<TEntity, TIdType, TDto>
        where TValidator : IValidator<TDto>
    {
        /// <summary>Initializes a new instance of <c>BaseSoftDeleteCrudService</c>.</summary>
        protected BaseSoftDeleteCrudService(IMapper mapper, TRepo baseRepo, TValidator validator)
            : base(mapper, baseRepo, validator) { }

        /// <summary>Retrieves active and soft-deleted items asynchronously.</summary>
        public virtual async Task<IEnumerable<TDto>> GetAllIncludingDeletedAsync(
            CancellationToken cancellationToken = default
        )
        {
            return await _baseRepo.GetAllDtoAsync(
                SoftDeleteQueryMode.IncludeDeleted,
                cancellationToken
            );
        }

        /// <summary>Retrieves only soft-deleted items asynchronously.</summary>
        public virtual async Task<IEnumerable<TDto>> GetAllDeletedAsync(
            CancellationToken cancellationToken = default
        )
        {
            return await _baseRepo.GetAllDtoAsync(
                SoftDeleteQueryMode.DeletedOnly,
                cancellationToken
            );
        }

        /// <summary>Retrieves an item by its identifier, including soft-deleted items.</summary>
        public virtual async Task<TDto> GetByIdIncludingDeletedAsync(
            TIdType id,
            CancellationToken cancellationToken = default
        )
        {
            return await _baseRepo.GetByIdDtoAsync(
                id,
                SoftDeleteQueryMode.IncludeDeleted,
                cancellationToken
            );
        }

        /// <summary>Restores a soft-deleted item asynchronously.</summary>
        public virtual async Task RestoreAsync(
            TIdType id,
            CancellationToken cancellationToken = default
        )
        {
            await _baseRepo.RestoreAsync(id, cancellationToken);
        }

        /// <summary>Permanently removes an item asynchronously.</summary>
        public virtual async Task HardRemoveAsync(
            TIdType id,
            CancellationToken cancellationToken = default
        )
        {
            await _baseRepo.HardRemoveAsync(id, cancellationToken);
        }
    }
}
