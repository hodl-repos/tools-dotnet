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
    /// <summary>Provides a validated key-wrapper CRUD service base for DTOs.</summary>
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
        /// <summary>Gets the mapper used for entity and DTO conversion.</summary>
        protected readonly IMapper _mapper;
        /// <summary>Gets the repository used by the service.</summary>
        protected readonly TRepo _baseRepo;
        /// <summary>Gets the validator applied before write operations.</summary>
        protected readonly TValidator _validator;

        /// <summary>Initializes a new instance of <c>BaseCrudServiceWithKeyWrapper</c>.</summary>
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

        /// <summary>Applies and validates the key-wrapper values on an item asynchronously.</summary>
        protected virtual Task SetAndValidateKeyAsync(
            TDto item,
            TKeyWrapper keyWrapper,
            CancellationToken cancellationToken
        )
        {
            return SetAndValidateKeyAsync(item, keyWrapper);
        }

        /// <summary>Adds a new item asynchronously.</summary>
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

        /// <summary>Retrieves all available items asynchronously.</summary>
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

        /// <summary>Retrieves all available items asynchronously.</summary>
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

        /// <summary>Retrieves an item by its identifier asynchronously.</summary>
        public virtual async Task<TDto> GetByIdAsync(
            TKeyWrapper keyWrapper,
            CancellationToken cancellationToken = default
        )
        {
            return await _baseRepo.GetByIdDtoAsync(keyWrapper, cancellationToken: cancellationToken);
        }

        /// <summary>Updates an existing item asynchronously.</summary>
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

        /// <summary>Removes an item asynchronously.</summary>
        public virtual async Task RemoveAsync(
            TKeyWrapper keyWrapper,
            CancellationToken cancellationToken = default
        )
        {
            await _baseRepo.RemoveAsync(keyWrapper, cancellationToken);
        }

    }
}
