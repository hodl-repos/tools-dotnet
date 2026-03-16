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
    public abstract class BaseConcurrentCrudServiceWithKeyWrapper<
        TEntity,
        TKeyWrapper,
        TDto,
        TConcurrencyToken,
        TRepo,
        TValidator
    > : IConcurrentCrudServiceWithKeyWrapper<TEntity, TKeyWrapper, TDto, TConcurrencyToken>
        where TEntity : class, IEntity
        where TKeyWrapper : class, IKeyWrapper<TEntity>
        where TDto : class, IDto
        where TRepo : IConcurrentCrudDtoRepoWithKeyWrapper<
            TEntity,
            TKeyWrapper,
            TDto,
            TConcurrencyToken
        >
        where TValidator : IValidator<TDto>
    {
        protected readonly IMapper _mapper;
        protected readonly TRepo _baseRepo;
        protected readonly TValidator _validator;

        protected BaseConcurrentCrudServiceWithKeyWrapper(
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

        public virtual async Task<TConcurrencyToken> GetConcurrencyTokenAsync(
            TKeyWrapper keyWrapper,
            CancellationToken cancellationToken = default
        )
        {
            return await _baseRepo.GetConcurrencyTokenAsync(keyWrapper, cancellationToken);
        }

        public virtual async Task UpdateAsync(
            TKeyWrapper keyWrapper,
            TDto item,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        )
        {
            await SetAndValidateKeyAsync(item, keyWrapper, cancellationToken);
            await _validator.ValidateAndThrowAsync(item, cancellationToken);
            await _baseRepo.UpdateAsync(keyWrapper, item, concurrencyToken, cancellationToken);
        }

        public virtual async Task RemoveAsync(
            TKeyWrapper keyWrapper,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        )
        {
            await _baseRepo.RemoveAsync(keyWrapper, concurrencyToken, cancellationToken);
        }
    }
}
