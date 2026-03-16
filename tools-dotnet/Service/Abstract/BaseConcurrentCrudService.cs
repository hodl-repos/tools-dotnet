using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using tools_dotnet.Dao.Crud;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Dto;
using tools_dotnet.Paging;

namespace tools_dotnet.Service.Abstract
{
    public abstract class BaseConcurrentCrudService<
        TEntity,
        TIdType,
        TDto,
        TConcurrencyToken,
        TRepo,
        TValidator
    > : IConcurrentCrudService<TDto, TIdType, TConcurrencyToken>
        where TEntity : class, IEntityWithId<TIdType>
        where TIdType : struct
        where TDto : class, IDtoWithId<TIdType>
        where TRepo : IConcurrentCrudDtoRepo<TEntity, TIdType, TDto, TConcurrencyToken>
        where TValidator : IValidator<TDto>
    {
        protected readonly IMapper _mapper;
        protected readonly TRepo _baseRepo;
        protected readonly TValidator _validator;

        protected BaseConcurrentCrudService(IMapper mapper, TRepo baseRepo, TValidator validator)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _baseRepo = baseRepo ?? throw new ArgumentNullException(nameof(baseRepo));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public virtual async Task<TIdType> AddAsync(
            TDto item,
            CancellationToken cancellationToken = default
        )
        {
            await _validator.ValidateAndThrowAsync(item, cancellationToken);

            return await _baseRepo.AddAsync(item, cancellationToken);
        }

        public virtual async Task<IEnumerable<TDto>> GetAllAsync(
            CancellationToken cancellationToken = default
        )
        {
            return await _baseRepo.GetAllDtoAsync(cancellationToken: cancellationToken);
        }

        public virtual async Task<IPagedList<TDto>> GetAllAsync(
            IApiPagination apiPagination,
            CancellationToken cancellationToken = default
        )
        {
            return await _baseRepo.GetAllDtoAsync(apiPagination, cancellationToken: cancellationToken);
        }

        public virtual async Task<TDto> GetByIdAsync(
            TIdType id,
            CancellationToken cancellationToken = default
        )
        {
            return await _baseRepo.GetByIdDtoAsync(id, cancellationToken: cancellationToken);
        }

        public virtual async Task<TConcurrencyToken> GetConcurrencyTokenAsync(
            TIdType id,
            CancellationToken cancellationToken = default
        )
        {
            return await _baseRepo.GetConcurrencyTokenAsync(id, cancellationToken);
        }

        public virtual async Task UpdateAsync(
            TDto item,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        )
        {
            await _validator.ValidateAndThrowAsync(item, cancellationToken);
            await _baseRepo.UpdateAsync(item, concurrencyToken, cancellationToken);
        }

        public virtual async Task RemoveAsync(
            TIdType id,
            TConcurrencyToken concurrencyToken,
            CancellationToken cancellationToken = default
        )
        {
            await _baseRepo.RemoveAsync(id, concurrencyToken, cancellationToken);
        }
    }
}
