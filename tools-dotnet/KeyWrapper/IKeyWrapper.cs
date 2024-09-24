using System;
using System.Linq.Expressions;

namespace tools_dotnet.KeyWrapper
{
    public interface IKeyWrapper<TEntity, TIdType> : IKeyWrapper<TEntity>
    {
        TIdType[] GetKey();
    }

    public interface IKeyWrapper<TEntity>
    {
        int KeyCount { get; }

        string[] GetKeyAsString();

        Expression<Func<TEntity, bool>> GetKeyFilter();

        /// <summary>
        /// Gets a partial key filter used to filter inside of GetAll
        /// </summary>
        Expression<Func<TEntity, bool>> GetContainingResourceFilter();
    }
}