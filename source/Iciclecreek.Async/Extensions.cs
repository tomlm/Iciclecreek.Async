using System.Reflection;
using System.Threading.Tasks;

namespace Iciclecreek.Async;

public static class Extensions
{
    public static IEnumerable<TResult> SelectParallelAsync<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, Task<TResult>> selector, int maxParallel = int.MaxValue)
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
                    await semaphore.WaitAsync();
                    return await selector(item, pos);
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }
        Task.WaitAll(tasks.ToArray());
        return tasks.Select(t => t.Result);
    }

    public static IEnumerable<TSource> WhereParallelAsync<TSource>(this IEnumerable<TSource> source, Func<TSource, int, Task<bool>> selector, int maxParallel = int.MaxValue)
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
                        await semaphore.WaitAsync();
                        var result = await selector(item, pos);
                        return new WhereResult<TSource>() { Item = item, Result = result };
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
        }
        Task.WaitAll(tasks.ToArray());
        return tasks.Select(t => t.Result).Where(task => task.Result).Select(task => task.Item!);
    }

    public static IEnumerable<TSource> WaitAll<TSource>(this IEnumerable<Task<TSource>> source)
    {
        var list = source.ToArray();
        Task.WaitAll(list);
        return list.Select(t => t.Result);
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
