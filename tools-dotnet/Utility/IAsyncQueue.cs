using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace tools_dotnet.Utility
{
    /// <summary>
    /// Lets a service queue items which are than handled from another service
    /// the order of the queue remains always the same (FIFO)
    /// Enqueue will block when there is already an unhandled item
    /// </summary>
    public interface IAsyncQueue<T> : IAsyncEnumerable<T>
    {
        void Enqueue(T item);

        Task<IAsyncEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    }
}