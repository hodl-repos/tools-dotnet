using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using tools_dotnet.Dao.Crud;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Dao.KeyWrapper;
using tools_dotnet.Dto;
using tools_dotnet.Paging;

namespace tools_dotnet.Service.Abstract
{
    public abstract class BaseCrudServiceWithKeyWrapper<
        TEntity,
        TKeyWrapper,
        TDto,
        TRepo,
        TValidator
    > : ICrudServiceWithKeyWrapper<TEntity, TKeyWrapper, TDto>
        where TEntity : class, IEntity
        where TKeyWrapper : class, IKeyWrapper<TEntity>
        where TDto : class, IDto
        where TRepo : ICrudDtoRepoWithKeyWrapper<TEntity, TKeyWrapper, TDto>
        where TValidator : IValidator<TDto>
    {
        protected readonly IMapper _mapper;
        protected readonly TRepo _baseRepo;
        protected readonly TValidator _validator;

        protected BaseCrudServiceWithKeyWrapper(
            IMapper mapper,
            TRepo baseRepo,
            TValidator validator
        )
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _baseRepo = baseRepo ?? throw new ArgumentNullException(nameof(baseRepo));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        /// <summary>
        /// must check if the parent-ressources are correctly set or set them
        /// eg.: customer.company_id get set here, but not the primary ID
        /// </summary>
        protected abstract Task SetAndValidateKeyAsync(TDto item, TKeyWrapper keyWrapper);

        protected virtual Task SetAndValidateKeyAsync(
            TDto item,
            TKeyWrapper keyWrapper,
            CancellationToken cancellationToken
        )
        {
            return SetAndValidateKeyAsync(item, keyWrapper);
        }

        public virtual async Task<TKeyWrapper> AddAsync(
            TKeyWrapper keyWrapper,
            TDto item,
            CancellationToken cancellationToken = default
        )
        {
            await SetAndValidateKeyAsync(item, keyWrapper, cancellationToken);

            await _validator.ValidateAndThrowAsync(item, cancellationToken);

            return await _baseRepo.AddAsync(keyWrapper, item, cancellationToken);
        }

        public virtual async Task<IEnumerable<TDto>> GetAllAsync(
            TKeyWrapper keyWrapper,
            CancellationToken cancellationToken = default
        )
        {
            return await _baseRepo.GetAllDtoAsync(
                keyWrapper.GetContainingResourceFilter(),
                cancellationToken: cancellationToken
            );
        }

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

        public virtual async Task<IPagedList<TDto>> GetAllAsync(
            IApiPagination apiPagination,
            TKeyWrapper keyWrapper,
            CancellationToken cancellationToken = default
        )
        {
            return await _baseRepo.GetAllDtoAsync(
                apiPagination,
                keyWrapper.GetContainingResourceFilter(),
                cancellationToken: cancellationToken
            );
        }

        public virtual async Task<TDto> GetByIdAsync(
            TKeyWrapper keyWrapper,
            CancellationToken cancellationToken = default
        )
        {
            return await _baseRepo.GetByIdDtoAsync(keyWrapper, cancellationToken: cancellationToken);
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

        public virtual async Task UpdateAsync(
            TKeyWrapper keyWrapper,
            TDto item,
            CancellationToken cancellationToken = default
        )
        {
            await SetAndValidateKeyAsync(item, keyWrapper, cancellationToken);

            await _validator.ValidateAndThrowAsync(item, cancellationToken);
            await _baseRepo.UpdateAsync(keyWrapper, item, cancellationToken);
        }

        public virtual async Task RemoveAsync(
            TKeyWrapper keyWrapper,
            CancellationToken cancellationToken = default
        )
        {
            await _baseRepo.RemoveAsync(keyWrapper, cancellationToken);
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
