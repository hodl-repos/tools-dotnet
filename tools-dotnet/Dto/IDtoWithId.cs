namespace tools_dotnet.Dto
{
    /// <summary>Defines a data transfer object with a mutable identifier.</summary>
    public interface IDtoWithId<T> : IDto
    {
        /// <summary>Gets or sets the item identifier.</summary>
        T Id { get; set; }
    }
}
