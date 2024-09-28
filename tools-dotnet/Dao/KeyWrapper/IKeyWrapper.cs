using System;
using System.Linq.Expressions;

namespace tools_dotnet.Dao.KeyWrapper
{
    public interface IKeyWrapper<TEntity>
    {
        string[] GetKeyAsString();

        /// <summary>
        /// updates the entity in case of missing properties from dto-mapping (eg. CompanyId in DTO may not be needed there, but is in entity)
        /// </summary>
        void UpdateEntityWithContainingResource(TEntity entity);

        /// <summary>
        /// updates the keywrapper by only entity-id's, but not parent-id's as navigation-properties may not be loaded
        /// </summary>
        void UpdateKeyWrapperByEntity(TEntity entity);

        Expression<Func<TEntity, bool>> GetKeyFilter();

        /// <summary>
        /// Gets a partial key filter used to filter inside of GetAll
        /// </summary>
        Expression<Func<TEntity, bool>> GetContainingResourceFilter();
    }
}