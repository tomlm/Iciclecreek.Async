using System.Reflection;

namespace Iciclecreek.Async;

public static class Extensions
{
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
