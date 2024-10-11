# Iciclecreek.Async
Async extensions for LINQ and PLINQ queries

# SelectParallelAsync() 
This allows you to run an async select task which runs each task in parallel, with max parallelism.

You can use it with a normal enumerable:
```csharp
    // using an enumerable, will max 10 tasks at a time concurrently
    var results = enumerable
        .SelectParallelAsync(async (item, index) =>
        {
            await ...;
            return result;
        }, maxParallel: 10)
        .ToList();
```

You can use it with a ParallelQuery
```csharp
    // using an enumerable, will max 10 tasks at a time concurrently
    var results = enumerable
        .AsParallel()
        .WithDegreeOfParallelism(10)
        .SelectParallelAsync(async (item, index) =>
        {
            await ...;
            return result;
        })
        .ToList();
```

# WhereParallelAsync() 
This allows you to run an async Where() task which runs each task in parallel, with max parallelism.

You can use it with a normal enumerable:
```csharp
    // using an enumerable, will max 10 tasks at a time concurrently
    var results = enumerable
        .WhereParallelAsync(async (item, index) =>
        {
            await ...;
            return true/false;
        }, maxParallel: 10)
        .ToList();
```

You can use it with a ParallelQuery
```csharp
    // using an enumerable, will max 10 tasks at a time concurrently
    var results = enumerable
        .AsParallel()
        .WithDegreeOfParallelism(10)
        .WhereParallelAsync(async (item, index) =>
        {
            await ...;
            return true/false;
        })
        .ToList();
```

# ForEachParallelAsync()
ForEachParallelAsync() will await all items in an enumerable of Task and return the original objects.
This is useful to perform parallel actions on an enumerable.

```csharp
    var results = enumerable
        .ForEachParallelAsync(async (item) => 
        {
            await ...;
        });
```

# WaitAll()
WaitAll() will await all items in an enumerable of Task<> and return the result of the tasks.

```csharp
    var results = enumerable
        .Select(async (item) => 
        {
            await ...;
            return result;
        })
        .WaitAll() // turns IEnumerable<Task<X>> => IEnumerable<X>
```

> NOTE: You don't need to use WaitAll() on result of SelectParallelAsync(). It does the equivelent of WaitAll() internally.
