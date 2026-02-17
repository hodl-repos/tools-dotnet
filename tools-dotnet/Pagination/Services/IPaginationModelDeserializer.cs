using tools_dotnet.Pagination.Models;

namespace tools_dotnet.Pagination.Services
{
    public interface IPaginationModelDeserializer
    {
        DeserializedPaginationModel Deserialize(PaginationModel model);
    }
}
