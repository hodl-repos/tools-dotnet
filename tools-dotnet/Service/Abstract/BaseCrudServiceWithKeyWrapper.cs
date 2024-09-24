using AutoMapper;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using tools_dotnet.Dao.Crud;
using tools_dotnet.Dao.Entity;
using tools_dotnet.Dao.KeyWrapper;
using tools_dotnet.Dto;
using tools_dotnet.Paging;

namespace tools_dotnet.Service.Abstract
{
    public abstract class BaseCrudServiceWithKeyWrapper<TEntity, TKeyWrapper, TDto, TRepo, TValidator> : ICrudServiceWithKeyWrapper<TEntity, TKeyWrapper, TDto>
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
            TValidator validator)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _baseRepo = baseRepo ?? throw new ArgumentNullException(nameof(baseRepo));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        protected abstract Task SetAndValidateKeyAsync(TDto item, TKeyWrapper keyWrapper);

        public virtual async Task<TKeyWrapper> AddAsync(TKeyWrapper keyWrapper, TDto item)
        {
            await SetAndValidateKeyAsync(item, keyWrapper);

            await _validator.ValidateAndThrowAsync(item);

            return await _baseRepo.AddAsync(keyWrapper, item);
        }

        public virtual async Task<IEnumerable<TDto>> GetAllAsync(TKeyWrapper keyWrapper)
        {
            return await _baseRepo.GetAllDtoAsync(keyWrapper.GetContainingResourceFilter());
        }

        public virtual async Task<IPagedList<TDto>> GetAllAsync(IApiSieve apiSieve, TKeyWrapper keyWrapper)
        {
            return await _baseRepo.GetAllDtoAsync(apiSieve, keyWrapper.GetContainingResourceFilter());
        }

        public virtual async Task<TDto> GetByIdAsync(TKeyWrapper keyWrapper)
        {
            return await _baseRepo.GetByIdDtoAsync(keyWrapper);
        }

        public virtual async Task UpdateAsync(TKeyWrapper keyWrapper, TDto item)
        {
            await SetAndValidateKeyAsync(item, keyWrapper);

            await _validator.ValidateAndThrowAsync(item);
            await _baseRepo.UpdateAsync(keyWrapper, item);
        }

        public virtual async Task RemoveAsync(TKeyWrapper keyWrapper)
        {
            await _baseRepo.RemoveAsync(keyWrapper);
        }
    }
}