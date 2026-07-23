namespace tools_dotnet.Dao.Entity
{
    /// <summary>Defines an entity with a mutable identifier.</summary>
    public interface IEntityWithId<T> : IEntity
    {
        /// <summary>Gets or sets the item identifier.</summary>
        T Id { get; set; }
    }
}
