namespace tools_dotnet.PropertyProcessor
{
    public interface IPropertyParser<T>
    {
        bool CanParse(string value);

        T Parse(string value);
    }
}