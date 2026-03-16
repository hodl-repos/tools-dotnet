using tools_dotnet.Pagination.Models;

namespace tools_dotnet.Pagination.Services
{
    /// <summary>
    /// Parses a raw <see cref="PaginationModel"/> into a validated model.
    /// </summary>
    public interface IPaginationModelDeserializer
    {
        /// <summary>
        /// Parses and normalizes a pagination model.
        /// </summary>
        /// <param name="model">Raw model from API query values.</param>
        /// <returns>Parsed and normalized model.</returns>
        DeserializedPaginationModel Deserialize(PaginationModel model);
    }
}
