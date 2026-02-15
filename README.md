# GarbageCollectorDemo (.NET 10 / C# 14)

This repository contains a fresh solution that demonstrates how the .NET Garbage Collector behaves under different memory patterns.

## What the demo shows

1. **Gen 0 pressure** by allocating many short-lived objects.
2. **Promotion to older generations** by keeping object references alive across collections.
3. **Weak references** and how objects can be reclaimed.
4. **No-GC region attempt** with `GC.TryStartNoGCRegion`.
5. **Full compacting collection** after releasing retained objects.

## Project layout

- `GarbageCollectorDemo.sln`
- `src/GarbageCollectorDemo/GarbageCollectorDemo.csproj`
- `src/GarbageCollectorDemo/Program.cs`

## Run

```bash
dotnet run --project src/GarbageCollectorDemo/GarbageCollectorDemo.csproj
```

> Note: this environment may not have the .NET SDK installed, but the project is configured for `net10.0` and C# preview language features.
