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

        public virtual async Task<TIdType> AddAsync(TDto item)
        {
            await _validator.ValidateAndThrowAsync(item);

            return await _baseRepo.AddAsync(item);
        }

        public virtual async Task<IEnumerable<TDto>> GetAllAsync()
        {
            return await _baseRepo.GetAllDtoAsync();
        }

        public virtual async Task<IEnumerable<TDto>> GetAllIncludingDeletedAsync()
        {
            return await _baseRepo.GetAllDtoIncludingDeletedAsync();
        }

        public virtual async Task<IEnumerable<TDto>> GetAllDeletedAsync()
        {
            return await _baseRepo.GetAllDeletedDtoAsync();
        }

        public virtual async Task<IPagedList<TDto>> GetAllAsync(IApiPagination apiPagination)
        {
            return await _baseRepo.GetAllDtoAsync(apiPagination);
        }

        public virtual async Task<TDto> GetByIdAsync(TIdType id)
        {
            return await _baseRepo.GetByIdDtoAsync(id);
        }

        public virtual async Task<TDto> GetByIdIncludingDeletedAsync(TIdType id)
        {
            return await _baseRepo.GetByIdDtoIncludingDeletedAsync(id);
        }

        public virtual async Task RemoveAsync(TIdType id)
        {
            await _baseRepo.RemoveAsync(id);
        }

        public virtual async Task RestoreAsync(TIdType id)
        {
            await _baseRepo.RestoreAsync(id);
        }

        public virtual async Task HardRemoveAsync(TIdType id)
        {
            await _baseRepo.HardRemoveAsync(id);
        }

        public virtual async Task UpdateAsync(TDto item)
        {
            await _validator.ValidateAndThrowAsync(item);

            await _baseRepo.UpdateAsync(item);
        }
    }
}
