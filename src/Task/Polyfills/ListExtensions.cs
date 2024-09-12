using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AggregateConfigBuildTask
{
    public static class ListExtensions
    {
        /// <summary>
        /// Splits a list into chunks of the specified size.
        /// </summary>
        /// <param name="source">The list to split into chunks.</param>
        /// <param name="chunkSize">The size of each chunk.</param>
        /// <returns>An IEnumerable of string arrays, each containing a chunk of the original list.</returns>
        public static IEnumerable<IEnumerable<string>> Chunk(this List<string> source, int chunkSize)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (chunkSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(chunkSize), "Chunk size must be greater than 0.");
            }

            for (int i = 0; i < source.Count; i += chunkSize)
            {
                yield return source.GetRange(i, Math.Min(chunkSize, source.Count - i));
            }
        }

        /// <summary>
        /// Asynchronously processes each element of a collection in parallel, with a limit on the number of concurrent tasks.
        /// </summary>
        /// <typeparam name="TSource">The type of elements in the source collection.</typeparam>
        /// <param name="source">The collection of elements to process.</param>
        /// <param name="degreeOfParallelism">The maximum number of tasks to run concurrently.</param>
        /// <param name="body">The asynchronous delegate to execute for each element in the source collection.</param>
        /// <returns>A task that represents the asynchronous processing of the collection.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the source or body is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the degree of parallelism is less than 1.</exception>
        public static async Task ForEachAsync<TSource>(
            this IEnumerable<TSource> source,
            int degreeOfParallelism,
            Func<TSource, Task> body)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (body == null) throw new ArgumentNullException(nameof(body));
            if (degreeOfParallelism < 1) throw new ArgumentOutOfRangeException(nameof(degreeOfParallelism), "Degree of parallelism must be at least 1.");

            using (var semaphore = new SemaphoreSlim(degreeOfParallelism))
            {
                var tasks = source.Select(async item =>
                {
                    await semaphore.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        await body(item).ConfigureAwait(false);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        }
    }
}
