namespace tools_dotnet.Dto
{
    public interface IDtoWithId<T> : IDto
    {
        T Id { get; set; }
    }
}