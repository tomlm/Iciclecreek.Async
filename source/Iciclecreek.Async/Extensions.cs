using System.Reflection;

namespace Iciclecreek.Async;

public static class Extensions
{
    /// <summary>
    /// SelectParallelAsync() - transform each item with max parallism on async threads, and wait for all to complete before continuing
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="source"></param>
    /// <param name="selector"></param>
    /// <param name="maxParallel"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static IList<TResult> SelectParallelAsync<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, CancellationToken, Task<TResult>> selector, int maxParallel = int.MaxValue, CancellationToken cancellationToken = default)
    {
        maxParallel = GetMaxParallel(source, maxParallel);
        SemaphoreSlim semaphore = new SemaphoreSlim(maxParallel);
        var tasks = new List<Task<TResult>>();
        int index = 0;
        foreach (var item in source)
        {
            var pos = index++;

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await semaphore.WaitAsync(cancellationToken);
                    return await selector(item, pos, cancellationToken);
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }
        Task.WaitAll(tasks.ToArray(), cancellationToken);
        return tasks.Select(t => t.Result).ToList();
    }

    /// <summary>
    /// WhereParallelAsync() - Filter each item in parallel with async and wait for all to complete before continuing
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <param name="source"></param>
    /// <param name="selector"></param>
    /// <param name="maxParallel"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static IList<TSource> WhereParallelAsync<TSource>(this IEnumerable<TSource> source, Func<TSource, int, CancellationToken, Task<bool>> selector, int maxParallel = int.MaxValue, CancellationToken cancellationToken = default)
    {
        maxParallel = GetMaxParallel(source, maxParallel);
        SemaphoreSlim semaphore = new SemaphoreSlim(maxParallel);

        var tasks = new List<Task<WhereResult<TSource>>>();
        int index = 0;
        foreach (var item in source)
        {
            var pos = index++;

            tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await semaphore.WaitAsync(cancellationToken);
                        var result = await selector(item, pos, cancellationToken);
                        return new WhereResult<TSource>() { Item = item, Result = result };
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
        }
        Task.WaitAll(tasks.ToArray(), cancellationToken);
        return tasks
            .Where(task => task.Result.Result)
            .Select(task => task.Result.Item!).ToList();
    }

    /// <summary>
    /// ForEachParallelAsync() - Perform action on each item in parallel and wait for them all to finish before continuing.
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <param name="source"></param>
    /// <param name="action"></param>
    /// <param name="maxParallel"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static IList<TSource> ForEachParallelAsync<TSource>(this IEnumerable<TSource> source, Func<TSource, int, CancellationToken, Task> action, int maxParallel = int.MaxValue, CancellationToken cancellationToken = default)
    {
        maxParallel = GetMaxParallel(source, maxParallel);
        SemaphoreSlim semaphore = new SemaphoreSlim(maxParallel);

        var tasks = new List<Task>();
        int index = 0;
        List<TSource> results = source.ToList();
        foreach (var item in results)
        {
            var pos = index++;
            var i = item;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await semaphore.WaitAsync(cancellationToken);
                    await action(i, pos, cancellationToken);
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }
        Task.WaitAll(tasks.ToArray(), cancellationToken);
        return results;
    }

    /// <summary>
    /// WaitAll() - Wait for a collection of tasks to finish before continuing.
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <param name="source"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static IList<TSource> WaitAll<TSource>(this IEnumerable<Task<TSource>> source, CancellationToken cancellationToken = default)
    {
        var list = source.ToArray();
        Task.WaitAll(list, cancellationToken);
        return list.Select(t => t.Result).ToList();
    }

    private static int GetMaxParallel<TSource>(IEnumerable<TSource> source, int maxParallel)
    {
        if (source is ParallelQuery<TSource>)
        {
            object? settings = source.GetType().GetProperty("SpecifiedQuerySettings", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(source);
            if (settings != null)
                maxParallel = (int?)settings.GetType().GetProperty("DegreeOfParallelism", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(settings) ?? maxParallel;
        }

        return maxParallel;
    }

    internal class WhereResult<T>
    {
        public bool Result { get; set; }

        public T? Item { get; set; }
    }

}
