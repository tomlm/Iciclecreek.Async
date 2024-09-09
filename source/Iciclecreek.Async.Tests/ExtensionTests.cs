using System.Diagnostics;

namespace Iciclecreek.Async.Tests;

[TestClass]
public class ExtensionTests
{

    public class Item
    {
        public int Value { get; set; }
        public bool IsEven { get; set; }
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

        foreach (var result in numbers
            .Select(selectAction)
            .WaitAll()
            .Where(item => item.IsEven))
        {
            Assert.AreEqual(true, result.IsEven);
        }
        sw.Stop();
        Assert.IsTrue(sw.Elapsed.Seconds <= 1);
    }

    private static async Task<Item> selectAction(int item, int pos)
    {
        await Task.Delay(1000);
        return new Item
        {
            Value = item,
            IsEven = item % 2 == 0
        };
    }

    private static async Task<bool> whereAction(int item, int pos)
    {
        await Task.Delay(1000);
        return item % 2 == 0;
    }

}