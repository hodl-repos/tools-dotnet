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
    /// <summary>Provides a validated CRUD service base for DTOs.</summary>
    public abstract class BaseCrudService<TEntity, TIdType, TDto, TRepo, TValidator>
        : ICrudService<TDto, TIdType>
        where TEntity : class, IEntityWithId<TIdType>
        where TIdType : struct
        where TDto : class, IDtoWithId<TIdType>
        where TRepo : ICrudDtoRepo<TEntity, TIdType, TDto>
        where TValidator : IValidator<TDto>
    {
        /// <summary>Gets the mapper used for entity and DTO conversion.</summary>
        protected readonly IMapper _mapper;
        /// <summary>Gets the repository used by the service.</summary>
        protected readonly TRepo _baseRepo;
        /// <summary>Gets the validator applied before write operations.</summary>
        protected readonly TValidator _validator;

        /// <summary>Initializes a new instance of <c>BaseCrudService</c>.</summary>
        protected BaseCrudService(IMapper mapper, TRepo baseRepo, TValidator validator)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _baseRepo = baseRepo ?? throw new ArgumentNullException(nameof(baseRepo));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        /// <summary>Adds a new item asynchronously.</summary>
        public virtual async Task<TIdType> AddAsync(
            TDto item,
            CancellationToken cancellationToken = default
        )
        {
            await _validator.ValidateAndThrowAsync(item, cancellationToken);

            return await _baseRepo.AddAsync(item, cancellationToken);
        }

        /// <summary>Retrieves all available items asynchronously.</summary>
        public virtual async Task<IEnumerable<TDto>> GetAllAsync(
            CancellationToken cancellationToken = default
        )
        {
            return await _baseRepo.GetAllDtoAsync(cancellationToken: cancellationToken);
        }

        /// <summary>Retrieves all available items asynchronously.</summary>
        public virtual async Task<IPagedList<TDto>> GetAllAsync(
            IApiPagination apiPagination,
            CancellationToken cancellationToken = default
        )
        {
            return await _baseRepo.GetAllDtoAsync(apiPagination, cancellationToken: cancellationToken);
        }

        /// <summary>Retrieves an item by its identifier asynchronously.</summary>
        public virtual async Task<TDto> GetByIdAsync(
            TIdType id,
            CancellationToken cancellationToken = default
        )
        {
            return await _baseRepo.GetByIdDtoAsync(id, cancellationToken: cancellationToken);
        }

        /// <summary>Removes an item asynchronously.</summary>
        public virtual async Task RemoveAsync(
            TIdType id,
            CancellationToken cancellationToken = default
        )
        {
            await _baseRepo.RemoveAsync(id, cancellationToken);
        }

        /// <summary>Updates an existing item asynchronously.</summary>
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
