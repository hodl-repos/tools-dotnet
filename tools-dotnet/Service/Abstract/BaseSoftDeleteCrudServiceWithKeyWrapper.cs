using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using tools_dotnet.Dao.Crud;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Dao.KeyWrapper;
using tools_dotnet.Dto;

namespace tools_dotnet.Service.Abstract
{
    public abstract class BaseSoftDeleteCrudServiceWithKeyWrapper<
        TEntity,
        TKeyWrapper,
        TDto,
        TRepo,
        TValidator
    > : BaseCrudServiceWithKeyWrapper<TEntity, TKeyWrapper, TDto, TRepo, TValidator>,
            ISoftDeleteCrudServiceWithKeyWrapper<TEntity, TKeyWrapper, TDto>
        where TEntity : class, IAuditableEntity, IEntity
        where TKeyWrapper : class, IKeyWrapper<TEntity>
        where TDto : class, IDto
        where TRepo : ISoftDeleteCrudDtoRepoWithKeyWrapper<TEntity, TKeyWrapper, TDto>
        where TValidator : IValidator<TDto>
    {
        protected BaseSoftDeleteCrudServiceWithKeyWrapper(
            IMapper mapper,
            TRepo baseRepo,
            TValidator validator
        )
            : base(mapper, baseRepo, validator) { }

        public virtual async Task<IEnumerable<TDto>> GetAllIncludingDeletedAsync(
            TKeyWrapper keyWrapper,
            CancellationToken cancellationToken = default
        )
        {
            return await _baseRepo.GetAllDtoAsync(
                keyWrapper.GetContainingResourceFilter(),
                SoftDeleteQueryMode.IncludeDeleted,
                cancellationToken
            );
        }

        public virtual async Task<IEnumerable<TDto>> GetAllDeletedAsync(
            TKeyWrapper keyWrapper,
            CancellationToken cancellationToken = default
        )
        {
            return await _baseRepo.GetAllDtoAsync(
                keyWrapper.GetContainingResourceFilter(),
                SoftDeleteQueryMode.DeletedOnly,
                cancellationToken
            );
        }

        public virtual async Task<TDto> GetByIdIncludingDeletedAsync(
            TKeyWrapper keyWrapper,
            CancellationToken cancellationToken = default
        )
        {
            return await _baseRepo.GetByIdDtoAsync(
                keyWrapper,
                SoftDeleteQueryMode.IncludeDeleted,
                cancellationToken
            );
        }

        public virtual async Task RestoreAsync(
            TKeyWrapper keyWrapper,
            CancellationToken cancellationToken = default
        )
        {
            await _baseRepo.RestoreAsync(keyWrapper, cancellationToken);
        }

        public virtual async Task HardRemoveAsync(
            TKeyWrapper keyWrapper,
            CancellationToken cancellationToken = default
        )
        {
            await _baseRepo.HardRemoveAsync(keyWrapper, cancellationToken);
        }
    }
}
