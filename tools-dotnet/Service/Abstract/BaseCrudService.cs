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
    public abstract class BaseCrudService<TEntity, TIdType, TDto, TRepo, TValidator>
        : ICrudService<TDto, TIdType>
        where TEntity : class, IEntityWithId<TIdType>
        where TIdType : struct
        where TDto : class, IDtoWithId<TIdType>
        where TRepo : ICrudDtoRepo<TEntity, TIdType, TDto>
        where TValidator : IValidator<TDto>
    {
        protected readonly IMapper _mapper;
        protected readonly TRepo _baseRepo;
        protected readonly TValidator _validator;

        protected BaseCrudService(IMapper mapper, TRepo baseRepo, TValidator validator)
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
            return await _baseRepo.GetAllDtoAsync(cancellationToken);
        }

        public virtual async Task<IEnumerable<TDto>> GetAllIncludingDeletedAsync(
            CancellationToken cancellationToken = default
        )
        {
            return await _baseRepo.GetAllDtoIncludingDeletedAsync(cancellationToken);
        }

        public virtual async Task<IEnumerable<TDto>> GetAllDeletedAsync(
            CancellationToken cancellationToken = default
        )
        {
            return await _baseRepo.GetAllDeletedDtoAsync(cancellationToken);
        }

        public virtual async Task<IPagedList<TDto>> GetAllAsync(
            IApiPagination apiPagination,
            CancellationToken cancellationToken = default
        )
        {
            return await _baseRepo.GetAllDtoAsync(apiPagination, cancellationToken);
        }

        public virtual async Task<TDto> GetByIdAsync(
            TIdType id,
            CancellationToken cancellationToken = default
        )
        {
            return await _baseRepo.GetByIdDtoAsync(id, cancellationToken);
        }

        public virtual async Task<TDto> GetByIdIncludingDeletedAsync(
            TIdType id,
            CancellationToken cancellationToken = default
        )
        {
            return await _baseRepo.GetByIdDtoIncludingDeletedAsync(id, cancellationToken);
        }

        public virtual async Task RemoveAsync(
            TIdType id,
            CancellationToken cancellationToken = default
        )
        {
            await _baseRepo.RemoveAsync(id, cancellationToken);
        }

        public virtual async Task RestoreAsync(
            TIdType id,
            CancellationToken cancellationToken = default
        )
        {
            await _baseRepo.RestoreAsync(id, cancellationToken);
        }

        public virtual async Task HardRemoveAsync(
            TIdType id,
            CancellationToken cancellationToken = default
        )
        {
            await _baseRepo.HardRemoveAsync(id, cancellationToken);
        }

        public virtual async Task UpdateAsync(
            TDto item,
            CancellationToken cancellationToken = default
        )
        {
            await _validator.ValidateAndThrowAsync(item, cancellationToken);

            await _baseRepo.UpdateAsync(item, cancellationToken);
        }
    }
}
