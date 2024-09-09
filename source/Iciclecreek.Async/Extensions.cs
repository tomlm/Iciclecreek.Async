using System.Reflection;

namespace Iciclecreek.Async;

public static class Extensions
{
    public static IEnumerable<TResult> SelectParallelAsync<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, Task<TResult>> selector, int maxParallel = int.MaxValue)
    {
        maxParallel = GetMaxParallel(source, maxParallel);
        SemaphoreSlim? semaphore = null;
        if (maxParallel > 0 && maxParallel < int.MaxValue)
            semaphore = new SemaphoreSlim(maxParallel);
        var tasks = new List<Task<TResult>>();
        int index = -1;
        foreach (var item in source)
        {
            index = index + 1;
            tasks.Add((semaphore?.WaitAsync() ?? Task.CompletedTask)
                .ContinueWith(t =>
                {
                    return selector(item, index)
                        .ContinueWith(t =>
                        {
                            semaphore?.Release();
                            return t.Result;
                        });
                }).Unwrap());
        }
        Task.WaitAll(tasks.ToArray());
        return tasks.Select(t => t.Result);
    }

    public static IEnumerable<TSource> WhereParallelAsync<TSource>(this IEnumerable<TSource> source, Func<TSource, int, Task<bool>> selector, int maxParallel = int.MaxValue)
    {
        maxParallel = GetMaxParallel(source, maxParallel);
        SemaphoreSlim? semaphore = null;
        if (maxParallel > 0 && maxParallel<int.MaxValue)
            semaphore = new SemaphoreSlim(maxParallel);

        var tasks = new List<Task<WhereResult<TSource>>>();
        int index = -1;
        foreach (var item in source)
        {
            index = index + 1;
            tasks.Add((semaphore?.WaitAsync() ?? Task.CompletedTask)
                .ContinueWith(t =>
                {
                    return selector(item, index)
                        .ContinueWith(t =>
                        {
                            semaphore?.Release();
                            return new WhereResult<TSource>() { Item = item, Result = t.Result };
                        });
                }).Unwrap());
        }
        Task.WaitAll(tasks.ToArray());
        return tasks.Where(t => t.Result.Result).Select(t => t.Result.Item);
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
