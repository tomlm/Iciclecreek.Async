using System.Reflection;

namespace Iciclecreek.Async;

public static class Extensions
{
    public static IEnumerable<TResult> SelectParallelAsync<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, Task<TResult>> selector, int maxParallel = int.MaxValue)
    {
        if (source is ParallelQuery<TSource>)
        {
            object? settings = source.GetType().GetProperty("SpecifiedQuerySettings", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(source);
            if (settings != null)
                maxParallel = (int?)settings.GetType().GetProperty("DegreeOfParallelism", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(settings) ?? maxParallel;
        }
        SemaphoreSlim semaphore = new SemaphoreSlim(maxParallel);
        var tasks = new List<Task<TResult>>();
        int index = -1;
        foreach (var item in source)
        {
            index = index + 1;
            tasks.Add(semaphore.WaitAsync()
                .ContinueWith(t =>
                {
                    return selector(item, index)
                        .ContinueWith(t =>
                        {
                            semaphore.Release();
                            return t.Result;
                        });
                }).Unwrap());
        }
        Task.WaitAll(tasks.ToArray());
        return tasks.Select(t => t.Result);
    }


    public static IEnumerable<TSource> WaitAll<TSource>(this IEnumerable<Task<TSource>> source)
    {
        var list = source.ToArray();
        Task.WaitAll(list);
        return list.Select(t => t.Result);
    }

}
