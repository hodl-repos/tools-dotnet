using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace tools_dotnet.Utility
{
    /// <inheritdoc />
    public class AsyncQueue<T> : IAsyncQueue<T>
    {
        private readonly SemaphoreSlim _enumerationSemaphore = new SemaphoreSlim(1);
        private readonly BufferBlock<T> _bufferBlock = new BufferBlock<T>();

        public void Enqueue(T item) => _bufferBlock.Post(item);

        public async Task<IAsyncEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            // We lock this so we only ever enumerate once at a time.
            // That way we ensure all items are returned in a continuous
            // fashion with no 'holes' in the data when two foreach compete.
            await _enumerationSemaphore.WaitAsync(cancellationToken);

            try
            {
                return _bufferBlock.ReceiveAllAsync(cancellationToken);
            }
            finally
            {
                _enumerationSemaphore.Release();
            }
        }

        public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)

        {
            // We lock this so we only ever enumerate once at a time.
            // That way we ensure all items are returned in a continuous
            // fashion with no 'holes' in the data when two foreach compete.
            await _enumerationSemaphore.WaitAsync(cancellationToken);

            try
            {
                // Return new elements until cancellationToken is triggered.
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Make sure to throw on cancellation so the Task will transfer into a canceled state
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return await _bufferBlock.ReceiveAsync(cancellationToken);
                }
            }
            finally
            {
                _enumerationSemaphore.Release();
            }
        }
    }
}