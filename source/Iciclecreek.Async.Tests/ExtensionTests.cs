using System.Diagnostics;

namespace Iciclecreek.Async.Tests;

[TestClass]
public class ExtensionTests
{

    public class Item
    {
        public int Value { get; set; }
        public bool IsEven { get; set; }
        public int Index { get; set; }
    }

    [TestMethod]
    public void SelectParallelAsync_MaxParallel()
    {
        Random rnd = new Random();
        var count = 20;
        var numbers = Enumerable.Range(0, count);

        Stopwatch sw = new Stopwatch();
        sw.Start();

        HashSet<int> ints = new HashSet<int>();
        foreach (var result in numbers
            .SelectParallelAsync(selectAction, int.MaxValue)
            .Where(item => item.IsEven))
        {
            ints.Add(result.Index);
            Assert.AreEqual(true, result.IsEven);
        }
        sw.Stop();
        for (int i = 0; i < count; i += 2)
            Assert.IsTrue(ints.Contains(i));
        Assert.AreEqual(1, sw.Elapsed.Seconds);
    }


    [TestMethod]
    public void SelectParallelAsync_Enumerable()
    {
        Random rnd = new Random();
        var count = 20;
        var numbers = Enumerable.Range(0, count);

        Stopwatch sw = new Stopwatch();
        sw.Start();

        foreach (var result in numbers
            .SelectParallelAsync(selectAction, 10)
            .Where(item => item.IsEven))
        {
            Assert.AreEqual(true, result.IsEven);
        }
        sw.Stop();
        Assert.AreEqual(2, sw.Elapsed.Seconds);
    }

    [TestMethod]
    public void SelectParallelAsync_Parallel()
    {
        Random rnd = new Random();
        var count = 20;
        var numbers = Enumerable.Range(0, count);

        Stopwatch sw = new Stopwatch();
        sw.Start();

        foreach (var result in numbers
            .AsParallel()
            .WithDegreeOfParallelism(10)
            .SelectParallelAsync(selectAction)
            .Where(item => item.IsEven))
        {
            Assert.AreEqual(true, result.IsEven);
        }

        sw.Stop();
        Assert.AreEqual(2, sw.Elapsed.Seconds);

        sw.Reset();
        sw.Start();

        foreach (var result in numbers
            .AsParallel()
            .SelectParallelAsync(selectAction, 10)
            .Where(item => item.IsEven))
        {
            Assert.AreEqual(true, result.IsEven);
        }

        sw.Stop();
        Assert.AreEqual(2, sw.Elapsed.Seconds);
    }

    [TestMethod]
    public void WhereParallelAsync_Enumerable()
    {
        Random rnd = new Random();
        var count = 20;
        var numbers = Enumerable.Range(0, count);

        HashSet<int> ints = new HashSet<int>();
        Stopwatch sw = new Stopwatch();
        sw.Start();

        foreach (var result in numbers
            .WhereParallelAsync(whereAction, 10))
        {
            Assert.IsTrue(result % 2 == 0);
        }
        sw.Stop();
        Assert.AreEqual(2, sw.Elapsed.Seconds);
    }

    [TestMethod]
    public void WhereParallelAsync_Parallel()
    {
        Random rnd = new Random();
        var count = 20;
        var numbers = Enumerable.Range(0, count);

        Stopwatch sw = new Stopwatch();
        sw.Start();

        foreach (var result in numbers
            .AsParallel()
            .WithDegreeOfParallelism(10)
            .WhereParallelAsync(whereAction, 10))
        {
            Assert.IsTrue(result % 2 == 0);
        }

        sw.Stop();
        Assert.AreEqual(2, sw.Elapsed.Seconds);

        sw.Reset();
        sw.Start();

        foreach (var result in numbers
            .AsParallel()
            .SelectParallelAsync(selectAction, 10)
            .Where(item => item.IsEven))
        {
            Assert.AreEqual(true, result.IsEven);
        }

        sw.Stop();
        Assert.AreEqual(2, sw.Elapsed.Seconds);
    }

    [TestMethod]
    public void WaitAsync_Enumerable()
    {
        Random rnd = new Random();
        var count = 20;
        var numbers = Enumerable.Range(0, count);

        Stopwatch sw = new Stopwatch();
        sw.Start();

        int pos = 0;
        foreach (var result in numbers
            .Select(val => selectAction(val, pos++))
            .WaitAll()
            .Where(item => item.IsEven))
        {
            Assert.AreEqual(true, result.IsEven);
        }
        sw.Stop();
        Assert.IsTrue(sw.Elapsed.Seconds <= 1);
    }

    [TestMethod]
    public void ForEachParallelAsync_Enumerable()
    {
        Random rnd = new Random();
        var count = 20;
        var numbers = Enumerable.Range(0, count);

        Stopwatch sw = new Stopwatch();
        sw.Start();

        int pos = 0;
        foreach (var result in numbers.Select(num => new MutableItem() { Value = num })
                    .ForEachParallelAsync(foreachAction))
        {
            Assert.AreEqual(true, (result.Value % 2) == 0);
        }
        sw.Stop();
        Assert.IsTrue(sw.Elapsed.Seconds <= 1);
    }

    [TestMethod]
    public void ForEachParallelAsync_Parallel()
    {
        Random rnd = new Random();
        var count = 20;
        var numbers = Enumerable.Range(0, count);

        Stopwatch sw = new Stopwatch();
        sw.Start();

        foreach (var result in numbers.Select(num => new MutableItem() { Value = num })
            .AsParallel()
            .WithDegreeOfParallelism(10)
            .ForEachParallelAsync(foreachAction))
        {
            Assert.AreEqual(true, (result.Value % 2) == 0);
        }

        sw.Stop();
        Assert.AreEqual(2, sw.Elapsed.Seconds);

        sw.Reset();
        sw.Start();

        foreach (var result in numbers.Select(num => new MutableItem() { Value = num })
            .AsParallel()
            .ForEachParallelAsync(foreachAction, 10))
        {
            Assert.AreEqual(true, (result.Value % 2) == 0);
        }

        sw.Stop();
        Assert.AreEqual(2, sw.Elapsed.Seconds);
    }

    public class MutableItem
    {
        public int Value { get; set; }
    }

    private static async Task<Item> selectAction(int item, int pos, CancellationToken ct = default)
    {
        await Task.Delay(1000, ct);
        return new Item
        {
            Value = item,
            IsEven = item % 2 == 0,
            Index = pos
        };
    }

    private static async Task<bool> whereAction(int item, int pos, CancellationToken ct)
    {
        await Task.Delay(1000, ct);
        return item % 2 == 0;
    }

    private static async Task foreachAction(MutableItem mutableItem, int pos, CancellationToken ct)
    {
        await Task.Delay(1000, ct);
        mutableItem.Value = mutableItem.Value * 2;
    }

}