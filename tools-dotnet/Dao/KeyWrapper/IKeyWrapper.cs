using System;
using System.Linq.Expressions;

namespace tools_dotnet.Dao.KeyWrapper
{
    /// <summary>
    /// Allows repos and services to identify entites that are located inside of other entites, eg. customer, which has a company-id it references
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public interface IKeyWrapper<TEntity>
    {
        /// <summary>
        /// is used in Exceptions; return string[] of keys, starting by the top-most key going in to the entity id at last
        /// </summary>
        /// <returns></returns>
        string[] GetKeyAsString();

        /// <summary>
        /// updates the entity in case of missing properties from dto-mapping (eg. CompanyId in DTO may not be needed there, but is in entity)
        /// </summary>
        void UpdateEntityWithContainingResource(TEntity entity);

        /// <summary>
        /// updates the keywrapper by only entity-id's, but not parent-id's as navigation-properties may not be loaded
        /// </summary>
        void UpdateKeyWrapperByEntity(TEntity entity);

        /// <summary>
        /// used to find a entity with the key
        /// </summary>
        Expression<Func<TEntity, bool>> GetKeyFilter();

        /// <summary>
        /// Gets a partial key filter used to filter inside of GetAll
        /// </summary>
        Expression<Func<TEntity, bool>> GetContainingResourceFilter();
    }
}