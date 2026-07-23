using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using tools_dotnet.Dao.Crud;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Dto;

namespace tools_dotnet.Service.Abstract
{
    /// <summary>Provides a validated DTO service base with soft delete and optimistic concurrency.</summary>
    public abstract class BaseConcurrentSoftDeleteCrudService<
        TEntity,
        TIdType,
        TDto,
        TConcurrencyToken,
        TRepo,
        TValidator
    > : BaseConcurrentCrudService<TEntity, TIdType, TDto, TConcurrencyToken, TRepo, TValidator>,
            IConcurrentSoftDeleteCrudService<TDto, TIdType, TConcurrencyToken>
        where TEntity : class, IAuditableEntity, IEntityWithId<TIdType>
        where TIdType : struct
        where TDto : class, IDtoWithId<TIdType>
        where TRepo : IConcurrentSoftDeleteCrudDtoRepo<
            TEntity,
            TIdType,
            TDto,
            TConcurrencyToken
        >
        where TValidator : IValidator<TDto>
    {
        /// <summary>Initializes a new instance of <c>BaseConcurrentSoftDeleteCrudService</c>.</summary>
        protected BaseConcurrentSoftDeleteCrudService(
            IMapper mapper,
            TRepo baseRepo,
            TValidator validator
        )
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
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        )
        {
            await _baseRepo.RestoreAsync(id, concurrencyToken, cancellationToken);
        }

        /// <summary>Permanently removes an item asynchronously.</summary>
        public virtual async Task HardRemoveAsync(
            TIdType id,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        )
        {
            await _baseRepo.HardRemoveAsync(id, concurrencyToken, cancellationToken);
        }
    }
}
