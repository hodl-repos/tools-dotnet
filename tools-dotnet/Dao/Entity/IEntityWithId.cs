namespace tools_dotnet.Dao.Entity
{
    public interface IEntityWithId<T> : IEntity
    {
        T Id { get; set; }
    }
}