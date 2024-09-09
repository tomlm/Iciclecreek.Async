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
            .SelectParallelAsync(action, 10)
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
            .SelectParallelAsync(action)
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
            .SelectParallelAsync(action, 10)
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
            .Select(action)
            .WaitAll()nit test
            .Where(item => item.IsEven))
        {
            Assert.AreEqual(true, result.IsEven);
        }
        sw.Stop();
        Assert.IsTrue(sw.Elapsed.Seconds <= 1);
    }

    private static async Task<Item> action(int item, int pos)
    {
        await Task.Delay(1000);
        return new Item
        {
            Value = item,
            IsEven = item % 2 == 0
        };
    }


}