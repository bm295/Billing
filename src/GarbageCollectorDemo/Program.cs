using System.Diagnostics;

Console.WriteLine("=== .NET 10 / C# 14 Garbage Collector Demo ===");
PrintGcSnapshot("Startup");

Console.WriteLine();
Console.WriteLine("1) Creating short-lived objects to trigger Gen 0 activity...");
CreateTransientObjects(iterations: 50_000, payloadBytes: 512);
PrintGcSnapshot("After transient allocations");

Console.WriteLine();
Console.WriteLine("2) Promoting objects to older generations...");
var survivors = CreateSurvivors(count: 20_000, payloadBytes: 1024);
for (var cycle = 1; cycle <= 3; cycle++)
{
    Console.WriteLine($"   - Forced collection cycle {cycle}");
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
}

Console.WriteLine($"   Surviving objects retained: {survivors.Count:N0}");
PrintGcSnapshot("After promotions");

Console.WriteLine();
Console.WriteLine("3) Demonstrating weak references...");
var weak = BuildWeakReference();
Console.WriteLine($"   Weak target alive before GC: {weak.IsAlive}");
GC.Collect();
GC.WaitForPendingFinalizers();
GC.Collect();
Console.WriteLine($"   Weak target alive after GC:  {weak.IsAlive}");

Console.WriteLine();
Console.WriteLine("4) Attempting a short no-GC region...");
const long noGcBudget = 4 * 1024 * 1024;
if (GC.TryStartNoGCRegion(noGcBudget))
{
    try
    {
        CreateTransientObjects(iterations: 5_000, payloadBytes: 128);
        Console.WriteLine("   No-GC region succeeded for this short workload.");
    }
    finally
    {
        GC.EndNoGCRegion();
    }
}
else
{
    Console.WriteLine("   No-GC region could not be started on this runtime state.");
}

Console.WriteLine();
Console.WriteLine("5) Releasing survivors and forcing a full collection...");
survivors.Clear();
GC.Collect(generation: GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
GC.WaitForPendingFinalizers();
PrintGcSnapshot("Final state");

Console.WriteLine();
Console.WriteLine("Demo completed.");

static void CreateTransientObjects(int iterations, int payloadBytes)
{
    for (var i = 0; i < iterations; i++)
    {
        _ = new byte[payloadBytes];
    }
}

static List<byte[]> CreateSurvivors(int count, int payloadBytes)
{
    var survivors = new List<byte[]>(capacity: count);
    for (var i = 0; i < count; i++)
    {
        survivors.Add(new byte[payloadBytes]);
    }

    return survivors;
}

static WeakReference BuildWeakReference()
{
    var value = new byte[2 * 1024 * 1024];
    var weak = new WeakReference(value);
    value = null;
    return weak;
}

static void PrintGcSnapshot(string label)
{
    Console.WriteLine($"[{label}]");
    Console.WriteLine($"   Total managed memory: {GC.GetTotalMemory(forceFullCollection: false):N0} bytes");
    Console.WriteLine($"   Gen 0 collections:    {GC.CollectionCount(0):N0}");
    Console.WriteLine($"   Gen 1 collections:    {GC.CollectionCount(1):N0}");
    Console.WriteLine($"   Gen 2 collections:    {GC.CollectionCount(2):N0}");
    Console.WriteLine($"   Process working set:  {Environment.WorkingSet:N0} bytes");
    Console.WriteLine($"   Uptime:               {Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency:N2}s");
}
